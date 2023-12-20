# Changelog

All notable changes to NexPlayer will be documented in this file.

### Version 2.3.2
- [Unity]
    - [Fixed] Removed some NexUtility methods that shouldn't be exposed on create asset menus.
    - [Fixed] Removed .jpg files and swapped them with .png files.

- [iOS]
    - [Fixed] Fixed mute value was leeking throught scenes.
    - [Fixed] Fixed loop when changing between scenes.
    - [Fixed] Fixed overrided methods were leeking into new NexPlayer instances.
    - [Fixed] After downloading a video, regular streaming contents will still play.

- [Windows]
    - [Fixed] Fixed an error that appeared when starting the video playback.
    - [Fixed] Fixed a crash when changing or reloading the scene.
    - [Fixed] Showing multistream error with better nomenclature.
    - [Fixed] PlaybackStarted event is now thrown if starting playback from Stopped state.
    
- [macOS]
    - [Fixed] Fixed crash when releasing.
    - [Fixed] PlaybackStopped event is now only called one time.

- [WebGL]
    - [Fixed] Fixed warning on build configuration window reapearing.
    - [Fixed] Fixed an error when canceling the build.
    - [Fixed] Fixed the player looping a few frames before end of content.
    - [Fixed] PlaybackStopped event is now only called one time.

### Version 2.3.1
- [Unity]
	- [Added] Added linear color space support.
	- [Updated] Deprecated the fullfeat sample event listeners in favour of the NexPlayerBehaviour event methods.
	- [Updated] Unified all platforms to use stretch to fullscreen aspect ratio by default.

- [iOS]
	- [Fixed] Fixed downloaded videos no longer playing after releasing and reinitialzing the player.
	- [Fixed] Fixed the NexPlayerList and NexPlayerMultipleLanguages samples being muted by default.
	- [Fixed] Made the Render Texture invisible on scene load, unifying it with the rest of the platforms.
	- [Fixed] Removed warnings when importing the package or switching platforms regarding the iOS download permissions on Unity 2022.
	- [Fixed] Fixed the HTTP Download permissions on the Build Configuration Tool on Unity 2022.

- [Windows]
	- [Added] Added support for multiple renderers.
	- [Fixed] Removed continously printed warnings regarding the texture on Unity 2022.
	- [Fixed] Fixed a crash when releasing the player after encountering a multistream critical error. The player releases automatically now.
	- [Fixed] EventPlaybackStarted() no longer gets called multiple times at the beginning of the video playback.
	
- [macOS]
	- [Updated] The events are now emitted per multistream instance.

- [WebGL]
	- [Added] Added support to control the playback looping.
	- [Updated] Updated the templates to support Unity 2022 (WebGLTemplates/2020_21_22).
	- [Fixed] Improved the player release and reinitialization, ensuring the player works properly when reloading a scene.

### Version 2.3.0
- [Unity]
	- [Removed] Unity 2018 version is no longer supported.
	- [Added] Changed the project structure to Unity's Package Manager.
	- [Added] Improved project organization in terms of namespaces. See section "20. Migrating from previous releases" of the Integration Guide for more information.
	- [Added] Added an error message when downloading an unsupported mp4 content.
	- [Added] Added an error message when binding the UI reference of the NexPlayer sample without the UI component.
	- [Updated] Improved loop behaviour.
	- [Updated] Improved player logs.

- [Android]
	- [Updated] Improved the multistream loop by applying it individually to each player instance
	- [Fixed] Run in background mode couldn't be disabled in multistream

- [macOS]
	- [Fixed] Fixed loop property.
	- [Fixed] Fixed run in background.
	- [Fixed] Fixed expand video button.
	- [Fixed] Added a shell script to resolve the "NexPlayermacOSX.bundle is damaged" error message: "NexPlayerUnity/Assets/NexPlayer/Scripts/SDK/Utility/repair_macos_bundle.sh"

- [WebGL]
	- [Added] Added a seekbar to the NexPlayer full-featured sample.
	- [Added] Added run in background.
	- [Added] Added expand video button.

### Version 2.2.16

- [Unity]
    - [Fixed] Fixed player routing to main Windows system volume instead of application volume.

### Version 2.2.15

- [Unity]
    - [Fixed] Fixed an issue that was preventing to change URL from Inspector on the Simple Sample.

### Version 2.2.14
- [Xbox]
    - [Added] Added support for Xbox Series X/S and Universal Windows Platform (UWP).
	
### Version 2.2.13

- [Windows]
    - [Fixed] Fixed EventHandleAudioPCM callback not being called after the video loops twice.
    - [Fixed] Fixed EventHandleAudioPCM callback containing 0 values after several minutes.
    
### Version 2.2.12

- [Windows]
	- [Fixed] Corrected behaviour when switching from streaming to local content with autoplay disabled.
	- [Fixed] Fixed a crash when changing from a valid video content to an invalid one.
	- [Fixed] Audio no longer plays through the built-in audio pipeline for a second when enabling PCM audio and autoplay.

