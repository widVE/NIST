using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using NexUtility;
using NexPlayerAPI;
using System;

namespace NexPlayerSample
{
    [CustomEditor(typeof(NexPlayer))]
    public class NexPlayerEditor : Editor
    {
        SerializedProperty _renderModeProp;
        SerializedProperty _streamingUriProp;
        SerializedProperty _isLiveStreamProp;
        SerializedProperty _assetUriProp;
        SerializedProperty _assetFileIndexProp;
        SerializedProperty _localUriProp;
        SerializedProperty _subtitlePathProp;
        SerializedProperty _streamingSubtitleUriProp;
        SerializedProperty _assetSubtitleUriProp;
        SerializedProperty _localSubtitleUriProp;
        SerializedProperty _subtitleFileIndexProp;
        SerializedProperty _autoPlayProp;
        SerializedProperty _loopPlayProp;
        SerializedProperty _mutePlayProp;
        SerializedProperty _volumeProp;
        SerializedProperty _volumeSliderProp;

        //SerializedProperty _contentMeta;

        SerializedProperty _userLicenseKeyProp;

        SerializedProperty _debugLogsProp;

        SerializedProperty _playPauseImageProp;
        SerializedProperty _totTimeProp;
        SerializedProperty _currentTimeProp;
        SerializedProperty _videoSizeProp;
        SerializedProperty _statusProp;
        SerializedProperty _seekbarProp;
        SerializedProperty _pausespriteProp;
        SerializedProperty _playspriteProp;

        SerializedProperty _streamingTypeProp;

        SerializedProperty _renderTextureProp;
        SerializedProperty _renderTextureRendererProp;
        SerializedProperty _captionTextProp;
        SerializedProperty _captionToggleProp;

        SerializedProperty _rendererProp;
        SerializedProperty _rawImageProp;

		SerializedProperty _asyncInitEventProp;
        SerializedProperty _registerAESKeyCallbackProp;
        [Obsolete("Override NexPlayer.Behaviour.NexPlayerEvent.NEXPLAYER_EVENT_INIT_COMPLETE instead")]
        SerializedProperty _readyToPlayEventProp;
        [Obsolete("Override NexPlayer.Behaviour.NexPlayerEvent.NEXPLAYER_EVENT_PLAYBACK_STARTED instead")]
        SerializedProperty _startToPlayEventProp;
        [Obsolete("Override NexPlayer.Behaviour.NexPlayerEvent.NEXPLAYER_EVENT_END_OF_CONTENT instead")]
        SerializedProperty _endOfPlayEventProp;

        SerializedProperty _muteImageProp;
        SerializedProperty _volumeImageProp;
        SerializedProperty _volumeOnSpriteProp;
        SerializedProperty _volumeOffSpriteProp;

        SerializedProperty _loopImageProp;
        SerializedProperty _loopOnSpriteProp;
        SerializedProperty _loopOffSpriteProp;
        //SerializedProperty _drmCacheProp;   NOT USED
        SerializedProperty _drmKeyServerURIProp;
        SerializedProperty _drmLicenseRequestTimeoutProp;

        SerializedProperty _drmHeaderKeysProp;
        SerializedProperty _drmHeaderValuesProp;
        SerializedProperty _wvHeaderSizeProp;
        SerializedProperty _httpHeaderKeysProp;
        SerializedProperty _httpHeaderValuesProp;
        SerializedProperty _httpHeaderSizeProp;

        SerializedProperty _numberOfStreamsProp;
        SerializedProperty _multiRawImagesProp;
        SerializedProperty _multiRenderTexturesProp;
        SerializedProperty _multiKeyServerProp;
        SerializedProperty _multiURLPathsProp;
        SerializedProperty _multiSubTextsProp;

        SerializedProperty _debuggingLogLevelProp;
        SerializedProperty _bufferingTimeProp;
        SerializedProperty _maxCaptionLengthProp;
        SerializedProperty _enableTrackDownProp;
        SerializedProperty _minRenderedFramesPercentageProp;
        //SerializedProperty _supportABRProp;  ALWAYS ENABLED
        SerializedProperty _runInBackgroundProp;
        //SerializedProperty _customTagsProp;    NOT USED
        SerializedProperty _BufferSubtitlesProp;
        SerializedProperty _SynchronizationEnableProp;
        SerializedProperty _DelayTimeProp;
        SerializedProperty _SpeedUpSyncTimeProp;
        SerializedProperty _JumpSyncTimeProp;

        SerializedProperty _expiredMessageProp;

        private string[] _availablePlatforms;
        private FontStyle _cachedFontStyle;
        private Color _cachedTextColor;
        private Texture2D NxPLogo;

        private int prePlaybackIndex = -1;
        private string SDKVersion;
        private bool isMobile;
        private static bool _showHTTPHeaders = false;
        private static bool _showDRM = false;
        private static bool _showDebug = false;
        private static bool _showAdvancedProperties = false;
        private static bool _showUserLicenseKey = true;
        private static bool _showSubtitles = false;
        private static bool _showUI = false;
        private static bool _showMultiStream = false;
        private static int multiStreamNumber = 0;
        private bool usingMulti{ get{ return multiStreamNumber > 0; } }
        private static bool _showEventsListeners = false;
        private static bool _showLocalDRM = false;

        private const string SettingsPrefix = "NexPlayer-Editor-";
        private const int MaxRecentFiles = 4;
        private static List<string> _recentFiles = new List<string>(MaxRecentFiles);

