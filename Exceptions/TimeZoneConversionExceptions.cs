

namespace TimeZoneConvertorLibrary.Exceptions
{
    /// <summary>
    /// Custom exception for export service operations with user-friendly messages
    /// </summary>
    public class TimeZoneConversionExceptions : Exception
    {
        /// <summary>
        /// User-friendly error message safe for front-end display
        /// </summary>
        public string UserFriendlyMessage { get; }

        /// <summary>
        /// Error code for categorizing different types of export errors
        /// </summary>
        public string ErrorCode { get; }

        public TimeZoneConversionExceptions(string userFriendlyMessage, string errorCode = null)
            : base(userFriendlyMessage)
        {
            UserFriendlyMessage = userFriendlyMessage;
            ErrorCode = errorCode ?? "CONVERSION_ERROR";
        }

        public TimeZoneConversionExceptions(string userFriendlyMessage, Exception innerException, string? errorCode = null)
            : base(userFriendlyMessage, innerException)
        {
            UserFriendlyMessage = userFriendlyMessage;
            ErrorCode = errorCode ?? "CONVERSION_ERROR";
        }

        public TimeZoneConversionExceptions(string message, string userFriendlyMessage, string? errorCode = null)
            : base(message)
        {
            UserFriendlyMessage = userFriendlyMessage;
            ErrorCode = errorCode ?? "CONVERSION_ERROR";
        }
    }

    /// <summary>
    /// Exception for timezone-related validation errors
    /// </summary>
    public class TimeZoneValidationException : TimeZoneConversionExceptions
    {
        public TimeZoneValidationException(string userFriendlyMessage)
            : base(userFriendlyMessage, "TIMEZONE_VALIDATION_ERROR")
        {
        }
    }

    /// <summary>
    /// Exception for file format validation errors
    /// </summary>
    public class DateTimeParsingException : TimeZoneConversionExceptions
    {
        public DateTimeParsingException(string userFriendlyMessage)
            : base(userFriendlyMessage, "DATE_TIME_PARSING_ERROR")
        {
        }
    }

    /// <summary>
    /// Exception for data processing errors
    /// </summary>
    public class DataProcessingException : TimeZoneConversionExceptions
    {
        public DataProcessingException(string userFriendlyMessage, Exception innerException = null)
            : base(userFriendlyMessage, innerException, "DATA_PROCESSING_ERROR")
        {
        }
    }

}