### Version 2.2.11

- [Windows]
	- [Added] Audio Properties for PCM Data functionality (Number of Channels, Sampling Rate).
	- [Fixed] Consistent Audio Data Output for the PCM Data functionality.
	- [Added] The Audio PCM Data functionality is started according to the autoplay configuration.
	- [Added] Flag to enable and disable the PCM data functionality.
	- [Fix] Removed bufferLength argument from the PCM data callback

### Version 2.2.10

- [Windows]
	- [Changed] EventHandleAudioPCM buff parameter now contains multiple float values (PCM samples).
	- [Added] EventHandleAudioPCM now has a buffLength parameter with the length of the Audio PCM buffer.

### Version 2.2.9

- [Windows]
	- [Added] Exposed audio PCM data buffer to Unity layer. The audio buffers are floats ranging from -1.0f to 1.0f. Only supported on Windows.

### Version 2.2.7

- [Unity]
	- [Removed] Unity 2018 version is no longer supported.
	- [Added] Changed the project structure to Unity's Package Manager.
	- [Added] Improved project organization in terms of namespaces. See section "20. Migrating from previous releases" of the Integration Guide for more information.
	- [Added] Added an error message when downloading an unsupported mp4 content.
	- [Added] Added an error message when binding the UI reference of the NexPlayer sample without the UI component.
	- [Updated] Improved loop behaviour.
	- [Updated] Improved player logs.

- [Android]
	- [Updated] Improved the multistream loop by applying it individually to each player instance
	- [Fixed] Run in background mode couldn't be disabled in multistream

- [macOS]
	- [Fixed] Fixed loop property.
	- [Fixed] Fixed run in background.
	- [Fixed] Fixed expand video button.
	- [Fixed] Added a shell script to resolve the "NexPlayermacOSX.bundle is damaged" error message: "NexPlayerUnity/Assets/NexPlayer/Scripts/SDK/Utility/repair_macos_bundle.sh"

- [WebGL]
	- [Added] Added a seekbar to the NexPlayer full-featured sample.
	- [Added] Added run in background.
	- [Added] Added expand video button.

### Version 2.2.6

- [Windows]
	- [Added] Added functionality to retrieve the AVG FPS and the Framerate.

- [macOS]
	- [Added] Added functionality to retrieve the AVG FPS and the Framerate.

### Version 2.2.5

- [Unity]
	- [Fixed] Unnecessary sprites have been removed.
	- [Fixed] Errors reported during the SDK package import have been resolved.

- [Android]
	- [Fixed] Show a warning when an unsupported mp4 content is trying to be downloaded.

- [iOS]
	- [Fixed] Show a warning when an unsupported mp4 content is trying to be downloaded.
	- [Fixed] Frame transformation options are no longer ignored on iOS.
	- [Fixed] RunInBackground property is no longer ignored on iOS.

- [macOS]
	- [Fixed] Unhandled NullReferenceException was thrown if UI References were set without the UI GameObject in the Scene. Now it shows a dialog to instantiate the NexPlayer_UI.

- [Windows]
	- [Fixed] Windows player texture was brighter than the source video. Two different shaders has been introduced for rawImage that is compatible with linear color space: NexPlayerDefaultShader.shader and NexPlayerDefaultShaderRawImage.shader.
	- [Fixed] Unhandled NullReferenceException was thrown if UI References were set without the UI GameObject in the Scene. Now it shows a dialog to instantiate the NexPlayer_UI.

### Version 2.2.4

- [Unity]
	- [Updated] Deprecated variables 'streamingUri', 'assetUri' and 'localURI'. The variable 'URL' can be used to play all content types along with the 'contentType' variable.
	- [Fixed] Changing the 'streamingUri' value no longer overrides the value of the 'contentType' variable.

- [Android]
	- [Fixed] Fixed the rendering when setting Android blit type to 'Never' or 'Auto'.

- [macOS]
	- [Fixed] Fixed player getting stuck after looping in AssetPlay mode.
	- [Fixed] Fixed sample Change Render Mode.

- [iOS]
	- [Fixed] Fixed playing contents with white spaces in AssetPlay mode.

### Version 2.2.3

- [Unity]
	- [Fixed] The player will no longer release when providing an empty URL.

### Version 2.2.2

- [Unity]
	- [Added] Added the field isLiveStream to NexPlayerBehaviour, which needs to be set to true when the video to be opened is a live stream.
	- [Added] Added an unlit shader to prevent environment lighting effects.

- [Android]
	- [Added] Added support for GetTotalTime() for live streams, retrieving the end of the range of the current content that is seekable.
	- [Added] Added the Event EventTotalTimeChanged() that it's triggered everytime the total time changes during the playback.