        string style;

        private void OnEnable()
        {
            style = getStyle();
            LoadEditorState();
            _renderModeProp = serializedObject.FindProperty("startingRenderMode");
            _autoPlayProp = serializedObject.FindProperty(nameof(NexPlayerBehaviour.autoPlay));
            _mutePlayProp = serializedObject.FindProperty(nameof(NexPlayerBehaviour.mutePlay));
            _volumeProp = serializedObject.FindProperty(nameof(NexPlayerBehaviour.volume));
            _userLicenseKeyProp = serializedObject.FindProperty("userWebGLLicenseKey");
            _debugLogsProp = serializedObject.FindProperty(nameof(NexPlayerBehaviour.debugLogs));
            _streamingUriProp = serializedObject.FindProperty("streamURI");
            _isLiveStreamProp = serializedObject.FindProperty(nameof(NexPlayer.isLiveStream));
            _assetUriProp = serializedObject.FindProperty("assetURI");
            _localUriProp = serializedObject.FindProperty("localURI");

            _assetFileIndexProp = serializedObject.FindProperty(nameof(NexPlayer.assetFileIndex));
            _subtitlePathProp = serializedObject.FindProperty(nameof(NexPlayer.subtitleURL));
            _assetSubtitleUriProp = serializedObject.FindProperty(nameof(NexPlayer.assetSubtitleUri));
            _localSubtitleUriProp = serializedObject.FindProperty(nameof(NexPlayer.localSubtitleUri));
            _streamingSubtitleUriProp = serializedObject.FindProperty(nameof(NexPlayer.streamingSubtitleUri));
            _subtitleFileIndexProp = serializedObject.FindProperty(nameof(NexPlayer.subtitleFileIndex));
            _loopPlayProp = serializedObject.FindProperty(nameof(NexPlayer.loop));
            _playPauseImageProp = serializedObject.FindProperty(nameof(NexPlayer.playPauseImage));
            _totTimeProp = serializedObject.FindProperty(nameof(NexPlayer.totTime));
            _currentTimeProp = serializedObject.FindProperty(nameof(NexPlayer.currentTime));
            _videoSizeProp = serializedObject.FindProperty(nameof(NexPlayer.videoSize));
            _statusProp = serializedObject.FindProperty(nameof(NexPlayer.status));
            _seekbarProp = serializedObject.FindProperty(nameof(NexPlayer.seekBar));
            _pausespriteProp = serializedObject.FindProperty(nameof(NexPlayer.pauseSprite));
            _playspriteProp = serializedObject.FindProperty(nameof(NexPlayer.playSprite));
            _streamingTypeProp = serializedObject.FindProperty(nameof(NexPlayer.playType));
            _renderTextureProp = serializedObject.FindProperty(nameof(NexPlayer.renderTexture));
            _renderTextureRendererProp = serializedObject.FindProperty(nameof(NexPlayer.renderTextureRenderer));
            _rendererProp = serializedObject.FindProperty(nameof(NexPlayer.renderTarget));
            _rawImageProp = serializedObject.FindProperty(nameof(NexPlayer.rawImage));
            _asyncInitEventProp = serializedObject.FindProperty(nameof(NexPlayer.asyncInitEvent));
            _expiredMessageProp = serializedObject.FindProperty(nameof(NexPlayer.expiredMessage));
            _registerAESKeyCallbackProp = serializedObject.FindProperty(nameof(NexPlayer.RegisterAESKeyCallback));
#pragma warning disable CS0618 // Type or member is obsolete
            _readyToPlayEventProp = serializedObject.FindProperty(nameof(NexPlayer.readyToPlayEvent));
            _startToPlayEventProp = serializedObject.FindProperty(nameof(NexPlayer.startToPlayEvent));
            _endOfPlayEventProp = serializedObject.FindProperty(nameof(NexPlayer.endOfPlayEvent));
#pragma warning restore CS0618 // Type or member is obsolete
            _captionTextProp = serializedObject.FindProperty(nameof(NexPlayer.caption));
            _captionToggleProp = serializedObject.FindProperty(nameof(NexPlayer.captionToggle));
            _volumeSliderProp = serializedObject.FindProperty(nameof(NexPlayer.volumeSlider));
            _muteImageProp = serializedObject.FindProperty(nameof(NexPlayer.muteButtonImage));
            _volumeImageProp = serializedObject.FindProperty(nameof(NexPlayer.volumeButtonImage));
            _volumeOnSpriteProp = serializedObject.FindProperty(nameof(NexPlayer._volumeOnSprite));
            _volumeOffSpriteProp = serializedObject.FindProperty(nameof(NexPlayer._volumeOffSprite));

            _loopImageProp = serializedObject.FindProperty(nameof(NexPlayer.loopButtonImage));
            _loopOnSpriteProp = serializedObject.FindProperty(nameof(NexPlayer._loopOnSprite));
            _loopOffSpriteProp = serializedObject.FindProperty(nameof(NexPlayer._loopOffSprite));
            //_drmCacheProp                 =   serializedObject.FindProperty("drmCache");
            _drmKeyServerURIProp = serializedObject.FindProperty(nameof(NexPlayer.keyServerURI));
            _drmLicenseRequestTimeoutProp = serializedObject.FindProperty(nameof(NexPlayer.licenseRequestTimeout));
            //_contentMeta = serializedObject.FindProperty(nameof(NexPlayer.wvdrmMetadata));


            _drmHeaderKeysProp = serializedObject.FindProperty("widevineHeaderKeys");
            _drmHeaderValuesProp = serializedObject.FindProperty("widevineHeaderValues");
            _wvHeaderSizeProp = serializedObject.FindProperty(nameof(NexPlayer.wvHeaderSize));
            _httpHeaderKeysProp = serializedObject.FindProperty(nameof(NexPlayer.httpHeaderKeys));
            _httpHeaderValuesProp = serializedObject.FindProperty(nameof(NexPlayer.httpHeaderValues));
            _httpHeaderSizeProp = serializedObject.FindProperty(nameof(NexPlayer.httpHeaderSize));

            _numberOfStreamsProp = serializedObject.FindProperty(nameof(NexPlayer.numberOfStreams));
            _multiRawImagesProp = serializedObject.FindProperty(nameof(NexPlayer.multiRawImages));
            _multiRenderTexturesProp = serializedObject.FindProperty(nameof(NexPlayer.multiRenderTextures));
            _multiKeyServerProp = serializedObject.FindProperty(nameof(NexPlayer.multiKeyServerURL));
            _multiURLPathsProp = serializedObject.FindProperty(nameof(NexPlayer.multiURLPaths));
            _multiSubTextsProp = serializedObject.FindProperty(nameof(NexPlayer.multiSubTexts));

            _bufferingTimeProp = serializedObject.FindProperty(nameof(NexPlayer.bufferingTime));
            _maxCaptionLengthProp = serializedObject.FindProperty(nameof(NexPlayer.maxCaptionLength));
            _enableTrackDownProp = serializedObject.FindProperty(nameof(NexPlayer.enableTrackDown));
            _minRenderedFramesPercentageProp = serializedObject.FindProperty(nameof(NexPlayer.minRenderedFramesPercentage));
            _runInBackgroundProp = serializedObject.FindProperty(nameof(NexPlayer.runInBackground));
            _BufferSubtitlesProp = serializedObject.FindProperty(nameof(NexPlayer.BufferSubtitles));
            _SynchronizationEnableProp = serializedObject.FindProperty(nameof(NexPlayer.SynchronizationEnable));
            _DelayTimeProp = serializedObject.FindProperty(nameof(NexPlayer.DelayTime));
            _SpeedUpSyncTimeProp = serializedObject.FindProperty(nameof(NexPlayer.SpeedUpSyncTime));
            _JumpSyncTimeProp = serializedObject.FindProperty(nameof(NexPlayer.JumpSyncTime));

            NxPLogo = Resources.Load<Texture2D>("NexPlayerLogo/NexPlayerLogo");
            SDKVersion = "NexPlayer version: " +
                         (int)NexPlayerUnityVersion.MAJOR_VERSION + "." +
                         (int)NexPlayerUnityVersion.MINOR_VERSION + "." +
                         (int)NexPlayerUnityVersion.PATCH_VERSION + "." +
                         (int)NexPlayerUnityVersion.BUILD_NUMBER;
        }

