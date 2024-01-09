#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
using UnityEditor;
using UnityEditor.Events;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NexPlayerAPI;
using NexUtility;

namespace NexPlayerSample
{
    public class NexPlayerSamplesController : MonoBehaviour
    {
        [SerializeField]
        public NEXPLAYER_SAMPLES activeSample = NEXPLAYER_SAMPLES.RawImage;
        [SerializeField] NexPlayer nexPlayer;
        [SerializeField]
        public NexMaterials materials;
        [SerializeField]
        public NexUISprites sprites;
        Transform root;
        [SerializeField]
        NexUIController uiController;

        //this is not supported by webgl style
#if !UNITY_WEBGL
        GameObject ScreenRenderChange;

        void Awake()
        {
            ScreenRenderChange = GameObject.Find("Screen");
            if (ScreenRenderChange)
            {
                ScreenRenderChange.SetActive(false);
            }
        }
#endif
        #region Methods called From Editor
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX

        public void AssignMaterials()
        {
            string nexPlayerFolder = NexPlayerFullFeatSampleFolderRoot.GetRelativePath();
            if (materials == null)
            {
                materials = (NexMaterials)AssetDatabase.LoadAssetAtPath(nexPlayerFolder + "/NexPlayer/Resources/NexMaterials.asset", typeof(NexMaterials));
            }
            materials.ResetReferences();
            if (sprites == null)
            {
                sprites = (NexUISprites)AssetDatabase.LoadAssetAtPath(nexPlayerFolder + "/NexPlayer/Resources/NexUISprites.asset", typeof(NexUISprites));
            }
            sprites.ResetReferences();
            //RawImage Sample
            Transform rawImageCanvasTransform = transform.GetChild(0).GetChild(0);
            rawImageCanvasTransform.GetComponentInChildren<RawImage>().material = materials.NexPlayerDefaultMaterialRawImage;

            // RenderTexture Sample
            Transform quad = transform.GetChild(1).GetChild(0);
            quad.GetComponent<MeshRenderer>().sharedMaterial = materials.RenderTexture0;
            quad.GetComponent<MeshFilter>().sharedMesh = materials.quad;

            // Transparency Sample
            Transform transparentQuad = transform.GetChild(2).GetChild(0);
            transparentQuad.GetComponent<MeshRenderer>().sharedMaterial = materials.Transparent;
            transparentQuad.GetComponent<MeshFilter>().sharedMesh = materials.quad;
            Transform nexCube = transform.GetChild(2).GetChild(1);
            nexCube.GetComponent<MeshRenderer>().sharedMaterial = materials.NexPlayer;
            nexCube.GetComponent<MeshFilter>().sharedMesh = materials.cube;

            // VideoSpread Sample
            Transform videoSpread = transform.GetChild(3);
            for (int i = 0; i < videoSpread.childCount; i++)
            {
                videoSpread.GetChild(i).GetComponent<MeshRenderer>().sharedMaterial = materials.WorldSpaceTiling;
                videoSpread.GetChild(i).GetComponent<MeshFilter>().sharedMesh = materials.quad;
            }

            // Material Override Sample
            Transform cube = transform.GetChild(4).GetChild(0);
            cube.GetComponent<MeshRenderer>().sharedMaterial = materials.NexPlayerDefaultMaterial;
            cube.GetComponent<MeshFilter>().sharedMesh = materials.cube;

            // Multiple Renderers Sample
            Transform multiplerenderers = transform.GetChild(5);
            for (int i = 0; i < multiplerenderers.childCount - 1; i++)
            {
                multiplerenderers.GetChild(i).GetComponent<MeshRenderer>().sharedMaterial = materials.NexPlayerDefaultMaterial;
                multiplerenderers.GetChild(i).GetComponent<MeshFilter>().sharedMesh = materials.cube;
            }

            // Change RenderMode Sample
            Transform changeRenderModeScreen = transform.GetChild(6).GetChild(0);
            changeRenderModeScreen.GetComponent<MeshRenderer>().sharedMaterial = materials.RenderTexture0;
            changeRenderModeScreen.GetComponent<MeshFilter>().sharedMesh = materials.screen;
            Transform changeRenderModeCube = transform.GetChild(6).GetChild(1);
            changeRenderModeCube.GetComponent<MeshRenderer>().sharedMaterial = materials.NexPlayerDefaultMaterial;
            changeRenderModeCube.GetComponent<MeshFilter>().sharedMesh = materials.cube;
            transform.GetChild(6).GetChild(2).GetChild(0).GetComponent<RawImage>().material = materials.NexPlayerDefaultMaterialRawImage;

            // 360 Sample
            Transform nex360 = transform.GetChild(7);
            nex360.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = materials.Mono;
            nex360.GetChild(0).GetComponent<MeshFilter>().sharedMesh = materials.sphere360;
            nex360.rotation = Quaternion.Euler(270, 0, 0);
            NexVRUIController vrUi = nex360.GetChild(0).GetComponent<NexVRUIController>();
            uiController = GameObject.FindObjectOfType<NexUIController>();
            vrUi.gameObjectToToggle = uiController.nexControlBar;
            NexPlayer360 np = nex360.GetChild(0).GetComponent<NexPlayer360>();
            np.cameraToRotate = Camera.main;
            np.toggleUI = vrUi;
            StereoMode sm = nex360.GetChild(0).GetComponent<StereoMode>();
            sm.replacementMainMaterial = materials.Mono;
            sm.replacementLeftMaterial = materials.Left;
            sm.replacementRightMaterial = materials.Right;
            sm.replacementOverMaterial = materials.Over;
            sm.replacementUnderMaterial = materials.Under;

            // Multistream RawImage Sample
            Transform multiRawImageCanvasTransform = transform.GetChild(8).GetChild(0);
            multiRawImageCanvasTransform.GetChild(0).GetComponent<RawImage>().material = materials.NexPlayerDefaultMaterialRawImage;
            multiRawImageCanvasTransform.GetChild(1).GetComponent<RawImage>().material = materials.NexPlayerDefaultMaterialRawImage;
            multiRawImageCanvasTransform.GetChild(2).GetComponent<RawImage>().material = materials.NexPlayerDefaultMaterialRawImage;
            multiRawImageCanvasTransform.GetChild(3).GetComponent<RawImage>().material = materials.NexPlayerDefaultMaterialRawImage;

            // Multistream RenderTexture Sample
            Transform multistream = transform.GetChild(9);
            multistream.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = materials.RenderTexture0;
            multistream.GetChild(0).GetComponent<MeshFilter>().sharedMesh = materials.quad;
            multistream.GetChild(1).GetComponent<MeshRenderer>().sharedMaterial = materials.RenderTexture1;
            multistream.GetChild(1).GetComponent<MeshFilter>().sharedMesh = materials.quad;
            multistream.GetChild(2).GetComponent<MeshRenderer>().sharedMaterial = materials.RenderTexture2;
            multistream.GetChild(2).GetComponent<MeshFilter>().sharedMesh = materials.quad;
            multistream.GetChild(3).GetComponent<MeshRenderer>().sharedMaterial = materials.RenderTexture3;
            multistream.GetChild(3).GetComponent<MeshFilter>().sharedMesh = materials.quad;
        }

