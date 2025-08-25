using ClosedXML.Excel;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeZoneConvertorLibrary.Models;

namespace TimeZoneConvertorLibrary.Interfaces
{
    /// <summary>
    /// Interface for Excel file processing operations
    /// </summary>
    public interface IExcelProcessingService
    {
        Task<byte[]> ProcessExcelFileAsync(
            byte[] excelData,
            string columnName,
            DateTimeZone sourceTimeZone,
            DateTimeZone targetTimeZone,
            CancellationToken cancellationToken,
            IProgress<ConversionProgress> progress = null);

        IEnumerable<string> GetAvailableColumns(byte[] excelData);
        IXLCell FindTargetColumn(IXLRow headerRow, string columnName);
    }

}
