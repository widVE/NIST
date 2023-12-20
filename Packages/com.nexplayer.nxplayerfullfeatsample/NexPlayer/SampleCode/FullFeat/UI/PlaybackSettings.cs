using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NexPlayerAPI;
using System;
using NexUtility;


namespace NexPlayerSample
{
    [Obsolete("This class has been deprecated and will be removed in the upcoming releases")]
    public class PlaybackSettings : MonoBehaviour
    {
        public InputField url;
        public InputField subtitleUrl;
        public InputField keyServerUrl;
        public Dropdown renderMode_dropdown;

        public AdditionalValueManager drmHeaderManager;
        public AdditionalValueManager httpHeaderManager;

        public Toggle autoPlayToggle;
        public Toggle loopPlayToggle;
        public Toggle mutePlayToggle;
        public Toggle thumbnailToggle;
        public Toggle drmCacheToggle;

        private string[] assetArray;
        private string[] subtitleAssetArray;

        void Start()
        {
            url.gameObject.SetActive(true);

            assetArray = StreamingAssetFileHelper.GetAssetVideoFiles();
            if (assetArray != null)
            {
                List<string> assetList = new List<string>(assetArray);
            }
            else
            {
                Debug.Log("There is no streamingasset video files");
            }

            subtitleAssetArray = StreamingAssetFileHelper.GetAssetSubtitleFiles();
            if (subtitleAssetArray != null)
            {
                List<string> subtitleAssetList = new List<string>(subtitleAssetArray);
            }
            else
            {
                Debug.Log("There is no streamingasset subtitle files");
            }
        }

        public void ChangePlayMode(Dropdown mode)
        {
            Debug.Log("play mode value : " + mode.value);
            switch (mode.value)
            {
                //Streaming Play
                case 0:
                    url.gameObject.SetActive(true);
                    break;
                //Asset Play
                case 1:
                    url.gameObject.SetActive(false);
                    break;
                //Local Play
                case 2:
                    break;
                default:
                    Debug.Log("Not support mode");
                    break;
            }
        }

        public void TogglePlaybackScene()
        {
            int renderModeIndex = renderMode_dropdown.value;

            switch (renderModeIndex)
            {
                case 0:
                    UnityEngine.SceneManagement.SceneManager.LoadScene("NexPlayer_RawImage_Sample");
                    break;

                case 1:
                    UnityEngine.SceneManagement.SceneManager.LoadScene("NexPlayer_MaterialOverride_Sample");
                    break;

                case 2:
                    UnityEngine.SceneManagement.SceneManager.LoadScene("NexPlayer_RenderTexture_Sample");
                    break;
            }

            NexPlayer.sharedURL = url.text;
            NexPlayer.sharedSubtitleURL = subtitleUrl.text;

            NexPlayer.sharedPlayType = 0;
            NexPlayer.sharedKeyServerURL = keyServerUrl.text;

            Dictionary<string, string> drmHeaderDic = drmHeaderManager.GetAdditionalDRMHeaders();
            Dictionary<string, string> httpHeaderDic = httpHeaderManager.GetAdditionalHTTPHeaders();

            NexPlayer.sharedAutoPlay = autoPlayToggle.isOn;
            NexPlayer.sharedLoopPlay = loopPlayToggle.isOn;
            NexPlayer.sharedMutePlay = mutePlayToggle.isOn;
            NexPlayer.sharedThumbnail = thumbnailToggle.isOn;
            NexPlayer.sharedDrmCache = drmCacheToggle.isOn;
        }

        public void Cancel()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}