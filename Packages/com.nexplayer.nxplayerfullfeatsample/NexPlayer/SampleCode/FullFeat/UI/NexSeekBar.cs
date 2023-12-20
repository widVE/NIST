using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NexPlayerSample
{
    public class NexSeekBar : MonoBehaviour, IPointerDownHandler, IEndDragHandler, IPointerUpHandler
    {
        [Tooltip("The graphic used for the sliding secondary “handle” part of the control")]
        public RectTransform handleRect;

        [Range(0.0f, 1.0f)]
        [Tooltip("Current numeric value of the secondary slider. If the value is set in the inspector it will be used as the initial value, but this will change at runtime when the value changes.")]
        public float secondaryValue;

        private Slider mainSlider;

        [SerializeField]
        private NexPlayer nexPlayer;


        public void FindReferences(NexPlayer npr)
        {
            handleRect = GameObject.Find("NexPlayer_UI/NexControlBar/SeekBar_Canvas/NexSeekBar/FillBufferArea/Fill").GetComponent<RectTransform>();
            nexPlayer = npr;
        }

        void Awake()
        {
            mainSlider = GetComponent<Slider>();
            mainSlider.targetGraphic = GameObject.Find("NexPlayer_UI/NexControlBar/SeekBar_Canvas/NexSeekBar/HandleSlide Area/Handle").GetComponent<Image>();
            mainSlider.onValueChanged.AddListener(delegate {
                AllowSeek(true);
                nexPlayer.SetIsSeeking(false);
            });
        }

        void Update()
        {
            handleRect.anchorMax = new Vector2(secondaryValue, 1.0f);
        }

        /// <summary>
        /// Set the secondary value of the SeekBar. Can be used to represent the buffered time
        /// </summary>
        /// <param name="value">secondary value of the seekBar</param>
        public void SetSecondaryValue(float value)
        {
            if (float.IsNaN(value))
            {
                Debug.LogWarning("Seekbar secondary value is not a valid number");
                return;
            }

            secondaryValue = value;
        }

        /// <summary>
        /// Set the main value of the SeekBar. Can be used to represent the current time of the playback
        /// </summary>
        /// <param name="value">primary value of the seekBar</param>
        public void SetValue(float value)
        {
            if (float.IsNaN(value))
            {
                Debug.LogWarning("value is not a number");
                return;
            }

            // In case the component is disabled at the beginning
            if (mainSlider == null) mainSlider = GetComponent<Slider>();

            mainSlider.value = value;
        }

        /// <summary>
        /// Returns the main value of the seekBar
        /// </summary>
        /// <returns>The main value of the seekBar</returns>
        public float GetValue()
        {
            return mainSlider.value;
        }

        public void AllowSeek(bool value)
        {
            if (nexPlayer)
                nexPlayer.allowSeek = value;
            else
                Debug.Log("There isn't any player");

            if (nexPlayer.allowSeek)
                nexPlayer.Seek();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            AllowSeek(true);
            nexPlayer.SetIsSeeking(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AllowSeek(true);
            nexPlayer.SetIsSeeking(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AllowSeek(true);
            nexPlayer.SetIsSeeking(false);
        }
    }
}
