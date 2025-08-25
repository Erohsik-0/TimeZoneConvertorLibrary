using NodaTime;
using System.Diagnostics;
using TimeZoneConversionLibrary;
using TimeZoneConvertorLibrary.Exceptions;
using TimeZoneConvertorLibrary.Interfaces;
using TimeZoneConvertorLibrary.Models;

namespace TimeZoneConvertorLibrary.Services
{
    /// <summary>
    /// Main orchestrator service that coordinates all timezone conversion operations
    /// </summary>
    public class TimeZoneConversionOrchestrator : ITimeZoneConversionOrchestrator
    {
        private readonly IValidationService _validationService;
        private readonly IExcelProcessingService _excelProcessingService;
        private readonly IDateTimeZoneProvider _tzProvider;
        private readonly ILogger _logger;

        public TimeZoneConversionOrchestrator(
            IValidationService validationService,
            IExcelProcessingService excelProcessingService,
            IDateTimeZoneProvider tzProvider,
            ILogger logger)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _excelProcessingService = excelProcessingService ?? throw new ArgumentNullException(nameof(excelProcessingService));
            _tzProvider = tzProvider ?? throw new ArgumentNullException(nameof(tzProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TimeZoneConversionResult> ConvertExcelTimeStampsAsync(
            TimeZoneConversionRequest request,
            IProgress<ConversionProgress> progress = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TimeZoneConversionResult
            {
                Statistics = new ConversionStatistics
                {
                    SourceTimeZone = request.SourceTimeZone,
                    TargetTimeZone = request.TargetTimeZone,
                    ColumnName = request.ColumnName
                }
            };

            try
            {
                _logger.LogInformation($"Starting timezone conversion from {request.SourceTimeZone} to {request.TargetTimeZone}");

                // Step 1: Validate the request
                progress?.Report(new ConversionProgress
                {
                    ProcessedItems = 0,
                    TotalItems = 100,
                    CurrentOperation = "Validating request..."
                });

                await _validationService.ValidateConversionRequestAsync(request);

                // Step 2: Get timezone objects
                var sourceTimeZone = _tzProvider[request.SourceTimeZone];
                var targetTimeZone = _tzProvider[request.TargetTimeZone];

                // Step 3: Analyze file for statistics
                progress?.Report(new ConversionProgress
                {
                    ProcessedItems = 10,
                    TotalItems = 100,
                    CurrentOperation = "Analyzing Excel file..."
                });

                var metadata = _validationService.ValidateAndAnalyzeExcelFile(request.ExcelData);
                result.Statistics.TotalRowsProcessed = metadata.TotalRows;

                // Step 4: Process the Excel file
                progress?.Report(new ConversionProgress
                {
                    ProcessedItems = 20,
                    TotalItems = 100,
                    CurrentOperation = "Processing Excel file..."
                });

                // Create a progress wrapper to scale the Excel processing progress
                var excelProgress = new Progress<ConversionProgress>(p =>
                {
                    var scaledProgress = new ConversionProgress
                    {
                        ProcessedItems = 20 + (int)(p.PercentageComplete * 0.75), // Scale to 20-95%
                        TotalItems = 100,
                        CurrentOperation = p.CurrentOperation
                    };
                    progress?.Report(scaledProgress);
                });

                result.ConvertedExcelData = await _excelProcessingService.ProcessExcelFileAsync(
                    request.ExcelData,
                    request.ColumnName,
                    sourceTimeZone,
                    targetTimeZone,
                    request.CancellationToken,
                    excelProgress);

                // Step 5: Finalize
                progress?.Report(new ConversionProgress
                {
                    ProcessedItems = 100,
                    TotalItems = 100,
                    CurrentOperation = "Conversion completed successfully"
                });

                stopwatch.Stop();
                result.Statistics.ProcessingTime = stopwatch.Elapsed;
                result.Statistics.SuccessfulConversions = result.Statistics.TotalRowsProcessed - result.Statistics.ErrorCount;
                result.Success = true;
                result.Message = $"Successfully converted {result.Statistics.SuccessfulConversions:N0} timestamps from {request.SourceTimeZone} to {request.TargetTimeZone}";

                _logger.LogInformation($"Conversion completed successfully in {stopwatch.Elapsed.TotalSeconds:F2} seconds. " +
                    $"Processed: {result.Statistics.SuccessfulConversions:N0}, Errors: {result.Statistics.ErrorCount:N0}");

                return result;
            }
            catch (TimeZoneConversionExceptions ex)
            {
                stopwatch.Stop();
                result.Statistics.ProcessingTime = stopwatch.Elapsed;
                result.Success = false;
                result.Message = ex.UserFriendlyMessage;

                _logger.LogWarning($"Conversion failed with user error: {ex.UserFriendlyMessage}");

                progress?.Report(new ConversionProgress
                {
                    ProcessedItems = 0,
                    TotalItems = 100,
                    CurrentOperation = $"Error: {ex.UserFriendlyMessage}"
                });

                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                result.Statistics.ProcessingTime = stopwatch.Elapsed;
                result.Success = false;
                result.Message = "The conversion operation was cancelled by the user.";

                _logger.LogInformation("Conversion operation was cancelled");

                progress?.Report(new ConversionProgress
                {
                    ProcessedItems = 0,
                    TotalItems = 100,
                    CurrentOperation = "Operation cancelled"
                });

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Statistics.ProcessingTime = stopwatch.Elapsed;
                result.Success = false;
                result.Message = "An unexpected error occurred during the conversion process. Please try again or contact support.";

                _logger.LogError($"Unexpected error during conversion: {ex}");

                progress?.Report(new ConversionProgress
                {
                    ProcessedItems = 0,
                    TotalItems = 100,
                    CurrentOperation = "Unexpected error occurred"
                });

                return result;
            }
        }

        public IEnumerable<string> GetAvailableTimeZones()
        {
            try
            {
                return _tzProvider.Ids
                    .OrderBy(tz => tz)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting available timezones: {ex.Message}");
                return new[] { "UTC", "America/New_York", "Europe/London", "Asia/Tokyo" };
            }
        }

        public bool IsValidTimeZone(string timeZoneId)
        {
            return _validationService.IsValidTimeZone(timeZoneId);
        }

        public async Task<ExcelFileMetadata> AnalyzeExcelFileAsync(byte[] excelData)
        {
            return await Task.Run(() =>
            {
                try
                {
                    return _validationService.ValidateAndAnalyzeExcelFile(excelData);
                }
                catch (TimeZoneConversionExceptions ex)
                {
                    throw new TimeZoneConversionExceptions(ex.UserFriendlyMessage, ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error analyzing Excel file: {ex.Message}");
                    throw new DataProcessingException("Unable to analyze the Excel file. Please ensure it's a valid .xlsx or .xlsm file.");
                }
            });
        }
    }
}
