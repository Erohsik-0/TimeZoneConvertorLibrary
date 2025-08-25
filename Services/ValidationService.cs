using ClosedXML.Excel;
using NodaTime;
using System.Text.RegularExpressions;
using TimeZoneConversionLibrary;
using TimeZoneConvertorLibrary.Exceptions;
using TimeZoneConvertorLibrary.Interfaces;
using TimeZoneConvertorLibrary.Models;

namespace TimeZoneConvertorLibrary.Services
{
    /// <summary>
    /// Service responsible for validating input data and parameters
    /// </summary>
    public class ValidationService : IValidationService
    {
        private readonly IDateTimeZoneProvider _tzProvider;
        private readonly ILogger _logger;
        private static readonly Regex ExcelMagicBytes = new(@"^PK\x03\x04", RegexOptions.Compiled);

        public ValidationService(IDateTimeZoneProvider tzProvider, ILogger logger)
        {
            _tzProvider = tzProvider ?? throw new ArgumentNullException(nameof(tzProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ValidateConversionRequestAsync(TimeZoneConversionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            await Task.Run(() =>
            {
                request.CancellationToken.ThrowIfCancellationRequested();

                // Validate file data
                if (request.ExcelData == null || request.ExcelData.Length == 0)
                {
                    throw new Exceptions.FileFormatException("Please provide a valid Excel file to process.");
                }

                if (request.ExcelData.Length > request.MaxFileSizeBytes)
                {
                    var maxSizeMB = request.MaxFileSizeBytes / (1024 * 1024);
                    var currentSizeMB = request.ExcelData.Length / (1024 * 1024);
                    throw new Exceptions.FileFormatException(
                        $"File size ({currentSizeMB:F1} MB) exceeds the maximum allowed size of {maxSizeMB} MB.");
                }

                // Validate Excel format
                ValidateExcelFormat(request.ExcelData);

                // Validate column name
                if (string.IsNullOrWhiteSpace(request.ColumnName))
                {
                    throw new DataProcessingException("Column name is required and cannot be empty.");
                }

                // Validate time zones
                ValidateTimeZones(request.SourceTimeZone, request.TargetTimeZone);
            });
        }

        public bool IsValidTimeZone(string timeZoneId)
        {
            return !string.IsNullOrWhiteSpace(timeZoneId) && _tzProvider.Ids.Contains(timeZoneId);
        }

        public IEnumerable<string> GetTimeZoneSuggestions(string invalidTimeZone)
        {
            if (string.IsNullOrWhiteSpace(invalidTimeZone))
                return new[] { "UTC", "America/New_York", "Europe/London" };

            return _tzProvider.Ids
                .Where(tz => tz.Contains(invalidTimeZone, StringComparison.OrdinalIgnoreCase))
                .Take(3)
                .DefaultIfEmpty("UTC")
                .Take(3);
        }

        public ExcelFileMetadata ValidateAndAnalyzeExcelFile(byte[] excelData)
        {
            var metadata = new ExcelFileMetadata
            {
                FileSizeBytes = excelData.Length,
                IsValidExcelFile = false
            };

            try
            {
                ValidateExcelFormat(excelData);
                metadata.IsValidExcelFile = true;

                using var stream = new MemoryStream(excelData);
                using var workbook = new XLWorkbook(stream);

                metadata.TotalWorksheets = workbook.Worksheets.Count;

                var allColumns = new HashSet<string>();
                int totalRows = 0;

                foreach (var worksheet in workbook.Worksheets)
                {
                    var headerRow = worksheet.FirstRowUsed();
                    if (headerRow != null)
                    {
                        foreach (var cell in headerRow.Cells())
                        {
                            var columnName = cell.GetString()?.Trim();
                            if (!string.IsNullOrEmpty(columnName))
                            {
                                allColumns.Add(columnName);
                            }
                        }

                        totalRows += worksheet.RowsUsed().Count() - 1; // Exclude header
                    }
                }

                metadata.AvailableColumns = allColumns.OrderBy(x => x).ToList();
                metadata.TotalRows = totalRows;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error analyzing Excel file: {ex.Message}");
                throw new Exceptions.FileFormatException("Unable to analyze the Excel file. Please ensure it's a valid .xlsx or .xlsm file.");
            }

            return metadata;
        }

        private void ValidateExcelFormat(byte[] excelData)
        {
            if (excelData.Length < 4)
            {
                throw new Exceptions.FileFormatException("The uploaded file is too small to be a valid Excel file.");
            }

            var header = System.Text.Encoding.ASCII.GetString(excelData.Take(4).ToArray());
            if (!ExcelMagicBytes.IsMatch(header))
            {
                throw new Exceptions.FileFormatException("The uploaded file is not a valid Excel format. Please upload a .xlsx or .xlsm file.");
            }
        }

        private void ValidateTimeZones(string sourceTimeZone, string targetTimeZone)
        {
            if (string.IsNullOrWhiteSpace(sourceTimeZone))
            {
                throw new TimeZoneValidationException("Source time zone is required.");
            }

            if (string.IsNullOrWhiteSpace(targetTimeZone))
            {
                throw new TimeZoneValidationException("Target time zone is required.");
            }

            if (!IsValidTimeZone(sourceTimeZone))
            {
                var suggestions = GetTimeZoneSuggestions(sourceTimeZone);
                throw new TimeZoneValidationException(
                    $"Invalid source time zone '{sourceTimeZone}'. Did you mean: {string.Join(", ", suggestions)}?");
            }

            if (!IsValidTimeZone(targetTimeZone))
            {
                var suggestions = GetTimeZoneSuggestions(targetTimeZone);
                throw new TimeZoneValidationException(
                    $"Invalid target time zone '{targetTimeZone}'. Did you mean: {string.Join(", ", suggestions)}?");
            }
        }
    }

}
