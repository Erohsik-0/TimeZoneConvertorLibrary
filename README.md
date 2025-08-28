// ===== README =====
// README.md
# TimeZone Conversion Library

A lightweight, robust .NET library for converting DateTime values between different timezones using NodaTime. This library follows Clean Architecture principles and provides comprehensive error handling with user-friendly messages.

## Features

- **Single DateTime Conversion**: Convert individual DateTime values between any two timezones
- **NodaTime Integration**: Leverages the powerful NodaTime library for accurate timezone calculations
- **Clean Architecture**: Well-structured codebase with clear separation of concerns
- **Comprehensive Error Handling**: Custom exceptions with user-friendly error messages
- **Dependency Injection Support**: Easy integration with .NET DI container
- **Async Support**: Asynchronous operations with cancellation token support
- **Timezone Validation**: Built-in validation with helpful suggestions for invalid timezone IDs

## Installation

```bash
dotnet add package TimeZoneConversionUtility
```

## Quick Start

### Basic Usage

```csharp
using TimeZoneConversionLibrary.Extensions;
using TimeZoneConversionLibrary.Interfaces;
using TimeZoneConversionLibrary.Models;

// Setup DI
var services = new ServiceCollection();
services.AddTimeZoneConversionServices();
var serviceProvider = services.BuildServiceProvider();

// Get the service
var conversionService = serviceProvider.GetRequiredService<ITimeZoneConversionService>();

// Convert DateTime
var sourceDateTime = new DateTime(2024, 12, 25, 15, 30, 0);
var convertedDateTime = conversionService.ConvertDateTime(
    sourceDateTime,
    "America/New_York",
    "Europe/London"
);

Console.WriteLine($"Original: {sourceDateTime} (EST)");
Console.WriteLine($"Converted: {convertedDateTime} (GMT)");
```

### Using Request/Result Pattern

```csharp
var request = new TimeZoneConversionRequest(
    new DateTime(2024, 12, 25, 15, 30, 0),
    "America/New_York",
    "Asia/Tokyo"
);

var result = conversionService.ConvertDateTime(request);

if (result.Success)
{
    Console.WriteLine($"Converted: {result.ConvertedDateTime}");
    Console.WriteLine($"Processing Time: {result.ProcessingTime.TotalMilliseconds}ms");
}
else
{
    Console.WriteLine($"Error: {result.Message}");
    Console.WriteLine($"Error Code: {result.ErrorCode}");
}
```

### Async Operations

```csharp
var request = new TimeZoneConversionRequest(
    DateTime.Now,
    "UTC",
    "Australia/Sydney"
);

var result = await conversionService.ConvertDateTimeAsync(request, cancellationToken);
```

## Timezone Validation

```csharp
var validationService = serviceProvider.GetRequiredService<ITimeZoneValidationService>();

// Check if timezone is valid
bool isValid = validationService.IsValidTimeZone("America/New_York"); // true

// Get suggestions for invalid timezone
var suggestions = validationService.GetTimeZoneSuggestions("New_York");
// Returns: ["America/New_York", "America/New_York/Nassau", ...]

// Get all available timezones
var allTimeZones = validationService.GetAvailableTimeZones();

// Get timezone information
var tzInfo = validationService.GetTimeZoneInfo("Europe/London");
```

## Error Handling

The library provides specific exception types for different scenarios:

```csharp
try
{
    var result = conversionService.ConvertDateTime(dateTime, "Invalid/Timezone", "UTC");
}
catch (TimeZoneValidationException ex)
{
    Console.WriteLine($"Timezone validation error: {ex.UserFriendlyMessage}");
    Console.WriteLine($"Error code: {ex.ErrorCode}");
}
catch (DateTimeParsingException ex)
{
    Console.WriteLine($"DateTime parsing error: {ex.UserFriendlyMessage}");
}
catch (TimeZoneConversionException ex)
{
    Console.WriteLine($"General conversion error: {ex.UserFriendlyMessage}");
}
```

## Dependency Injection Options

### Scoped Services (Default)
```csharp
services.AddTimeZoneConversionServices();
```

### Singleton Services (Better Performance)
```csharp
services.AddTimeZoneConversionSingletonServices();
```

### With Custom Logger
```csharp
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
services.AddTimeZoneConversionServices(loggerFactory);
```

## Common Timezone Examples

```csharp
// US Timezones
"America/New_York"    // Eastern Time
"America/Chicago"     // Central Time  
"America/Denver"      // Mountain Time
"America/Los_Angeles" // Pacific Time

// European Timezones
"Europe/London"       // GMT/BST
"Europe/Paris"        // CET/CEST
"Europe/Berlin"       // CET/CEST

// Asian Timezones
"Asia/Tokyo"          // JST
"Asia/Shanghai"       // CST
"Asia/Kolkata"        // IST

// Other
"UTC"                 // Coordinated Universal Time
"Australia/Sydney"    // AEST/AEDT
```

## Architecture

The library follows Clean Architecture principles:

- **Models**: Request/Response models and DTOs
- **Interfaces**: Service contracts and abstractions
- **Services**: Business logic implementations
- **Extensions**: Dependency injection configuration
- **Exceptions**: Custom exception hierarchy

## Dependencies

- **NodaTime 3.2.2**: Core timezone conversion functionality
- **Microsoft.Extensions.Logging 9.0.8**: Logging abstraction
- **.NET 9.0**: Target framework

## License

This project is licensed under the MIT License.