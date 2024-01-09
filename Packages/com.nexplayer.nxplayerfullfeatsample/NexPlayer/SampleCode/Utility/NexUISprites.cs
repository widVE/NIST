using UnityEngine;
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
using UnityEditor;
#endif

namespace NexUtility
{
    [System.Serializable]
    public class NexUISprites : ScriptableObject
    {
        [Header("Unity Internal")]
        public Sprite UiSprite;
        public Sprite Background;
        public Sprite UiMask;
        public Sprite Knob;
        [Header("Resources")]
        public Sprite nexPlayerLogo;
        public Sprite uiButtonDefault;
        public Sprite arrow;
        public Sprite languageCaptionSettings;
        public Sprite caption;
        public Sprite maximizeScreen;
        public Sprite resizeScreen;
        public Sprite originSize;
        public Sprite fullScreen;
        public Sprite fitVertically;
        public Sprite fitHorizontal;
        public Sprite previous;
        public Sprite play;
        public Sprite pause;
        public Sprite stop;
        public Sprite next;
        public Sprite download;
        public Sprite offlineList;
        public Sprite loop;
        public Sprite noLoop;
        public Sprite audioMute;
        public Sprite audio;
        public Material whiteMat;
        [Header("360")]
        public Texture2D handGrab;
        public Texture2D handHover;

        public void ResetReferences()
        {
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        string nexPlayerFolder = NexPlayerFullFeatSampleFolderRoot.GetRelativePath();
        UiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        UiMask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
        Knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        uiButtonDefault = AssetDatabase.LoadAssetAtPath<Sprite>(nexPlayerFolder + "/NexPlayer/Resources/MainMenu/UIButtonDefault.png");
        string uiFolder = nexPlayerFolder + "/NexPlayer/Resources/PlayerUI/";
        nexPlayerLogo = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "NexPlayerLogoSprite.png");
        arrow = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icon-arrow.png");
        languageCaptionSettings = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icons8-gear.png");
        caption = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icons8-closed-captioning-64.png");
        maximizeScreen = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icons8-expand-50.png");
        whiteMat = AssetDatabase.LoadAssetAtPath<Material>(nexPlayerFolder + "/NexPlayer/NexPlayer360/VRMenu/Resources/Materials/360_Text_Material.mat"); ;
        resizeScreen = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icons8-resize-50.png");
        originSize = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icons8-original-size-filled-50.png");
        fullScreen = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icons8-stretch-uniform-50.png");
        fitVertically = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icons8-height-filled-50.png");
        fitHorizontal = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icons8-width-50.png");
        previous = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "skip-start-filled.png");
        play = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icon_play.png");
        pause = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icon_pause.png");
        stop = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icon_stop.png"); ;
        next = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "skip-end-filled.png");
        download = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icon_download.png");
        offlineList = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "icon_offline_list.png");
        loop = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "repeat.png");
        noLoop = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "no-repeat.png");
        audioMute = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "audio-mute.png");
        audio = AssetDatabase.LoadAssetAtPath<Sprite>(uiFolder + "audio.png");

        handGrab = AssetDatabase.LoadAssetAtPath<Texture2D>(nexPlayerFolder + "/NexPlayer/NexPlayer360/Resources/font-awesome_4-7-0_hand-rock-o_256_0_ffffff_none.png");
        handHover = AssetDatabase.LoadAssetAtPath<Texture2D>(nexPlayerFolder + "/NexPlayer/NexPlayer360/Resources/font-awesome_4-7-0_hand-paper-o_256_0_ffffff_none.png");

            EditorUtility.SetDirty(this);
#endif
        }
    }
}