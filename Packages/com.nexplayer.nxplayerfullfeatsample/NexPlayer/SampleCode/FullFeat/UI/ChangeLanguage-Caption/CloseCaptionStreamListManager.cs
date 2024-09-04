using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NexPlayerAPI;


namespace NexPlayerSample
{
    public class CloseCaptionStreamListManager : MonoBehaviour
    {
        public NexPlayer player;

        private NexPlayerCaptionStream[] captionStreams = null;
        private List<GameObject> closedCaptionList = new List<GameObject>();
        public GameObject captionBtnPrefab;
        private GameObject cloneStreamObject = null;

        public void FindReferences(NexPlayer NxP)
        {
            player = NxP;
            captionBtnPrefab = transform.GetChild(0).gameObject;
        }
        void SetCaptionOnClick(NexPlayerCaptionStream captionInfo)
        {
            player.SetCaptionStream(captionInfo);
        }

        void CreateStreamButtons()
        {
            int count = 0;

            try
            {
                captionStreams = player.GetCaptionStreamList();
            }
            catch (System.Exception e)
            {
                Debug.Log($"There isn't any subtitle. {e.Message}");
            }

            if (captionStreams != null)
            {
                count = captionStreams.Length;
                Debug.Log(captionStreams.Length);
            }

            for (int i = 0; i < count; i++)
            {
                cloneStreamObject = Instantiate(captionBtnPrefab, transform);
                cloneStreamObject.SetActive(true);

                Button cloneBtn = cloneStreamObject.GetComponent<Button>();
                NexCaptionInfo captionInfo = cloneBtn.GetComponent<NexCaptionInfo>();
                if (captionInfo != null && captionStreams != null)
                {
                    captionInfo.subtitleInfo = captionStreams[i];
                    cloneBtn.onClick.AddListener(() => SetCaptionOnClick(captionInfo.subtitleInfo));
                }

                Text captionName = cloneBtn.GetComponentInChildren<Text>();
                captionName.text = captionStreams[i].name;
                if (cloneStreamObject != null)
                {
                    closedCaptionList.Add(cloneStreamObject);
                }
                if(string.IsNullOrEmpty(captionStreams[i].name))
                {
                    cloneStreamObject.SetActive(false);
                }
            }
            captionBtnPrefab.SetActive(false);
        }

        public void EmptyStreamButtons()
        {
            for (int i = 0; i < closedCaptionList.Count; i++)
            {
                GameObject closedCaptionObj = closedCaptionList[i];
                Destroy(closedCaptionObj);
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