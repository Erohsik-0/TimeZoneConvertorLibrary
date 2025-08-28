using Microsoft.Extensions.Logging;
using NodaTime;
using TimeZoneConvertorLibrary.Exceptions;
using TimeZoneConvertorLibrary.Interfaces;
using TimeZoneConvertorLibrary.Models;
using TimeZoneInfo = TimeZoneConvertorLibrary.Models.TimeZoneInfo;

namespace TimeZoneConvertorLibrary.Services
{
    /// <summary>
    /// Service responsible for validating timezone parameters and requests
    /// </summary>
    public class TimeZoneValidationService : ITimeZoneValidationService
    {
        private readonly IDateTimeZoneProvider _tzProvider;
        private readonly ILogger<TimeZoneValidationService> _logger;

        public TimeZoneValidationService(IDateTimeZoneProvider tzProvider, ILogger<TimeZoneValidationService> logger)
        {
            _tzProvider = tzProvider ?? throw new ArgumentNullException(nameof(tzProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsValidTimeZone(string timeZoneId)
        {
            return !string.IsNullOrWhiteSpace(timeZoneId) && _tzProvider.Ids.Contains(timeZoneId);
        }

        public IEnumerable<string> GetTimeZoneSuggestions(string invalidTimeZone)
        {
            if (string.IsNullOrWhiteSpace(invalidTimeZone))
                return new[] { "UTC", "America/New_York", "Europe/London", "Asia/Tokyo" };

            var suggestions = _tzProvider.Ids
                .Where(tz => tz.Contains(invalidTimeZone, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();

            if (!suggestions.Any())
            {
                suggestions.AddRange(new[] { "UTC", "America/New_York", "Europe/London", "Asia/Tokyo" });
            }

            return suggestions.Take(5);
        }

        public void ValidateConversionRequest(TimeZoneConversionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate DateTime
            if (request.DateTime == default)
            {
                throw new DateTimeParsingException("DateTime value is required and cannot be default value.");
            }

            // Validate source timezone
            if (string.IsNullOrWhiteSpace(request.SourceTimeZone))
            {
                throw new TimeZoneValidationException("Source timezone is required and cannot be empty.");
            }

            if (!IsValidTimeZone(request.SourceTimeZone))
            {
                var suggestions = GetTimeZoneSuggestions(request.SourceTimeZone);
                throw new TimeZoneValidationException(
                    $"Invalid source timezone '{request.SourceTimeZone}'. Did you mean: {string.Join(", ", suggestions)}?");
            }

            // Validate target timezone
            if (string.IsNullOrWhiteSpace(request.TargetTimeZone))
            {
                throw new TimeZoneValidationException("Target timezone is required and cannot be empty.");
            }

            if (!IsValidTimeZone(request.TargetTimeZone))
            {
                var suggestions = GetTimeZoneSuggestions(request.TargetTimeZone);
                throw new TimeZoneValidationException(
                    $"Invalid target timezone '{request.TargetTimeZone}'. Did you mean: {string.Join(", ", suggestions)}?");
            }

            _logger.LogDebug($"Validation passed for conversion from {request.SourceTimeZone} to {request.TargetTimeZone}");
        }

        public IEnumerable<string> GetAvailableTimeZones()
        {
            try
            {
                return _tzProvider.Ids.OrderBy(tz => tz).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available timezones");
                return new[] { "UTC", "America/New_York", "Europe/London", "Asia/Tokyo" };
            }
        }

        public TimeZoneInfo GetTimeZoneInfo(string timeZoneId)
        {
            if (!IsValidTimeZone(timeZoneId))
            {
                var suggestions = GetTimeZoneSuggestions(timeZoneId);
                throw new TimeZoneValidationException(
                    $"Invalid timezone '{timeZoneId}'. Did you mean: {string.Join(", ", suggestions)}?");
            }

            try
            {
                var dateTimeZone = _tzProvider[timeZoneId];
                var now = SystemClock.Instance.GetCurrentInstant();
                var zoneInterval = dateTimeZone.GetZoneInterval(now);

                return new TimeZoneInfo(
                    timeZoneId,
                    timeZoneId, // NodaTime doesn't have display names like System.TimeZoneInfo
                    zoneInterval.StandardOffset.ToTimeSpan(),
                    zoneInterval.Savings != Offset.Zero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting timezone info for {timeZoneId}");
                throw new TimeZoneValidationException($"Unable to retrieve information for timezone '{timeZoneId}'.");
            }
        }

    }

}
