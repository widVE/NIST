using UnityEditor;
using UnityEngine;

namespace NexPlayerSample
{
    [CustomEditor(typeof(NexPlayerSamplesController))]
    public class NexPlayerSamplesEditor : Editor
    {
        SerializedProperty _activeSampleProp;
        SerializedProperty _nexPlayerProp;
        SerializedProperty _nexSpritesProp;
        SerializedProperty _nexMaterialsProp;

        private void OnEnable()
        {
            _activeSampleProp = serializedObject.FindProperty("activeSample");
            _nexPlayerProp = serializedObject.FindProperty("nexPlayer");
            _nexSpritesProp = serializedObject.FindProperty("sprites");
            _nexMaterialsProp = serializedObject.FindProperty("materials");
        }

        public override void OnInspectorGUI()
        {
            NexPlayerSamplesController current = (NexPlayerSamplesController)target;

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ACTIVE SAMPLE:", GUILayout.Width(100));
            EditorGUILayout.PropertyField(_activeSampleProp, GUIContent.none, GUILayout.Width(315));
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                current.ActivateSample(_activeSampleProp.enumValueIndex);
                current.SetNexPlayerForSample((NEXPLAYER_SAMPLES)_activeSampleProp.enumValueIndex);
            }
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("NexPlayer:", GUILayout.Width(70));
            EditorGUILayout.PropertyField(_nexPlayerProp, GUIContent.none, GUILayout.Width(200));
            if (GUILayout.Button("Bind to NexPlayer", GUILayout.Width(150)))
            {
                current.Bind();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sprites:", GUILayout.Width(70));
            EditorGUILayout.PropertyField(_nexSpritesProp, GUIContent.none, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Materials:", GUILayout.Width(70));
            EditorGUILayout.PropertyField(_nexMaterialsProp, GUIContent.none, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}