        public override void OnInspectorGUI()
        {
#if UNITY_ANDROID || UNITY_IOS
            isMobile = true;
#else
            isMobile = false;
#endif
            if (usingMulti && (_renderModeProp.enumValueIndex != 1 && _renderModeProp.enumValueIndex != 3))
                _renderModeProp.enumValueIndex = 3;


            _cachedFontStyle = EditorStyles.label.fontStyle;
            _cachedTextColor = EditorStyles.textField.normal.textColor;

            DrawBoldLabel(SDKVersion);

            EditorGUILayout.Space();
            DrawMediaSource();

            EditorGUILayout.Space();
            DrawMediaOutput();

            EditorGUILayout.Space();
            DrawPlaybackProperties();

            EditorGUILayout.Space();
            DrawUserLicenseKey();

            EditorGUILayout.Space();
            DrawDebug();

            EditorGUILayout.Space();
            DrawAdvancedProperties();

            EditorGUILayout.Space();
            DrawUIReferences();

            EditorGUILayout.Space();
            DrawMultiStreamProperties();

            EditorGUILayout.Space();
            DrawEvents();
            EditorGUILayout.Space();
            DrawLocalDRM();
            serializedObject.ApplyModifiedProperties();
        }
        private void OnDisable()
        {
            SaveEditorState();
        }

