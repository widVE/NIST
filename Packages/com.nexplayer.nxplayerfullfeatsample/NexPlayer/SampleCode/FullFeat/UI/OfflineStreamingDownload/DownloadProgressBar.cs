using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace NexPlayerSample
{
    public class DownloadProgressBar : MonoBehaviour
    {
        public NexPlayer player;
        public Text percentageText;
        public string downloadFileName;
        public UnityEvent downloadDone = new UnityEvent();
        private bool downloading = false;

        public void FindReferences(NexPlayer NxP)
        {
            player = NxP;
            percentageText = transform.GetChild(1).GetComponent<Text>();
        }
        private void OnEnable()
        {
            StartCoroutine(PercentageShow());
        }

        public IEnumerator PercentageShow()
        {
            if (player == null)
                yield break;
            if (player.StreamDownloadController == null)
                yield break;
            int percentageDownloadInt = 0;
            downloading = false;
            do
            {
                if (percentageDownloadInt >= 90)
                    downloading = true;

                percentageDownloadInt = player.StreamDownloadController.GetPercentageDownload();

                if (NexUtility.NexUtil.GetURLExtension(player.URL).Equals("mp4") || NexUtility.NexUtil.GetURLExtension(player.URL).Equals("m4a"))
                {
                    percentageText.text = "This content is not supported for download";
                    yield break;
                }
                else
                {
                    SetDownloadProgress(percentageDownloadInt);
                    yield return true;
                }

            } while ((percentageDownloadInt != 0 && percentageDownloadInt != 100) || downloading != true);

            downloading = false;
            downloadDone.Invoke();
            yield break;
        }

        void SetDownloadProgress(int percentageDownloadInt)
        {
            float percentageDownloadFloat = (float)percentageDownloadInt / 100.0f;
            percentageText.text = $"{percentageDownloadInt}% - {player.URL}";
            GetComponent<Slider>().value = percentageDownloadFloat;
        }
    }
}
