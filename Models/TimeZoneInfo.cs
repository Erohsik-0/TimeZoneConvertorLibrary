

namespace TimeZoneConvertorLibrary.Models
{
    /// <summary>
    /// Information about a timezone
    /// </summary>
    public class TimeZoneInfo
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public TimeSpan UtcOffset { get; set; }
        public bool SupportsDaylightSavingTime { get; set; }

        public TimeZoneInfo() { }

        public TimeZoneInfo(string id, string displayName, TimeSpan utcOffset, bool supportsDaylightSavingTime)
        {
            Id = id;
            DisplayName = displayName;
            UtcOffset = utcOffset;
            SupportsDaylightSavingTime = supportsDaylightSavingTime;
        }

    }

}
