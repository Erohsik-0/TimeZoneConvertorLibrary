using TimeZoneConvertorLibrary.Models;

namespace TimeZoneConvertorLibrary.Interfaces
{
    /// <summary>
    /// Interface for timezone conversion operations
    /// </summary>
    public interface ITimeZoneConversionService
    {
        /// <summary>
        /// Converts a DateTime from source timezone to target timezone
        /// </summary>
        DateTime ConvertDateTime(DateTime dateTime, string sourceTimeZone, string targetTimeZone);

        /// <summary>
        /// Converts a DateTime from source timezone to target timezone with detailed result
        /// </summary>
        TimeZoneConversionResult ConvertDateTimeWithResult(DateTime dateTime, string sourceTimeZone, string targetTimeZone);

        /// <summary>
        /// Converts a DateTime using a conversion request model
        /// </summary>
        TimeZoneConversionResult ConvertDateTime(TimeZoneConversionRequest request);

        /// <summary>
        /// Asynchronous version of ConvertDateTime for consistency with existing patterns
        /// </summary>
        Task<TimeZoneConversionResult> ConvertDateTimeAsync(TimeZoneConversionRequest request, CancellationToken cancellationToken = default);
    }

}
