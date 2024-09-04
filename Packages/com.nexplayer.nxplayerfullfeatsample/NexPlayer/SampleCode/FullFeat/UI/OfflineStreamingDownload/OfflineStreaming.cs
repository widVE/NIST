using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NexUtility;


namespace NexPlayerSample
{
    public class OfflineStreaming : MonoBehaviour
    {

        public NexPlayer player;
        public OfflineDownloadButtons offlineButtons;
        public GameObject downloadPanel = null;
        GameObject cloneDownloadObject = null;
        List<GameObject> currentButtons = new List<GameObject>();


        public GameObject FindReferences(NexPlayer NxP, OfflineDownloadButtons buttons)
        {
            player = NxP;
            offlineButtons = buttons;
            downloadPanel = transform.GetChild(0).gameObject;
            return downloadPanel;
        }


        void Start()
        {
            SetDownloadPanels();
        }

        public void CloseButtonMenu()
        {
            offlineButtons.EnableOfflineList();
        }

        public void SetDownloadPanels()
        {
            //Destroy previous list
            DestroyButtons();

            List<string> fileList = CheckDownloadFiles();

            if (fileList != null)
            {
                for (int i = 0; i < fileList.Count; i++)
                {
                    cloneDownloadObject = Instantiate(downloadPanel, transform);
                    cloneDownloadObject.SetActive(true);

                    Text text = cloneDownloadObject.GetComponentInChildren<Text>();
                    text.text = fileList[i];

                    cloneDownloadObject.GetComponent<NexHolder>().player = player;
                    cloneDownloadObject.GetComponent<NexHolder>().offDload = this;

                    if (Application.platform == RuntimePlatform.IPhonePlayer)
                    {
                        fileList[i] = fileList[i].Replace("|", "/");
                    }
                    cloneDownloadObject.GetComponent<NexHolder>().nexStoreURL = fileList[i];

                    currentButtons.Add(cloneDownloadObject);
                }
            }
            downloadPanel.SetActive(false);
        }

        private void DestroyButtons()
        {
            for (int i = 0; i < currentButtons.Count; i++)
            {
                if (currentButtons[i] != null)
                {
                    Destroy(currentButtons[i]);
                }
            }
        }

        public List<string> CheckDownloadFiles()
        {
            List<string> fileList = new List<string>();
            DirectoryInfo info = null;

            if (Application.platform == RuntimePlatform.Android)
            {
                info = new DirectoryInfo(GetAndroidExternalStoragePath() + "/NexPlayerCache");
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                info = new DirectoryInfo(Application.persistentDataPath + "/STORE/");
            }
            else
            {
                Debug.Log("There's no Offline Streaming in this Platform");
            }
            if (info == null)
                return null;
            if (info.Exists)
            {
                FileInfo[] fileInfo = info.GetFiles();
                DirectoryInfo[] directoryInfo = info.GetDirectories();

                if (Application.platform == RuntimePlatform.Android)
                {
                    foreach (FileInfo file in fileInfo)
                    {
                        if (file.Extension == ".store")
                            fileList.Add(Path.GetFileName(file.FullName));
                    }
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    foreach (DirectoryInfo directory in directoryInfo)
                    {
                        fileInfo = directory.GetFiles();
                        fileList.Add(Path.GetFileName(directory.FullName));
                    }
                }
                else
                {
                    Debug.Log("There's no Offline Streaming in this Platform");
                }
                return fileList;
            }
            return null;
        }

        public string GetAndroidExternalStoragePath()
        {
            string path = "";
            try
            {
                AndroidJavaClass jc = new AndroidJavaClass("android.os.Environment");
                path = jc.CallStatic<AndroidJavaObject>("getExternalStorageDirectory").Call<string>("getAbsolutePath");
                return path;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                return "Nothing!";
            }
        }
    }
}
