using NexUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NexPlayerAPI;

namespace NexPlayerSample
{
    public partial class NexPlayer
    {
        #region MULTISTREAM VARIABLES

        public int numberOfStreams;

        // Input
        [Tooltip("This list is for Multi-Stream. Paths to load in order.")]
        public List<string> multiURLPaths;

        [Tooltip("This list is for Multi-Stream.")]
        public List<string> multiKeyServerURL;

        [Tooltip("This list is for Multi-Stream. Paths to load in order.")]
        public List<Text> multiSubTexts;

        // Output
        [Tooltip("This list is for Multi-Stream. RawImages to draw to.")]
        public List<RawImage> multiRawImages;

        [Tooltip("This list is for Multi-Stream. Render Textures to draw to.")]
        public List<RenderTexture> multiRenderTextures;

        [Obsolete]
        public bool multiStreamSync;
        [Obsolete]
        public SynchronizedMultiStreamState multiStreamSetupState = SynchronizedMultiStreamState.MULTISTREAM_SETUP;

        #endregion MULTISTREAM VARIABLES

        #region MULTISTREAM FUNCTIONS

        [Obsolete]
        public void SyncMultiStreams_Update()
        {
            switch (multiStreamSetupState)
            {
                case SynchronizedMultiStreamState.MULTISTREAM_SETUP:
                    MultistreamController.SetMuteMultiStreams(true); // Mute MultiProperty seek
                    if (MultistreamController.AllPlayersAreReady())
                    {
                        MultistreamController.ResumeMultiStreams();
                        multiStreamSetupState = SynchronizedMultiStreamState.MULTISTREAM_FIXING_PLAYERS;
                    }
                    break;

                case SynchronizedMultiStreamState.MULTISTREAM_FIXING_PLAYERS:
                    if (!MultistreamController.AllPlayersAreReady() && !MultistreamController.CheckFailedMultiStreams())
                    {
                        multiStreamSetupState = SynchronizedMultiStreamState.PLAYERS_FIXED;
                    }
                    break;

                case SynchronizedMultiStreamState.PLAYERS_FIXED:
                    MultistreamController.RestartMultiStreamsPlayback();
                    multiStreamSetupState = SynchronizedMultiStreamState.MULTISTREAMS_RESTARTING;
                    break;

                case SynchronizedMultiStreamState.MULTISTREAMS_RESTARTING:
                    if (seekingMultiStreams == 0)
                    {
                        MultistreamController.SetMultiStreamPlayersVisible(true);
                        readyToPlayEvent.Invoke();
                        startToPlayEvent.Invoke();
                        multiStreamSetupState = SynchronizedMultiStreamState.MULTISTREAM_PLAYING;
                    }
                    break;

                case SynchronizedMultiStreamState.MULTISTREAM_PLAYING:
                    break;

                default:
                    break;
            }
        }
        #endregion

        #region OBSOLETE MULTISTREAM FUNCTIONS
        //The following functions are obsolete as they have been migrated
        //into the NexPlayerMultistreamController

        [Obsolete("Use MultistreamController.StartAllPlayers instead")]
        public void StartAllPlayers()
        {
            MultistreamController.StartAllPlayers();
        }

        [Obsolete("Use MultistreamController.ResumeMultiStreams instead")]
        public void ResumeMultiStreams()
        {
            MultistreamController.ResumeMultiStreams();
        }

        [Obsolete("Use MultistreamController.ChooseControlIndex instead")]
        public void ChooseControlIndex(int index)
        {
            MultistreamController.ChooseControlIndex(index);
        }

        [Obsolete("Use MultistreamController.StartChosenPlayer instead")]
        public void StartChosenPlayer()
        {
            MultistreamController.StartChosenPlayer();
        }

        [Obsolete("Use MultistreamController.StartVideo instead")]
        public void StartVideo(string url)
        {
            MultistreamController.StartVideo(url);
        }

        [Obsolete("Use MultistreamController.IsMultiStream instead")]
        public bool IsMultiStream()
        {
            return MultistreamController != null && MultistreamController.IsMultiStream();
        }

        [Obsolete("Use MultistreamController.GetMultiStreamNumber instead")]
        public int GetMultiStreamNumber()
        {
            return MultistreamController.GetMultiStreamNumber();
        }

        [Obsolete("Use MultistreamController.MultiStreamSetProperties instead")]
        public void MultiStreamSetProperties()
        {
            MultistreamController.MultiStreamSetProperties();
        }

        [Obsolete("Use MultistreamController.SetMuteMultiStreams instead")]
        public void SetMuteMultiStreams(bool setMuted)
        {
            MultistreamController.SetMuteMultiStreams(setMuted);
        }

        [Obsolete("Use MultistreamController.SetMultiStreamPlayersVisible instead")]
        public void SetMultiStreamPlayersVisible(bool bVisible)
        {
            MultistreamController.SetMultiStreamPlayersVisible(bVisible);
        }

        [Obsolete("Use MultistreamController.MultiEditHttp instead")]
        public void MultiEditHttp()
        {
            MultistreamController.MultiEditHttp();
        }

        [Obsolete("Use MultistreamController.MultiStreamSetHTTPHeaders instead")]
        public void MultiStreamSetHTTPHeaders()
        {
            MultistreamController.MultiStreamSetHTTPHeaders();
        }

        [Obsolete("Use MultistreamController.AllPlayersAreReady instead")]
        public bool AllPlayersAreReady()
        {
            return MultistreamController.AllPlayersAreReady();
        }

        [Obsolete("Use MultistreamController.RestartMultiStreamsPlayback instead")]
        public void RestartMultiStreamsPlayback()
        {
            MultistreamController.RestartMultiStreamsPlayback();
        }

        [Obsolete("Use MultistreamController.CheckFailedMultiStreams instead")]
        public bool CheckFailedMultiStreams()
        {
            return MultistreamController.CheckFailedMultiStreams();
        }

        [Obsolete("Use MultistreamController.SetMultiStreamsBitrate instead")]
        public void SetMultiStreamsBitrate(int bitrate)
        {
            MultistreamController.SetMultiStreamsBitrate(bitrate);
        }

        #endregion MULTISTREAM FUNCTIONS
    }
}