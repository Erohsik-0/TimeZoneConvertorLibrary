using TimeZoneConvertorLibrary.Models;
using TimeZoneInfo = TimeZoneConvertorLibrary.Models.TimeZoneInfo;

namespace TimeZoneConvertorLibrary.Interfaces
{

    /// <summary>
    /// Interface for timezone validation operations
    /// </summary>
    public interface ITimeZoneValidationService
    {
        /// <summary>
        /// Validates if a timezone ID is valid
        /// </summary>
        bool IsValidTimeZone(string timeZoneId);

        /// <summary>
        /// Gets suggestions for similar timezone IDs
        /// </summary>
        IEnumerable<string> GetTimeZoneSuggestions(string invalidTimeZone);

        /// <summary>
        /// Validates a conversion request
        /// </summary>
        void ValidateConversionRequest(TimeZoneConversionRequest request);

        /// <summary>
        /// Gets all available timezone IDs
        /// </summary>
        IEnumerable<string> GetAvailableTimeZones();

        /// <summary>
        /// Gets timezone information
        /// </summary>
        TimeZoneInfo GetTimeZoneInfo(string timeZoneId);
    }

}
