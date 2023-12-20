using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NexPlayerAPI;

namespace NexPlayerSample
{
    public class AudioStreamListManager : MonoBehaviour
    {
        public NexPlayer player;

        private NexPlayerAudioStream[] audioStreams = null;
        private List<GameObject> closedAudioList = new List<GameObject>();
        public GameObject audioBtnPrefab;
        private GameObject cloneStreamObject = null;


        public void FindReferences(NexPlayer NxP)
        {
            player = NxP;
            audioBtnPrefab = transform.GetChild(0).gameObject;
        }

        void SetAudioOnClick(NexPlayerAudioStream audioInfo)
        {
            player.SetAudioStream(audioInfo);
        }

        void CreateStreamButtons()
        {
            int count = 0;

            try {
                audioStreams = player.GetAudioStreamList();
            }
            catch (System.Exception e) {
                Debug.Log($"There isn't any audio. {e.Message}");
            }

            if (audioStreams != null)
                count = audioStreams.Length;

            for (int i = 0; i < count; i++)
            {
                cloneStreamObject = Instantiate(audioBtnPrefab, transform);
                cloneStreamObject.SetActive(true);

                Button cloneBtn = cloneStreamObject.GetComponent<Button>();
                NexAudioInfo audioInfo = cloneBtn.GetComponent<NexAudioInfo>();
                if (audioInfo != null && audioStreams.Length != 0)
                {
                    audioInfo.audioInfo = audioStreams[i];
                    cloneBtn.onClick.AddListener(() => SetAudioOnClick(audioInfo.audioInfo));
                }

                Text audioName = cloneBtn.GetComponentInChildren<Text>();
                audioName.text = audioStreams[i].name;
                if (cloneStreamObject != null)
                    closedAudioList.Add(cloneStreamObject);
                if (string.IsNullOrEmpty(audioStreams[i].name))
                    cloneStreamObject.SetActive(false);
            }
            audioBtnPrefab.SetActive(false);
        }

        public void EmptyStreamButtons()
        {
            for (int i = 0; i < closedAudioList.Count; i++)
            {
                GameObject closedAudioObj = closedAudioList[i];
                Destroy(closedAudioObj);
            }
        }

        private void OnEnable()
        {
            CreateStreamButtons();
        }

        private void OnDisable()
        {
            EmptyStreamButtons();
        }
    }
}