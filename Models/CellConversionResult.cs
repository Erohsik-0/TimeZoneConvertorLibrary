

namespace TimeZoneConvertorLibrary.Models
{
    /// <summary>
    /// Cell conversion result
    /// </summary>
    public class CellConversionResult
    {
        public bool Success { get; set; }
        public DateTime? ConvertedValue { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OriginalValue { get; set; }
    }

}
