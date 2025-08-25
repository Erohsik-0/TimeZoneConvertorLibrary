

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
            ErrorCode = errorCode ?? "EXPORT_ERROR";
        }

        public TimeZoneConversionExceptions(string userFriendlyMessage, Exception innerException, string? errorCode = null)
            : base(userFriendlyMessage, innerException)
        {
            UserFriendlyMessage = userFriendlyMessage;
            ErrorCode = errorCode ?? "EXPORT_ERROR";
        }

        public TimeZoneConversionExceptions(string message, string userFriendlyMessage, string? errorCode = null)
            : base(message)
        {
            UserFriendlyMessage = userFriendlyMessage;
            ErrorCode = errorCode ?? "EXPORT_ERROR";
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
    public class FileFormatException : TimeZoneConversionExceptions
    {
        public FileFormatException(string userFriendlyMessage)
            : base(userFriendlyMessage, "FILE_FORMAT_ERROR")
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