- [Windows]
	- [Added] Added support for GetTotalTime() for live streams, retrieving the end of the range of the current content that is seekable.
	- [Added] Added the Event EventTotalTimeChanged() that it's triggered everytime the total time changes during the playback.
	- [Fixed] Fixed run in background behaviour. Now, when coming from background, the video keeps the status it had before.
	- [Fixed] Fixed issue that used to render a transparent frame during the loading process.

- [macOS]
	- [Fixed] Fixed opening and closing multiple times no longer renders a stale frame from the previous content.
	- [Fixed] Fixed run in background behaviour. Now, when coming from background, the video keeps the status it had before.

- [iOS]
	- [Fixed] Fixed run in background behaviour. Now, when coming from background, the video keeps the status it had before.

### Version 2.2.1

- [iOS]
	- [Fixed] Fixed wrong loop behaviour which avoided start the playback from the beginning.

- [macOS]
	- [Fixed] Fixed wrong loop behaviour which avoided start the playback from the beginning.
	- [Fixed] Fixed maximize screen button.

- [Windows]
	- [Fixed] Fixed EndOfPlay event is not called when stopping the video via the 'Stop' UI button.

- [WebGL]
	- [Fixed] Fixed wrong loop behaviour which avoided start the playback from the beginning.
	- [Fixed] Fixed maximize screen button.

### Version 2.2.0

- [Unity]
	- [Added] Added support to seek by pressing on the seekbar for the full-featured NexPlayer sample.

- [Windows]
	- [Added] Added ID3 GEOB metadata support.
	- [Added] Added ID3 TXXX metadata support.

### Version 2.1.4

- [Unity]
	- [Added] Widevine support added for the playlist sample (NexPlayerList).
	- [Added] Support for Unity version 2021.2.X
	- [Fixed] The render texture no longer remains in the background in the ChangeRenderMode sample.
	- [Fixed] The NexPlayerSimple sample now sets the volume correctly.
	- [Fixed] The NexPlayer controllers components are now referencing the scripts correctly when a scene without the SDK is imported.

- [Android]
	- [Added] Added support for VoD synchronisation. You can play up to 5 videos synchronised with each other.
	- [Updated] GetCurrentTime API was returning inconsistent values for the live contents. It is now retrieving the PTS value of the audio track when playing a livestream.
	- [Updated] GetTotalTime API was returning outdated value for the live playback. It is now retrieving the correct value during the live playback.
	- [Fixed] The audio from multi-stream no longer leaks when changing to a scene with the player with a single instance.
	- [Fixed] Audio was playing when the MutePlay flag was enabled for the multi-stream. It is now respecting the value that is set.
	- [Fixed] Changed the internal texture format from RGBA to RGB to resolve video transparency problems.
	- [Fixed] The player was crashing when attempting to play local .avi videos, which are not supported on Android. It is now triggering NexErrorCode.NOT_SUPPORT_VIDEO_CODEC error.
	- [Fixed] The player was briefly rendering a frame from the previous video when closing and opening a new content. It is now cleaning the internal texture.
	- [Fixed] The NexPlayer Build Configuration Window was not properly setting the Graphics APIs in all Unity versions. It is now setting them correctly.

- [Windows]
	- [Added] Added logs for each plugin internal function, taking into account the logging level.
	- [Updated] GetCurrentTime API was returning inconsistent values for the live contents. It is now retrieving the server time.
	- [Updated] EventOnTime frequency is changed to once per second instead of per frame.
	- [Fixed] Resolved an Unity crash with HLS livestreams when pausing and resuming many times.
	- [Fixed] Resolved an Unity crash with HLS livestreams when disabling autoplay and resuming the player.
	- [Fixed] The player was releasing and getting destroyed when unloading any scenes. It is now doing it only when the unloaded scene was the one where the player was originally instantiated at.
	
- [macOS]
	- [Added] Added support for CEA-608 subtitles.
	- [Added] Added support for audio and text (Closed Captions/Subtitles) track selection.
	- [Added] Added ID3 GEOB metadata support.
	- [Added] Added ID3 TXXX metadata support.
	- [Updated] EventOnTime frequency is changed to once per second instead of per frame.

- [WebGL]
	- [Fixed] The Build Configuration tool was checking the wrong project setting. It is now verifying that Decompression Fallback is enabled.

### Version 2.1.0

- [Unity]
	- [Add] Added MultiView sample
	- [Updated] Improved the package structure and the integration
	- [Updated] Improved the code samples
	- [Updated] Improved the documentation

- [Android]
	- [Add] Added compatibility with Universal Render Pipeline (URP)
	- [Add] Support for encoded urls
	- [Updated] HLS/MP4 improvements
	- [Updated] MultiView improvements
	- [Fix] Offline Playback improvements

- [iOS]
	- [Add] Support for encoded urls
	- [Updated] HLS/MP4 improvements	

### Version 2.0.4

- [macOS]
	- [Add] IL2CPP support

- [Unity]
	- [Updated] Improved backwards compatibility

