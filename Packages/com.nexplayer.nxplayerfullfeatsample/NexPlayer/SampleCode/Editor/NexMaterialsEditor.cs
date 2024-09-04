using UnityEngine;
using UnityEditor;

namespace NexUtility
{
    [CustomEditor(typeof(NexMaterials))]
    public class NexMaterialsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            NexMaterials current = (NexMaterials)target;

            DrawDefaultInspector();

            if (GUILayout.Button("Reset References"))
            {
                current.ResetReferences();
            }
        }
    }
}