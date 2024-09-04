using UnityEngine;
using NexPlayerAPI;
using UnityEngine.UI;


namespace NexPlayerSample
{
    /// <summary>
    /// Sample Code showing how to make a list of of contents.
    /// It inherits from NexPlayerBehaviour and uses a NexPlayerRenderController to display the video on an Unity object.
    /// Widevine protected content can also be included in the list, for that, uncomment the Widevine region.
    /// </summary>
    [RequireComponent(typeof(NexPlayerRenderController), typeof(NexPlayerMediaListController))]
    public class NexPlayerList : NexPlayerBehaviour
    {
        private NexPlayerRenderController renderController;
        private NexPlayerMediaListController mediaListController;

        protected override void InitControllers()
        {
            base.InitControllers();

            //Render
            renderController = GetComponent<NexPlayerRenderController>();

            renderController.Init(this);

            //Media List
            mediaListController = GetComponent<NexPlayerMediaListController>();
            mediaListController.Init(this);
            URL = mediaListController.GetCurrentMedia();
            keyServerURI = mediaListController.GetCurrentMediaKey();
        }

        protected override void SetPreInitConfiguration()
        {
            base.SetPreInitConfiguration();

            isLiveStream = false;

            /// In this region all the Widevine related variables are set.
            /// Widevine content URL and KeyServer have already been set in the NexPlayerMediaListController
            #region Widevine Playback
            licenseRequestTimeout = 0; // no timeout
            #endregion

            /// In this region the fields for telling how many logs the player outputs are set.
            /// This case is configured to output all the logs
            #region Debug
            SetLogLevelForDebugging();  // Enabling debug logs.
            #endregion

        }

        protected override void EventPlaybackStarted() {
            base.EventPlaybackStarted();
            CheckVolume();
            PrintVersionInfo();
        }
    }
}