- [iOS]
	- [Fix] Subtitle/Close Captions improvements	
### Version 2.0.3

- [Unity]
	- [Updated] Made custom players easier to create by adding even more features to NexPlayerBehaviour and additional controllers
	- [Updated] Simplified setting playback properties on custom players by overriding the method SetPreInitConfiguration()
	- [Updated] Setting debug logs can now be done by changing a single boolean
	- [Add] Added custom player samples

- [Android]
	- [Add] Playback commands now get queued up, with async methods (play, pause, stop, seek, open, close)
	- [Fix] Subtitle/Close Captions improvements
	- [Fix] PDT improvements

- [iOS]
	- [Add] Playback commands now get queued up, with async methods (play, pause, stop, seek, open, close)
	- [Fix] Subtitle/Close Captions improvements
	- [Fix] Aspect ratio improvements

- [macOS]
	- [Fix] Loop improvements

### Version 2.0.2

- [iOS] 
	- [Fix] Improved iOS material override rendering

- [Android] 
	- [Fix] Simplified build process on Android based VR devices

### Version 2.0.1

- [Android] 
	- [Fix] Improved Multistream SPD/PDT synchronization (MultiView)

### Version 2.0.0

- [Unity]
	- [Updated] Major architecture improvements
	- [Updated] Slimmed down player: NexPlayerSimple.cs
	- [Updated] Full- featured example player: NexPlayer.cs
	- [Updated] Shared base class: NexPlayerBehavior.cs
	- [Updated] Custom players should now inherit from NexPlayerBehavior.cs and override the desired functions (Events, Errors, Controllers)
	- [Updated] Organized the SDK in controllers and partial classes
	- [Updated] Reflected this changes in the Integration Guide

- [iOS]
	- [Fix] Fixed render texture rendering issue when flipping the device

- [Windows]
	- [Fix] Fixed playback issues involving seeking, autoplay and loop
	- [Fix] Fixed multistream playback issues

- [macOS]
	- [Fix] Fixed a crash when using Asset play and not having a subtitle file

### Version 1.9.8

- [Android]
	- [Add] Multibyte (UTF- 8) URL support for DASH
	- [Fix] Removed scale logs from GetPixelSizeOfMeshRenderer()

- [iOS]
	- [Add] Multibyte (UTF- 8) URL support for DASH
	- [Fix] Removed printf and NSLog iOS logs

### Version 1.9.7

- [iOS]
	- [Fix] Fixed issue with the pssh key for Widevine

### Version 1.9.5

- [iOS]
	- [Fix] Added support for variable PSSH Key length

### Version 1.9.4

- [Windows]
	- [Fix] Faulty Windows Plugin

### Version 1.9.3

- [Windows]
	- [Fix] Improvement in seekbar Behaviour

- [WebGL]
	- [Fix] Fixed HUD events in Unity editor when it is in webgl platform

### Version 1.9.2

- [Unity]
	- [Updated] Changed the name of the Integration Guide included in the package from `Unity_Integration_Guide.pdf` to `NexPlayer_Integration_Guide.pdf`.
	- [Updated] Replaced all the .jpg files with the equivalent .png files.
	- [Updated] Replaced FBX models for COLLADA models
	- [Deleted] The .mp4 and .srt files along with the "Streaming Assets" folder included in the package have been deleted

- [Windows]
	- [Fix] Fixed Windows Builds in Unity 2018.

- [MacOSX]
	- [Fix] Fixed subtitles on MacOSX.

- [Android]
	- [Fix] Allow offline streaming in Android version 10 or higher.


### Version 1.9.1.00 

- [WebGl] 
	- [Add] Added WebGl support for Unity 2020

- [iOS]
	- [Fix] Fixed ABR starts in true as default
	- [Fix] Fixed OpenGL dependencies for XCode builds
- [MacOSX]
	- [Fix] Fixed multistream initialization

### Version 1.9.0.00 

- [Unity]
	- [Delete] Deleted all prefabs
	- [Delete] Deleted all Scenes 
	- [Update] Updated NexPlayer.cs Editor so is collapsable and prevents human errors in imputs

- [WebGl] 
	- Added WebGl support

- [Unity]
	- [Add] Added Context Menu Actions that substitude the old prefabs
	- [Add] [Add] Added Master Sample Scene that contains all the old ones
	- [Add] Added NexMaterials.asset and NexUISprites.asset to centralize the resources

### Version 1.8.6.00

- [Unity]
	- [Add] Added support for setting the plugins folder inside or outside Nexplayer folder

- [MacOSX]
	- [Fix] Fixed Video Render Flickering

- [Android]
	- [Fix] Fixed Subtittle Toggle
	- [Fix] Fixed Missing texture during resolution change

### Version 1.8.5.02

- [Android]
	- [Fix] Fixed 360video stop rendering when UI is toggled

- [Unity]
	- [Fix] Fixed Transparency.shader to work in HSV color space

