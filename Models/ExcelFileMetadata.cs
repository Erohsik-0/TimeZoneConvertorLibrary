

namespace TimeZoneConvertorLibrary.Models
{
    /// <summary>
    /// Excel file metadata
    /// </summary>
    public class ExcelFileMetadata
    {
        public List<string> AvailableColumns { get; set; } = new();
        public int TotalWorksheets { get; set; }
        public int TotalRows { get; set; }
        public long FileSizeBytes { get; set; }
        public bool IsValidExcelFile { get; set; }
    }

}
