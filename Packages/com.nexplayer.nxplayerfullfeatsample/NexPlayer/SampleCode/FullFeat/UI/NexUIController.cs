#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
using UnityEditor;
using UnityEditor.Events;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NexPlayerAPI;
using NexUtility;


namespace NexPlayerSample
{
    public class NexUIController : MonoBehaviour
    {
        [SerializeField] NexPlayer nexPlayer;
        [SerializeField] NexUISprites sprites;
        private Vector3 lastMousePosition;

        public GameObject nexControlBar;//gameObjectToTogle
        [SerializeField] Transform nexButtons;
        [Header("Buttons")]
        [SerializeField] GameObject back;
        [SerializeField] GameObject languageCaptionSettings;
        [SerializeField] GameObject captionToggle;
        [SerializeField] GameObject maximizeScreen;
        [SerializeField] GameObject resizeScreen;
        [SerializeField] GameObject playPause;
        [SerializeField] GameObject stop;
        [SerializeField] GameObject offlineDownloadToggle;
        [SerializeField] GameObject offlineList;
        [SerializeField] GameObject loop;
        [SerializeField] GameObject volumeOptions;
        [SerializeField] GameObject playPreviousVideo;
        [SerializeField] GameObject playNextVideo;
        [SerializeField] GameObject originSize;
        [SerializeField] GameObject fitToScreen;
        [SerializeField] GameObject strechToFullScreen;
        [SerializeField] GameObject fitVertically;
        [SerializeField] GameObject fitHorizontal;
        [Header("Panels")]
        [SerializeField] GameObject screenSizePanel;
        [SerializeField] GameObject downloadingPanel;
        [SerializeField] GameObject offlineListPanel;
        [Header("Language List Panel")]
        [SerializeField] GameObject languageListPanel;
        [SerializeField] GameObject firstSubtitlesButton;
        [Header("Volume Options Panel")]
        [SerializeField] GameObject volumeOptionPanel;
        [SerializeField] GameObject muteButton;
        [SerializeField] GameObject volumeSlider;
        [Header("Special Objects")]
        [SerializeField] GameObject seekBar;
        [SerializeField] NexSeekBar nexSeekBar;
        [SerializeField] GameObject offlineButtons;
        [SerializeField] OfflineDownloadButtons offlineDownloadButtons;
        [SerializeField] GameObject offlineDownloadContent;
        [SerializeField] OfflineStreaming offlineStreaming;
        [SerializeField] GameObject audioList;
        [SerializeField] AudioStreamListManager audioListManager;
        [SerializeField] GameObject captionList;
        [SerializeField] CloseCaptionStreamListManager captionListManager;
        [SerializeField] GameObject expiredMessage;

        [HideInInspector]
        public bool ReferencesFound = false;
        [HideInInspector]
        public bool ComponentsFound = false;
        [HideInInspector]
        public bool EventsAssigned = false;

        // Active panel
        private GameObject activePanel;

        // Buttons array
        private List<GameObject> buttons = new List<GameObject>();

        #region Methods Called From Editor
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX

        public void FixPrefab()
        {
            FindReferences();
            FindOrAddComponents();
            SetSprites();
            SetSortingOrders();
            DisableObjects();
        }

        public void Bind() {
            if (nexPlayer == null) {
                nexPlayer = FindObjectOfType<NexPlayer>();
                if (nexPlayer == null)
                    Debug.Log("There is not a NexPlayer instance in the Scene!");
            }
            else
                AddPersistantListeners();
        }