### Version 1.8.5.00

- [Android]
	- [Add] Added method NexPlayerUnity_ReleasePlayer() 
	- [Add] Updated to Nexplayer iOS SDK Version 6.71.0.831
	- [Fix] Fixed crash after trying to play DRM content with airplane mode ON 
	- [Fix] Fixed crash after stop or release when trying to play content without license file 
	- [Fix] Fixed infinite loading phenomenom when playing DRM content in the network unavailable state
	- [Fix] Fixed incompatibility between Background functionality after enabling Airplane Mode
	- [Fix] Fixed DRM crash on Android versions 6.0.1 & 7.0

- [iOS]
	- [Fix] Fixed memory accumulation issue
	- [Fix] Fixed Issue with DRM initialization.

### Version 1.8.4.00

- [Unity]
	- [Update] Slightly changed the OnDisable and `NEXPLAYER_EVENT_END_OF_CONTENT` implementation to provide more versatility for external player destruction
	
- [iOS]
	- [Update] Fixed Issue on Upload to the App Store.
	- [Update] Updated to Nexplayer iOS SDK Version 5.40.1.5139

### Version 1.8.3.00

- [Unity] 
	- [Fix] Fixed a crash when disabling and enabling the player

- [Windows]
	-  [Fix] Fixed a crash when using an invalid url

- [iOS]
	- [Fix] Fixed a crash when using Local Playback DRM settings

### Version 1.8.2.00

- [iOS]
	- [Fix] Fixed a crash on end of content when using null references to UI elements

### Version 1.8.1.00

- [iOS]
	- [Update] Updated the internal widevine libraries to support iOS 14

### Version 1.7.7.00

- [Windows]
	- [Fix] Fixed the Post Build Process to make it compatible with other Windows Plugins for Unity

### Version 1.7.6.00

- [iOS]
	- [Fix] Clean subtitle url on disable.

- [MacOSX]
	- [Fix] Fixed freeze when exiting the multistream scenes by fixing nativeShutdown function in the MacOSX library.
	- [Fix] Fixed seek function that used to do an uncorrect behaviour with certain .mp4 files.

- [Android]
	- [Fix] Fixed PlayOffline function.
	- [Update] Updated internal NexPlayer Android SDK to version 6.69.2.818.

- [Unity]
	- [Fix] Fixed NexPlayer UI buttons in some scenes.
	- [Delete] Deleted the scene NexPlayer_PlaybackSetting_Sample.

- [Windows]
	- [Update] Updated WindowsNexPlayerSDK.dll.

### Version 1.7.5.00

- [Windows]
	- [Fix] Fixed NexPlayer Events to dispatch them in the same order as Android.
	- [Fix] Fixed first frame issue that used to render the last frame before playing.

- [Android]
	- [Fix] Rollbacked internal NexPlayer Android SDK to version 6.69.2.811

- [MacOSX]
	- [Fix] Fixed change render mode issue that used to lose the the raw image reference.
	- [Fix] Fixed NexPlayer Events to dispatch them in the same order as Android.
	- [Fix] Fixed crash on MacOSX after playing a scene.

### Version 1.7.4.00

- [Android]
	- [Update] Updated internal NexPlayer Android SDK to version 6.69.2.819

### Version 1.7.3.00

- [MacOSX]
	- [Add] Load SRT subtitles from Streaming Assets
	- [Fix] Fixed total time issue. The Stop Callback was deleting the total time display.

- [Unity]
	- [Change] Reorganized the file structure for the 360 Module. This modification makes the 360 module fully independant.

- [Android/iOS]
	- [Fix] Fixed Secondary Area of the Seek Bar GameObject. The secondary area is filled with the buffering of the player.

### Version 1.7.2.00

- [Android]
	- [Fix] Created the override  public override void SetTextureColor(Color color) for the current function SetTextureColor(Color color, GameObject gameObject)

- [MacOSX]
	- [Fix] Fixed issue on Maximize Screen function.
	- [Fix] Fixed Callbacks for Paused, Play and Stop functions.

### Version 1.7.1.00

- [Android]
	- [Fix] Fixed issue with livestreaming and SPD. The player used to set on and off continiously everytime the application was running in background.

- [Windows]
	- [Fix] Fixed crash issue. The player was crashing in Unity if the URL was empty. 

### Version 1.7.0.00

- [Android/iOS]
	- [Add] Added functionality to synchronize live streams with SPD (DASH) & PDT (HLS).

- [Windows]
    - [Deleted] Deleted Windows compatiblity with x86

### Version 1.6.1.00

- [Android]
	- [Fix] Fixed compatibility with Oculus devices due to an error on the NexPlayer Android Core.

### Version 1.6.0.00 

- [MacOSX]
	- [Add] Video Player Compatibility with Mac OS.

### Version 1.5.4.00

- [Unity]
	- [Add]Added Compatibility with Unity version 2019.3.

