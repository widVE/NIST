using UnityEngine;
using NexUtility;

public class PrefabDataCreator : MonoBehaviour 
{
    public string fileName;
    public PrefabSystem system;
    
    public void SaveObject()
    {
        if (system == null)
            system = new PrefabSystem();
        string root = string.Concat(NexPlayerFullFeatSampleFolderRoot.GetFullPath(), "/NexPlayer/Prefabs/");
        system.SaveGameObject(string.Concat(root, fileName), this.gameObject);
    }
}
