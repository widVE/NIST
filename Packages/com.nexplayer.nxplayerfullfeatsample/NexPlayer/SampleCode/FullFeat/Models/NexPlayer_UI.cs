using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NexUtility;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NexPlayerSample
{
    public partial class NexPlayer
    {
        [SerializeField]
        public Slider volumeSlider;

        [SerializeField]
        public Image muteButtonImage;
        public Image volumeButtonImage;
        [SerializeField]
        public Sprite _volumeOnSprite;

        [SerializeField]
        public Sprite _volumeOffSprite;

        //[SerializeField]
        public Image loopButtonImage;

        [SerializeField]
        public Sprite _loopOnSprite;

        [SerializeField]
        public Sprite _loopOffSprite;

        [Tooltip("Text element that will be updated with the total time of the video")]
        [SerializeField]
        public Text totTime;
        [Tooltip("Text element that will be updated with the current time of the playback")]
        [SerializeField]
        public Text currentTime;
        [Tooltip("Text element that will be updated with the current video resolution of the playback")]
        [SerializeField]
        public Text videoSize;
        [Tooltip("Text element that will be updated with the current status of the playback")]
        [SerializeField]
        public Text status;
        [Tooltip("Seek bar used to display the current time and the last buffered content in the secondary progress")]
        [SerializeField]
        public NexSeekBar seekBar;
        [Tooltip("Image used int the play / pause button")]
        [SerializeField]
        public Image playPauseImage;
        [Tooltip("Sprite used to represent the ability to pause the video")]
        [SerializeField]
        public Sprite pauseSprite;
        [Tooltip("Sprite used to represent the ability to play the video")]
        [SerializeField]
        public Sprite playSprite;
        [SerializeField]
        public GameObject expiredMessage;
        [SerializeField]
        public Text caption;
        [SerializeField]
        public Toggle captionToggle;

        public void SetPlayPauseImage(bool playing)
        {
            if (playPauseImage != null)
            {
                if (playing)
                    playPauseImage.sprite = pauseSprite;
                else
                    playPauseImage.sprite = playSprite;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
#if UNITY_EDITOR
            EditorApplication.update -= UpdateHUDFromEditor;
            EditorApplication.update += UpdateHUDFromEditor;
#endif
        }

        private void UpdateHUDFromEditor()
        {
            float threshold = 0.01f;

            if (volumeSlider != null)
            {
                if (volume != (int)volumeSlider.value)
                {
                    if (Math.Abs(volumeSlider.value - volume) >= threshold)
                        volumeSlider.value = volume;
                }
            }

            if (muteButtonImage != null && volumeButtonImage != null)
            {
                if (mutePlay)
                {
                    muteButtonImage.sprite = _volumeOffSprite;
                    volumeButtonImage.sprite = _volumeOffSprite;
                }
                else
                {
                    muteButtonImage.sprite = _volumeOnSprite;
                    volumeButtonImage.sprite = _volumeOnSprite;
                }
            }

            if (loopButtonImage != null)
            {
                if (!loopPlay)
                    loopButtonImage.sprite = _loopOffSprite;
                else
                    loopButtonImage.sprite = _loopOnSprite;
            }

            if (captionToggle != null && !Application.isPlaying)
            {
                SetCaptionToggleColor(Color.gray);
            }
        }

        private void SetCaptionText(string txt)
        {
            if (caption != null)
            {
                caption.text = txt;
            }
        }

        private void SetCaptionToggle()
        {
            if (captionToggle != null)
            {
                captionToggle.isOn = IsOnSubtitle();
            }
        }

        private void UpdateCaption()
        {
            if (captionToggle != null)
            {
                SetOnSubtitle(captionToggle.isOn);
                bOnSubtitle = captionToggle.isOn;
                OnOffSubtitle(captionToggle.isOn);
            }
        }

        private void SetCaptionToggleColor(Color color)
        {
            captionToggle.GetComponentInChildren<Image>().color = color;
        }

        private void HandleEventTextRender()
        {
            if (IsPlayerCreated() && caption != null)
            {
                if (captionToggle != null)
                {
                    if (captionToggle.isOn)
                    {
                        subtitleElement = GetCurrentSubtitleElement();
                    }
                    else
                    {
                        subtitleElement.caption = null;
                    }
                }
                else
                {
                    subtitleElement = GetCurrentSubtitleElement();
                }

                if (subtitleElement.caption != null)
                {
                    //TODO: Remove GameObject.Find, cache this reference in Init
                    var subIndexObj = GameObject.Find("SubIndex");
                    if (subIndexObj != null)
                    {
                        var subIndexObjText = subIndexObj.GetComponent<Text>();
                        if (subIndexObjText != null)
                        {
                            //sub player index
                            subIndexObjText.text = $"Last Subtitle on Stream: {GetSubtitlePlayerIndex()}";
                        }
                    }
                    if (multiSubTexts.Count > 1 && GetSubtitlePlayerIndex() >= 0)
                    {
                        if (Application.platform != RuntimePlatform.IPhonePlayer)
                        { // Crash on iOS for no reason (cant read Text.text)
                            multiSubTexts[GetSubtitlePlayerIndex()].text = subtitleElement.caption;
                        }
                    }
                    else
                    {
                        caption.text = subtitleElement.caption;
                        savedCaption = subtitleElement.caption;
                        savedStartCaptionTime = subtitleElement.startTime;
                        savedEndCaptionTime = subtitleElement.endTime;
                    }
                }
            }
        }

        protected override void SetLoopImage()
        {
            if (loopButtonImage != null)
            {
                if (!loopPlay)
                    loopButtonImage.sprite = _loopOffSprite;
                else
                    loopButtonImage.sprite = _loopOnSprite;
            }
        }

        protected override void SetMuteImage()
        {
            if (muteButtonImage != null)
            {
                if (mutePlay)
                {
                    muteButtonImage.sprite = _volumeOffSprite;
                    volumeButtonImage.sprite = _volumeOffSprite;
                }
                else
                {
                    muteButtonImage.sprite = _volumeOnSprite;
                    volumeButtonImage.sprite = _volumeOnSprite;
                }
            }
        }

        private void ResetPlayerUI()
        {
            Log("ResetPlayerUI");
            if (currentTime != null)
            {
                currentTime.text = NexUtil.GetTimeString(0);
            }
            if (seekBar != null)
            {
                seekBar.SetValue(0);
                seekBar.SetSecondaryValue(0);
            }
            if (totTime != null)
            {
                totTime.text = NexUtil.GetTimeString(0);
            }
            if (caption != null)
            {
                caption.text = string.Empty;
            }
            if (playPauseImage != null)
            {
                playPauseImage.sprite = playSprite;
            }
        }

        public override void SetPlayerStatus(string playerStatus)
        {
            if (status != null)
            {
                if (status.text != "NETWORK ERROR")
                {
                    status.text = playerStatus;
                }
            }

            switch (playerStatus)
            {
                case "Opened":
                case "Stop":
                case "Pause":
                    if (playPauseImage != null)
                    {
                        playPauseImage.sprite = playSprite;
                    }

                    break;
                case "Playing":
                case "Close":
                    if (playPauseImage != null)
                    {
                        playPauseImage.sprite = pauseSprite;
                    }

                    break;
                case "EXPIRED SDK TRIAL":
                    break;
                default:
                    Log($"playerStatus {playerStatus} not handled");
                    break;
            }
        }

        private void SetVideoSizeText(string widthAndHeight)
        {
            if (videoSize != null)
            {
                videoSize.text = widthAndHeight;
            }
        }

        private void UpdateUIWithCurrentTime()
        {
            Log("SetCurrentTime() called");

            if (IsPlayerCreated())
            {
                int playerCurrentTime = GetCurrentTime();
                int playerTotalTime = GetTotalTime();

                if (currentTime != null)
                {
                    currentTime.text = NexUtil.GetTimeString(playerCurrentTime);
                }

                if (seekBar != null && !GetIsSeeking() && Math.Abs(playerCurrentTime - lastTimeStamp) > 1000)
                {
                    if (playerCurrentTime > 0f)
                    {
                        seekBar.SetValue(playerCurrentTime / (float)playerTotalTime);
                        lastTimeStamp = playerCurrentTime;
                        seekBar.SetSecondaryValue(Math.Min(1.0f, GetBufferedEnd() / (float)playerTotalTime));
                    }
                    else
                    {
                        seekBar.SetValue(0f);
                        lastTimeStamp = 0;
                        seekBar.SetSecondaryValue(0f);
                    }
                }
            }
        }

        IEnumerator SetTotalTimeCoroutine()
        {
            if (GetTotalTime() == 0)
            {
                yield return new WaitForSecondsRealtime(1f);
            }

            if (totTime != null)
            {
                totTime.text = NexUtil.GetTimeString(GetTotalTime());
            }

            yield return null;
        }

        private void SetTotalTime()
        {
            Log("SetTotalTime() called");

            if (totTime != null)
            {
                if (IsPlayerCreated())
                {
                    totTime.text = NexUtil.GetTimeString(GetTotalTime());
                }
                else
                {
                    totTime.text = "00:00:00";
                }
            }
        }

        private float GetSeekBarValue()
        {
            if (seekBar != null)
            {
                return seekBar.GetValue();
            }

            return 0f;
        }

        private void SetSeekBar(float primaryValue, float secondaryValue)
        {
            seekBar.SetValue(primaryValue);
            seekBar.secondaryValue = secondaryValue;
        }

        private void EnableUIComponent(bool bEnable)
        {
            Log($"EnableUIComponent({bEnable}) called");

            if (IsPlayerCreated())
            {
                if (muteButtonImage != null)
                {
                    muteButtonImage.enabled = bEnable;
                    if (bEnable)
                    {
                        if (muteButtonImage != null)
                        {
                            if (mutePlay)
                            {
                                muteButtonImage.sprite = _volumeOffSprite;
                                volumeButtonImage.sprite = _volumeOffSprite;
                            }
                            else
                            {
                                muteButtonImage.sprite = _volumeOnSprite;
                                volumeButtonImage.sprite = _volumeOnSprite;
                            }
                        }
                    }
                }
                if (volumeSlider != null)
                {
                    volumeSlider.enabled = bEnable;
                    if (bEnable)
                    {
                        volumeSlider.value = volume;
                    }
                }
                if (playPauseImage != null)
                {
                    playPauseImage.enabled = bEnable;
                }
                if (status != null)
                {
                    status.enabled = bEnable;
                }
                if (loopButtonImage != null)
                {
                    loopButtonImage.enabled = bEnable;
                    if (bEnable)
                    {
                        if (loopButtonImage != null)
                        {
                            if (loopPlay)
                            {
                                loopButtonImage.sprite = _loopOnSprite;
                            }
                            else
                            {
                                loopButtonImage.sprite = _loopOffSprite;
                            }
                        }
                    }
                }

                if (bEnable)
                {
                    SetOnSubtitle(bOnSubtitle);
                }
            }
        }

        public void FillUI()
        {
#if UNITY_EDITOR
            // project resources
            string uiFolder = NexPlayerFullFeatSampleFolderRoot.GetRelativePath() + "/NexPlayer/Resources/PlayerUI/";
            pauseSprite = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icon_pause.png");
            playSprite = AssetDatabase.LoadAssetAtPath< Sprite>(uiFolder + "icon_play.png");
            _loopOnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "repeat.png");
            _loopOffSprite = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "no-repeat.png");
            _volumeOnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "audio.png");
            _volumeOffSprite = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "audio-mute.png");
            // scene references
            playPauseImage = GameObject.Find("NexButtons/PlayPause").GetComponent<Image>();
            loopButtonImage = GameObject.Find("NexButtons/Loop").GetComponent<Image>();
            seekBar = GameObject.Find("SeekBar_Canvas/NexSeekBar").GetComponent<NexSeekBar>();
            totTime = GameObject.Find("SeekBar_Canvas/NexSeekBar/TotTime").GetComponent<Text>();
            currentTime = GameObject.Find("SeekBar_Canvas/NexSeekBar/CurrentTime").GetComponent<Text>();
            captionToggle = GameObject.Find("NexButtons/CaptionToggle").GetComponent<Toggle>();
            caption = GameObject.Find("Caption_Canvas/CaptionText").GetComponent<Text>();
            videoSize = GameObject.Find("NexInfo/VideoSize").GetComponent<Text>();
            status = GameObject.Find("NexInfo/Status").GetComponent<Text>();
            volumeButtonImage = GameObject.Find("VolumeOptions").GetComponent<Image>();
            Transform volumePanel = GameObject.Find("NexPanels").transform.GetChild(4);
            volumeSlider = volumePanel.GetChild(0).gameObject.GetComponent<Slider>();
            muteButtonImage = volumePanel.GetChild(1).gameObject.GetComponent<Image>();
            expiredMessage = GameObject.Find("NexControlBar").transform.GetChild(3).gameObject;

            Debug.Log("All UI references found");
#endif
        }
    }
}