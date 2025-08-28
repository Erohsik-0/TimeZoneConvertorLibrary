using NodaTime;
using NodaTime.Text;
using System.Collections.Concurrent;
using TimeZoneConvertorLibrary.Interfaces;
using TimeZoneConvertorLibrary.Models;
using Microsoft.Extensions.Logging;

namespace TimeZoneConvertorLibrary.Services
{
    /// <summary>
    /// Service responsible for timezone transformations and datetime parsing
    /// </summary>
    public class TimeZoneTransformationService : ITimeZoneTransformationService
    {
        private readonly ILogger<TimeZoneTransformationService> _logger;
        private readonly ConcurrentDictionary<string, LocalDateTimePattern> _patternCache = new();

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

        public TimeZoneTransformationService(ILogger<TimeZoneTransformationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializePatternCache();
        }

        public DateTime ConvertDateTime(DateTime dateTime, DateTimeZone sourceZone, DateTimeZone targetZone)
        {
            try
            {
                var localDateTime = LocalDateTime.FromDateTime(dateTime);
                var zonedSource = localDateTime.InZoneLeniently(sourceZone);
                var zonedTarget = zonedSource.WithZone(targetZone);
                return zonedTarget.ToDateTimeUnspecified();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error converting datetime {dateTime}: {ex.Message}");
                throw;
            }
        }

        public CellConversionResult ConvertCellValue(string cellValue, DateTimeZone sourceZone, DateTimeZone targetZone)
        {
            var result = new CellConversionResult
            {
                OriginalValue = cellValue
            };

            if (string.IsNullOrWhiteSpace(cellValue))
            {
                result.Success = false;
                result.ErrorMessage = "Cell value is empty";
                return result;
            }

            try
            {
                if (TryParseDateTime(cellValue.Trim(), out DateTime parsedDateTime))
                {
                    var convertedDateTime = ConvertDateTime(parsedDateTime, sourceZone, targetZone);
                    result.Success = true;
                    result.ConvertedValue = convertedDateTime;
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = $"Could not parse datetime: '{cellValue}'";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogWarning($"Error converting cell value '{cellValue}': {ex.Message}");
            }

            return result;
        }

        public bool TryParseDateTime(string dateTimeString, out DateTime result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(dateTimeString))
                return false;

            // Try cached patterns first (fastest)
            foreach (var cachedPattern in _patternCache.Values)
            {
                var parseResult = cachedPattern.Parse(dateTimeString);
                if (parseResult.Success)
                {
                    result = parseResult.Value.ToDateTimeUnspecified();
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
                        result = parseResult.Value.ToDateTimeUnspecified();
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
                result = parsedDate;
                return true;
            }

            return false;
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
    }

}
