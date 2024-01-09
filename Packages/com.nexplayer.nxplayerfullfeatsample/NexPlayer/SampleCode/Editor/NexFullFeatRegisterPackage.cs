using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace NexUtility 
{
    [InitializeOnLoad]
    public class NexFullFeatRegisterPackage
    {
        static NexFullFeatRegisterPackage() 
        {
            NexDefineSymbol.RegisterPackage("com.nexplayer.nxplayerfullfeatsample");
        }
    }
}

