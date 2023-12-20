using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using NexUtility;

public static class NxPMenuItems
{
    [MenuItem("NexPlayer/Build Configuration Window", false, 0)]
    public static void ShowBuildConfigurationWindow()
    {
        NexBuildConfigurationWindow.ShowWindow();
    }

}
