using System;
using UnityEngine;
using UnityEngine.UI;
using NexPlayerAPI;
using NexUtility;

namespace NexPlayerSample
{
    /// <summary>
    /// Sample code showing how to access different audio and caption tracks.
    /// Changing the audio and caption tracks is only supported in Android and iOS.
    /// </summary>
    [RequireComponent(typeof(NexPlayerRenderController))]
    public class NexPlayerMultipleLanguages : NexPlayerBehaviour
    {
        #region CC & Languages
        [Header("Captions")]
        [Tooltip("Subtitle URL is used to load subtitle")]
        [SerializeField] string subtitleURL = null;


        NexSubtitleElement subtitleElement;      // variable for holding one subtitle element at a time
        NexPlayerCaptionStream[] captionStreams; // variable for storing the information about the closed captions tracks present inside the manifest
        NexPlayerAudioStream[] audioStreams;     // variable for storing the information about the audio tracks present inside the manifest

        public Text captionLabel; // Unity UI's Text element for display

        protected override void EventTextRender()
        {
            base.EventTextRender();

            subtitleElement = GetCurrentSubtitleElement(); // get information about the string that must be shown 

            if (subtitleElement.caption != null)
            {
                captionLabel.text = subtitleElement.caption; // show the string by assigning it to a Unity UI text element
            }
        }

        protected override void EventPlaybackStarted()
        {
            base.EventPlaybackStarted();
            CheckVolume();

            // At this event the player has finished reading the manifest and is safe to ask for the CC and audio tracks
            captionStreams = GetCaptionStreamList();
            audioStreams = GetAudioStreamList();
        }

        protected override string GetSubtitleURL()
        {
            base.GetSubtitleURL();

            string subtitle = null;
            if (subtitleURL != null && !String.IsNullOrEmpty(subtitleURL))
            {
                subtitle = NexUtil.GetFullUri((int)GetContentType(), subtitleURL);
            }

            return subtitle;
        }

        /// <summary>
        /// Public method wraping the SDK one to call it using index. It sets the current audio to the audio track with the given index.
        /// </summary>
        /// <param name="index">Index of the tracks stored in the audioStreams variable</param>
        public void SetAudioStream(int index)
        {
            if (audioStreams == null)
            {
                Log("Audio Streams is null");
                return;
            }

            if (index < 0 || index >= audioStreams.Length)
            {
                Log($"Requested index: {index}, is out of bounds");
                return;
            }

            SetAudioStream(audioStreams[index]);
        }

        /// <summary>
        /// Public method wraping the SDK one to call it using index. It sets the current closed caption track to the one with the given index.
        /// </summary>
        /// <param name="index">Index of the tracks stored in the captionStreams variable</param>
        public void SetCaptionStream(int index)
        {
            if (captionStreams == null)
            {
                Log("Caption Streams is null");
                return;
            }

            if (index < 0 || index >= captionStreams.Length)
            {
                Log($"Requested index: {index}, is out of bounds");
                return;
            }

            SetCaptionStream(captionStreams[index]);
        }
        #endregion

        private NexPlayerRenderController renderController;

        protected override void InitControllers()
        {
            base.InitControllers();

            //Render
            renderController = GetComponent<NexPlayerRenderController>();
            renderController.Init(this);
        }

        protected override void SetPreInitConfiguration()
        {
            base.SetPreInitConfiguration();

            URL = "https://d7wce5shv28x4.cloudfront.net/sample_streams/sintel_subtitles_hls/master.m3u8";
            isLiveStream = false;
            SetLogLevelForDebugging();
        }
    }
}
