using Microsoft.Extensions.Logging;
using NodaTime;
using System.Diagnostics;
using TimeZoneConvertorLibrary.Exceptions;
using TimeZoneConvertorLibrary.Interfaces;
using TimeZoneConvertorLibrary.Models;

namespace TimeZoneConvertorLibrary.Services
{
    /// <summary>
    /// Service responsible for timezone conversion operations
    /// </summary>
    public class TimeZoneConversionService : ITimeZoneConversionService
    {
        private readonly ITimeZoneValidationService _validationService;
        private readonly IDateTimeZoneProvider _tzProvider;
        private readonly ILogger<TimeZoneConversionService> _logger;

        public TimeZoneConversionService(
            ITimeZoneValidationService validationService,
            IDateTimeZoneProvider tzProvider,
            ILogger<TimeZoneConversionService> logger)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _tzProvider = tzProvider ?? throw new ArgumentNullException(nameof(tzProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public DateTime ConvertDateTime(DateTime dateTime, string sourceTimeZone, string targetTimeZone)
        {
            var request = new TimeZoneConversionRequest(dateTime, sourceTimeZone, targetTimeZone);
            var result = ConvertDateTime(request);

            if (!result.Success)
            {
                throw new TimeZoneConversionExceptions(result.Message!, result.ErrorCode);
            }

            return result.ConvertedDateTime;
        }

        public TimeZoneConversionResult ConvertDateTimeWithResult(DateTime dateTime, string sourceTimeZone, string targetTimeZone)
        {
            var request = new TimeZoneConversionRequest(dateTime, sourceTimeZone, targetTimeZone);
            return ConvertDateTime(request);
        }

        public TimeZoneConversionResult ConvertDateTime(TimeZoneConversionRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate the request
                _validationService.ValidateConversionRequest(request);

                _logger.LogDebug($"Converting DateTime {request.DateTime:yyyy-MM-dd HH:mm:ss} from {request.SourceTimeZone} to {request.TargetTimeZone}");

                // Get timezone objects
                var sourceTimeZone = _tzProvider[request.SourceTimeZone];
                var targetTimeZone = _tzProvider[request.TargetTimeZone];

                // Perform the conversion
                var convertedDateTime = ConvertDateTimeInternal(request.DateTime, sourceTimeZone, targetTimeZone);

                stopwatch.Stop();

                _logger.LogDebug($"Conversion completed successfully in {stopwatch.Elapsed.TotalMilliseconds:F2}ms. " +
                    $"Result: {convertedDateTime:yyyy-MM-dd HH:mm:ss}");

                return TimeZoneConversionResult.CreateSuccess(
                    convertedDateTime,
                    stopwatch.Elapsed,
                    $"Successfully converted from {request.SourceTimeZone} to {request.TargetTimeZone}");
            }
            catch (TimeZoneConversionExceptions ex)
            {
                stopwatch.Stop();
                _logger.LogWarning($"Conversion failed with validation error: {ex.UserFriendlyMessage}");
                return TimeZoneConversionResult.CreateFailure(ex.UserFriendlyMessage, ex.ErrorCode, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"Unexpected error during timezone conversion: {ex.Message}");
                return TimeZoneConversionResult.CreateFailure(
                    "An unexpected error occurred during the timezone conversion. Please verify your input parameters.",
                    "UNEXPECTED_ERROR",
                    stopwatch.Elapsed);
            }
        }

        public async Task<TimeZoneConversionResult> ConvertDateTimeAsync(TimeZoneConversionRequest request, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ConvertDateTime(request);
            }, cancellationToken);
        }

        private DateTime ConvertDateTimeInternal(DateTime dateTime, DateTimeZone sourceZone, DateTimeZone targetZone)
        {
            try
            {
                // Convert DateTime to LocalDateTime
                var localDateTime = LocalDateTime.FromDateTime(dateTime);

                // Create a ZonedDateTime in the source timezone
                var zonedSource = localDateTime.InZoneLeniently(sourceZone);

                // Convert to the target timezone
                var zonedTarget = zonedSource.WithZone(targetZone);

                // Return as DateTime
                return zonedTarget.ToDateTimeUnspecified();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error converting datetime {dateTime:yyyy-MM-dd HH:mm:ss} from {sourceZone.Id} to {targetZone.Id}");
                throw new TimeZoneConversionExceptions(
                    $"Failed to convert datetime from {sourceZone.Id} to {targetZone.Id}. Please verify the datetime value and timezone parameters.",
                    ex,
                    "CONVERSION_FAILED");
            }
        }

    }

}
