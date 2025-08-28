using ClosedXML.Excel;
using NodaTime;
using Microsoft.Extensions.Logging;
using TimeZoneConvertorLibrary.Exceptions;
using TimeZoneConvertorLibrary.Interfaces;
using TimeZoneConvertorLibrary.Models;

namespace TimeZoneConvertorLibrary.Services
{
    /// <summary>
    /// Service responsible for Excel file processing and manipulation
    /// </summary>
    public class ExcelProcessingService : IExcelProcessingService
    {
        private readonly ITimeZoneTransformationService _transformationService;
        private readonly ILogger<ExcelProcessingService> _logger;

        public ExcelProcessingService(
            ITimeZoneTransformationService transformationService,
            ILogger<ExcelProcessingService> logger)
        {
            _transformationService = transformationService ?? throw new ArgumentNullException(nameof(transformationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<byte[]> ProcessExcelFileAsync(
            byte[] excelData,
            string columnName,
            DateTimeZone sourceTimeZone,
            DateTimeZone targetTimeZone,
            CancellationToken cancellationToken,
            IProgress<ConversionProgress> progress = null)
        {
            return await Task.Run(() =>
            {
                using var inputStream = new MemoryStream(excelData);
                using var workbook = new XLWorkbook(inputStream);

                bool columnFound = false;
                int totalProcessed = 0;
                int errorCount = 0;
                int totalRows = 0;

                // First pass: count total rows for progress reporting
                foreach (var ws in workbook.Worksheets)
                {
                    var headerRow = ws.FirstRowUsed();
                    if (headerRow == null) continue;

                    var targetColumn = FindTargetColumn(headerRow, columnName);
                    if (targetColumn != null)
                    {
                        totalRows += ws.RowsUsed().Count() - 1; // Exclude header
                    }
                }

                if (totalRows == 0)
                {
                    var availableColumns = GetAvailableColumns(excelData);
                    throw new DataProcessingException(
                        $"Column '{columnName}' not found in any worksheet. Available columns: {string.Join(", ", availableColumns.Take(10))}");
                }

                // Second pass: process the data
                foreach (var ws in workbook.Worksheets)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var headerRow = ws.FirstRowUsed();
                    if (headerRow == null) continue;

                    var targetColumn = FindTargetColumn(headerRow, columnName);
                    if (targetColumn == null) continue;

                    columnFound = true;
                    int colIndex = targetColumn.Address.ColumnNumber;

                    _logger.LogDebug($"Found column {columnName} at index {colIndex} in worksheet {ws.Name}");

                    // Process rows with batch processing for better performance
                    var dataRows = ws.RowsUsed().Skip(1).ToList();
                    var batchSize = Math.Min(1000, Math.Max(100, dataRows.Count / 10));

                    for (int i = 0; i < dataRows.Count; i += batchSize)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var batch = dataRows.Skip(i).Take(batchSize);
                        var (processed, errors) = ProcessRowBatch(
                            batch, colIndex, sourceTimeZone, targetTimeZone, cancellationToken);

                        totalProcessed += processed;
                        errorCount += errors;

                        // Report progress
                        var currentProgress = Math.Min(totalProcessed + errorCount, totalRows);
                        progress?.Report(new ConversionProgress
                        {
                            ProcessedItems = currentProgress,
                            TotalItems = totalRows,
                            CurrentOperation = $"Processed {currentProgress:N0} of {totalRows:N0} rows"
                        });

                        // Log progress for large files
                        if (totalRows > 5000 && i % (batchSize * 10) == 0)
                        {
                            _logger.LogDebug($"Processed {Math.Min(i + batchSize, dataRows.Count):N0}/{dataRows.Count:N0} rows in worksheet {ws.Name}");
                        }
                    }
                }

                if (!columnFound)
                {
                    var availableColumns = GetAvailableColumns(excelData);
                    throw new DataProcessingException(
                        $"Column '{columnName}' not found in any worksheet. Available columns: {string.Join(", ", availableColumns)}");
                }

                _logger.LogInformation($"Processing completed: {totalProcessed:N0} cells processed, {errorCount:N0} errors");

                progress?.Report(new ConversionProgress
                {
                    ProcessedItems = totalRows,
                    TotalItems = totalRows,
                    CurrentOperation = "Saving file..."
                });

                // Save with compression for better performance
                using var outputStream = new MemoryStream();
                workbook.SaveAs(outputStream);
                return outputStream.ToArray();

            }, cancellationToken);
        }

        public IEnumerable<string> GetAvailableColumns(byte[] excelData)
        {
            var columns = new HashSet<string>();

            try
            {
                using var stream = new MemoryStream(excelData);
                using var workbook = new XLWorkbook(stream);

                foreach (var ws in workbook.Worksheets)
                {
                    var headerRow = ws.FirstRowUsed();
                    if (headerRow != null)
                    {
                        foreach (var cell in headerRow.Cells())
                        {
                            var value = cell.GetString()?.Trim();
                            if (!string.IsNullOrEmpty(value))
                            {
                                columns.Add(value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting available columns: {ex.Message}");
                throw new DataProcessingException("Unable to read column names from the Excel file.");
            }

            return columns.Take(50).OrderBy(x => x);
        }

        public IXLCell FindTargetColumn(IXLRow headerRow, string columnName)
        {
            // First try exact match (case insensitive)
            var exactMatch = headerRow.Cells()
                .FirstOrDefault(c =>
                    !string.IsNullOrWhiteSpace(c.GetString()) &&
                    string.Equals(c.GetString().Trim(), columnName.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null) return exactMatch;

            // Then try partial match
            return headerRow.Cells()
                .FirstOrDefault(c =>
                    !string.IsNullOrWhiteSpace(c.GetString()) &&
                    c.GetString().Trim().Contains(columnName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private (int processed, int errors) ProcessRowBatch(
            IEnumerable<IXLRow> rows,
            int colIndex,
            DateTimeZone sourceTimeZone,
            DateTimeZone targetTimeZone,
            CancellationToken cancellationToken)
        {
            int processed = 0;
            int errors = 0;

            // Process cells sequentially to avoid Excel threading issues
            foreach (var row in rows)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var cell = row.Cell(colIndex);
                    var cellValue = GetCellValueAsString(cell);

                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        continue; // Skip empty cells
                    }

                    var conversionResult = _transformationService.ConvertCellValue(
                        cellValue, sourceTimeZone, targetTimeZone);

                    if (conversionResult.Success && conversionResult.ConvertedValue.HasValue)
                    {
                        cell.Value = conversionResult.ConvertedValue.Value;
                        processed++;
                    }
                    else if (!string.IsNullOrEmpty(conversionResult.ErrorMessage))
                    {
                        errors++;
                        _logger.LogWarning($"Error processing cell {cell.Address}: {conversionResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning($"Unexpected error processing row: {ex.Message}");
                }
            }

            return (processed, errors);
        }

        private string GetCellValueAsString(IXLCell cell)
        {
            try
            {
                if (cell.IsEmpty())
                    return string.Empty;

                // Handle different cell types
                return cell.DataType switch
                {
                    XLDataType.DateTime => cell.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    XLDataType.TimeSpan => cell.GetTimeSpan().ToString(),
                    XLDataType.Number => cell.GetDouble().ToString(),
                    XLDataType.Text => cell.GetString(),
                    XLDataType.Boolean => cell.GetBoolean().ToString(),
                    _ => cell.GetString()
                };
            }
            catch
            {
                return cell.GetString();
            }
        }
    }
}