        void FindOrAddComponents()
        {
            try {
                // NexSeekBar
                nexSeekBar = AddComponentIfMissing<NexSeekBar>(seekBar);
                nexSeekBar.FindReferences(nexPlayer);
                // Offline
                offlineStreaming = AddComponentIfMissing<OfflineStreaming>(offlineDownloadContent);
                offlineDownloadButtons = AddComponentIfMissing<OfflineDownloadButtons>(offlineButtons);
                GameObject go = offlineStreaming.FindReferences(nexPlayer, offlineDownloadButtons);
                NexHolder nh = AddComponentIfMissing<NexHolder>(go);
                UnityEventTools.RemovePersistentListener(AddComponentIfMissing<Button>(go).onClick, nh.RunThisVideo);
                UnityEventTools.AddPersistentListener(AddComponentIfMissing<Button>(go).onClick, nh.RunThisVideo);
                GameObject progressBar = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/DownloadingPanel/ProgressBar");
                DownloadProgressBar downloadProgressBar = AddComponentIfMissing<DownloadProgressBar>(progressBar);
                downloadProgressBar.FindReferences(nexPlayer);
                UnityEventTools.RemovePersistentListener(downloadProgressBar.downloadDone, offlineDownloadButtons.HandleDownloadDone);
                UnityEventTools.AddPersistentListener(downloadProgressBar.downloadDone, offlineDownloadButtons.HandleDownloadDone);
                downloadProgressBar.GetComponent<Slider>().fillRect = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/DownloadingPanel/ProgressBar/Fill Area/Fill").GetComponent<RectTransform>();
                offlineDownloadButtons.FindReferences(nexPlayer, downloadingPanel, offlineListPanel, progressBar, offlineStreaming, offlineDownloadToggle, this);
                // Audio & caption
                audioListManager = AddComponentIfMissing<AudioStreamListManager>(GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel/LanguageList/List").gameObject);
                audioListManager.FindReferences(nexPlayer);
                AddComponentIfMissing<NexAudioInfo>(GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel/LanguageList/List/AudioButton").gameObject);
                captionListManager = AddComponentIfMissing<CloseCaptionStreamListManager>(GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel/CaptionList/List").gameObject);
                captionListManager.FindReferences(nexPlayer);
                AddComponentIfMissing<NexCaptionInfo>(GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel/CaptionList/List/CaptionButton").gameObject);

                ComponentsFound = true;
            }
            catch (Exception e) {
                ComponentsFound = false;
                Debug.LogError("Adding Components failed, exception" + e);
            }
        }
        void SetSprites()
        {
            if (sprites == null)
                sprites = (NexUISprites)AssetDatabase.LoadAssetAtPath(NexPlayerFullFeatSampleFolderRoot.GetRelativePath() + "/NexPlayer/Resources/NexUISprites.asset", typeof(NexUISprites));
            sprites.ResetReferences();
            //Buttons
            back.GetComponent<Image>().sprite = sprites.arrow;
            languageCaptionSettings.GetComponentInChildren<Image>().sprite = sprites.languageCaptionSettings;
            captionToggle.GetComponentInChildren<Image>().sprite = sprites.caption;
            captionToggle.GetComponentInChildren<Image>().color = Color.gray;
            maximizeScreen.GetComponent<Image>().sprite = sprites.maximizeScreen;
            maximizeScreen.GetComponent<Image>().material = sprites.whiteMat;
            resizeScreen.GetComponent<Image>().sprite = sprites.resizeScreen;
            originSize.GetComponent<Image>().sprite = sprites.originSize;
            fitToScreen.GetComponent<Image>().sprite = sprites.maximizeScreen;
            strechToFullScreen.GetComponent<Image>().sprite = sprites.fullScreen;
            fitVertically.GetComponent<Image>().sprite = sprites.fitVertically;
            fitHorizontal.GetComponent<Image>().sprite = sprites.fitHorizontal;
            playPause.GetComponent<Image>().sprite = sprites.play;
            stop.GetComponent<Image>().sprite = sprites.stop;
            offlineDownloadToggle.GetComponent<Image>().sprite = sprites.download;
            offlineList.GetComponent<Image>().sprite = sprites.offlineList;
            loop.GetComponent<Image>().sprite = sprites.loop;
            volumeOptions.GetComponent<Image>().sprite = sprites.audio;
            muteButton.GetComponent<Image>().sprite = sprites.audio;
            // Panels
            Sprite background = sprites.Background;
            GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel/LanguageList").GetComponent<Image>().sprite = background;
            GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel/CaptionList").GetComponent<Image>().sprite = background;
            screenSizePanel.GetComponent<Image>().sprite = background;
            downloadingPanel.GetComponent<Image>().sprite = background;
            offlineListPanel.GetComponent<Image>().sprite = background;
            GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/OfflineDownloadListPanel/Viewport").GetComponent<Image>().sprite = sprites.UiMask;
            GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/OfflineDownloadListPanel/Viewport/Content/DownloadedFilePanel").GetComponent<Image>().sprite = sprites.uiButtonDefault;
            volumeOptionPanel.GetComponent<Image>().sprite = background;
            volumeSlider.transform.GetChild(0).GetComponent<Image>().sprite = background;
            volumeSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>().sprite = sprites.UiSprite;
            volumeSlider.transform.GetChild(2).GetChild(0).GetComponent<Image>().sprite = sprites.Knob;
            // NexSeekBar
            GameObject.Find("NexPlayer_UI/NexControlBar/SeekBar_Canvas/NexSeekBar/Background").GetComponent<Image>().sprite = background;
            GameObject.Find("NexPlayer_UI/NexControlBar/SeekBar_Canvas/NexSeekBar/FillBufferArea/Fill").GetComponent<Image>().sprite = sprites.UiSprite;
            GameObject.Find("NexPlayer_UI/NexControlBar/SeekBar_Canvas/NexSeekBar/FillPlayedArea/Fill").GetComponent<Image>().sprite = sprites.UiSprite;
            GameObject.Find("NexPlayer_UI/NexControlBar/SeekBar_Canvas/NexSeekBar/HandleSlide Area/Handle").GetComponent<Image>().sprite = sprites.Knob;
        }
        void SetSortingOrders()
        {
            GameObject.Find("NexPlayer_UI/Caption_Canvas").GetComponent<Canvas>().sortingOrder = 1;
            GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas").GetComponent<Canvas>().sortingOrder = 2;
            GameObject.Find("NexPlayer_UI/NexControlBar/SeekBar_Canvas").GetComponent<Canvas>().sortingOrder = 3;
            GameObject.Find("NexPlayer_UI/NexControlBar/Info_Canvas").GetComponent<Canvas>().sortingOrder = 4;
        }
        void DisableObjects()
        {
            playPreviousVideo.SetActive(false);
            playNextVideo.SetActive(false);
            languageListPanel.SetActive(false);
            screenSizePanel.SetActive(false);
            downloadingPanel.SetActive(false);
            offlineListPanel.SetActive(false);
            volumeOptionPanel.SetActive(false);
            expiredMessage.SetActive(false);
        }

        void AddPersistantListeners()
        {
            try {
                ClearPersistentListeners();
                UnityEventTools.AddPersistentListener(back.GetComponent<Button>().onClick, nexPlayer.ToggleQuit);
                // Main Buttons
                UnityEventTools.AddPersistentListener(languageCaptionSettings.GetComponent<Button>().onClick, ToggleLanguageList);
                UnityEventTools.AddPersistentListener(captionToggle.GetComponent<Toggle>().onValueChanged, nexPlayer.ToggleCaption);
                UnityEventTools.AddPersistentListener(maximizeScreen.GetComponent<Button>().onClick, nexPlayer.MaximizeScreen);
                UnityEventTools.AddPersistentListener(resizeScreen.GetComponent<Button>().onClick, ToggleScreenSize);
                UnityEventTools.AddPersistentListener(playPause.GetComponent<Button>().onClick, nexPlayer.TogglePlayPause);
                UnityEventTools.AddPersistentListener(stop.GetComponent<Button>().onClick, nexPlayer.Stop);
                UnityEventTools.AddPersistentListener(offlineDownloadToggle.GetComponent<Button>().onClick, offlineDownloadButtons.DownloadVideo);
                UnityEventTools.AddPersistentListener(offlineList.GetComponent<Button>().onClick, offlineDownloadButtons.EnableOfflineList);
                UnityEventTools.AddPersistentListener(loop.GetComponent<Button>().onClick, nexPlayer.ToggleLoop);
                UnityEventTools.AddPersistentListener(volumeOptions.GetComponent<Button>().onClick, ToggleVolumeOption);
                // SizePanel
                UnityEventTools.AddPersistentListener(originSize.GetComponent<Button>().onClick, OriginSize);
                UnityEventTools.AddPersistentListener(fitToScreen.GetComponent<Button>().onClick, FitToScreen);
                UnityEventTools.AddPersistentListener(strechToFullScreen.GetComponent<Button>().onClick, StretchToFullScreen);
                UnityEventTools.AddPersistentListener(fitVertically.GetComponent<Button>().onClick, FitVertically);
                UnityEventTools.AddPersistentListener(fitHorizontal.GetComponent<Button>().onClick, FitHorizontally);
                // VolumePanel
                UnityEventTools.AddPersistentListener(volumeSlider.GetComponent<Slider>().onValueChanged, nexPlayer.SetVolume01);
                UnityEventTools.AddPersistentListener(muteButton.GetComponent<Button>().onClick, nexPlayer.ToggleMute);
                // SeekBar
                //UnityEventTools.AddPersistentListener(seekBar.GetComponent<Slider>().onValueChanged, Seek);

                EditorUtility.SetDirty(languageCaptionSettings.GetComponent<Button>());
                nexPlayer.expiredMessage = expiredMessage;
                EventsAssigned = true;
            }
            catch (Exception e) {
                EventsAssigned = false;
                Debug.LogError("Adding Listeners failed, exception" + e);
            }

        }

        void ClearPersistentListeners()
        {
            UnityEventTools.RemovePersistentListener(back.GetComponent<Button>().onClick, nexPlayer.ToggleQuit);
            // Main Buttons
            UnityEventTools.RemovePersistentListener(languageCaptionSettings.GetComponent<Button>().onClick, ToggleLanguageList);
            UnityEventTools.RemovePersistentListener<bool>(captionToggle.GetComponent<Toggle>().onValueChanged, nexPlayer.ToggleCaption);
            UnityEventTools.RemovePersistentListener(maximizeScreen.GetComponent<Button>().onClick, nexPlayer.MaximizeScreen);
            UnityEventTools.RemovePersistentListener(resizeScreen.GetComponent<Button>().onClick, ToggleScreenSize);
            UnityEventTools.RemovePersistentListener(playPause.GetComponent<Button>().onClick, nexPlayer.TogglePlayPause);
            UnityEventTools.RemovePersistentListener(stop.GetComponent<Button>().onClick, nexPlayer.Stop);
            UnityEventTools.RemovePersistentListener(offlineDownloadToggle.GetComponent<Button>().onClick, offlineDownloadButtons.DownloadVideo);
            UnityEventTools.RemovePersistentListener(offlineList.GetComponent<Button>().onClick, offlineDownloadButtons.EnableOfflineList);
            UnityEventTools.RemovePersistentListener(loop.GetComponent<Button>().onClick, nexPlayer.ToggleLoop);
            UnityEventTools.RemovePersistentListener(volumeOptions.GetComponent<Button>().onClick, ToggleVolumeOption);
            // SizePanel
            UnityEventTools.RemovePersistentListener(originSize.GetComponent<Button>().onClick, OriginSize);
            UnityEventTools.RemovePersistentListener(fitToScreen.GetComponent<Button>().onClick, FitToScreen);
            UnityEventTools.RemovePersistentListener(strechToFullScreen.GetComponent<Button>().onClick, StretchToFullScreen);
            UnityEventTools.RemovePersistentListener(fitVertically.GetComponent<Button>().onClick, FitVertically);
            UnityEventTools.RemovePersistentListener(fitHorizontal.GetComponent<Button>().onClick, FitHorizontally);
            // VolumePanel
            UnityEventTools.RemovePersistentListener<float>(volumeSlider.GetComponent<Slider>().onValueChanged, nexPlayer.SetVolume01);
            UnityEventTools.RemovePersistentListener(muteButton.GetComponent<Button>().onClick, nexPlayer.ToggleMute);
            // SeekBar
            //UnityEventTools.RemovePersistentListener<float>(seekBar.GetComponent<Slider>().onValueChanged, Seek);

            EditorUtility.SetDirty(languageCaptionSettings.GetComponent<Button>());
        }

        public void FillNexPlayerUIReferences()
        {
            nexPlayer.FillUI();
        }
#endif
        #endregion

        #region NexUIFunctionality
        void Start()
        {
            DisableForWinMacWeb();
            FindReferences();
            if (Application.platform == RuntimePlatform.WSAPlayerX64)
                SetEventSystem();
        }

        void Update()
        {
            if (PointerIsNotOverUI() && HasThePointerBeingClicked())
                ToogleUI();

            if (Application.platform == RuntimePlatform.WSAPlayerX64)
                updateXBoxController();
        }

        private void SetEventSystem()
        {
            GameObject ev = GameObject.Find("EventSystem");
            ev.GetComponent<EventSystem>().firstSelectedGameObject = playPause;
            StandaloneInputModule sim = ev.GetComponent<StandaloneInputModule>();
            sim.horizontalAxis = "DPad X";
            sim.verticalAxis = "DPad Y";
            sim.forceModuleActive = true;
        }

        void FindReferences()
        {
            try
            {
                nexPlayer = FindObjectOfType<NexPlayer>();
                if (nexPlayer == null)
                    Debug.Log("There is not a NexPlayer instance in the Scene!");

                nexControlBar = GameObject.Find("NexPlayer_UI/NexControlBar");

                buttons = new List<GameObject>();
                // Buttons
                back = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/Back_Button");
                buttons.Add(back);
                languageCaptionSettings = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/LanguageCaptionSettings");
                buttons.Add(languageCaptionSettings);
                captionToggle = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/CaptionToggle");
                buttons.Add(captionToggle);
                maximizeScreen = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/MaximizeScreen");
                buttons.Add(maximizeScreen);
                resizeScreen = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/ResizeScreen");
                buttons.Add(resizeScreen);
                playPreviousVideo = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/PlayPreviousVideo");
                buttons.Add(playPreviousVideo);
                playPause = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/PlayPause");
                buttons.Add(playPause);
                stop = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/Stop");
                buttons.Add(stop);
                playNextVideo = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/PlayNextVideo");
                buttons.Add(playNextVideo);
                offlineButtons = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/OfflineButtons");
                offlineDownloadToggle = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/OfflineButtons/OfflineDownloadToggle");
                buttons.Add(offlineDownloadToggle);
                offlineList = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/OfflineButtons/OfflineDownloadListToggle");
                buttons.Add(offlineList);
                loop = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/Loop");
                buttons.Add(loop);
                volumeOptions = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexButtons/VolumeOptions");
                buttons.Add(volumeOptions);

                // Panels
                downloadingPanel = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/DownloadingPanel");
                // Screen Size Panel
                screenSizePanel = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/SizeOptions");
                originSize = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/SizeOptions/OriginSize");
                buttons.Add(originSize);
                fitToScreen = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/SizeOptions/FitToScreen");
                buttons.Add(fitToScreen);
                strechToFullScreen = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/SizeOptions/StretchToFullScreen");
                buttons.Add(strechToFullScreen);
                fitVertically = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/SizeOptions/FitVertically");
                buttons.Add(fitVertically);
                fitHorizontal = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/SizeOptions/FitHorizontally");
                buttons.Add(fitHorizontal);

                // OfflineDownloadListPanel
                offlineListPanel = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/OfflineDownloadListPanel");
                offlineDownloadContent = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/OfflineDownloadListPanel/Viewport/Content");

                // volumePanel
                volumeOptionPanel = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/VolumePanel");
                volumeSlider = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/VolumePanel/VolumeSlider");
                volumeSlider.GetComponent<Slider>().fillRect = (RectTransform)GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/VolumePanel/VolumeSlider/Fill Area/Fill").transform;
                volumeSlider.GetComponent<Slider>().handleRect = (RectTransform)GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/VolumePanel/VolumeSlider/Handle Slide Area/Handle").transform;
                volumeSlider.GetComponent<Slider>().targetGraphic = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/VolumePanel/VolumeSlider/Handle Slide Area/Handle").GetComponent<Image>();
                ColorBlock cb = volumeSlider.GetComponent<Slider>().colors;
                cb.selectedColor = Color.red;
                cb.fadeDuration = 0.2f;
                volumeSlider.GetComponent<Slider>().colors = cb;

                muteButton = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/VolumePanel/Mute");
                buttons.Add(muteButton);

                // language options panel
                languageListPanel = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel");
                audioList = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel/LanguageList");
                captionList = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel/CaptionList");
                GameObject b0 = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel/LanguageList/List/AudioButton");
                GameObject b1 = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel/CaptionList/List/CaptionButton");
                buttons.Add(b0);
                buttons.Add(b1);

                // SeekBar
                seekBar = GameObject.Find("NexPlayer_UI/NexControlBar/SeekBar_Canvas/NexSeekBar");
                seekBar.GetComponent<Slider>().fillRect = (RectTransform)GameObject.Find("NexPlayer_UI/NexControlBar/SeekBar_Canvas/NexSeekBar/FillPlayedArea/Fill").transform;
                seekBar.GetComponent<Slider>().handleRect = (RectTransform)GameObject.Find("NexPlayer_UI/NexControlBar/SeekBar_Canvas/NexSeekBar/HandleSlide Area/Handle").transform;
                cb = seekBar.GetComponent<Slider>().colors;
                cb.selectedColor = Color.red;
                cb.fadeDuration = 0.2f;
                seekBar.GetComponent<Slider>().colors = cb;

                expiredMessage = GameObject.Find("NexPlayer_UI/NexControlBar/NexExpired");

                // Changing buttons color
                foreach (GameObject g in buttons)
                {
                    if (g != null)
                    {
                        Button b = g.GetComponent<Button>();
                        if (b != null) {
                            cb = b.colors;
                            cb.fadeDuration = 0.2f;
                            if (Application.platform == RuntimePlatform.WSAPlayerX64)
                                cb.selectedColor = Color.red;
                            else
                                cb.selectedColor = Color.white;
                            g.GetComponent<Button>().colors = cb;
                        }
                        else
                        {
                            cb = g.GetComponent<Toggle>().colors;
                            cb.fadeDuration = 0.2f;
                            if (Application.platform == RuntimePlatform.WSAPlayerX64)
                                cb.selectedColor = Color.red;
                            else
                                cb.selectedColor = Color.white;
                            g.GetComponent<Toggle>().colors = cb;
                        }
                    }
                }

                ReferencesFound = true;
            }
            catch (Exception e)
            {
                ReferencesFound = false;
                Debug.LogError("Find references failed, exception: " + e);
            }
        }

        private void updateXBoxController()
        {
            float dpad_x = Input.GetAxisRaw("DPad X");
            float dpad_y = Input.GetAxisRaw("DPad Y");

            if (Input.GetAxisRaw("xBox_Vertical") > 0 || Input.GetAxisRaw("xBox_Vertical") < 0)
            {
                // left joystick vertical
            }
            else if (Input.GetAxisRaw("xBox_Horizontal") > 0 || Input.GetAxisRaw("xBox_Horizontal") < 0)
            {
                // left joystick horizontal
            }
            else if (dpad_x != 0)
            {
                /*
                if (dpad_x > 0)
                    Debug.Log("dPad button right");
                else
                    Debug.Log("dPad button left");
                */
            }
            else if (dpad_y != 0)
            {
                /*
                if (dpad_y > 0)
                    Debug.Log("dPad button up");
                else
                    Debug.Log("dPad button down");
                */
            }
            else if (Input.GetButtonDown("A_XBox"))
            {
                GameObject currentButton = EventSystem.current.currentSelectedGameObject;
                // When 'A' pushed on volumeOptions button
                if (currentButton == volumeOptions)
                {
                    EventSystem.current.SetSelectedGameObject(muteButton);
                    if (activePanel != null) activePanel.SetActive(false);
                    activePanel = volumeOptionPanel;
                    activePanel.SetActive(false);
                    currentButton.GetComponent<Button>().onClick.Invoke();
                }
                // When 'A' pushed on language captions button
                else if (currentButton == languageCaptionSettings)
                {
                    if (activePanel != null)
                        activePanel.SetActive(false);

                    activePanel = languageListPanel;
                    activePanel.SetActive(false);
                    currentButton.GetComponent<Button>().onClick.Invoke();

                    GameObject languageList = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel/LanguageList/List");
                    GameObject subtitleList = GameObject.Find("NexPlayer_UI/NexControlBar/Control_Canvas/NexPanels/StreamListPanel/CaptionList/List");

                    if (languageList.transform.childCount > 1)
                        firstSubtitlesButton = audioList.transform.GetChild(1).gameObject;
                    else if (subtitleList.transform.childCount > 1)
                        firstSubtitlesButton = subtitleList.transform.GetChild(1).gameObject;
                    else
                        firstSubtitlesButton = languageCaptionSettings;

                    EventSystem.current.SetSelectedGameObject(firstSubtitlesButton);
                }
                else
                {
                    if (currentButton.GetComponent<Button>() != null)
                        currentButton.GetComponent<Button>().onClick.Invoke();
                    else if (currentButton.GetComponent<Toggle>() != null)
                    {
                        currentButton.GetComponent<Toggle>().isOn = !currentButton.GetComponent<Toggle>().isOn;
                        bool isOn = currentButton.GetComponent<Toggle>().isOn;
                        currentButton.GetComponent<Toggle>().onValueChanged.Invoke(isOn);
                    }
                }
            }
            else if (Input.GetButtonDown("B_XBox"))
            {
                if (activePanel != null) {
                    activePanel.SetActive(false);
                    activePanel = null;
                }

                EventSystem.current.SetSelectedGameObject(playPause);
            }
            else if (Input.GetButtonDown("Y_XBox"))
                nexControlBar.SetActive(!nexControlBar.activeSelf);
        }

        /// <summary>
        /// Toggles the UI visibility taking into account the VR mode
        /// </summary>
        private void ToogleUI()
        {
            nexControlBar.SetActive(!nexControlBar.activeSelf);
        }
        private bool IsUIVisible()
        {
            return nexControlBar.activeSelf;
        }
        /// <summary>
        /// Informs if cardboard is present in the build
        /// </summary>
        private bool DoesTheBuildSupportCardboard()
        {
#if UNITY_5_6_OR_NEWER
            return Array.Exists(UnityEngine.XR.XRSettings.supportedDevices, s => s.ToLower().Contains("cardboard"));
#else
            return false;
#endif
        }
        /// <summary>
        /// Informs whether the mouse has been clicked or not
        /// </summary>
        private bool HasThePointerBeingClicked()
        {
            bool hasThePointerBeingClicked = false;
            if (Input.GetButtonDown("Fire1"))
                lastMousePosition = Input.mousePosition;
            else if (Input.GetButtonUp("Fire1"))
                hasThePointerBeingClicked = Vector3.Distance(lastMousePosition, Input.mousePosition) < 10;

            return hasThePointerBeingClicked;
        }
        /// <summary>
        /// Provides information to know if the pointer or touch input is over the game objects without being blocked by any UI element
        /// </summary>
        /// <returns>true if no UI element is blocking the pointer our touch</returns>
        private bool PointerIsNotOverUI()
        {
            bool isOverGameObject = true;
            if (!UnityEngine.XR.XRSettings.enabled)
            {
                if (Input.touchCount > 0)
                {
                    Touch[] touches = Input.touches;
                    int i = 0;
                    while (isOverGameObject && i < touches.Length)
                    {
                        if (EventSystem.current.IsPointerOverGameObject(touches[i].fingerId))
                            isOverGameObject = false;
                        i++;
                    }
                }
                else
                {
                    isOverGameObject = !EventSystem.current.IsPointerOverGameObject();
                }
            }
            return isOverGameObject;
        }

        public void DisableForWinMacWeb()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.WSAPlayerX64 || Application.platform == RuntimePlatform.WSAPlayerARM)
            {
                offlineDownloadToggle.SetActive(false);
                offlineList.SetActive(false);
                resizeScreen.SetActive(false);
            }

            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                offlineDownloadToggle.SetActive(false);
                offlineList.SetActive(false);
                resizeScreen.SetActive(false);
            }

            else if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                offlineDownloadToggle.SetActive(false);
                offlineList.SetActive(false);
                languageCaptionSettings.SetActive(false);
                resizeScreen.SetActive(false);
                captionToggle.SetActive(false);
                resizeScreen.SetActive(false);
            }
        }
        #endregion

