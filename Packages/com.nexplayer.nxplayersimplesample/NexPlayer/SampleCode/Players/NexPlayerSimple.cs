using UnityEngine;
using NexPlayerAPI;
using UnityEngine.UI;

namespace NexPlayerSample
{
    /// <summary>
    /// Sample Code showing how to make a basic playback.
    /// It inherits from NexPlayerBehaviour and uses a NexPlayerRenderController to display the video on an Unity object.
    /// It also plays Widevine protected content, for that, uncomment the Widevine region.
    /// </summary>
    [RequireComponent(typeof(NexPlayerRenderController))] 
    public class NexPlayerSimple : NexPlayerBehaviour
    {
        private NexPlayerRenderController renderController;

        protected override void Reset()
        {
            base.Reset();

            autoPlay = true;
            loopPlay = false;
            mutePlay = false;
            volume = 0.5f;
        }

        /// <summary>
        /// Initializing the render controller to see the video.
        /// </summary>
        protected override void InitControllers()
        {
            base.InitControllers();

            renderController = GetComponent<NexPlayerRenderController>();

            /// In this region the render mode is set from script
            /// Uncomment the content of the region to set the Render Mode from script
            #region Render Mode
            /*
            NexRenderMode targetRenderMode = NexRenderMode.RawImage; // Change the sample's render mode

            switch (targetRenderMode)
            {
                case NexRenderMode.RawImage:
                    RawImage targetRawImage = FindObjectOfType<RawImage>(); // one of many ways to obtain a reference to the desired Raw Image
                    renderController.rawImage = targetRawImage; // Set the target Raw Image
                    renderController.enableVerticalFlip = true;
                    renderController.StartingRenderMode = NexRenderMode.RawImage; // Set render mode to Raw Image
                    break;
                case NexRenderMode.RenderTexture:
                    RenderTexture targetRenderTexture = Resources.Load<RenderTexture>("PathToAssetInsideResources"); // one of many ways to obtain a reference to the desired Render Texture
                    renderController.renderTexture = targetRenderTexture; // Set the target Render Texture
                    renderController.StartingRenderMode = NexRenderMode.RenderTexture; // Set render mode to Render Texture
                    break;
                case NexRenderMode.MaterialOverride:
                    Renderer targetMaterialOverride = FindObjectOfType<Renderer>(); // one of many ways to obtain a reference to the desired Material Override
                    renderController.materialOverride = targetMaterialOverride; // Set the target Material Override
                    renderController.StartingRenderMode = NexRenderMode.MaterialOverride; // Set render mode to Material Override
                    break;
                default:
                    break;
            }
            */
            #endregion

            renderController.Init(this);
        }


        /// <summary>
        /// Configure all the variables that need to be set before the player is opened
        /// </summary>
        protected override void SetPreInitConfiguration()
        {
            base.SetPreInitConfiguration();

            /// In this region all the media content variables are set for non DRM content.
            /// Uncomment the content of the region and an example content will play.
            #region Basic Playback
            /*
            URL = "https://bitdash-a.akamaihd.net/content/MI201109210084_1/m3u8s/f08e80da-bf1d-4e3d-8899-f0f6155f6efa.m3u8";
            isLiveStream = false;
            
            // Widevine variables set to default (nothing)
            keyServerURI = string.Empty;
            licenseRequestTimeout = 0;
            ClearWidevineHeaders();
           */
            #endregion

            /// In this region all the Widevine related variables are set.
            /// Uncomment the content of the region and an example content will play.
            /// The example content is Widevine protected and requires one extra key-value pair.
            #region Widevine Playback
            /*
            URL = "https://media.axprod.net/TestVectors/v9-MultiFormat/Encrypted_Cenc/Manifest_1080p.mpd"; // Widevine encrypted content

            // Widevine variables for the specific content above
            keyServerURI = "https://drm-widevine-licensing.axtest.net/AcquireLicense";
            licenseRequestTimeout = 0; // no timeout 
            string[] wvHeaderKeys = new string[] { "X-AxDRM-Message" };
            string[] wvheaderValues = new string[] { "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ2ZXJzaW9uIjoxLCJjb21fa2V5X2lkIjoiNjllNTQwODgtZTllMC00NTMwLThjMWEtMWViNmRjZDBkMTRlIiwibWVzc2FnZSI6eyJ0eXBlIjoiZW50aXRsZW1lbnRfbWVzc2FnZSIsImtleXMiOlt7ImlkIjoiZjhjODBjMjUtNjkwZi00NzM2LTgxMzItNDMwZTVjNjk5NGNlIiwiZW5jcnlwdGVkX2tleSI6ImlYcTQ5Wjg5czhkQ2owam0yQTdYelE9PSJ9XSwicGxheXJlYWR5Ijp7Im1pbl9hcHBfc2VjdXJpdHlfbGV2ZWwiOjE1MCwicGxheV9lbmFibGVycyI6WyI3ODY2MjdEOC1DMkE2LTQ0QkUtOEY4OC0wOEFFMjU1QjAxQTciXX19fQ.hRBkpC-9i6nXUmxTPLEfb16MAwh5LhxUZ2b8z1o1e5g" };
            SetWidevineHeaders(wvHeaderKeys, wvheaderValues);
            */
            #endregion


            /// Uncomment this region to test Synchronization functionality
            /// Synchronization is meant for live content only.
            #region Synchronization
            /*
            URL = "https://ccf3786b925ee51c.mediapackage.us-east-1.amazonaws.com/out/v1/5e32dbc2dd0048d9818826cc2c0d63cb/index.mpd"; // Live DASH content
            SynchronizationEnable = true;  // enable Synchronization functionality
            DelayTime = 1000;              // set the presentation delay to one second
            SpeedUpSyncTime = 500;         // set the max time out of synchronization to trigger speed up 
            JumpSyncTime = 1000;           // set the max time out of synchronization to trigger seeking
            */
            #endregion

            //////// COMMON ////////

            /// In this region all the variables that determine the initial state of the player after opening the content are set.
            /// Uncomment the content of the region to change this settings from code.
            #region Playback settings
            /*
            autoPlay = true;  // After opening the content the player will automatically start playing it.
            loopPlay = false; // The player will stop when it reaches the end of the content.
            mutePlay = false; // The player starts with sound enabled.
            volumeSize = 1f; // The player starts with maximum volume.
            */
            #endregion

            /// In this region the field to activate printing detailed debug logs is set.
            /// Uncomment the content of the region to output all the logs
            #region Debug
            /*
            debugLogs = true;  // enabling debug logs.
            */
            #endregion

            //////// END COMMON ////////
        }

        /// <summary>
        /// This event occurs whenever the player has opened a video content.
        /// </summary>
        protected override void EventInitComplete()
        {
            base.EventInitComplete();

            UpdateContentInfo();

            SetPlayerStatus("Opened");

        }

        /// <summary>
        /// This event occurs whenever the player has started the playback or has resumed.
        /// </summary>
        protected override void EventPlaybackStarted()
        {
            base.EventPlaybackStarted();
            CheckVolume();
            PrintVersionInfo();
        }

        /// In this region different information is retrieved from the player.
        /// Uncomment the content of the region to fetch informatiom from the player once per second.
        #region Player information
        /*
        // Override EventOnTime to execute the following code once per second.
        // This is useful for UI.
        protected override void EventOnTime()
        {
            base.EventOnTime();

            NexPlayerStatus currentStatus = GetPlayerStatus();
            int currentTime = GetCurrentTime();
            NexRenderMode currentMode = GetRenderMode();
        }
        */
        #endregion
    }
}