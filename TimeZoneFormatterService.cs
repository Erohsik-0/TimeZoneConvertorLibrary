using ClosedXML.Excel;
using NodaTime;
using NodaTime.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TimeZoneConversionLibrary
{
    /// <summary>
    /// Interface for Excel timezone conversion operations
    /// </summary>
    public interface ITimeZoneFormatterService
    {
        /// <summary>
        /// Converts timestamps in an Excel file from one timezone to another asynchronously
        /// </summary>
        /// <param name="excelBytes">The Excel file as byte array</param>
        /// <param name="columnName">Name of the column containing timestamps</param>
        /// <param name="sourceTimeZone">Source IANA timezone (e.g., "UTC")</param>
        /// <param name="targetTimeZone">Target IANA timezone (e.g., "Asia/Kolkata")</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <param name="maxFileSizeBytes">Maximum allowed file size in bytes (default: 50MB)</param>
        /// <param name="onProgress">Optional progress callback (processed, total, message)</param>
        /// <returns>Modified Excel file as byte array</returns>
        Task<byte[]> ConvertExcelTimeStampsAsync(
            byte[] excelBytes,
            string columnName,
            string sourceTimeZone,
            string targetTimeZone,
            CancellationToken cancellationToken = default,
            long maxFileSizeBytes = 50 * 1024 * 1024,
            Action<int, int, string> onProgress = null);

        /// <summary>
        /// Gets all available IANA timezone identifiers
        /// </summary>
        /// <returns>Collection of timezone identifiers</returns>
        IEnumerable<string> GetAvailableTimeZones();

        /// <summary>
        /// Validates if a timezone identifier is valid
        /// </summary>
        /// <param name="timeZoneId">IANA timezone identifier</param>
        /// <returns>True if valid, false otherwise</returns>
        bool IsValidTimeZone(string timeZoneId);
    }

    /// <summary>
    /// Service for converting Excel timestamp columns between timezones using IANA timezone identifiers
    /// </summary>
    public class TimeZoneFormatterService : ITimeZoneFormatterService
    {
        private readonly IDateTimeZoneProvider _tzProvider;
        private readonly ILogger _logger;

        // Cache for timezone objects and parsed patterns
        private readonly ConcurrentDictionary<string, DateTimeZone> _timezoneCache = new();
        private readonly ConcurrentDictionary<string, LocalDateTimePattern> _patternCache = new();

        // Pre-compiled regex for Excel file validation
        private static readonly Regex ExcelMagicBytes = new(@"^PK\x03\x04", RegexOptions.Compiled);

        // Common datetime patterns for caching
        private static readonly string[] CommonPatterns = {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            "MM/dd/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
            "MM-dd-yyyy HH:mm:ss",
            "dd-MM-yyyy HH:mm:ss",
            "M/d/yyyy h:mm:ss tt",
            "d/M/yyyy H:mm:ss"
        };

        /// <summary>
        /// Initializes a new instance of the TimeZoneFormatterService
        /// </summary>
        /// <param name="tzProvider">Optional timezone provider (uses Tzdb if null)</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        public TimeZoneFormatterService(
            IDateTimeZoneProvider tzProvider = null,
            ILogger logger = null)
        {
            _tzProvider = tzProvider ?? DateTimeZoneProviders.Tzdb;
            _logger = logger ?? new ConsoleLogger();

            // Pre-warm pattern cache
            InitializePatternCache();
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAvailableTimeZones()
        {
            return _tzProvider.Ids.OrderBy(x => x);
        }

        /// <inheritdoc />
        public bool IsValidTimeZone(string timeZoneId)
        {
            return !string.IsNullOrWhiteSpace(timeZoneId) && _tzProvider.Ids.Contains(timeZoneId);
        }

        private void InitializePatternCache()
        {
            foreach (var pattern in CommonPatterns)
            {
                try
                {
                    _patternCache[pattern] = LocalDateTimePattern.CreateWithInvariantCulture(pattern);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to create pattern for {pattern}: {ex.Message}");
                }
            }
        }

        /// <inheritdoc />
        public async Task<byte[]> ConvertExcelTimeStampsAsync(
            byte[] excelBytes,
            string columnName,
            string sourceTimeZone,
            string targetTimeZone,
            CancellationToken cancellationToken = default,
            long maxFileSizeBytes = 50 * 1024 * 1024,
            Action<int, int, string> onProgress = null)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation($"Starting Excel timezone conversion for column {columnName} from {sourceTimeZone} to {targetTimeZone}");

            try
            {
                // Comprehensive input validation
                await ValidateInputsAsync(excelBytes, columnName, sourceTimeZone, targetTimeZone,
                    maxFileSizeBytes, cancellationToken);

                // Get cached timezone objects
                var sourceTimeZoneObj = GetCachedTimeZone(sourceTimeZone);
                var targetTimeZoneObj = GetCachedTimeZone(targetTimeZone);

                // Process Excel file
                var result = await ProcessExcelFileAsync(
                    excelBytes, columnName, sourceTimeZoneObj, targetTimeZoneObj,
                    cancellationToken, onProgress);

                stopwatch.Stop();
                _logger.LogInformation($"Excel conversion completed successfully in {stopwatch.ElapsedMilliseconds}ms");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError($"Excel conversion failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
                throw;
            }
        }

        private async Task ValidateInputsAsync(
            byte[] excelBytes,
            string columnName,
            string sourceTimeZone,
            string targetTimeZone,
            long maxFileSizeBytes,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Validate file size
            if (excelBytes == null || excelBytes.Length == 0)
            {
                throw new ArgumentException("Excel data cannot be null or empty", nameof(excelBytes));
            }

            if (excelBytes.Length > maxFileSizeBytes)
            {
                throw new ArgumentException(
                    $"File size ({excelBytes.Length:N0} bytes) exceeds maximum allowed size ({maxFileSizeBytes:N0} bytes)",
                    nameof(excelBytes));
            }

            // Validate Excel file format (basic magic byte check)
            await Task.Run(() => ValidateExcelFormat(excelBytes), cancellationToken);

            // Validate other parameters
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new ArgumentException("Column name cannot be null or empty", nameof(columnName));
            }

            if (string.IsNullOrWhiteSpace(sourceTimeZone))
            {
                throw new ArgumentException("Source time zone cannot be null or empty", nameof(sourceTimeZone));
            }

            if (string.IsNullOrWhiteSpace(targetTimeZone))
            {
                throw new ArgumentException("Target time zone cannot be null or empty", nameof(targetTimeZone));
            }

            // Validate time zones
            if (!IsValidTimeZone(sourceTimeZone))
            {
                var suggestions = GetTimeZoneSuggestions(sourceTimeZone);
                throw new ArgumentException($"Invalid source time zone: '{sourceTimeZone}'. " +
                    $"Did you mean: {string.Join(", ", suggestions)}?", nameof(sourceTimeZone));
            }

            if (!IsValidTimeZone(targetTimeZone))
            {
                var suggestions = GetTimeZoneSuggestions(targetTimeZone);
                throw new ArgumentException($"Invalid target time zone: '{targetTimeZone}'. " +
                    $"Did you mean: {string.Join(", ", suggestions)}?", nameof(targetTimeZone));
            }
        }

        private IEnumerable<string> GetTimeZoneSuggestions(string invalidTimeZone)
        {
            if (string.IsNullOrWhiteSpace(invalidTimeZone))
                return new[] { "UTC", "America/New_York", "Europe/London" };

            return _tzProvider.Ids
                .Where(tz => tz.Contains(invalidTimeZone, StringComparison.OrdinalIgnoreCase))
                .Take(3)
                .DefaultIfEmpty("UTC")
                .Take(3);
        }

        private void ValidateExcelFormat(byte[] excelBytes)
        {
            // Check for ZIP file signature (Excel files are ZIP archives)
            if (excelBytes.Length < 4)
            {
                throw new ArgumentException("File is too small to be a valid Excel file");
            }

            // Check magic bytes for ZIP/Office format
            var header = System.Text.Encoding.ASCII.GetString(excelBytes.Take(4).ToArray());
            if (!ExcelMagicBytes.IsMatch(header))
            {
                throw new ArgumentException("File does not appear to be a valid Excel file format (.xlsx/.xlsm expected)");
            }
        }

        private DateTimeZone GetCachedTimeZone(string timeZoneId)
        {
            return _timezoneCache.GetOrAdd(timeZoneId, id => _tzProvider[id]);
        }

        private async Task<byte[]> ProcessExcelFileAsync(
            byte[] excelBytes,
            string columnName,
            DateTimeZone sourceTimeZoneObj,
            DateTimeZone targetTimeZoneObj,
            CancellationToken cancellationToken,
            Action<int, int, string> onProgress = null)
        {
            return await Task.Run(() =>
            {
                using var inputStream = new MemoryStream(excelBytes);
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

                // Second pass: process the data
                foreach (var ws in workbook.Worksheets)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var headerRow = ws.FirstRowUsed();
                    if (headerRow == null) continue;

                    // Find target column with improved search
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
                            batch, colIndex, sourceTimeZoneObj, targetTimeZoneObj, cancellationToken);

                        totalProcessed += processed;
                        errorCount += errors;

                        // Report progress
                        var currentProgress = Math.Min(totalProcessed, totalRows);
                        onProgress?.Invoke(currentProgress, totalRows,
                            $"Processed {currentProgress:N0} of {totalRows:N0} rows");

                        // Log progress for large files
                        if (totalRows > 5000 && i % (batchSize * 10) == 0)
                        {
                            _logger.LogDebug($"Processed {Math.Min(i + batchSize, dataRows.Count):N0}/{dataRows.Count:N0} rows in worksheet {ws.Name}");
                        }
                    }
                }

                if (!columnFound)
                {
                    var availableColumns = GetAvailableColumns(workbook);
                    throw new ArgumentException($"Column '{columnName}' not found in any worksheet. " +
                        $"Available columns: {string.Join(", ", availableColumns)}");
                }

                _logger.LogInformation($"Processing completed: {totalProcessed:N0} cells processed, {errorCount:N0} errors");

                onProgress?.Invoke(totalRows, totalRows, "Saving file...");

                // Save with compression for better performance
                using var outputStream = new MemoryStream();
                workbook.SaveAs(outputStream);
                return outputStream.ToArray();

            }, cancellationToken);
        }

        private IXLCell FindTargetColumn(IXLRow headerRow, string columnName)
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
            // But use optimized processing within each cell
            foreach (var row in rows)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var cell = row.Cell(colIndex);
                    var (success, convertedValue, error) = ConvertCellValue(cell, sourceTimeZone, targetTimeZone);

                    if (success && convertedValue.HasValue)
                    {
                        cell.Value = convertedValue.Value;
                        processed++;
                    }
                    else if (!string.IsNullOrEmpty(error))
                    {
                        errors++;
                        _logger.LogWarning($"Error processing cell {cell.Address}: {error}");
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

        private (bool success, DateTime? value, string error) ConvertCellValue(
            IXLCell cell,
            DateTimeZone sourceTimeZone,
            DateTimeZone targetTimeZone)
        {
            try
            {
                if (cell.DataType == XLDataType.DateTime)
                {
                    var dt = cell.GetDateTime();
                    var convertedValue = ConvertDateTime(dt, sourceTimeZone, targetTimeZone);
                    return (true, convertedValue, null);
                }
                else if (cell.DataType == XLDataType.Text && !string.IsNullOrWhiteSpace(cell.GetString()))
                {
                    var cellText = cell.GetString().Trim();
                    if (TryParseDateTime(cellText, sourceTimeZone, targetTimeZone, out DateTime convertedDateTime))
                    {
                        return (true, convertedDateTime, null);
                    }
                    return (false, null, $"Could not parse datetime: '{cellText}'");
                }

                // Cell is empty or not a datetime - skip
                return (false, null, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        private DateTime ConvertDateTime(DateTime dt, DateTimeZone sourceZone, DateTimeZone targetZone)
        {
            var localDateTime = LocalDateTime.FromDateTime(dt);
            var zonedSource = localDateTime.InZoneLeniently(sourceZone);
            var zonedTarget = zonedSource.WithZone(targetZone);
            return zonedTarget.ToDateTimeUnspecified();
        }

        private bool TryParseDateTime(string dateTimeString, DateTimeZone sourceZone, DateTimeZone targetZone, out DateTime result)
        {
            result = default;

            // Try cached patterns first (fastest)
            foreach (var cachedPattern in _patternCache.Values)
            {
                var parseResult = cachedPattern.Parse(dateTimeString);
                if (parseResult.Success)
                {
                    result = ConvertDateTime(parseResult.Value.ToDateTimeUnspecified(), sourceZone, targetZone);
                    return true;
                }
            }

            // Try additional dynamic patterns for uncommon formats
            var dynamicPatterns = GenerateDynamicPatterns(dateTimeString);
            foreach (var pattern in dynamicPatterns)
            {
                try
                {
                    var cachedPattern = _patternCache.GetOrAdd(pattern,
                        p => LocalDateTimePattern.CreateWithInvariantCulture(p));

                    var parseResult = cachedPattern.Parse(dateTimeString);
                    if (parseResult.Success)
                    {
                        result = ConvertDateTime(parseResult.Value.ToDateTimeUnspecified(), sourceZone, targetZone);
                        return true;
                    }
                }
                catch
                {
                    // Pattern creation failed, skip
                    continue;
                }
            }

            // Fallback to standard DateTime.TryParse
            if (DateTime.TryParse(dateTimeString, out DateTime parsedDate))
            {
                result = ConvertDateTime(parsedDate, sourceZone, targetZone);
                return true;
            }

            return false;
        }

        private IEnumerable<string> GenerateDynamicPatterns(string dateTimeString)
        {
            var patterns = new HashSet<string>();

            // Analyze the string to determine likely patterns
            if (dateTimeString.Contains('T'))
            {
                if (dateTimeString.EndsWith('Z'))
                {
                    patterns.Add("yyyy-MM-dd'T'HH:mm:ss.ffffffff'Z'");
                    patterns.Add("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'");
                    patterns.Add("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
                }
                else
                {
                    patterns.Add("yyyy-MM-dd'T'HH:mm:ss.ffffffff");
                    patterns.Add("yyyy-MM-dd'T'HH:mm:ss.ffffff");
                    patterns.Add("yyyy-MM-dd'T'HH:mm:ss.fff");
                }
            }

            // Add patterns based on separators found
            if (dateTimeString.Contains('/'))
            {
                patterns.UnionWith(new[]
                {
                    "M/d/yyyy H:mm:ss",
                    "M/d/yyyy h:mm:ss tt",
                    "yyyy/M/d H:mm:ss",
                    "d/M/yyyy H:mm:ss"
                });
            }

            if (dateTimeString.Contains('-') && !dateTimeString.Contains('T'))
            {
                patterns.UnionWith(new[]
                {
                    "yyyy-M-d H:mm:ss",
                    "d-M-yyyy H:mm:ss",
                    "M-d-yyyy H:mm:ss"
                });
            }

            return patterns;
        }

        private IEnumerable<string> GetAvailableColumns(XLWorkbook workbook)
        {
            var columns = new HashSet<string>();

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

            return columns.Take(20).OrderBy(x => x);
        }
    }

    /// <summary>
    /// Simple logger interface for the package
    /// </summary>
    public interface ILogger
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogDebug(string message);
    }

    /// <summary>
    /// Default console logger implementation
    /// </summary>
    internal class ConsoleLogger : ILogger
    {
        public void LogInformation(string message) => Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        public void LogWarning(string message) => Console.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        public void LogError(string message) => Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        public void LogDebug(string message) => Console.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }

    /// <summary>
    /// Extension methods for better Excel cell handling
    /// </summary>
    public static class ExcelExtensions
    {
        /// <summary>
        /// Gets string value from any cell type
        /// </summary>
        public static string GetString(this IXLCell cell)
        {
            return cell.DataType switch
            {
                XLDataType.Text => cell.GetText(),
                XLDataType.Number => cell.GetDouble().ToString("F"),
                XLDataType.DateTime => cell.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss"),
                XLDataType.Boolean => cell.GetBoolean().ToString(),
                _ => cell.Value.ToString() ?? string.Empty
            };
        }
    }
}