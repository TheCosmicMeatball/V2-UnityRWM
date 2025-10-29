# Share Plugin Implementation

This plugin enables native image sharing on Android and iOS platforms.

## Files Created

### iOS (Assets/Plugins/iOS/)
- **SharePlugin.mm**: Objective-C++ plugin that invokes UIActivityViewController for native iOS sharing

### Android (Assets/Plugins/Android/)
- **AndroidManifest.xml**: Registers FileProvider for secure file sharing on Android 7.0+
- **res/xml/filepaths.xml**: Defines shared cache directory paths for FileProvider

## What Was Fixed

### Android Issue
**Problem**: Used deprecated `Uri.fromFile()` which throws `FileUriExposedException` on Android 7.0+ (API 24+)

**Solution**: Replaced with `FileProvider.getUriForFile()` which uses content:// URIs instead of file:// URIs, complying with modern Android security requirements.

### iOS Issue
**Problem**: Only logged a debug message instead of invoking the native share sheet

**Solution**: Implemented native Objective-C++ plugin that:
- Loads the image from the file path
- Creates a UIActivityViewController with the image and message
- Presents the native iOS share sheet
- Handles iPad popover presentation properly

## Technical Details

### Android FileProvider Configuration
- Authority: `{Application.identifier}.fileprovider`
- Shared path: Application temporary cache directory
- Permissions: Read-only URI permissions granted to receiving apps

### iOS Native Share Sheet
- Uses UIActivityViewController (standard iOS sharing interface)
- Supports all standard iOS share destinations (Messages, Mail, Social Media, etc.)
- Excludes irrelevant activities (Print, Add to Contact, etc.)
- Handles iPad popover presentation requirements

## Testing Notes

For Android:
- Test on devices running Android 7.0 (API 24) or higher
- Verify that share sheet appears and image can be shared to various apps

For iOS:
- Test on both iPhone and iPad
- Verify native share sheet appears with correct image and message
- Test sharing to various destinations (Messages, Mail, etc.)

## Dependencies

- Android: Requires AndroidX Core library (should be included in modern Unity Android builds)
- iOS: Uses UIKit framework (standard iOS framework, no additional dependencies)
