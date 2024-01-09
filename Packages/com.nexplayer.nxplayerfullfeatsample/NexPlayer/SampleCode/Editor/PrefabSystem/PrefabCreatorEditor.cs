using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PrefabDataCreator))]
public class PrefabCreatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PrefabDataCreator current = (PrefabDataCreator)target;
        DrawDefaultInspector();

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(current.fileName));
        if (GUILayout.Button("Save"))
        {
            current.SaveObject();
        }
        EditorGUI.EndDisabledGroup();
    }
}