- [Unity/iOS]
	- [Delete] Deleted PBX library from the folder "Editor" to replace it with a simplified script named "BuildPostProcessor.cs". The folder "Editor" must contain only the following files:
		- BuildPostProcessor.cs
		- NexCustomEditor.cs
		- NexPlayerEditor.cs

### Version 1.5.3.00

- [Windows]
	- [Add] Added functionality to use multiple streamings at the same time on Windows.

### Version 1.5.2.00

- [Android]
	- [Fix] Fixed grey texture appearing on opening content.

### Version 1.5.1.00

- [Android]
	- [Fix] Fixed error on init error trigger when there isn't Wifi or LTE connection.

### Version 1.5.0.00

- [iOS]
	- Added functionality to play multiple streams at the same time.
	- [Fix] Fixed HTTP header functionality on simple and multiple streaming.
	- [Fix] Fixed crash error caused by Caption Text gameobject.
	- [Fix] Fixed audio and video error happening with loop option.
	- [Fix] Fixed functionality to set the texture color before playback.
	- [Fix] Fixed freeze error with DRM content without Internet Connection.

- [Android]
	- [Fix] Fixed functionality to set the texture color before playback.
	- [Fix] Fixed freeze error with DRM content without Internet Connection.

### Version 1.4.1.00

- [Android]
	- [Fix] Fixed HTTP header functionality on simple and multiple streaming.

### Version 1.4.0.00

- [Windows]
	- [Add] Added functionality to play multiple streams at the same time.


### Version 1.3.7.00

- [iOS]
	- [Fix] Fixed error in the loop function.

### Version 1.3.6.00

- [Android]
	- [Fix] Fixed freeze error with DRM content on Air Plane Mode.

### Version 1.3.5.00

- [Android]
	- [Fix] Fixed error that didn't allow to set the Raw Image texture in single video without setting the configuration.

### Version 1.3.4.00

- [Android]
	- [Fix] Fixed functionality to set Http Headers during runtime with single and multiple streams.

### Version 1.3.3.00

- [iOS]
	- [Add] Added functionality to play local playback with DRM.
	- [Fix] Fixed functionality of Widevine headers to play local playback with DRM.		
### Version 1.3.2.00

- [Android]
	- [Add] Added functionality to play local playback with DRM.
	- [Fix] Fixed functionality of Widevine headers to play local playback with DRM.			
### Version 1.3.1.00

- [Android]
	- [Add] Added functionality to set multiple HTPP headers per stream.
	- [Add] Added player selection on multiple stream playback.
	- [Add] Added autoplay to selected streams.
	- [Add] Added basic interaction to multiple stream selection (Play, Pause, Maximize)
	- [Add] Added Webvtt output on multiple steeams.
	- [Add] Added functionality to change a selected stream in runtime with multiple streams.

### Version 1.3.0.00

- [Android]
	- [Add] Added functionality to play multiple streams at the same time.

### Version 1.2.1.00

- [Unity]
	- [Fix] Fixed Live Stream setting to refresh the player after the app pauses on Android and iOS.

### Version 1.2.0.00

- [Android/iOS]
		 - [Add] Added functionality to save and play offline streams and play them without internet connection.

### Version 1.1.1.00

- [Unity]
	- Added functionality to change the audio stream from Unity.
	- Fixed error on the video render freeze that happened everytime the button "captionSettings" was clicked.
	- Changed the UI to show and hide multiple audios and captions using the button "captionSettings".

### Version 1.1.0.00

- [Android]
     - [Add] Compatibility with the 64- bit architecture.

- [Windows]
	- [Add] Support SPU (Sub- Picture Unit) subtitle content.
	- [Add] Implemented full HD video playback.

- [Unity]
	- [Fix] Modify the OnApplicationFocus function to not have conflicts with the Run In Background property.

### Version 1.0.8.00

- [Windows]
	- [Add] Webvtt Support

- [Android]
	- [Add] Webvtt Support

- [iOS]
	- [Add] Webvtt Support
	- [Fix] Set the maximum characters of subtitles Webvtt to 10000.
	- [Fix] Fix error that was making the video to freeze the image everytime a large subtitle was loaded.

### Version 1.0.7.01

- [Android]
	- [Fix] Changing render mode to current render mode no longer loads the mode again. 

- [Unity]
	- [Fix] Maximize screen set to correct aspects. 
	- [Fix] Seeking no longer overwrites the current player state. 
	- [Fix] Player now supporst subtitles with over 2052 characters. 
	- [Change] Ui prefab updated to work better with the Closed Caption functionalities. 
	- [Change] Closed Caption option no longer show up on unsupported platforms. 

### Version 1.0.7.00

