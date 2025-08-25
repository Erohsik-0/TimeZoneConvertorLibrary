

namespace TimeZoneConvertorLibrary.Models
{
    /// <summary>
    /// Result model for timezone conversion operations
    /// </summary>
    public class TimeZoneConversionResult
    {
        public byte[]? ConvertedExcelData { get; set; }
        public ConversionStatistics? Statistics { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
