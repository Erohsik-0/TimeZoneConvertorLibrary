

namespace TimeZoneConvertorLibrary.Models
{
    /// <summary>
    /// Request model for timezone conversion operations
    /// </summary>
    public class TimeZoneConversionRequest
    {
        public byte[]? ExcelData { get; set; }
        public string? ColumnName { get; set; }
        public string? SourceTimeZone { get; set; }
        public string? TargetTimeZone { get; set; }
        public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB default
        public CancellationToken CancellationToken { get; set; } = default;
    }

}
