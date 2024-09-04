using UnityEditor;
using UnityEngine;

namespace NexPlayerSample
{
    [CustomEditor(typeof(NexUIController))]
    public class NexUIEditor : Editor
    {
        bool showDefaultInspector;
        SerializedProperty _nexPlayerProp;


        private void OnEnable()
        {
            _nexPlayerProp = serializedObject.FindProperty("nexPlayer");
        }
        public override void OnInspectorGUI()
        {
            NexUIController currentUI = (NexUIController)target;

            EditorGUILayout.Space();

            showDefaultInspector = EditorGUILayout.Toggle("Show Reference", showDefaultInspector);
            if (showDefaultInspector)
            {
                DrawDefaultInspector();
            }
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (!showDefaultInspector)
            {
                EditorGUILayout.LabelField("NexPlayer:", GUILayout.Width(70));
                EditorGUILayout.PropertyField(_nexPlayerProp, GUIContent.none, GUILayout.Width(200));
            }

            if (GUILayout.Button("Bind to NexPlayer", GUILayout.Width(150)))
            {
                currentUI.FixPrefab();
                currentUI.Bind();
                currentUI.FillNexPlayerUIReferences();
                var runtime = GameObject.FindObjectOfType<NexPlayerSamplesRunTimeControl>();
                if (runtime != null)
                {
                    runtime.Bind();
                }
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}