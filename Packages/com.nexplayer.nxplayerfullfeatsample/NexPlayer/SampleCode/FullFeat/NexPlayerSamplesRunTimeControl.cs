#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
using UnityEditor.Events;
#endif
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NexPlayerAPI;
using NexPlayerSample;

namespace NexPlayerSample
{
    public class NexPlayerSamplesRunTimeControl : MonoBehaviour
    {
        [SerializeField] NexPlayerSamplesController sampleController;
        [SerializeField] NexPlayer nexPlayer;
        [SerializeField] GameObject mainPanel;
        [SerializeField] CanvasGroup group;
        [SerializeField] Text SelectedSampleText;

        Transform root;
        Transform canvas;
        GameObject frame;
        GameObject logo;
        GameObject previousButton;
        GameObject nextButton;
        GameObject startButton;
        GameObject backButton;

        private int sample;
        private int lastSample;

        private void Start()
        {
            lastSample = sample = (int)sampleController.activeSample;
            SelectedSampleText.text = ((NEXPLAYER_SAMPLES)sample).ToString();
        }


        #region Methods called From Editor
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        public void FindReferences(NexPlayerSamplesController samples)
        {
            sampleController = samples;

            canvas = transform.GetChild(0);
            mainPanel = canvas.GetChild(0).gameObject;
            group = mainPanel.transform.GetChild(0).GetComponent<CanvasGroup>();
            frame = group.transform.GetChild(1).gameObject;
            logo = group.transform.GetChild(2).gameObject;

            var selection = group.transform.GetChild(3);
            SelectedSampleText = selection.GetComponent<Text>();
            previousButton = selection.GetChild(0).gameObject;
            nextButton = selection.GetChild(1).gameObject;

            startButton = group.transform.GetChild(4).gameObject;
            backButton = canvas.transform.GetChild(1).gameObject;

            SetSprites();
        }
        void SetSprites()
        {
            frame.GetComponent<Image>().sprite = sampleController.sprites.UiSprite;
            logo.GetComponent<Image>().sprite = sampleController.sprites.nexPlayerLogo;
            previousButton.GetComponent<Image>().sprite = sampleController.sprites.previous;
            nextButton.GetComponent<Image>().sprite = sampleController.sprites.next;
            startButton.GetComponent<Image>().sprite = sampleController.sprites.UiSprite;
            backButton.GetComponent<Image>().sprite = sampleController.sprites.arrow;

            group.alpha = 0;
            mainPanel.gameObject.SetActive(false);
            canvas.GetComponent<Canvas>().sortingOrder = 10;
        }
        public void Bind()
        {
            if (nexPlayer == null)
            {
                nexPlayer = FindObjectOfType<NexPlayer>();
                if (nexPlayer == null)
                {
                    Debug.LogError("There is not a NexPlayer instance in the Scene!");
                    return;
                }
            }
            AddPersistantListeners();
        }
        void AddPersistantListeners()
        {
            ClearPersistentListeners();
            UnityEventTools.AddPersistentListener(backButton.GetComponent<Button>().onClick, OpenMenu);
            UnityEventTools.AddPersistentListener(previousButton.GetComponent<Button>().onClick, PreviousSample);
            UnityEventTools.AddPersistentListener(nextButton.GetComponent<Button>().onClick, NextSample);
            UnityEventTools.AddPersistentListener(startButton.GetComponent<Button>().onClick, SelectSample);
        }
        void ClearPersistentListeners()
        {
            UnityEventTools.RemovePersistentListener(backButton.GetComponent<Button>().onClick, OpenMenu);
            UnityEventTools.RemovePersistentListener(previousButton.GetComponent<Button>().onClick, PreviousSample);
            UnityEventTools.RemovePersistentListener(nextButton.GetComponent<Button>().onClick, NextSample);
            UnityEventTools.RemovePersistentListener(startButton.GetComponent<Button>().onClick, SelectSample);
        }
#endif
        #endregion

