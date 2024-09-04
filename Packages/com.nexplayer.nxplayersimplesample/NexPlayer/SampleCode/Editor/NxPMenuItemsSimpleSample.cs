using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NexPlayerSample;

public static class NxPMenuItemsSimpleSample
{
    [MenuItem("NexPlayer/Create NexPlayer Simple Object", false, 20)]
    public static void CreateNexPlayerSimple()
    {
        GameObject nps = new GameObject("NexPlayerSimple");
        nps.AddComponent<NexPlayerSimple>();
        Undo.RegisterCreatedObjectUndo(nps, "Create " + nps.name);
    }
}