        #region Event Callbacks
        private void TogglePanel(GameObject panel)
        {
            if (panel == null)
                return;

            if (panel.activeSelf)
            {
                panel.SetActive(false);
            }
            else
            {
                HideAllPanels();
                panel.SetActive(true);
            }
        }
        public void HideAllPanels()
        {
            languageListPanel.SetActive(false);
            screenSizePanel.SetActive(false);
            downloadingPanel.SetActive(false);
            offlineListPanel.SetActive(false);
            volumeOptionPanel.SetActive(false);
        }
        public void ToggleLanguageList()
        {
            TogglePanel(languageListPanel);
        }
        public void ToggleScreenSize()
        {
            TogglePanel(screenSizePanel);
        }
        public void ToggleVolumeOption()
        {
            TogglePanel(volumeOptionPanel);
        }
        public void OriginSize()
        {
            nexPlayer.ChangeAspectRatio(VideoAspectRatio.OriginSize);
        }
        public void FitToScreen()
        {
            nexPlayer.ChangeAspectRatio(VideoAspectRatio.FitToScreen);
        }
        public void StretchToFullScreen()
        {
            nexPlayer.ChangeAspectRatio(VideoAspectRatio.StretchToFullScreen);
        }
        public void FitVertically()
        {
            nexPlayer.ChangeAspectRatio(VideoAspectRatio.FitVertically);
        }
        public void FitHorizontally()
        {
            nexPlayer.ChangeAspectRatio(VideoAspectRatio.FitHorizontally);
        }
        public void Seek(float value)
        {
            nexPlayer.Seek();
        }

        public void AllowSeek(bool value)
        {
            nexPlayer.allowSeek = value;

            if (nexPlayer.allowSeek)
            {
                nexPlayer.Seek();
            }
        }
        #endregion

        #region Helpers
        private T AddComponentIfMissing<T>(GameObject go) where T : Component
        {
            T result = go.GetComponent<T>();
            if (result == null)
            {
                result = go.AddComponent<T>() as T;
            }
            return result;
        }
        #endregion
    }
}