        public void SetMultiStreamNumber(int number)
        {
            number = Mathf.Clamp(number, 2, 8);
            multiStreamNumber = number;
        }

#region MEDIA SOURCE
        private void DrawMediaSource()
        {
            EditorGUILayout.BeginVertical(style);
            CreateTitle("MEDIA SOURCE");

            if (!usingMulti)
            {
                _availablePlatforms = new string[] { "StreamingPlay", "AssetPlay", "LocalPlay" };
                _streamingTypeProp.intValue = GUILayout.SelectionGrid(_streamingTypeProp.intValue, _availablePlatforms, _availablePlatforms.Length, EditorStyles.miniButton);
                _streamingTypeProp.intValue = Mathf.Clamp(_streamingTypeProp.intValue, 0, _availablePlatforms.Length - 1);
            }
            else 
            {
                _availablePlatforms = new string[] { "StreamingPlay" };
                _streamingTypeProp.intValue = GUILayout.SelectionGrid(_streamingTypeProp.intValue, _availablePlatforms, _availablePlatforms.Length, EditorStyles.miniButton);
                _streamingTypeProp.intValue = Mathf.Clamp(_streamingTypeProp.intValue, 0, _availablePlatforms.Length - 1);
                EditorGUILayout.HelpBox("Number of streams is greater than 0 so Multistream settings are used instead.", MessageType.Info);
                EditorGUILayout.Space();
            }
            EditorGUI.BeginDisabledGroup(usingMulti);
            DrawMediaPath();
#if  UNITY_ANDROID || UNITY_IOS
            DrawAdditionalHTTPHeaders();
#endif
            DrawDigitalRightsManagement();
            EditorGUI.EndDisabledGroup();

            GUILayout.EndVertical();
        }
        void DrawMediaPath()
        {
            // Streaming Video Path field
            if (_streamingTypeProp.intValue == 0)
            {
                if (_streamingTypeProp.intValue != prePlaybackIndex)
                {
                    GUI.FocusControl(null);
                }
                prePlaybackIndex = _streamingTypeProp.intValue;
                EditorGUILayout.Space();
                DrawBoldLabel("URL : ");
                EditorStyles.textField.wordWrap = true;

                _streamingUriProp.stringValue = EditorGUILayout.TextField(_streamingUriProp.stringValue, GUILayout.Height(30));
                EditorStyles.textField.wordWrap = false;

                if (string.IsNullOrEmpty(_streamingUriProp.stringValue))
                {
                    EditorGUILayout.HelpBox("No path for content, please specify one", MessageType.Error);
                }

#if UNITY_EDITOR_OSX
                if (_streamingUriProp.stringValue.Contains(".mpd"))
                    EditorGUILayout.HelpBox(" DASH content will NOT run on MacOSX.\n DASH content DOES run on IOS DEVICES", MessageType.Warning);
#endif
                EditorGUILayout.PropertyField(_isLiveStreamProp);
            }

            //Asset Video Path field
            else if (_streamingTypeProp.intValue == 1)
            {
                EditorGUILayout.Space();
                DrawBoldLabel("Asset video file : ");
                EditorStyles.textField.wordWrap = true;

                string[] filesArray = StreamingAssetFileHelper.GetAssetVideoFiles();
                if (filesArray == null)
                {
                    EditorGUILayout.HelpBox("To use this option, please create a StreamingAssets folder and add video assets.", MessageType.Info);
                }
                else
                {

                    _assetFileIndexProp.intValue = EditorGUILayout.Popup(_assetFileIndexProp.intValue, filesArray);
                    if (_assetFileIndexProp.intValue < filesArray.Length)
                        _assetUriProp.stringValue = filesArray[_assetFileIndexProp.intValue];
                    else
                        _assetUriProp.stringValue = filesArray[0];
                }
            }

            // Local Video Path field
            else if (_streamingTypeProp.intValue == 2)
            {
                if (_streamingTypeProp.intValue != prePlaybackIndex)
                {
                    GUI.FocusControl(null);
                }
                prePlaybackIndex = _streamingTypeProp.intValue;

                EditorGUILayout.Space();
                DrawBoldLabel("Mobile local video file name : ");
                EditorStyles.textField.wordWrap = true;

                _localUriProp.stringValue = EditorGUILayout.TextField(_localUriProp.stringValue, GUILayout.Height(30));
                EditorStyles.textField.wordWrap = false;

                if (string.IsNullOrEmpty(_localUriProp.stringValue))
                {
                    EditorGUILayout.HelpBox("No path for content, please specify one", MessageType.Error);
                }
            }
        }
        void DrawAdditionalHTTPHeaders()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space();

            EditorStyles.foldout.fontStyle = FontStyle.Bold;
            _showHTTPHeaders = EditorGUILayout.Foldout(_showHTTPHeaders, "Additional Http Headers", true);
            EditorStyles.foldout.fontStyle = _cachedFontStyle;

            if (_showHTTPHeaders)
            {
                GUILayout.BeginVertical("Box");

                CreateArraySizeController("Number of Additional Headers:", _httpHeaderSizeProp);
                _httpHeaderKeysProp.arraySize = _httpHeaderSizeProp.intValue;
                _httpHeaderValuesProp.arraySize = _httpHeaderSizeProp.intValue;

                if (_httpHeaderSizeProp.intValue > 0)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Header Key", GUILayout.MaxWidth(100));
                    EditorGUILayout.LabelField("Header value", GUILayout.MaxWidth(100));
                    GUILayout.EndHorizontal();

                    for (int i = 0; i < _httpHeaderSizeProp.intValue; i++)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(_httpHeaderKeysProp.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.MaxWidth(100));
                        EditorGUILayout.PropertyField(_httpHeaderValuesProp.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.MaxWidth(100));
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();
            }
            EditorGUI.indentLevel--;
        }
        void DrawDigitalRightsManagement()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space();
            if (!isMobile)
            {
                EditorGUILayout.HelpBox("DRM only supported on Android and IOS devices.", MessageType.Info);
                EditorGUILayout.Space();
            }
            EditorGUI.BeginDisabledGroup(!isMobile);
            EditorStyles.foldout.fontStyle = FontStyle.Bold;
            _showDRM = EditorGUILayout.Foldout(_showDRM, "DRM (digital rights management)", true);
            EditorStyles.foldout.fontStyle = _cachedFontStyle;

