using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEditor;
using NexPlayerSample;
using NexUtility;

 public static class NxPMenuItemsFullFeatSample
{
    [MenuItem("NexPlayer/Create NexPlayer Object/NexPlayer_Manager", false, 20)]
    [MenuItem("GameObject/NexPlayer/NexPlayer_Manager", false, 20)]
    static void CreateNexPlayerController(MenuCommand menuCommand)
    {
        if (GameObject.FindObjectOfType<NexPlayer>() != null)
        {
            Debug.Log("There is already a NexPlayer_Manager object in the scene");
            return;
        }

        GameObject go = new GameObject("NexPlayer_Manager");
        NexPlayer NxP = go.AddComponent<NexPlayer>();
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
        NxP.autoPlay = true;
        NxP.loopPlay = true;
        NxP.runInBackground = true;
    }

    [MenuItem("NexPlayer/Create NexPlayer Object/NexPlayer_UI", false, 21)]
    [MenuItem("GameObject/NexPlayer/NexPlayer_UI", false, 21)]
    public static void CreateNexPlayerUI(MenuCommand menuCommand)
    {
        if (GameObject.FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        if (GameObject.FindObjectOfType<NexPlayer>() == null)
        {
            if (EditorUtility.DisplayDialog("There is no NexPlayer_Manager object in the scene", "Do you want to create one?", "Create", "Cancel"))
            {
                CreateNexPlayerController(menuCommand);
            }
            else
            {
                Debug.Log("There is no NexPlayer_Manager object in the scene. Please create one before creating the NexPlayer_UI object");
                return;
            }
        }

        PrefabSystem system = new PrefabSystem();
        string file = string.Concat(NexPlayerFullFeatSampleFolderRoot.GetFullPath(), "/NexPlayer/Prefabs/NexPlayerUI.json");
        GameObject go = system.CreatePrefab(file);
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
        NexUIController controller = go.GetComponent<NexUIController>();
        controller.FixPrefab();
        controller.Bind();
        controller.FillNexPlayerUIReferences();
    }

    [MenuItem("NexPlayer/Create NexPlayer Object/NexPlayer_Samples", false, 22)]
    [MenuItem("GameObject/NexPlayer/NexPlayer_Samples", false, 22)]
    public static void CreateNexPlayerSamples(MenuCommand menuCommand)
    {
        if (GameObject.FindObjectOfType<NexPlayer>() == null)
        {
            if (EditorUtility.DisplayDialog("There is no NexPlayer_Manager object in the scene", "Do you want to create one?", "Create", "Cancel"))
            {
                CreateNexPlayerController(menuCommand);
            }
            else
            {
                Debug.Log("There is no NexPlayer_Manager object in the scene. Please create one before creating the NexPlayer_UI object");
                return;
            }
        }

        if (GameObject.FindObjectOfType<NexUIController>() == null)
        {

            if (EditorUtility.DisplayDialog("There is no NexPlayer_UI object in the scene", "Do you want to create one?", "Create", "Cancel"))
            {
                CreateNexPlayerUI(menuCommand);
            }
            else
            {
                Debug.Log("There is no NexPlayer_UI object in the scene. Please create one before creating the NexPlayer_Samples object");
                return;
            }
        }

        AddCubeTags();

        PrefabSystem system = new PrefabSystem();
        // EDITOR MANAGER
        string file = string.Concat(NexPlayerFullFeatSampleFolderRoot.GetFullPath(), "/NexPlayer/Prefabs/NexPlayerSamples.json");
        GameObject samplesGo = system.CreatePrefab(file);
        samplesGo.name = "NexPlayerSamplesController";
        GameObjectUtility.SetParentAndAlign(samplesGo, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(samplesGo, "Create " + samplesGo.name);
        Selection.activeObject = samplesGo;

        // RUNTIME MANAGER
        //file = string.Concat(Application.dataPath, "/NexPlayer/Prefabs/RunTimeManager.json");
        //GameObject runtime_go = system.CreatePrefab(file);
        //GameObjectUtility.SetParentAndAlign(runtime_go, menuCommand.context as GameObject);
        //Undo.RegisterCreatedObjectUndo(runtime_go, "Create " + runtime_go.name);
        //var runtimeManager = runtime_go.GetComponent<NexPlayerSamplesRunTimeControl>();
        //// PARENT
        //var parent = new GameObject("NexPlayer_Samples");
        //editor_go.transform.SetParent(parent.transform);
        //runtime_go.transform.SetParent(parent.transform);
        // BINDING
        var samplesController = samplesGo.GetComponent<NexPlayerSamplesController>();
        samplesController.AssignMaterials();
        var nxp = GameObject.FindObjectOfType<NexPlayer>();
        if (nxp != null)
        {
            samplesController.Bind();
            samplesController.ActivateSample((int)samplesController.activeSample);
            samplesController.SetNexPlayerForSample(samplesController.activeSample);
            //runtimeManager.FindReferences(editorManager);
            //runtimeManager.Bind();
        }
    }
    static void AddCubeTags()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        AddTag(tagsProp, "Cube3");
        AddTag(tagsProp, "Cube2");
        AddTag(tagsProp, "Cube1");
        tagManager.ApplyModifiedProperties();
    }
    static void AddTag(SerializedProperty tagsProp, string newTag)
    {
        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(newTag)) { found = true; break; }
        }

        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty n = tagsProp.GetArrayElementAtIndex(0);
            n.stringValue = newTag;
        }
    }
}
