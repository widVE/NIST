using System;

namespace NexPlayerSample
{
    public partial class NexPlayer
    {
        public bool downloadingContent = false;

        [Obsolete("Use StreamDownloadController.StreamOfflineSaver() instead")]
        public void StreamOfflineSaver()
        {
            StreamDownloadController.StreamOfflineSaver();
        }

        [Obsolete("Use StreamDownloadController.PlayOffline() instead")]
        public void PlayOffline(String url)
        {
            StreamDownloadController.PlayOffline(url);
        }

        [Obsolete("Use StreamDownloadController.GetPercentageDownload() instead")]
        public int GetPercentageDownload()
        {
            return StreamDownloadController.GetPercentageDownload();
        }
    }
}
