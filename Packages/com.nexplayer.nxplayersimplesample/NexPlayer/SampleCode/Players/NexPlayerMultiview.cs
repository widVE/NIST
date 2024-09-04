//////////////////////////////////////////////////
/// MULTIVIEW TECH IS ONLY AVAIBLE FOR ANDROID ///
//////////////////////////////////////////////////


using NexPlayerAPI;
using UnityEngine;
using System.Linq;

namespace NexPlayerSample
{
    /// <summary>
    /// Sample Code showing how to make a multiview playback.
    /// It inherits from NexPlayerBehaviour and uses NexPlayerRenderController and NexPlayerMultistreamController
    /// to display the video on an Unity object
    /// </summary>

    [RequireComponent(typeof(NexPlayerRenderController), typeof(NexPlayerMultistreamController))]
    public class NexPlayerMultiview : NexPlayerBehaviour
    {
        private NexPlayerRenderController renderController;

        private int maxBitrate = 0;
        private int minBitrate = 0;
        private int mainScreenIndex = 0;

        private bool initialStateSetted = false;

        /// <summary>
        /// Initializing the render and multistream controllers.
        /// </summary>
        protected override void InitControllers()
        {
            base.InitControllers();

            // Multistream
            MultistreamController = GetComponent<NexPlayerMultistreamController>();
            MultistreamController.Init(this);

            // Render
            renderController = GetComponent<NexPlayerRenderController>();
            renderController.Init(this);
        }

        /// <summary>
        /// Configure all the variables that need to be set before the player is opened.
        /// </summary>  
        protected override void SetPreInitConfiguration()
        {
            base.SetPreInitConfiguration();

            EnableVODSyncVariables(true);

            /// In this region all the variables that determine the initial state of the player after opening the content are set
            #region Playback
            autoPlay = true;   // After opening the content, the player will automatically start playing it
            supportABR = true; // Adaptive bitrate. The stream will automatically change its resolution regarding the network connection
            #endregion

            /// Uncomment this section to test Synchronization functionality
            /// Synchronization is meant for live content only
            #region Synchronization
            SynchronizationEnable = true; // Enable Synchronization functionality
            DelayTime = 25000;            // Set the presentation delay in ms
            SpeedUpSyncTime = 350;        // Set the max time out of synchronization to trigger speed up in ms
            JumpSyncTime = 2000;          // Set the max time out of synchronization to trigger seeking in ms
            #endregion
        }


        /// <summary>
        /// Override this method to reset the multiview instance to control when the video begins playing.
        /// </summary>
        protected override void EventPlaybackStarted()
        {
            base.EventPlaybackStarted();

            if (initialStateSetted) return;

            minBitrate = GetMinBitrate();
            for (int i = 1; i < MultistreamController.GetMultiStreamNumber(); i++)
            {
                ForceBitRate(i, minBitrate);
                ShouldTextureBlit(i, true);
            }

            initialStateSetted = true;
        }


        #region Track Change
        /// <summary>
        /// Alternate which multiview instance plays at the highest bitrate and which at the lowest
        /// It scales up the multiview instance playing at the highest bitrate to simplify tracking the status
        /// </summary>
        public void Swap()
        {
            // Only load the maximum and minimum bitrate once
            if (minBitrate == 0) minBitrate = GetMinBitrate();
            if (maxBitrate == 0) maxBitrate = GetMaxBitrate();

            ForceBitRate(mainScreenIndex, minBitrate);
            ShouldTextureBlit(mainScreenIndex, false);

            mainScreenIndex = (mainScreenIndex + 1) % MultistreamController.GetMultiStreamNumber();

            ForceBitRate(mainScreenIndex, maxBitrate);
            ShouldTextureBlit(mainScreenIndex, true);
        }

        /// <summary>
        /// Change streams' resolutions
        /// </summary>
        /// <param name="index">index of the player to force the bitrate</param>
        /// <param name="bitrate">bitrate value </param>
        private void ForceBitRate(int index, int bitrate)
        {
            // Choose player index to control
            MultistreamController.ChooseControlIndex(index);
            // Set bandwidth to specific tracks' bitrate
            SetMaxAndTargetBitrate(bitrate);
        }

        /// <summary>
        /// Get minimum tracks' bitrate
        /// </summary>
        /// <returns></returns>
        private int GetMinBitrate()
        {
            int[] bitrates = GetTrackBitrates();
            int minBitrate = bitrates.Min();
            return minBitrate;
        }

        /// <summary>
        /// Get maximum tracks' bitrate
        /// </summary>
        /// <returns></returns>
        private int GetMaxBitrate()
        {
            int[] bitrates = GetTrackBitrates();
            int maxBitrate = bitrates.Max();
            return maxBitrate;
        }

        /// <summary>
        /// Get the bitrate of all the tracks
        /// </summary>
        /// <returns></returns>
        private int[] GetTrackBitrates()
        {
            NexPlayerTrack[] tracks = GetTracks();
            int tracksCount = tracks.Length;
            int[] bitrates = new int[tracksCount];
            for (int i = 0; i < tracksCount; i++)
            {
                bitrates[i] = tracks[i].bitrate;
            }
            return bitrates;
        }
        #endregion
    }
}