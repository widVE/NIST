using System.Collections;
using UnityEngine;
using UnityEngine.Android;

namespace NexPlayerSample
{
    public class OfflineDownloadButtons : MonoBehaviour
    {
        [SerializeField] NexPlayer player;
        public GameObject downloadingPanel;
        public GameObject offlineList;
        public GameObject progressBar;
        public GameObject downloadButton;
        public OfflineStreaming downloadContent;
        static bool downloaded;
        [SerializeField] NexUIController UI;

        public void FindReferences(NexPlayer NxP, GameObject downloading, GameObject offList, GameObject bar, OfflineStreaming content, GameObject button, NexUIController ui)
        {
            player = NxP;
            downloadingPanel = downloading;
            offlineList = offList;
            progressBar = bar;
            downloadButton = button;
            downloadContent = content;
            downloaded = false;
            UI = ui;
        }

        public void EnableOfflineList()
        {
            if (offlineList.activeInHierarchy)
            {
                offlineList.SetActive(false);
            }
            else
            {
                UI.HideAllPanels();
                offlineList.SetActive(true);
                downloadContent.SetDownloadPanels();
                downloadingPanel.SetActive(false);
            }
        }

        public void EnableDownloading()
        {
            if (downloadingPanel.activeInHierarchy)
            {
                downloadingPanel.SetActive(false);
            }
            else
            {
                UI.HideAllPanels();
                downloadingPanel.SetActive(true);
                offlineList.SetActive(false);
            }
        }

        public void HandleDownloadDone()
        {
            StartCoroutine(DownloadDone());
        }

        IEnumerator DownloadDone()
        {
            yield return null;
            downloaded = true;
            EnableDownloading();
            EnableOfflineList();
        }

        public void DownloadVideo()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                player.SetPlayerStatus("Download Failed");
                player.Log("Download Failed. No internet access.");
                return;
            }

            if(Application.platform == RuntimePlatform.Android && !CanDownloadStream())
            {
                player.SetPlayerStatus("Download Failed");
                player.Log("Android Download Failed. No write permissions.");
                return;
            }

            progressBar.SetActive(!downloaded);
            downloadingPanel.transform.GetChild(1).gameObject.SetActive(downloaded);

            if (player.playType == 0)
            {
                if (!downloaded)
                {
                    if (player.URL.Contains("mp4"))
                    {
                        OfflineDownload();
                    }
                    else
                    {
                        player.StreamDownloadController.StreamOfflineSaver();
                    }
                }

                EnableDownloading();
            }
        }

        private bool CanDownloadStream()
        {
            //Check for external storage permissions
            if (Application.platform == RuntimePlatform.Android)
            {
                return Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite);
            }
            else
            {
                return true;
            }
        }


        public void DisableButtons()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WSAPlayerX64 || Application.platform == RuntimePlatform.WSAPlayerARM)
            {
                if (player != null && !player.URL.Contains(".mp4"))
                {
                    gameObject.SetActive(false);
                }
            }

            if (player.playType != 0)
            {
                downloadButton.SetActive(false);
            }
        }

        public void OfflineDownload()
        {
            player.downloadingContent = true;
        }
    }
}
