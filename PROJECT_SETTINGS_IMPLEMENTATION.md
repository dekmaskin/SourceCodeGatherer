# Project-Specific Settings Implementation

## Overview
This document describes the implementation of project-specific settings for the Source Code Gatherer application. The feature allows the application to remember window settings and user preferences for each project (root directory) separately.

## Features Implemented

### 1. Project Window Settings Model
- **File**: `SourceCodeGatherer/Models/ProjectWindowSettings.cs`
- **Purpose**: Stores window and project-specific settings
- **Properties**:
  - Window dimensions (Width, Height)
  - Window position (Left, Top)
  - Window state (Normal, Maximized, Minimized)
  - Selected file extensions
  - Maximum file size limit
  - Last output path

### 2. Enhanced App Settings
- **File**: `SourceCodeGatherer/Models/AppSettings.cs`
- **Enhancement**: Added `ProjectSettings` dictionary to store multiple project configurations
- **Key**: Normalized project path (lowercase, full path)
- **Value**: ProjectWindowSettings object

### 3. Settings Service Extensions
- **File**: `SourceCodeGatherer/Services/ISettingsService.cs` & `SourceCodeGatherer/Services/SettingsService.cs`
- **New Methods**:
  - `GetProjectWindowSettingsAsync(string projectPath)`: Retrieves settings for a specific project
  - `SaveProjectWindowSettingsAsync(string projectPath, ProjectWindowSettings settings)`: Saves settings for a specific project
- **Features**:
  - Path normalization for consistent storage
  - Graceful handling of missing settings
  - Automatic creation of default settings

### 4. Main Window Integration
- **File**: `SourceCodeGatherer/MainWindow.xaml.cs`
- **Enhancements**:
  - Automatic settings restoration when project changes
  - Window state persistence (size, position, state)
  - Settings saving on window close
  - Project change detection via ViewModel property changes

### 5. ViewModel Support
- **File**: `SourceCodeGatherer/ViewModel/MainViewModel.cs`
- **New Methods**:
  - `GetSelectedExtensions()`: Returns currently selected file extensions
  - `RestoreProjectSettings(ProjectWindowSettings settings)`: Applies project-specific settings
- **Features**:
  - Integration with existing settings system
  - Preservation of user preferences per project

## How It Works

### 1. Project Detection
- The system uses the `RootPath` property in MainViewModel as the project identifier
- When `RootPath` changes, the system automatically saves settings for the previous project and loads settings for the new project

### 2. Settings Storage
- Settings are stored in the existing JSON settings file in `%AppData%\SourceCodeGatherer\settings.json`
- Project settings are stored in a dictionary with normalized paths as keys
- Path normalization ensures consistent storage across different path formats

### 3. Automatic Restoration
- When a project is selected, the system:
  1. Saves current window settings for the previous project
  2. Loads settings for the new project
  3. Applies window size, position, and state
  4. Restores file type selections and other preferences

### 4. Graceful Degradation
- If no settings exist for a project, default settings are used
- All operations are wrapped in try-catch blocks to prevent crashes
- Settings failures are handled silently to maintain application stability

## Testing

### Unit Tests
- **File**: `SourceCodeGatherer.Tests/Services/ProjectWindowSettingsTests.cs`
- **Coverage**:
  - Default settings creation
  - Settings persistence and retrieval
  - Multiple project independence
  - Edge case handling (empty paths, etc.)

### Test Results
- All existing tests continue to pass (45 tests)
- New project settings tests pass (5 tests)
- Total test coverage: 50 tests

## Usage Example

1. User opens project A (`C:\Projects\ProjectA`)
   - Window opens with default settings
   - User adjusts window size to 1200x800
   - User selects `.cs` and `.js` file types
   - Settings are automatically saved

2. User switches to project B (`C:\Projects\ProjectB`)
   - Window automatically resizes to default (800x650)
   - Default file type selections are applied
   - User customizes settings for project B

3. User returns to project A
   - Window automatically restores to 1200x800
   - `.cs` and `.js` file types are automatically selected
   - All previous settings are restored

## Benefits

1. **Improved User Experience**: No need to reconfigure settings when switching projects
2. **Project-Specific Workflows**: Different projects can have different file type preferences
3. **Window Management**: Maintains preferred window layouts per project
4. **Seamless Integration**: Works transparently with existing functionality
5. **Data Persistence**: Settings survive application restarts

## Technical Considerations

1. **Performance**: Settings are loaded/saved asynchronously to avoid UI blocking
2. **Memory Usage**: Settings are cached in memory for performance
3. **File System**: Uses existing settings infrastructure for reliability
4. **Error Handling**: Comprehensive error handling prevents data loss
5. **Backward Compatibility**: Existing settings files continue to work without modification

## Future Enhancements

Potential future improvements could include:
- Export/import of project settings
- Settings templates for similar projects
- Project grouping and organization
- Cloud synchronization of settings
- More granular preference controls