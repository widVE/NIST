using System;
using UnityEngine;
using System.IO;
using NexUtility;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

public class PostBuildUtil
{
#if UNITY_EDITOR
    [PostProcessBuild(0)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        string path = pathToBuiltProject.Replace(".exe", "");

        if (target == BuildTarget.StandaloneWindows64)
        {
            string baseSourcePath = NexPlayerFolderRoot.GetFullPath() + "/NexPlayer/Plugins/x64/";
            string x64TargetPath = path + "_Data/Plugins/";

            if (!Directory.Exists(baseSourcePath))
            {
                throw new Exception("OnPostprocessBuild: " + baseSourcePath + " could not be found to support Windows. Please check that you have correctly imported the NexPlayer for Unity SDK package");
            }

            // manually copy files (skipping .meta), deleting the duplicates in /x86_64
            string[] filePaths = Directory.GetFiles(baseSourcePath);
            foreach (string filePath in filePaths)
            {
                if (Path.GetExtension(filePath) != ".meta")
                {
                    string filename = Path.GetFileName(filePath);
                    if (File.Exists(x64TargetPath + filename))
                    {
                        File.Delete(x64TargetPath + filename); // delete existing dlls
                    }

                    FileUtil.CopyFileOrDirectory(filePath, x64TargetPath + filename); // copy new dlls
                }
            }
        }
    }
#endif

}