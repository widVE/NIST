using UnityEngine;
using UnityEditor;

namespace NexUtility
{
    public class AutoVersion : ScriptableWizard
    {
        static bool alreadyShown;

        //[InitializeOnLoadMethod]
        private static void InitializeOnLoad(){
#if NEXPLAYER_FULL_FEAT_SAMPLE
            if (!NexVersionHelper.IsAlreadySameVerion(NexPlayerFullFeatSampleFolderRoot.GetFullPath() + "/NexPlayer/Prefabs", GetFormattedUnityVersion()) || !alreadyShown)
            {
                if (!NexVersionHelper.IsAlreadySameVerion(Application.dataPath + "/NexPlayer/Prefabs", GetFormattedUnityVersion()) || !alreadyShown)
                {
                    return;
                }
                CreateWizard();
            }
#endif
        }
        //[MenuItem("NexPlayer/Auto Version Helper")]
        static void CreateWizard()
        {
            alreadyShown = EditorPrefs.GetBool("Autoversion-Shown", false);
            DisplayWizard<AutoVersion>("NexPlayer Version Helper", "Remove Versions", "Keep Versions");
        }

        void OnWizardUpdate()
        {
            maxSize = new Vector2(300, 250);
            minSize = maxSize;

            helpString = "\nINFO: There is NexPlayer compatibility for more than one Unity version. Would you like to Remove unnacesary assets?\n\n\n" +
                "WARNING: If you are a developer who uses more than one Unity version, keeping the extra assets is recomended. " + alreadyShown;
        }

        // Called by the Remove Versions button
        private void OnWizardCreate()
        {
            NexVersionHelper.ChangeVersion(GetFormattedUnityVersion());
            alreadyShown = true;
            Close();
            EditorPrefs.SetBool("Autoversion-Shown", alreadyShown);
        }
        // Called by the Keep Versions button
        private void OnWizardOtherButton()
        {
            alreadyShown = true;
            Close();
            EditorPrefs.SetBool("Autoversion-Shown", alreadyShown);
        }

        private static uint GetFormattedUnityVersion()
        {
            string[] unityVersion = Application.unityVersion.Split('.');
            uint result = 0;

            return uint.TryParse(unityVersion[0], out result) ? result : 0;
        }
    }
}