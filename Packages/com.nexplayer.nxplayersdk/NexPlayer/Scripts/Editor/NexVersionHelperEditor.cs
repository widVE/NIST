using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NexUtility
{
    [CustomEditor(typeof(NexVersionHelper))]
    class NexVersionHelperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("2017"))
            {
                NexVersionHelper.ChangeVersion(2017);
            }
            else if (GUILayout.Button("2018"))
            {
                NexVersionHelper.ChangeVersion(2018);
            }
            else if (GUILayout.Button("2019"))
            {
                NexVersionHelper.ChangeVersion(2019);
            }
        }
    }
}