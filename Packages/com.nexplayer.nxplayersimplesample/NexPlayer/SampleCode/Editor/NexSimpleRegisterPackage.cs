using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace NexUtility
{
    [InitializeOnLoad]
    public class NexSimpleRegisterPackage
    {
        static NexSimpleRegisterPackage()
        {
            NexDefineSymbol.RegisterPackage("com.nexplayer.nxplayersimplesample");
        }
    }
}
