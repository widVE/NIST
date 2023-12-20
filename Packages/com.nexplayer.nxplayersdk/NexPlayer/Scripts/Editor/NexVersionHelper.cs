using System.IO;
using UnityEditor;
using UnityEngine;
using NexUtility;

// Version helper that changes every needed file for every different version from existing files.
//[ExecuteInEditMode]
public class NexVersionHelper 
{
    const string tempPath = "/NexPlayer/.Temp";
    static readonly string[] folders = { "/NexPlayer/Prefabs", "/NexPlayer/Scenes", "/NexPlayer/NexPlayer360/Scenes" };

    private static string folderRoot;
    private static char specialDirectoryEndCharacter = '_';

    // we go through every important directory, making all the necessary changes 
    public static void ChangeVersion(uint version)
    {
        // needed variables. ApplicationPath only needs to be set once but its easier to do it here
        folderRoot = NexPlayerFolderRoot.GetFullPath();
        string versionPath = "/" + version + specialDirectoryEndCharacter;

        int i = 0;
        foreach (string folder in folders)
        {
            if (!IsAlreadySameVerion(folderRoot + folder, version))
            {
                string auxTempPath = folderRoot + tempPath + i; // create a new temporal path for each directory we want to change

                // first, we remove previous temporal content, if any
                DeleteDirectory(auxTempPath);

                // then, we move the content of the source directory into a temporary folder
                CopyDirectoryContent(folderRoot + folder, auxTempPath);

                // we delete the previous folder
                DeleteDirectory(folderRoot + folder); // TODO: in 2018, if you access the prefabs or scenes folder after importing the package, 
                // Unity editor will crash 

                // and create an empty one
                CreateDirectory(folderRoot + folder);

                // in order to move the version directory into it
                MoveDirectory(auxTempPath + versionPath, folderRoot + folder + '/' + version + '_');

                // (OPTIONAL) then, we move the backup files into the source directory
                //BackupDirectories(auxTempPath, applicationPath + folder);

                // finally, we create an auxiliary file in order to indicate us the current version of the folder.
                CreateAuxiliaryVersionFile(folderRoot + folder, version);

                i++;
            }
            else
            {
                Debug.Log("This project is already in " + version + " version");
            }
        }

        ReloadAssetsInEditor();
    }

    // copy source content into destination. Source needs to exist. Destination must be deleted before this method
    private static void CopyDirectoryContent(string source, string destination)
    {
        if (Directory.Exists(source))
        {
            FileUtil.CopyFileOrDirectory(source, destination);
        }
        else
        {
            Debug.LogWarning(source + " does not exists");
        }
    }

    // recover all the previous support directories
    private static void BackupDirectories(string source, string destination)
    {
        if (Directory.Exists(source))
        {
            string[] directories = Directory.GetDirectories(source);

            foreach (string directory in directories)
            {
                string[] aux = directory.Split('\\');
                string backupDirectoryName = aux[aux.Length - 1];

                if (IsBackupDirectory(backupDirectoryName))
                    Directory.Move(directory, destination + "/" + backupDirectoryName);
            }
        }
        else
        {
            Debug.LogWarning(source + " does not exists");
        }
    }

    // this method moves a directory into another directory
    private static void MoveDirectory(string source, string destination)
    {
        if (Directory.Exists(source))
        {
            Directory.Move(source, destination);
        }
        else
        {
            Debug.LogWarning(source + " does not exists");
        }
    }

    private static void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    // delete previous content of destination and copy content of source into destination
    private static void ReplaceDirectoryContent(string source, string destination)
    {
        if (Directory.Exists(source))
        {
            FileUtil.ReplaceDirectory(source, destination);
        }
        else
        {
            Debug.LogWarning(source + " does not exists");
        }
    }

    // simple delete
    private static void DeleteDirectory(string target)
    {
        if (Directory.Exists(target))
        {
            Directory.Delete(target, true);
        }
        else
        {
            Debug.LogWarning(target + " does not exists. Cannot delete it.");
        }
    }

    // this is used to filter our backups directories, They end with 'specialDirectoryEndCharacter', following the version (2018, 2019...)
    private static bool IsBackupDirectory(string directory)
    {
        return directory[directory.Length - 1] == specialDirectoryEndCharacter;
    }

    private static void ReloadAssetsInEditor()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    // creates an auxiliary file to indicate the current version of the prefabs, assets, ...
    private static void CreateAuxiliaryVersionFile(string target, uint version)
    {
        if (Directory.Exists(target))
        {
            File.Create(target + "/." + version);
        }
        else
        {
            Debug.LogWarning(target + " does not exists. Cannot delete it.");
        }
    }

    // auxiliary method. Uses an auxiliary file (if any) to check the current version of the folder
    public static bool IsAlreadySameVerion(string target, uint version)
    {
        return File.Exists(target + "/." + version);
    }
}