        public void Bind()
        {
            if (nexPlayer == null)
            {
                nexPlayer = FindObjectOfType<NexPlayer>();
                if (nexPlayer == null)
                {
                    Debug.LogError("There is not a NexPlayer instance in the Scene!");
                    return;
                }
            }
            AddPersistantListeners();
            AddMaterial();
        }
        void AddPersistantListeners()
        {
            try
            {
                ClearPersistentListeners();
                // Multiple Renderers Sample
                Transform canvas = transform.GetChild(5).GetChild(4);
                UnityEventTools.AddPersistentListener(canvas.GetChild(0).GetComponent<Button>().onClick, AddRenderer);
                UnityEventTools.AddPersistentListener(canvas.GetChild(1).GetComponent<Button>().onClick, RemoveRenderer);
                // Change RenderMode Sample
                canvas = transform.GetChild(6).GetChild(2);
                UnityEventTools.AddIntPersistentListener(canvas.GetChild(1).GetComponent<Button>().onClick, ChangeRenderMode, 3);
                UnityEventTools.AddIntPersistentListener(canvas.GetChild(2).GetComponent<Button>().onClick, ChangeRenderMode, 1);
                UnityEventTools.AddIntPersistentListener(canvas.GetChild(3).GetComponent<Button>().onClick, ChangeRenderMode, 2);
                // Multistream RawImage Sample
                canvas = transform.GetChild(8).GetChild(0);
                UnityEventTools.AddIntPersistentListener(canvas.GetChild(0).GetChild(0).GetComponent<Button>().onClick, ChooseControl, 0);
                UnityEventTools.AddIntPersistentListener(canvas.GetChild(1).GetChild(0).GetComponent<Button>().onClick, ChooseControl, 1);
                UnityEventTools.AddIntPersistentListener(canvas.GetChild(2).GetChild(0).GetComponent<Button>().onClick, ChooseControl, 2);
                UnityEventTools.AddIntPersistentListener(canvas.GetChild(3).GetChild(0).GetComponent<Button>().onClick, ChooseControl, 3);
            }
            catch (Exception e)
            {
                Debug.LogError("Adding Listeners failed, exception" + e);
            }
        }
        void AddMaterial()
        {
           nexPlayer.MaxScreenMaterial = materials.NexPlayerDefaultMaterialRawImage;
        }
        void ClearPersistentListeners()
        {
            Transform canvas = transform.GetChild(5).GetChild(4);
            UnityEventTools.RemovePersistentListener(canvas.GetChild(0).GetComponent<Button>().onClick, AddRenderer);
            UnityEventTools.RemovePersistentListener(canvas.GetChild(1).GetComponent<Button>().onClick, RemoveRenderer);
            canvas = transform.GetChild(6).GetChild(2);
            UnityEventTools.RemovePersistentListener<int>(canvas.GetChild(1).GetComponent<Button>().onClick, ChangeRenderMode);
            UnityEventTools.RemovePersistentListener<int>(canvas.GetChild(2).GetComponent<Button>().onClick, ChangeRenderMode);
            UnityEventTools.RemovePersistentListener<int>(canvas.GetChild(3).GetComponent<Button>().onClick, ChangeRenderMode);
            canvas = transform.GetChild(8).GetChild(0);
            UnityEventTools.RemovePersistentListener<int>(canvas.GetChild(0).GetChild(0).GetComponent<Button>().onClick, ChooseControl);
            UnityEventTools.RemovePersistentListener<int>(canvas.GetChild(1).GetChild(0).GetComponent<Button>().onClick, ChooseControl);
            UnityEventTools.RemovePersistentListener<int>(canvas.GetChild(2).GetChild(0).GetComponent<Button>().onClick, ChooseControl);
            UnityEventTools.RemovePersistentListener<int>(canvas.GetChild(3).GetChild(0).GetComponent<Button>().onClick, ChooseControl);
        }
#endif
#endregion

#region Event Callbacks
        void AddRenderer()
        {
            nexPlayer.AddRenderer();
        }
        void RemoveRenderer()
        {
            nexPlayer.RemoveRenderer();
        }
        void ChangeRenderMode(int mode)
        {
#if !UNITY_WEBGL
            if (mode!=1)
            {
                ScreenRenderChange.SetActive(false);
            }
            else
            {
               ScreenRenderChange.SetActive(true);
            }
#endif
            nexPlayer.ChangeRenderMode(mode);
        }
        void ChooseControl(int index)
        {
            nexPlayer.MultistreamController.ChooseControlIndex(index);
        }
#endregion