- [Unity]
	- [Fix] Setting volume no longer overwrites muting. 
	- [Fix] Restarting video or looping no longer overwrites current audio settings. 
	- [Fix] Running application in backround pauses video and starts it again when the focus is returned. 
	- [Fix] White flash before starting streams on RenderTexture.
	- [Fix] Autoplay function fixed. 
	- [Change] NexPlayer.cs Moved EnableUIComponent function from Awake() to Start() in order to give the player sufficient time to  initialize before setting up the UI. 
	- [Change] Updated the Integration guide pdf. 

- [Android/iOS]
	- [Add] Closed Caption API applied.
	- [Add] Transparency sample scene. 

- [Windows]
	- [Fix]Adding multiple renderers in the scene "NexPlayer_PlaybackMultipleRenderer_Sample" fixed. 

- [Android]
	- [Fix]Player storing and showing last frame of video between scenes using Asset streaming fixed.
	- [Fix] Changing renderers no longer shows override material when it shouldn't 
	- [Fix] Screen size text show correct aspects. 

### Version 1.0.6.34

- [Unity]
	- [Change] Wrapped up NexPlayerBase.cs to neamespace NexPlayerBaseAPI
	- [Fix] Fixed an issue that is not able to copy licensefile on Windows platform.
	- [Add] Added an interface that get ContentInfo and Content Statistic Info.

- [Android]
	- [Fix] Fixed an out of memory

### Version 1.0.5.33

- [iOS/Windows]
	- [Change] Move plug- in folders to Plugins/($Platform)/NexPlayer from Plugins/($Platform) except for Android.

- [Windows]
	- [Change] Use Timelock on Windows SDK

### Version 1.0.4.32

- [Unity]
	- [Change] Changed to default setting that ABR is on and Buffering Time is 2000ms

- [Android]
	- [Fix] Reduced TimeOut to waiting update before releasing at WVCDM.
	- [Fix] Fixed an crash issue where it could be happen when player attempt to get null event queue.
	- [Fix] Fixed an issue where player is crashed when player is repeated to close and open in short time.

- [Windows]
	- Fixed an issue where dll files is not set correctly on 64bit windows

### Version 1.0.3.31

- [Add] Implemented AES Callback Function
- [Add] Search license file in streaming assets directory when there is no license file in persistent datapath
- [Change] Moved plugin Library from Plugin/NexPlayer to Plugin
- [Fix] fixed that not initialized UI when calling stop api

### Version 1.0.2.30

- [Fix] Fixed unity package build script.
- [Fix] Fixed an issue where HttpAdditionalHeader api is not worked on Windows.
- [Fix] Fixed an issue where TimedMetadata Tag bug.

### Version 1.0.1.29

- [Change] Moved Library and streamingAsset to Plugins/NexPlayer and streamingAsset/NexPlayer directory.

### Version 1.0.1.28

- [Add] Implemented new PlaybackSetting scene which can modify player settings
- [Add] Add close api to support playback new video
- [Fix] Fixed an issue that windowEditor's EndOfContent callback not work 

### Version 1.0.1.27

- [Add] Implemented TimedMetaData Interface.
- [Fix] Fixed an issue where SetTextureColor api is not worked on Windows platform.
- [Fix] Fixed an issue where the display screen shot is shown before content is play.
- [Fix] Fixed an issue where player is crashed when stop or resume.
		
### Version 1.0.1.26

- Implemented nexplayer library on windows platform
- Changed APIs return value type to NexError Code
- Fixed minor bugs
- Improved UI Icons intuitively
		
### Version 1.0.1.25

- Support an feature that change render mode or add texture while content is open.
- Fixed an issue where video size is change according to main camera's move
- Improved Additional Http Headers and DRM Optional Header input fields.
- Fixed minor bugs.

### Version 1.0.1.24

- [Add] Support 360 Video module and sample.

### Version 1.0.1.23

- [Update]Update SetTextureColor api is able to variable colors before content is open.
- [Add] Add a new scene to NexPlayer Unity Sample, "VideoSpread Render Texture" spread multiple texture with Render texture mode.
- [Add] Add Transparency Custom Shader and Material for Material rendering mode.
- [Add] Add a interface for setting HTTP additional Headers.

### Version 1.0.1.22

- [Unity]
	- [Fix] Fixed the bug that render mode is not set then player is not working properly.
	- [Fix] Fixed the bug that video size is changed according to Camera postion and rotation.
	- [Add] Implement Event LOADSTART before content is open.
	- [Change] Change Sample UI Design.
	- [Change] Change Default Setting for Player prefs.

- [Android] 
	- [Add] Support renewal license for SW widevine.
	- [Add] Support HLS ABR(Adaptive Bit Rate)

- [iOS]
	- [Add] support HLS ABR(Adaptive Bit Rate)

### Version 1.0.1.21

- [Android]
	- [Fix]fixed the issue that texture is flickering before content is open.
	- [Add] support build option "split application binary"

- [iOS]
	- [Fix] support that setProperty api with NexPlayerProperty in NexPlayerTypes.cs.
	- [Fix] support setting buffering Time for initial buffering and rebuffering duration.

