using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeZoneConvertorLibrary.Models;

namespace TimeZoneConvertorLibrary.Interfaces
{
    /// <summary>
    /// Interface for the main timezone conversion orchestrator
    /// </summary>
    public interface ITimeZoneConversionOrchestrator
    {
        Task<TimeZoneConversionResult> ConvertExcelTimeStampsAsync(
            TimeZoneConversionRequest request,
            IProgress<ConversionProgress> progress = null);

        IEnumerable<string> GetAvailableTimeZones();
        bool IsValidTimeZone(string timeZoneId);
        Task<ExcelFileMetadata> AnalyzeExcelFileAsync(byte[] excelData);
    }
}