            if (_showDRM)
            {
                DrawKeyServerPath();
                DrawLicenseRequestTime();

                DrawDRMHeaders();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }
        void DrawKeyServerPath()
        {
            EditorGUILayout.Space();
            DrawBoldLabel("KeyServer URL");
            EditorStyles.textField.wordWrap = true;
            _drmKeyServerURIProp.stringValue = EditorGUILayout.TextField(_drmKeyServerURIProp.stringValue, GUILayout.Height(30));
            EditorStyles.textField.wordWrap = false;
        }
        void DrawLicenseRequestTime()
        {
            EditorGUILayout.Space();
            DrawBoldLabel("LicenseRequestTimeout(sec)");
            EditorStyles.textField.wordWrap = true;
            _drmLicenseRequestTimeoutProp.intValue = EditorGUILayout.IntField(_drmLicenseRequestTimeoutProp.intValue, GUILayout.Height(30));
            EditorStyles.textField.wordWrap = false;
        }
        void DrawDRMHeaders()
        {
            EditorGUILayout.Space();
            DrawBoldLabel("Widevine Optional DRM Headers");

            GUILayout.BeginVertical("Box");

            CreateArraySizeController("Number of Optional Headers:", _wvHeaderSizeProp);
            _drmHeaderKeysProp.arraySize = _wvHeaderSizeProp.intValue;
            _drmHeaderValuesProp.arraySize = _wvHeaderSizeProp.intValue;

            if (_wvHeaderSizeProp.intValue > 0)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Header Key", GUILayout.MaxWidth(100));
                EditorGUILayout.LabelField("Header value", GUILayout.MaxWidth(100));
                GUILayout.EndHorizontal();

                for (int i = 0; i < _wvHeaderSizeProp.intValue; i++)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(_drmHeaderKeysProp.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.MaxWidth(100));
                    EditorGUILayout.PropertyField(_drmHeaderValuesProp.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.MaxWidth(100));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
        }
#endregion

        private void DrawMediaOutput()
        {
            EditorGUILayout.BeginVertical(style);
            CreateTitle("MEDIA OUTPUT");
            if (usingMulti)
            {
                EditorGUILayout.HelpBox("Number of streams is greater than 0 so Multistream settings are used instead.", MessageType.Info);
                EditorGUILayout.Space();
            }
            EditorGUI.BeginDisabledGroup(usingMulti);

            EditorGUILayout.BeginHorizontal();
            EditorStyles.label.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("Render Mode", GUILayout.MaxWidth(100));
            EditorStyles.label.fontStyle = _cachedFontStyle;

            int[] modeOptions = { 3, 1, 2 };
            string[] modeOptionsText = { "RawImage", "RenderTexture", "Material Override" };
            _renderModeProp.enumValueIndex = EditorGUILayout.IntPopup(_renderModeProp.enumValueIndex, modeOptionsText, modeOptions);
            EditorGUILayout.EndHorizontal();

            if (_renderModeProp.enumValueIndex == 0)
            {
                EditorGUILayout.HelpBox("Please select a render mode", MessageType.Error);
            }
            else
            {
                switch (_renderModeProp.enumValueIndex)
                {
                    case 1:
                        EditorGUILayout.PropertyField(_renderTextureProp, new GUIContent("Target RenderTexture"), true);
                        EditorGUILayout.PropertyField(_renderTextureRendererProp, new GUIContent("renderTextureRenderer"), true);
                        break;

                    case 2:
                        EditorGUILayout.PropertyField(_rendererProp, new GUIContent("Target Renderer"), true);
                        break;

                    case 3:
                        EditorGUILayout.PropertyField(_rawImageProp, new GUIContent("Target RawImage"), true);
                        break;

                    case 4:
                        EditorGUILayout.PropertyField(_rendererProp, new GUIContent("Target Renderer"), true);
                        break;
                }
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.EndVertical();
        }

#region PLAYBACK PROPERTIES
        private void DrawPlaybackProperties()
        {
            EditorGUILayout.BeginVertical(style);
            CreateTitle("PLAYBACK PROPERTIES");

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AutoPlay:", GUILayout.MaxWidth(120));
            _autoPlayProp.boolValue = EditorGUILayout.Toggle(_autoPlayProp.boolValue);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Loop:", GUILayout.MaxWidth(120));
            _loopPlayProp.boolValue = EditorGUILayout.Toggle(_loopPlayProp.boolValue);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("RunInBackground:", GUILayout.MaxWidth(120));
            _runInBackgroundProp.boolValue = EditorGUILayout.Toggle(_runInBackgroundProp.boolValue);
            GUILayout.EndHorizontal();

            DrawVolume();

            GUILayout.EndVertical();
        }
        void DrawVolume()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MutePlay:", GUILayout.MaxWidth(120));
            _mutePlayProp.boolValue = EditorGUILayout.Toggle(_mutePlayProp.boolValue);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Volume", GUILayout.MaxWidth(120));
            //if (GUILayout.Button("Reset", EditorStyles.miniButton, GUILayout.MaxWidth(70)))
            //{
            //    _volumeProp.intValue = 50;
            //}
            _volumeProp.floatValue = EditorGUILayout.Slider(_volumeProp.floatValue, 0, 1);
            GUILayout.EndHorizontal();
        }
        #endregion