        public Transform ActivateSample(int index)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(i == index);
            }
            root = transform.GetChild(index);

            return root;
        }
        public void SetNexPlayerForSample(NEXPLAYER_SAMPLES index)
        {
            if (nexPlayer == null)
                nexPlayer = FindObjectOfType<NexPlayer>();

            if ((int)index < 7)
            {
                SetMultistream(false);
                nexPlayer.streamURI = "https://bitdash-a.akamaihd.net/content/MI201109210084_1/m3u8s/f08e80da-bf1d-4e3d-8899-f0f6155f6efa.m3u8";
            }
            else if ((int)index == 7)
            {
                SetMultistream(false);
                nexPlayer.streamURI = "https://s3.eu-west-3.amazonaws.com/content.nexplayersdk.com/360test/NYCTimewarp/HLS-360/master.m3u8";
            }
            else
            {
                SetMultistream(true);
            }

            switch (index)
            {
                case NEXPLAYER_SAMPLES.RawImage:
                    nexPlayer.SetStartingRenderMode(NexRenderMode.RawImage);
                    nexPlayer.rawImage = root.GetComponentInChildren<RawImage>();
                    break;
                case NEXPLAYER_SAMPLES.RenderTexture:
                case NEXPLAYER_SAMPLES.Transparency:
                case NEXPLAYER_SAMPLES.VideoSpread:
                    nexPlayer.SetStartingRenderMode(NexRenderMode.RenderTexture);
                    nexPlayer.renderTexture = materials.renderTextures[0];
                    nexPlayer.renderTextureRenderer = root.GetChild(0).GetComponent<MeshRenderer>();
                    break;
                case NEXPLAYER_SAMPLES.MaterialOverride:
                    nexPlayer.SetStartingRenderMode(NexRenderMode.MaterialOverride);
                    nexPlayer.renderTarget = root.GetChild(0).GetComponent<MeshRenderer>();
                    break;
                case NEXPLAYER_SAMPLES.MultipleRenderers:
                    nexPlayer.SetStartingRenderMode(NexRenderMode.MaterialOverride);
                    nexPlayer.renderTarget = root.GetChild(0).GetComponent<MeshRenderer>();
                    break;
                case NEXPLAYER_SAMPLES.ChangeRenderMode:
                    nexPlayer.SetStartingRenderMode(NexRenderMode.RawImage);
                    nexPlayer.rawImage = root.GetComponentInChildren<RawImage>();
                    nexPlayer.renderTexture = materials.renderTextures[0];
                    nexPlayer.renderTextureRenderer = root.GetChild(0).GetComponent<MeshRenderer>();
                    nexPlayer.renderTarget = root.GetChild(1).GetComponent<MeshRenderer>();
                    break;
                case NEXPLAYER_SAMPLES.MultistreamRawImage:
                    nexPlayer.SetStartingRenderMode(NexRenderMode.RawImage);
                    nexPlayer.multiRawImages = new List<RawImage>(root.GetComponentsInChildren<RawImage>());
                    break;
                case NEXPLAYER_SAMPLES.MultistreamRenderTexture:
                    nexPlayer.SetStartingRenderMode(NexRenderMode.RenderTexture);
                    nexPlayer.multiRenderTextures = new List<RenderTexture>(materials.renderTextures);
                    break;
                case NEXPLAYER_SAMPLES.NexPlayer360:
                    nexPlayer.SetStartingRenderMode(NexRenderMode.MaterialOverride);
                    nexPlayer.renderTarget = root.GetChild(0).GetComponent<MeshRenderer>();
                    nexPlayer.caption.text = string.Empty;
                    break;
            }

            nexPlayer.keyServerURI = string.Empty;
        }
        void SetMultistream(bool multi)
        {
            if (multi)
            {

                nexPlayer.numberOfStreams = 4;
                nexPlayer.multiURLPaths = new List<string> {"https://bitdash-a.akamaihd.net/content/MI201109210084_1/m3u8s/f08e80da-bf1d-4e3d-8899-f0f6155f6efa.m3u8",
                                                                    "https://s3.eu-west-3.amazonaws.com/content.nexplayersdk.com/hls/BustiContent/Race1/master.m3u8",
                                                                    "https://s3.eu-west-3.amazonaws.com/content.nexplayersdk.com/hls/BustiContent/Race2/master.m3u8",
                                                                    "https://s3.eu-west-3.amazonaws.com/content.nexplayersdk.com/hls/BustiContent/Race3/master.m3u8"};

                nexPlayer.multiKeyServerURL = new List<string>(4);
                nexPlayer.multiRawImages = new List<RawImage>(4);
                nexPlayer.multiRenderTextures = new List<RenderTexture>(4);
                nexPlayer.multiSubTexts = new List<Text>(4);
                nexPlayer.streamURI = "https://bitdash-a.akamaihd.net/content/MI201109210084_1/m3u8s/f08e80da-bf1d-4e3d-8899-f0f6155f6efa.m3u8";
            }
            else
            {
                nexPlayer.numberOfStreams = 0;
                nexPlayer.multiURLPaths = new List<string>();
                nexPlayer.multiKeyServerURL = new List<string>();
                nexPlayer.multiRawImages = new List<RawImage>();
                nexPlayer.multiRenderTextures = new List<RenderTexture>();
                nexPlayer.multiSubTexts = new List<Text>();
            }
        }
    }

    public enum NEXPLAYER_SAMPLES
    {
        RawImage = 0,
        RenderTexture = 1,
        Transparency = 2,
        VideoSpread = 3,
        MaterialOverride = 4,
        MultipleRenderers = 5,
        ChangeRenderMode = 6,
        NexPlayer360 = 7,
        MultistreamRawImage = 8,
        MultistreamRenderTexture = 9
    }
}