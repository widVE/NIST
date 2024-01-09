using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR_OSX && UNITY_IOS
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace NexPlayerAPI
{
    public class BuildPostProcessor
    {
        [PostProcessBuild]
        public static void ChangeXcodePlist(BuildTarget buildTarget, string path)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                string plistPath = path + "/Info.plist";
                PlistDocument plist = new PlistDocument();
                plist.ReadFromFile(plistPath);

                PlistElementDict rootDict = plist.root;          
                File.WriteAllText(plistPath, plist.WriteToString());
            }
        }

        [PostProcessBuildAttribute(1)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target == BuildTarget.iOS)
            {
                PBXProject project = new PBXProject();
                string sPath = PBXProject.GetPBXProjectPath(path);
                project.ReadFromFile(sPath);

    #if UNITY_2019_3_OR_NEWER
               string guidName = project.GetUnityFrameworkTargetGuid();
    #else
                string guidName = project.TargetGuidByName("Unity-iPhone");

    #endif
                project.AddCapability(guidName, PBXCapabilityType.BackgroundModes);
                ModifyFrameworksSettings(project, guidName);

                File.WriteAllText(sPath, project.WriteToString());
            }
        }

        static void ModifyFrameworksSettings(PBXProject project, string guid)
        {
            project.AddFrameworkToProject(guid, "VideoToolBox.framework", false);

            project.AddBuildProperty(guid, "OTHER_LDFLAGS", "-lstdc++");

            project.AddBuildProperty(guid, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            project.AddBuildProperty(guid, "FRAMEWORK_SEARCH_PATHS", "@executable_path/Frameworks");

            project.SetBuildProperty(guid, "ENABLE_BITCODE", "NO");
        }
    }
}
#endif