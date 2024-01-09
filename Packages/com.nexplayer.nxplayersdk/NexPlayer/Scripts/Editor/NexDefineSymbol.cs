using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEngine;

namespace NexUtility 
{
    [InitializeOnLoad]
    public class NexDefineSymbol : AssetPostprocessor ,IActiveBuildTargetChanged
    {

        private static readonly Dictionary<string, string> PACKAGESYMBOL = new Dictionary<string, string> { { "com.nexplayer.nxplayerfullfeatsample", "NEXPLAYER_FULL_FEAT_SAMPLE" }, { "com.nexplayer.nxplayersimplesample", "NEXPLAYER_SIMPLE_SAMPLE" } };
        private static List<string> SymbolsToDefine;
        private static List<string> SymbolsToRemove;

        static void removeRepeat(List<string> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list.Count; j++)
                {
                    if (j != i)
                    {
                        if (list[i] == list[j])
                        {
                            list.Remove(list[j]);
                        }
                    }
                }
            }
        }


        static NexDefineSymbol()
        {
            SymbolsToDefine = new List<string>();
            SymbolsToRemove = new List<string>();

#if !UNITY_2018 && !UNITY_2019
             Events.registeredPackages += packageRegistered;
#else
            Application.logMessageReceived += ReloadAssembliesSymbols;
#endif
        }

        public int callbackOrder { get { return 0; } }

        public static void RegisterPackage(string _package)
        {
            foreach (var package in PACKAGESYMBOL)
            {
                if (package.Key == _package)
                {
                    bool exist = false;
                    foreach (var symbol in SymbolsToDefine)
                    {
                        if (symbol == package.Value)
                        {
                            exist = true;
                            break;
                        }

                    }
                    if (!exist)
                    {
                        SymbolsToDefine.Add(package.Value);
                    }
                }
            }
            SetSymbol(Application.platform);
        }
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            if (previousTarget != newTarget)
                SetSymbol(Application.platform);
        }
#if !UNITY_2018 && !UNITY_2019
        public static void packageRegistered(PackageRegistrationEventArgs action) 
        {
            removeRepeat(SymbolsToRemove);
            removeRepeat(SymbolsToDefine);
            foreach (var addedPackage in action.added)
            {
                foreach (var targetPackage in PACKAGESYMBOL)
                {
                    if (addedPackage.name == targetPackage.Key) 
                    {
                        SymbolsToDefine.Add(targetPackage.Value);
                        SymbolsToRemove.Remove(targetPackage.Value);

                    }
                        
                }
            }
            foreach (var removedPackage in action.removed)
            {
                foreach (var targetPackage in PACKAGESYMBOL)
                {
                    if (removedPackage.name == targetPackage.Key) 
                    {
                        SymbolsToDefine.Remove(targetPackage.Value);
                        SymbolsToRemove.Add(targetPackage.Value);
                    }
                        
                }
            }
            SetSymbol(Application.platform);
        }
#else
        private static void ReloadAssembliesSymbols(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error) 
            {
                string manifestPath = Application.dataPath + "/../Packages/manifest.json";
                string manifest = File.ReadAllText(manifestPath);
                string [] files = Directory.GetDirectories(Application.dataPath + "/../Packages");
                string filesInLine="";
                for (int i = 0; i < files.Length; i++) 
                {
                    filesInLine += files[i] + " ";
                }

                foreach (var targetPackage in PACKAGESYMBOL)
                {
                    if (manifest.Contains(targetPackage.Key) || filesInLine.Contains(targetPackage.Key))
                    {
                        SymbolsToDefine.Add(targetPackage.Value);
                        SymbolsToRemove.Remove(targetPackage.Value);
                    }
                    else
                    {
                        SymbolsToDefine.Remove(targetPackage.Value);
                        SymbolsToRemove.Add(targetPackage.Value);
                    }
                }
                SetSymbol(Application.platform);
            } 
        }


#endif
        static void SetSymbol(RuntimePlatform currentPlatform)
        {
            if (currentPlatform == RuntimePlatform.WindowsEditor || currentPlatform == RuntimePlatform.WindowsPlayer
              || currentPlatform == RuntimePlatform.OSXEditor || currentPlatform == RuntimePlatform.OSXPlayer)
            {
                string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
                foreach (var symbol in SymbolsToRemove)
                {
                    if (symbols.Contains(symbol))
                    {
                        symbols = symbols.Replace(symbol,"");
                    }
                }
                foreach (var symbol in SymbolsToDefine) 
                {
                    if (!symbols.Contains(symbol)) 
                    {
                        symbols += ';' + symbol;
                    }
                }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
            }
            else if (currentPlatform == RuntimePlatform.Android)
            {
                string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
                foreach (var symbol in SymbolsToRemove)
                {
                    if (symbols.Contains(symbol))
                    {
                        symbols = symbols.Replace(symbol, "");
                    }
                }
                foreach (var symbol in SymbolsToDefine)
                {
                    if (!symbols.Contains(symbol))
                    {
                        symbols += ';' + symbol;
                    }
                }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, symbols);
            }
            else if (currentPlatform == RuntimePlatform.IPhonePlayer)
            {
                string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS);
                foreach (var symbol in SymbolsToRemove)
                {
                    if (symbols.Contains(symbol))
                    {
                        symbols = symbols.Replace(symbol, "");
                    }
                }
                foreach (var symbol in SymbolsToDefine)
                {
                    if (!symbols.Contains(symbol))
                    {
                        symbols += ';' + symbol;
                    }
                }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, symbols);
            }
            else if (currentPlatform == RuntimePlatform.WebGLPlayer)
            {
                string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL);
                foreach (var symbol in SymbolsToRemove)
                {
                    if (symbols.Contains(symbol))
                    {
                        symbols = symbols.Replace(symbol, "");
                    }
                }
                foreach (var symbol in SymbolsToDefine)
                {
                    if (!symbols.Contains(symbol))
                    {
                        symbols += ';' + symbol;
                    }
                }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, symbols);
            }
        }
    }
}
