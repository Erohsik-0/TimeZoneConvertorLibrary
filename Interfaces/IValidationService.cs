using TimeZoneConvertorLibrary.Models;

namespace TimeZoneConvertorLibrary.Interfaces
{
    /// <summary>
    /// Interface for validation operations
    /// </summary>
    public interface IValidationService
    {
        Task ValidateConversionRequestAsync(TimeZoneConversionRequest request);
        bool IsValidTimeZone(string timeZoneId);
        IEnumerable<string> GetTimeZoneSuggestions(string invalidTimeZone);
        ExcelFileMetadata ValidateAndAnalyzeExcelFile(byte[] excelData);
    }

}
