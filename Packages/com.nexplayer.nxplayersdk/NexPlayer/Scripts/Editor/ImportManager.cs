using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Xml;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace NexUtility
{
    public class ImportManager : MonoBehaviour
    {

        [MenuItem("NexPlayer / Import Tool/ Update Package")]
        static async void DeleteSomething()
        {

            string packagePath = EditorUtility.OpenFilePanel("Select the NexPlayer Unity Package", "", "unitypackage");
            if (packagePath == "")
                return;

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogWarning("Internet connection is required to update the package");
                return;
            }

            string json = await GetJson();
     
            DirectoriesJson directories = new DirectoriesJson();
            directories = JsonUtility.FromJson<DirectoriesJson>(json);
      
            for (int i = 0; i < directories.directories.Count; i++)
            {
                if (File.Exists(directories.directories[i])) 
                {
                    FileUtil.DeleteFileOrDirectory(directories.directories[i]);
                }
           
            }
            string folderRoot = NexPlayerFolderRoot.GetFullPath();
#if NEXPLAYER_FULL_FEAT_SAMPLE
            string folderFullFeatSampleRoot = NexPlayerFullFeatSampleFolderRoot.GetFullPath();
#endif
#if NEXPLAYER_SIMPLE_SAMPLE
            string folderSimpleSampleRoot = NexPlayerSimpleSampleFolderRoot.GetFullPath();
#endif
            string[] foldersToCheck = { folderRoot
#if NEXPLAYER_FULL_FEAT_SAMPLE 
                ,folderFullFeatSampleRoot
#endif
#if NEXPLAYER_SIMPLE_SAMPLE
                ,folderSimpleSampleRoot
#endif
            };
            for (int i = 0; i < foldersToCheck.Length; i++)
            {
                if (Directory.Exists(foldersToCheck[i])) 
                {
                    DeleteFolder(foldersToCheck[i]);
                }
            
            }

            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh();
        }

        [System.Serializable]
        private class DirectoriesJson
        {
            public List<string> directories = new List<string>();
        }

        private static void DeleteFolder(string path)
        {
            Dictionary<string, bool> directory;
            bool deletedSomething = true;
            var directoryInfo = new DirectoryInfo(path);
            while (deletedSomething)
            {
                directory = new Dictionary<string, bool>();
                deletedSomething = false;
                foreach (var subDirectory in directoryInfo.GetDirectories("*.*", SearchOption.AllDirectories))
                {
                    if (subDirectory.Exists)
                    {

                        List<DirectoryInfo> d = subDirectory.GetDirectories("*.*", SearchOption.TopDirectoryOnly).ToList();
                        List<FileInfo> f = subDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly).ToList();
                        directory.Add(subDirectory.FullName, !f.Any(x => !x.Name.Contains(".meta") && d.Count == 0));
                    }
                }
                foreach (var subDirectory in directory)
                {
                    if (subDirectory.Value)
                    {
                        FileUtil.DeleteFileOrDirectory(subDirectory.Key);
                        FileUtil.DeleteFileOrDirectory(subDirectory.Key + ".meta");
                        deletedSomething = true;
                    }
                }
            }
        }

        public static async Task<string> GetJson()
        {
            string url = "https://nexplayer-unity.s3.eu-west-1.amazonaws.com/JsonPackageFiles/Directories.json";
            UnityWebRequest www = UnityWebRequest.Get(url);
            string json = string.Empty;

            www.SetRequestHeader("Content-Type", "application/json");

            var operation = www.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

#if UNITY_2018 || UNITY_2019
        json = www.downloadHandler.text;
#else

            if (www.result == UnityWebRequest.Result.Success)
            {
                json = www.downloadHandler.text;
            }
            else
            {
                Debug.LogError($"Failed : {www.error}");
            }

#endif
            return json;
        }
    }
}