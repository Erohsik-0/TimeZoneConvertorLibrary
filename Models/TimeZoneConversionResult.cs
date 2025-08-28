

namespace TimeZoneConvertorLibrary.Models
{
    /// <summary>
    /// Result model for timezone conversion operations
    /// </summary>
    public class TimeZoneConversionResult
    {
        public DateTime ConvertedDateTime { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// Creates a successful conversion result
        /// </summary>
        public static TimeZoneConversionResult CreateSuccess(DateTime convertedDateTime, TimeSpan processingTime, string? message = null)
        {
            return new TimeZoneConversionResult
            {
                ConvertedDateTime = convertedDateTime,
                Success = true,
                Message = message ?? "Conversion completed successfully",
                ProcessingTime = processingTime
            };
        }

        /// <summary>
        /// Creates a failed conversion result
        /// </summary>
        public static TimeZoneConversionResult CreateFailure(string message, string? errorCode = null, TimeSpan processingTime = default)
        {
            return new TimeZoneConversionResult
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                ProcessingTime = processingTime
            };
        }

    }

}
