using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;


namespace NexPlayerSample
{
    public class OfflineDownload : MonoBehaviour
    {

        public void Download(string url)
        {
            StartCoroutine(DownloadFile(url));
        }

        IEnumerator DownloadFile(string url)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();
#pragma warning disable CS0618 // Type or member is obsolete
                if (www.isNetworkError || www.isHttpError)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    Debug.Log(www.error);
                }
                else
                {
                    string savePath = string.Format("{0}/{1}.mp4", Application.streamingAssetsPath + " /NexPlayer", url);
                    File.WriteAllBytes(savePath, www.downloadHandler.data);
                    Debug.Log("DOWNLOAD PROGESS " + www.downloadProgress);
                }
            }
        }
    }
}