        private void DrawUserLicenseKey()
        {
            EditorGUILayout.BeginVertical(style);
            CreateToggleHEader("USER WEBGL LICENSE KEY", ref _showUserLicenseKey);

            if (_showUserLicenseKey)

            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("License WebGL Key:", GUILayout.MaxWidth(120));
                EditorStyles.textField.wordWrap = true;
                _userLicenseKeyProp.stringValue = EditorGUILayout.TextField(_userLicenseKeyProp.stringValue, GUILayout.Height(30));
                EditorStyles.textField.wordWrap = false;
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        private void DrawDebug()
        {
            EditorGUILayout.BeginVertical(style);
            CreateToggleHEader("DEBUG", ref _showDebug);

            if (_showDebug)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Enable debug logs:", GUILayout.MaxWidth(120));
                _debugLogsProp.boolValue = EditorGUILayout.Toggle(_debugLogsProp.boolValue);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        private void DrawAdvancedProperties()
        {
            EditorGUILayout.BeginVertical(style);

            CreateToggleHEader("ADVANCED PROPERTIES", ref _showAdvancedProperties);

            if (_showAdvancedProperties)
            {
                if (!isMobile)
                {
                    EditorGUILayout.HelpBox("Advance properties only supported on Android and IOS devices.", MessageType.Info);
                }
                EditorGUI.BeginDisabledGroup(!isMobile);
                EditorGUILayout.Space();
                // Buffering Time
                EditorGUILayout.PropertyField(_bufferingTimeProp);



                // Trackdown
                EditorGUI.BeginDisabledGroup(_SynchronizationEnableProp.boolValue);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_enableTrackDownProp);
                if (_enableTrackDownProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_minRenderedFramesPercentageProp);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.EndDisabledGroup();


                // Synchronization
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_SynchronizationEnableProp);
                if (_SynchronizationEnableProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_DelayTimeProp);
                    EditorGUILayout.PropertyField(_SpeedUpSyncTimeProp);
                    EditorGUILayout.PropertyField(_JumpSyncTimeProp);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndVertical();

        }

#region UI
        private void DrawUIReferences()
        {
            EditorGUILayout.BeginVertical(style);
            CreateToggleHEader("UI REFERENCES", ref _showUI);

            if (_showUI)
            {
                EditorGUILayout.Space();

                DrawBoldLabel("Play pause");
                EditorGUILayout.PropertyField(_playPauseImageProp, new GUIContent("playPauseImage"), true);
                EditorGUILayout.PropertyField(_pausespriteProp, new GUIContent("pauseSprite"), true);
                EditorGUILayout.PropertyField(_playspriteProp, new GUIContent("playSprite"), true);
                DrawBoldLabel("Loop");
                EditorGUILayout.PropertyField(_loopImageProp, new GUIContent("loopButtonImage"), true);
                EditorGUILayout.PropertyField(_loopOnSpriteProp, new GUIContent("loopOnSprite"), true);
                EditorGUILayout.PropertyField(_loopOffSpriteProp, new GUIContent("loopOffSprite"), true);
                DrawBoldLabel("Volume");
                EditorGUILayout.PropertyField(_volumeImageProp, new GUIContent("VolumeButtonImage"), true);
                EditorGUILayout.PropertyField(_muteImageProp, new GUIContent("MuteButtonImage"), true);
                EditorGUILayout.PropertyField(_volumeOnSpriteProp, new GUIContent("volumeOnSprite"), true);
                EditorGUILayout.PropertyField(_volumeOffSpriteProp, new GUIContent("volumeOffSprite"), true);
                EditorGUILayout.PropertyField(_volumeSliderProp, new GUIContent("volume"), true);
                DrawBoldLabel("Seek bar");
                EditorGUILayout.PropertyField(_seekbarProp, new GUIContent("seekBar"), true);
                EditorGUILayout.PropertyField(_totTimeProp, new GUIContent("totTime"), true);
                EditorGUILayout.PropertyField(_currentTimeProp, new GUIContent("currentTime"), true);
                DrawBoldLabel("Caption");
                EditorGUILayout.PropertyField(_captionToggleProp, new GUIContent("captionToggle"), true);
                EditorGUILayout.PropertyField(_captionTextProp, new GUIContent("captionText"), true);
                DrawBoldLabel("Info");
                EditorGUILayout.PropertyField(_videoSizeProp, new GUIContent("videoSize"), true);
                EditorGUILayout.PropertyField(_statusProp, new GUIContent("status"), true);
                EditorGUILayout.PropertyField(_expiredMessageProp, new GUIContent("expired message"), true);

                EditorGUILayout.Space();

                if (GameObject.Find("NexPlayer_UI") != null)
                {
                    if (GUILayout.Button("Set UI References"))
                    {
                        NexPlayer player = (NexPlayer)target;
                        player.FillUI();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Before attempting to place the UI references, please instantiate NexPlayer_UI GameObject", MessageType.Error);
                    if (GUILayout.Button("Instantiate NexPlayer_UI"))
                        NxPMenuItemsFullFeatSample.CreateNexPlayerUI(new MenuCommand(null, 0));
                }
            }

            GUILayout.EndVertical();
        }
        
#endregion

        private void DrawMultiStreamProperties()
        {
            EditorGUILayout.BeginVertical(style);
            CreateToggleHEader("MULTISTREAM", ref _showMultiStream);

            if (_showMultiStream)
            {
                EditorGUILayout.Space();

                CreateMultiSizeController("Number of Streams: ", _numberOfStreamsProp);
                multiStreamNumber = _numberOfStreamsProp.intValue;
                _multiURLPathsProp.arraySize = _numberOfStreamsProp.intValue;
                _multiKeyServerProp.arraySize = _numberOfStreamsProp.intValue;
                _multiRawImagesProp.arraySize = _numberOfStreamsProp.intValue;
                _multiRenderTexturesProp.arraySize = _numberOfStreamsProp.intValue;
                _multiSubTextsProp.arraySize = _numberOfStreamsProp.intValue;

                if (usingMulti)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorStyles.label.fontStyle = FontStyle.Bold;
                    EditorGUILayout.LabelField("Render Mode", GUILayout.MaxWidth(100));
                    EditorStyles.label.fontStyle = _cachedFontStyle;

                    int[] modeOptions = { 3, 1 };
                    string[] modeOptionsText = { "RawImage", "RenderTexture" };
                    _renderModeProp.enumValueIndex = EditorGUILayout.IntPopup(_renderModeProp.enumValueIndex, modeOptionsText, modeOptions);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    DrawBoldLabel("URL Paths");
                    DrawSimpleArray(_multiURLPathsProp);

                    EditorGUI.indentLevel++;

                    _multiKeyServerProp.isExpanded = true; // Expanded by default since certain unity versions don't support expanding/collapsing

                    DrawArrayWithoutSize(_multiKeyServerProp);
                    EditorGUI.indentLevel--;
                    if (_renderModeProp.enumValueIndex == 1)
                    {
                        DrawBoldLabel("RenderTextures");
                        DrawSimpleArray(_multiRenderTexturesProp);
                    }
                    else if (_renderModeProp.enumValueIndex == 3)
                    {
                        DrawBoldLabel("RawImages");
                        DrawSimpleArray(_multiRawImagesProp);
                    }

                    EditorGUI.indentLevel++;
                    DrawArrayWithoutSize(_multiSubTextsProp);
                    EditorGUI.indentLevel--;
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawEvents()
        {
            EditorGUILayout.BeginVertical(style);
            CreateToggleHEader("EVENT LISTENERS", ref _showEventsListeners);
            if (_showEventsListeners)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_asyncInitEventProp, new GUIContent("Async Initialization"), true, GUILayout.MinHeight(50));
                EditorGUILayout.PropertyField(_registerAESKeyCallbackProp, new GUIContent("RegisterAESKeyCallback"), true, GUILayout.MinWidth(50));

#pragma warning disable CS0618 // Type or member is obsolete
                EditorGUILayout.HelpBox("This event is obsolete.\nOverride NexPlayer.EventInitComplete() instead.", MessageType.Warning);
                EditorGUILayout.PropertyField(_readyToPlayEventProp, new GUIContent("PrepareToPlay"), true, GUILayout.MinWidth(50));
                EditorGUILayout.HelpBox("This event is obsolete.\nOverride NexPlayer.EventPlaybackStarted() instead.", MessageType.Warning);
                EditorGUILayout.PropertyField(_startToPlayEventProp, new GUIContent("StartToPlay"), true, GUILayout.MinWidth(50));
                EditorGUILayout.HelpBox("This event is obsolete.\nOverride NexPlayer.EventEndOfContent() instead.", MessageType.Warning);
                EditorGUILayout.PropertyField(_endOfPlayEventProp, new GUIContent("EndOfPlay"), true, GUILayout.MinWidth(50));
#pragma warning restore CS0618 // Type or member is obsolete
            }

            GUILayout.EndVertical();
        }
        private void DrawLocalDRM()
        {
            EditorGUILayout.BeginVertical(style);
            CreateToggleHEader("Local Playback DRM Settings", ref _showLocalDRM);

            if (_showLocalDRM)
            {
                if (!isMobile)
                {
                    EditorGUILayout.HelpBox("Local DRM only supported on Android and IOS devices.", MessageType.Info);
                }
                //EditorGUI.BeginDisabledGroup(!isMobile);
                //EditorGUILayout.Space();
                //EditorGUI.indentLevel++;
                //DrawChildrenWithoutParent(_contentMeta);
                //EditorGUI.indentLevel--;
                //EditorGUI.EndDisabledGroup();
            }

            GUILayout.EndVertical();
        }
#region HELPERS
        void CreateTitle(string s)
        {
            //var centeredStyle = GUI.skin.GetStyle("Label");
            //centeredStyle.alignment = TextAnchor.MiddleCenter;
            EditorStyles.toolbarButton.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField(s, EditorStyles.toolbarButton);
            EditorStyles.toolbarButton.fontStyle = _cachedFontStyle;
            EditorGUILayout.Space();
        }

        void CreateToggleHEader(string s, ref bool b)
        {
            EditorStyles.toolbarButton.fontStyle = FontStyle.Bold;

            if (GUILayout.Button(s, EditorStyles.toolbarButton))
            {
                b = !b;
            }

            EditorStyles.toolbarButton.fontStyle = _cachedFontStyle;
        }

        void CreateArraySizeController(string label, SerializedProperty size)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.MaxWidth(200));
            size.intValue = EditorGUILayout.IntField(size.intValue, EditorStyles.textField, GUILayout.MaxWidth(30));
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.MaxWidth(20)))
            {
                size.intValue++;
            }
            if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.MaxWidth(20)))
            {
                if (size.intValue > 0)
                    size.intValue--;
            }
            if (GUILayout.Button("Reset", EditorStyles.miniButton, GUILayout.MaxWidth(80)))
            {
                size.intValue = 0;
            }
            GUILayout.EndHorizontal();
        }
        void CreateMultiSizeController(string label, SerializedProperty size)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.MaxWidth(200));
            size.intValue = EditorGUILayout.IntField(size.intValue, EditorStyles.textField, GUILayout.MaxWidth(30));
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.MaxWidth(20)))
            {
                size.intValue++;
                size.intValue = Mathf.Clamp(size.intValue, 2, 8);
            }
            if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.MaxWidth(20)))
            {
                if (size.intValue > 0)
                    size.intValue--;
                if (size.intValue == 1)
                    size.intValue = 0;
            }
            if (GUILayout.Button("Reset", EditorStyles.miniButton, GUILayout.MaxWidth(80)))
            {
                size.intValue = 0;
            }
            GUILayout.EndHorizontal();
        }

        void DrawBoldLabel(string s)
        {
            EditorStyles.label.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField(s);
            EditorStyles.label.fontStyle = _cachedFontStyle;
        }

        void DrawChildrenWithoutParent(SerializedProperty sp)
        {
            IEnumerable<SerializedProperty> children = GetChildren(sp);

            for (int i = 0; i < children.Count(); i++)
            {
                EditorGUILayout.PropertyField(children.ElementAt(i));
            }
        }
        IEnumerable<SerializedProperty> GetChildren(SerializedProperty serializedProperty)
        {
            SerializedProperty currentProperty = serializedProperty.Copy();
            SerializedProperty nextSiblingProperty = serializedProperty.Copy();
            {
                nextSiblingProperty.Next(false);
            }

            if (currentProperty.Next(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;

                    yield return currentProperty;
                }
                while (currentProperty.Next(false));
            }
        }

        void DrawArrayWithoutSize(SerializedProperty sp)
        {
            EditorGUILayout.PropertyField(sp, false);
            if (sp.isExpanded)
            {
                for (int i = 0; i < sp.arraySize; i++)
                {
                    EditorGUILayout.PropertyField(sp.GetArrayElementAtIndex(i), GUIContent.none);
                }
            }
        }
        void DrawSimpleArray(SerializedProperty sp)
        {
            //EditorGUILayout.LabelField(sp.displayName);
            for (int i = 0; i < sp.arraySize; i++)
            {
                EditorGUILayout.PropertyField(sp.GetArrayElementAtIndex(i), GUIContent.none);
            }
        }

        /// <summary>
        /// Returns "FrameBox" for Unity 2020-2021 and "Box" in any other case
        /// </summary>
        /// <returns></returns>
        private string getStyle()
        {
            string[] unityVersion = Application.unityVersion.Split('.');

            if (unityVersion.Length != 3) // Unknown unity version
            {
                return "Box";
            }

            string majorVersion = unityVersion[0];

            if (majorVersion == "2020" || majorVersion == "2021")
            {
                return "FrameBox";
            }
            else
            {
                return "Box";
            }
        }
        #endregion

        #region Save&Load
        private static void LoadEditorState()
        {
            _showDebug = EditorPrefs.GetBool(SettingsPrefix + "ShowDebug", false);
            _showDRM = EditorPrefs.GetBool(SettingsPrefix + "ShowDRM", false);
            _showHTTPHeaders = EditorPrefs.GetBool(SettingsPrefix + "ShowHTTPHeaders", false);
            _showAdvancedProperties = EditorPrefs.GetBool(SettingsPrefix + "ShowAdvanced", false);
            _showSubtitles = EditorPrefs.GetBool(SettingsPrefix + "ShowSubtitles", false);
            _showUI = EditorPrefs.GetBool(SettingsPrefix + "ShowUI", false);
            _showMultiStream = EditorPrefs.GetBool(SettingsPrefix + "ShowMulti", false);
            multiStreamNumber = EditorPrefs.GetInt(SettingsPrefix + "NumberMulti");
            _showEventsListeners = EditorPrefs.GetBool(SettingsPrefix + "ShowEvents", false);
            _showLocalDRM = EditorPrefs.GetBool(SettingsPrefix + "ShowLocalDRM", false);

            string recentFilesString = EditorPrefs.GetString(SettingsPrefix + "RecentFiles", string.Empty);
            _recentFiles = new List<string>(recentFilesString.Split(new string[] { ";" }, System.StringSplitOptions.RemoveEmptyEntries));
        }

        private static void SaveEditorState()
        {
            EditorPrefs.SetBool(SettingsPrefix + "ShowDebug", _showDebug);
            EditorPrefs.SetBool(SettingsPrefix + "ShowDRM", _showDRM);
            EditorPrefs.SetBool(SettingsPrefix + "ShowHTTPHeaders", _showHTTPHeaders);
            EditorPrefs.SetBool(SettingsPrefix + "ShowAdvanced", _showAdvancedProperties);
            EditorPrefs.SetBool(SettingsPrefix + "ShowSubtitles", _showSubtitles);
            EditorPrefs.SetBool(SettingsPrefix + "ShowUI", _showUI);
            EditorPrefs.SetBool(SettingsPrefix + "ShowMulti", _showMultiStream);
            EditorPrefs.SetInt(SettingsPrefix  + "NumberMulti", multiStreamNumber);
            EditorPrefs.SetBool(SettingsPrefix + "ShowEvents", _showEventsListeners);
            EditorPrefs.SetBool(SettingsPrefix + "ShowLocalDRM", _showLocalDRM);

            string recentFilesString = string.Empty;
            if (_recentFiles.Count > 0)
            {
                recentFilesString = string.Join(";", _recentFiles.ToArray());
            }
            EditorPrefs.SetString(SettingsPrefix + "RecentFiles", recentFilesString);
        }
#endregion
    }
}