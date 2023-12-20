using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NexUtility
{
    public class NexPlayerFullFeatSampleFolderRoot : ScriptableObject
    {
        /// <summary>
        /// Editor only. Get the first full path to the folder containing <see cref="NexPlayerFullFeatSampleFolderRoot"/>.
        /// </summary>
        /// <returns>Path to the root NexPlayer folder.</returns>
        public static string GetFullPath()
        {
            string relativePath = GetRelativePath();
            string fullPath = string.IsNullOrEmpty(relativePath) ? string.Empty : System.IO.Path.GetFullPath(relativePath);
            return fullPath;
        }

        /// <summary>
        /// Editor only. Get the first relative path to the folder containing <see cref="NexPlayerFullFeatSampleFolderRoot"/>.
        /// </summary>
        /// <returns>Path to the root NexPlayer folder.</returns>
        public static string GetRelativePath()
        {
            string relativePath = string.Empty;

#if UNITY_EDITOR
            string[] folderRootGuids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(NexPlayerFullFeatSampleFolderRoot)}");

            foreach (string guid in folderRootGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);

                // remove the file name from the path
                relativePath = path.Substring(0, path.Length - ("/".Length + System.IO.Path.GetFileName(path).Length));
                break;
            }
#endif

            return relativePath;
        }
    }

}
