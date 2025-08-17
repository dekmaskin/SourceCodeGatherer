# Logging in Source Code Gatherer

This application uses structured logging with Serilog to provide comprehensive logging capabilities.

## Log Location

Logs are automatically saved to:
```
%LOCALAPPDATA%\SourceCodeGatherer\Logs\
```

For example: `C:\Users\[Username]\AppData\Local\SourceCodeGatherer\Logs\`

## Log Files

- **app-[date].log**: Daily rolling log files
- **Retention**: Configurable (default 30 days, adjustable in Settings)
- **Format**: Structured text format with timestamps, log levels, and context

## Log Levels

The application logs at different levels:

- **Trace**: Very detailed information (only in debug builds)
- **Debug**: Detailed information for debugging
- **Information**: General application flow information
- **Warning**: Potentially harmful situations
- **Error**: Error events that don't stop the application
- **Fatal**: Very severe error events that might cause termination

## What Gets Logged

### Application Lifecycle
- Application startup and shutdown
- Unhandled exceptions
- Version and system information

### File Operations
- Directory scanning operations
- File extension discovery
- Export operations (file and clipboard)
- File access errors and warnings
- Performance metrics (timing)

### User Interface
- Window operations (drag/drop, settings changes)
- Project path changes
- File management operations
- Settings persistence

### Settings Management
- Settings loading and saving
- Project-specific settings
- Recent paths management
- Configuration errors

## Debug vs Release Builds

- **Debug builds**: Include console output and more verbose logging
- **Release builds**: File logging only, optimized for performance

## Privacy

The logging system is designed to protect user privacy:
- File paths are logged for debugging but can be considered sensitive
- No file contents are logged
- Personal information is not logged
- Logs are stored locally only

## Configuration

### Log Retention

You can configure how long log files are kept:

1. Open the application
2. Go to **Settings** (gear icon or menu)
3. In the **Logging Settings** section, adjust **Log retention (days)**
4. Valid range: 1-365 days (default: 30 days)
5. Click **OK** to save

**Note**: Log cleanup happens automatically when the application starts. Changing the retention setting will take effect on the next application startup.

## Troubleshooting

If you encounter issues:

1. Check the latest log file in the logs directory
2. Look for ERROR or FATAL level messages
3. Check timestamps to correlate with when issues occurred
4. Performance issues may show up as long operation times in INFO messages
5. If logs are missing, check the retention setting in Settings

## Log File Example

```
2024-01-15 10:30:15.123 +00:00 [INF] SourceCodeGatherer.App: Application startup initiated
2024-01-15 10:30:15.456 +00:00 [INF] SourceCodeGatherer.Services.LoggingService: === Source Code Gatherer Application Started ===
2024-01-15 10:30:16.789 +00:00 [INF] SourceCodeGatherer.Services.FileService: Starting file extension scan for path: C:\Projects\MyProject
2024-01-15 10:30:17.012 +00:00 [INF] SourceCodeGatherer.Services.FileService: Found 15 unique file extensions in 223ms
```