

namespace TimeZoneConvertorLibrary.Models
{
    /// <summary>
    /// Statistics about the conversion process
    /// </summary>
    public class ConversionStatistics
    {
        public int TotalRowsProcessed { get; set; }
        public int SuccessfulConversions { get; set; }
        public int ErrorCount { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public List<string> Warnings { get; set; } = new();
        public string? SourceTimeZone { get; set; }
        public string? TargetTimeZone { get; set; }
        public string? ColumnName { get; set; }
    }

}
