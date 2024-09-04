using NexPlayerAPI;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using NexUtility;
using System.Collections;
using System.Threading.Tasks;

namespace NexPlayerSample
{
    /// <summary>
    /// Sample Code showing how to make a multiview playback.
    /// It inherits from NexPlayerBehaviour and uses NexPlayerRenderController and NexPlayerMultistreamController
    /// to display the video on an Unity object
    /// </summary>

    [RequireComponent(typeof(NexPlayerRenderController), typeof(NexPlayerMultistreamController))]
    public class NexPlayerMultiviewVOD : NexPlayerBehaviour
    {
        [Header("VOD SYNCHRONIZATION")]
        [SerializeField]
        private bool syncAutoplay = true;
        [SerializeField]
        private bool syncLoop = true;
        [SerializeField]
        private List<Transform> renderObjects;
        private Vector3 initialScale;

        private NexPlayerRenderController renderController;

        private int maxBitrate = 0;
        private int minBitrate = 0;
        private int mainScreenIndex = 0;

        private bool initialStateSetted = false;
        private Task currentTask;

        #region NexPlayerBehaviour Overrides
        protected override void Awake()
        {
            base.Awake();

            if (renderObjects != null && renderObjects.Count > 0)
                initialScale = renderObjects[0].localScale;
        }

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
            autoPlay = false;   // autoPlay must be false for VOD synchronization - USE syncAutoplay.
            loopPlay = false;   // loopPlay must be false for VOD synchronization - USE syncLoop.
            supportABR = true;  // Adaptive bitrate. The stream will automatically change its resolution regarding the network connection
            #endregion
        }

        /// <summary>
        /// Set the initial conditions setting one player as main and the others to lowest track
        /// </summary>
        protected override void EventPlaybackStarted()
        {
            base.EventPlaybackStarted();

            if (initialStateSetted) return;

            minBitrate = GetMinBitrate();
            for (int i = 1; i < MultistreamController.GetMultiStreamNumber(); i++)
            {
                ForceBitRate(i, minBitrate);
            }

            maxBitrate = GetMaxBitrate();
            ForceBitRate(mainScreenIndex, maxBitrate);
            if (renderObjects != null && renderObjects.Count > 0)
                renderObjects[mainScreenIndex].transform.localScale = initialScale * 1.2f;

            initialStateSetted = true;
        }

        /// <summary>
        /// Handle loop logic
        /// </summary>
        protected override void EventEndOfContent()
        {
            base.EventEndOfContent();
            StopAllPlayers();

            if (!syncLoop) return;

            for (int i = 0; i < CallbackHandlers.Count; i++)
            {
                CallbackHandlers[i].WasStopped = true;
            }

            currentTask = DoSyncLoop(syncAutoplay);
        }

        /// <summary>
        /// Handle the disposal of the currentTask
        /// </summary>
        protected override void OnDisable()
        {
            if (!currentTask.IsCompleted) currentTask.Dispose();
            currentTask = null;

            base.OnDisable();
        }

        #endregion

        #region Sync Start
        protected override void SetPostInitConfiguration()
        {
            base.SetPostInitConfiguration();

            currentTask = DoSyncStart(syncAutoplay);
        }

        async Task DoSyncStart(bool autoplay)
        {
            while (!CallbackHandlers.All(c => c.IsPaused))
            {
                await Task.Yield();
            }

            if (autoplay)
            {
                ResumeAllSync();
            }
        }
        #endregion

        #region Pause & Resume
        public async void ResumeAllSync()
        {
            await currentTask;

            ResumeAllPlayers();

            int currentControl = MultistreamController.ControlIndex;

            for (int i = 0; i < CallbackHandlers.Count; i++)
            {
                if (CallbackHandlers[i].IsPlaying) continue;

                ChooseControlInstance(i);
                Resume();
            }

            ChooseControlInstance(currentControl);
        }

        public async void PauseAllSync()
        {
            await currentTask;

            PauseAllPlayers();

            int currentControl = MultistreamController.ControlIndex;

            for (int i = 0; i < CallbackHandlers.Count; i++)
            {
                if (CallbackHandlers[i].IsPaused) continue;

                ChooseControlInstance(i);
                Pause();
            }

            ChooseControlInstance(currentControl);
        }
        #endregion

        #region Sync Stop & Loop
        public async void StopAllSync()
        {
            await currentTask;

            StopAllPlayers();

            for (int i = 0; i < CallbackHandlers.Count; i++)
            {
                CallbackHandlers[i].WasStopped = true;
            }

            currentTask = DoSyncLoop(false);
        }


        async Task DoSyncLoop(bool autoPlay)
        {
            while (!CallbackHandlers.All(c => c.IsReadyAfterLoop))
            {
                await Task.Yield();
            }

            ResumeAllPlayers();

            currentTask = DoSyncStart(autoPlay);
        }
        #endregion

        #region Sync Seek
        public void FastSeekAll(bool forward)
        {
            int time = GetCurrentTime();
            time = forward ? time + 10000 : time - 10000;
            SeekAllSync(time);
        }
        public async void SeekAllSync(int time)
        {
            await currentTask;
            SeekAllPlayers(time);
            currentTask = DoSyncSeek();
        }

        async Task DoSyncSeek()
        {
            while (!CallbackHandlers.All(c => c.IsReadyAfterSeek))
            {
                await Task.Yield();
            }

            ResumeAllPlayers();
        }
        #endregion

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
            if (renderObjects != null && renderObjects.Count > 0)
                renderObjects[mainScreenIndex].transform.localScale = initialScale;

            mainScreenIndex = (mainScreenIndex + 1) % MultistreamController.GetMultiStreamNumber();

            ForceBitRate(mainScreenIndex, maxBitrate);
            if (renderObjects != null && renderObjects.Count > 0)
                renderObjects[mainScreenIndex].transform.localScale = initialScale * 1.2f;

            ChooseControlInstance(mainScreenIndex);
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