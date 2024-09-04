using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace NexUtility
{
    public static class NexBuildConfigurationHelper
    {
        public static BuildTarget CurrentBuildTarget => EditorUserBuildSettings.activeBuildTarget;
        public static bool isPlatformSupported => IsPlatformSupported();
        private static bool IsPlatformSupported()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

            return target == BuildTarget.iOS || target == BuildTarget.Android || target == BuildTarget.StandaloneWindows64 || target == BuildTarget.StandaloneOSX || target == BuildTarget.WebGL || target == BuildTarget.WSAPlayer;
        }

        private static Dictionary<BuildTarget, GraphicsDeviceType[]> _buildTargetToGraphicAPIs = new Dictionary<BuildTarget, GraphicsDeviceType[]>();

        private static GraphicsDeviceType[] WindowsGraphicAPIs = new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D11 };

#if UNITY_2021_2_OR_NEWER
        private static GraphicsDeviceType[] AndroidGraphicAPIs = new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES2 };
#else
        private static GraphicsDeviceType[] AndroidGraphicAPIs = new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3 };
#endif
        private static GraphicsDeviceType[] iOsGraphicAPIs = new GraphicsDeviceType[] { GraphicsDeviceType.Metal };

        public static void Configure()
        {
            _buildTargetToGraphicAPIs.Clear();
            _buildTargetToGraphicAPIs.Add(BuildTarget.StandaloneWindows64, WindowsGraphicAPIs);
            _buildTargetToGraphicAPIs.Add(BuildTarget.WSAPlayer, WindowsGraphicAPIs);
            _buildTargetToGraphicAPIs.Add(BuildTarget.Android, AndroidGraphicAPIs);
            _buildTargetToGraphicAPIs.Add(BuildTarget.iOS, iOsGraphicAPIs);
            _buildTargetToGraphicAPIs.Add(BuildTarget.StandaloneOSX, iOsGraphicAPIs);
        }

        public static void SetBuildTarget(object target)
        {
            BuildTarget targetBuild = (BuildTarget)target;
            if (targetBuild == BuildTarget.StandaloneWindows64 || targetBuild == BuildTarget.StandaloneOSX)
            {
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Standalone, targetBuild);
            }
            else if (targetBuild == BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, targetBuild);
            }
            else if (targetBuild == BuildTarget.iOS)
            {
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.iOS, targetBuild);
            }
            else if (targetBuild == BuildTarget.WebGL)
            {
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.WebGL, targetBuild);
            }
            else if (targetBuild == BuildTarget.WSAPlayer)
            {
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.WSA, targetBuild);
            }
        }
        public static void SetGraphicAPIs(BuildTarget target)
        {
            if (_buildTargetToGraphicAPIs.TryGetValue(target, out var graphicAPIs))
            {
                PlayerSettings.SetGraphicsAPIs(target, graphicAPIs);
            }
        }

        public static void SetGraphicAPIsAndroid(BuildTarget target, GraphicsDeviceType[] Graphics)
        {
            PlayerSettings.SetGraphicsAPIs(target, Graphics);
        }

        public static bool AreRecomendedGraphicAPIsSet(BuildTarget target)
        {
        if(target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
        {
            GraphicsDeviceType[] currentGraphicAPIs = PlayerSettings.GetGraphicsAPIs(target);
            GraphicsDeviceType[] targetGraphicAPIs;

            if(_buildTargetToGraphicAPIs.TryGetValue(target, out targetGraphicAPIs))
            {
                return currentGraphicAPIs.SequenceEqual(targetGraphicAPIs);
            }
            else
            {
                return false;
            }
        }
            bool result = true;
            GraphicsDeviceType[] currentAPIs = PlayerSettings.GetGraphicsAPIs(target);

            if (_buildTargetToGraphicAPIs.TryGetValue(target, out var first))
            {
                if (currentAPIs[0] != first[0])
                {
                    result = false;
                }
            }

            return result;
        }

        public static bool IsVulkan(BuildTarget target)
        {
            bool result = false;
            GraphicsDeviceType[] currentAPIs = PlayerSettings.GetGraphicsAPIs(target);

            for (int i = 0; i < currentAPIs.Length; i++)
            {
                if (currentAPIs[i] == GraphicsDeviceType.Vulkan)
                {
                    result = true;
                }
            }

            return result;
        }

        public static bool ISOpenGL(BuildTarget target)
        {
            bool result = true;
            GraphicsDeviceType[] currentAPIs = PlayerSettings.GetGraphicsAPIs(target);

            for (int i = 0; i < currentAPIs.Length; i++)
            {
                result = currentAPIs[i] == GraphicsDeviceType.OpenGLES3 ||
                         currentAPIs[i] == GraphicsDeviceType.OpenGLES2 ||
                         currentAPIs[i] == GraphicsDeviceType.OpenGLCore;
            }

            return result;
        }
    }

}