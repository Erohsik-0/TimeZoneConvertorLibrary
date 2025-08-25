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
    /// Interface for timezone transformation operations
    /// </summary>
    public interface ITimeZoneTransformationService
    {
        DateTime ConvertDateTime(DateTime dateTime, DateTimeZone sourceZone, DateTimeZone targetZone);
        CellConversionResult ConvertCellValue(string cellValue, DateTimeZone sourceZone, DateTimeZone targetZone);
        bool TryParseDateTime(string dateTimeString, out DateTime result);
    }

}
