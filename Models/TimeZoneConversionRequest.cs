

namespace TimeZoneConvertorLibrary.Models
{
    /// <summary>
    /// Request model for single datetime timezone conversion operations
    /// </summary>
    public class TimeZoneConversionRequest
    {
        public DateTime DateTime { get; set; }
        public string SourceTimeZone { get; set; } = string.Empty;
        public string TargetTimeZone { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new timezone conversion request
        /// </summary>
        public TimeZoneConversionRequest(DateTime dateTime, string sourceTimeZone, string targetTimeZone)
        {
            DateTime = dateTime;
            SourceTimeZone = sourceTimeZone ?? throw new ArgumentNullException(nameof(sourceTimeZone));
            TargetTimeZone = targetTimeZone ?? throw new ArgumentNullException(nameof(targetTimeZone));
        }

        /// <summary>
        /// Parameterless constructor for serialization support
        /// </summary>
        public TimeZoneConversionRequest() { }
    }

}
