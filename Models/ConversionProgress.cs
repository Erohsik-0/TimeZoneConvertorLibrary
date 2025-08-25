

namespace TimeZoneConvertorLibrary.Models
{
    /// <summary>
    /// Progress information for async operations
    /// </summary>
    public class ConversionProgress
    {
        public int ProcessedItems { get; set; }
        public int TotalItems { get; set; }
        public string? CurrentOperation { get; set; }
        public double PercentageComplete => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;
    }

}