        #region Event Callbacks
        private void OpenMenu()
        {

            if (mainPanel.activeInHierarchy)
            {
                if (nexPlayer.GetPlayerStatus() == NexPlayerStatus.NEXPLAYER_STATUS_PAUSE || nexPlayer.GetPlayerStatus() == NexPlayerStatus.NEXPLAYER_STATUS_STOP)
                    nexPlayer.TogglePlayPause();

                group.alpha = 0;
                mainPanel.gameObject.SetActive(false);
            }
            else
            {
                if (nexPlayer.GetPlayerStatus() == NexPlayerStatus.NEXPLAYER_STATUS_PLAY)
                    nexPlayer.TogglePlayPause();

                mainPanel.gameObject.SetActive(true);
                StartCoroutine(FadeIn());
            }
        }
        public void NextSample()
        {
            sample++;
            if (sample > (int)NEXPLAYER_SAMPLES.NexPlayer360)
                sample = (int)NEXPLAYER_SAMPLES.RawImage;

            SelectedSampleText.text = ((NEXPLAYER_SAMPLES)sample).ToString();

            if (nexPlayer.GetPlayerStatus() != NexPlayerStatus.NEXPLAYER_STATUS_STOP)
                nexPlayer.Stop();
        }
        public void PreviousSample()
        {
            sample--;
            if (sample < (int)NEXPLAYER_SAMPLES.RawImage)
                sample = (int)NEXPLAYER_SAMPLES.NexPlayer360;

            SelectedSampleText.text = ((NEXPLAYER_SAMPLES)sample).ToString();

            if (nexPlayer.GetPlayerStatus() != NexPlayerStatus.NEXPLAYER_STATUS_STOP)
                nexPlayer.Stop();
        }
        private void SelectSample()
        {
            if (lastSample != sample)
            {
                SelectSample(sample);
            }
            else
            {
                nexPlayer.Seek(0);
                nexPlayer.TogglePlayPause();
                Camera.main.transform.rotation = Quaternion.identity;
                if (GameObject.Find("MaxScreenCanvas"))
                    if (GameObject.Find("MaxScreenCanvas").transform.GetChild(0).GetComponent<RawImage>().enabled)
                        nexPlayer.MaximizeScreen();

                OpenMenu();
                lastSample = sample;
            }

        }
        #endregion

        IEnumerator FadeIn()
        {
            while (group.alpha < 0.9f)
            {
                group.alpha += Time.deltaTime;
                yield return null;
            }
            group.alpha = 1;
        }

        private void SelectSample(int _selectedTrack)
        {
            ActivateSample(_selectedTrack);

            switch ((NEXPLAYER_SAMPLES)_selectedTrack)
            {
                case NEXPLAYER_SAMPLES.RawImage:

                    nexPlayer.URL = "https://d7wce5shv28x4.cloudfront.net/sample_streams/sintel_subtitles_hls/master.m3u8";
                    nexPlayer.rawImage = root.GetComponentInChildren<RawImage>();
                    ChangeRenderMode(3);
                    break;
                case NEXPLAYER_SAMPLES.RenderTexture:
                case NEXPLAYER_SAMPLES.Transparency:
                case NEXPLAYER_SAMPLES.VideoSpread:

                    nexPlayer.URL = "https://d7wce5shv28x4.cloudfront.net/sample_streams/sintel_subtitles_hls/master.m3u8";
                    nexPlayer.renderTexture = sampleController.materials.renderTextures[0];
                    ChangeRenderMode(1);
                    break;
                case NEXPLAYER_SAMPLES.MaterialOverride:

                    nexPlayer.URL = "https://d7wce5shv28x4.cloudfront.net/sample_streams/sintel_subtitles_hls/master.m3u8";
                    nexPlayer.renderTarget = root.GetChild(0).GetComponent<MeshRenderer>();
                    ChangeRenderMode(2);
                    break;
                case NEXPLAYER_SAMPLES.MultipleRenderers:

                    nexPlayer.URL = "https://d7wce5shv28x4.cloudfront.net/sample_streams/sintel_subtitles_hls/master.m3u8";
                    nexPlayer.renderTarget = root.GetChild(0).GetComponent<MeshRenderer>();
                    ChangeRenderMode(2);
                    break;
                case NEXPLAYER_SAMPLES.ChangeRenderMode:

                    nexPlayer.URL = "https://d7wce5shv28x4.cloudfront.net/sample_streams/sintel_subtitles_hls/master.m3u8";
                    nexPlayer.rawImage = root.GetComponentInChildren<RawImage>();
                    nexPlayer.renderTexture = sampleController.materials.renderTextures[0];
                    nexPlayer.renderTextureRenderer = root.GetChild(0).GetComponent<MeshRenderer>();
                    nexPlayer.renderTarget = root.GetChild(1).GetComponent<MeshRenderer>();
                    ChangeRenderMode(3);
                    break;
                case NEXPLAYER_SAMPLES.NexPlayer360:

                    nexPlayer.URL = "https://s3.eu-west-3.amazonaws.com/content.nexplayersdk.com/360test/NYCTimewarp/HLS-360/master.m3u8";
                    nexPlayer.renderTarget = root.GetChild(0).GetComponent<MeshRenderer>();
                    ChangeRenderMode(2);
                    break;
            }

            Camera.main.transform.rotation = Quaternion.identity;
            var maxScreenCanavs = GameObject.Find("MaxScreenCanvas");
            if (maxScreenCanavs != null && maxScreenCanavs.transform.GetChild(0).GetComponent<RawImage>().enabled)
            {
                nexPlayer.MaximizeScreen();
            }

            OpenMenu();
            nexPlayer.Close();
            nexPlayer.Open();
            lastSample = sample;
        }
        void ActivateSample(int index)
        {
            root = sampleController.ActivateSample(index);
        }

        void ChangeRenderMode(int mode)
        {
            nexPlayer.ChangeRenderMode(mode);
        }
    }
}