### Version 1.0.1.20

- [Unity]
	- support that fill color what you want to the texture before content is open.
	- support setting log level for debugging api.

- [Android]
	- [Fix] Support that setProperty api with NexPlayerProperty in NexPlayerTypes.cs.
	- [Fix] Support setting buffering Time for initial buffering and rebuffering duration.
		

### Version 1.0.1.19

- [Unity]
	- [Fix] Fixed NexSeekBar issue that SeekBar is updated during Seeking			
- [Android]
	- [Fix] Fixed a issue that Nexplayers would behave differently by pressing the home key or power key of some Android devices		

- [Windows]
	-  [Fix] fixed a issue that player state is  paused when Windows Editor is Stop after paused

### Version 1.0.1.18

- [Unity]
	- [Fix] Fixed the bug when url is empty.
	- [Fix] Fixed the build issue about il2cpp setting
 	- [Update] updated integration document.	
- [Android]
	- [Fix] Fixed the issue that display transparent background.
	- [Fix] fixed the problem when video reaches end of content and loop is off, can't play again when push play button

- [iOS]
	- Fixed the problem when video reaches end of content and loop is off, can't play again when push play button

- [Windows]
	- Fixed the issue that caption is not displayed properly when player is looped in Windows Editor.

### Version 1.0.1.17

- [iOS]
	- [Fix] Fixed the issue with crash on certain devices and operating systems

- [Android]
	- [Fix] Fixed the issue with not updated the seekbar point and current time ui when player was paused 
	- [Fix] Fixed the issue with invisible texture when setActive false to true

### Version 1.0.1.16

- [Unity]
	- [Fix]Fixed the issue that caption is disappeared when toggle caption switch off to on

- [Android/iOS]
	- [Add] Add setting Request TimeOut API, default value is 30sec
	  [Fix] Fixed the issue that video flickering when player is enabled and disabled

### Version 1.0.1.15

- [Unity] 
	- [Fix] Add Exception Null check
	- [Fix] Fixed UI (Mute, Volume, Loop) interference Problem
	- [Change] Change error code format to "error + error code"

- [Windows]  
	- [Fix] Fixed the issue that player is freeze when player is swap in Windows Editor

- [Android]
	- [Fix] Fixed problem that DRM Session is not released when the player is closed with DRM Inited 

- [iOS]
	- [Fix] Fixed the bug that go to background and return to foreground in iPhone Player
	- [Change] Remove iOS Popup Dialog when encountered error

### Version 1.0.1.14

- [Fix] Fix bug about Image Rotation
- [Add] Add DRM Cache On/Off to NexPlayer inspector.
- [Change] When return to foreground or player active, player status is according to prev status

### Version 1.0.1.13

- [Change] Change NexPlayer Object Active/Deactive scenario
	- Before : Close - Open Player
	- Current : Pause - Resume
- [Change] Change Initial scale factor to 'Fit To Screen' in Windwos Editor
- [Add] Loop UI
- [Add] Image Rotation UI(only support rawImage)
- [Add] license cache feature for widevine Contents
- [Fix] UI Bugs
- [Fix] Player Bugs

### Version 1.0.1.12

- [Fix] Fix memory leak

### Version 1.0.1.11

- [Add] Add Stop Button UI
- [Add] Syncronize UI and Inspector
- [Improvement] Improve Scaling API, PC and iOS
- [Fix] Fix subtitle toggle state when NexPlayer is active or inactive

### Version 1.0.1.10

- [Improvement] Navigate StreamingAsset sub directorys

### Version 1.0.1.9

- [Add] Subtitle Parser(only support SRT) in Windows PC Editor
- [Add] Scaling API for Windows PC Editor And iOS
- [Improvement] Fix Crash Issues

### Version 1.0.1.8

- [Add] Player Object Acive routine
- [Improvement] Fix Crash Issues

### Version 1.0.1.7

- [Fix] Unify Thumbnail display action 
- [Add] Add Mute,Volume UI and api
- [Improvement] Fix Crash Issues

### Version 1.0.1.6

- [Add] Support SetMute,SetVolumes,AutoPlay,Loop for Unity WindowsEditprPlayer
- [Add] Add Caption Area for Render Texture Sample/ Material override Sample
- [Fix] Fix UI Inspector Bugs

### Version 1.0.1.5

- [Improvement] Stabilization work on Windows Unity PC Editor
- [Add] Added import guide video for Unity PC Editor
- [Change] Change Package structure

### Version 1.0.1.4

- [Add] Widevine support for DASH on Android , iOS
- [Add] Added Scale Feature on Android

### Version 1.0.1.3
- [Add] Added Windows Standalone and Windows Editor support

### Version 1.0.1.2

- [Improvement] Various improvements in Android and iOS
- [Add] Added localPlayback mode on Android and iOS

### Version 1.0.1.1

- [Add] Initial release of the NexPlayer SDK for Unity
