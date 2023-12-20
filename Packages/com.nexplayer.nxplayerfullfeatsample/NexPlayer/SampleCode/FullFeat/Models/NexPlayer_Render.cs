using UnityEngine;
using UnityEngine.UI;
using NexPlayerAPI;
using System;
using UnityEngine.Serialization;

namespace NexPlayerSample
{
    public partial class NexPlayer
    {
        [SerializeField]
        private NexRenderMode startingRenderMode = NexRenderMode.RawImage;

        [Tooltip("Array of renderer component which texture will be updated")]
        [SerializeField]
        [FormerlySerializedAs("renderer")]
        public Renderer renderTarget;

        [Obsolete("Use renderTarget instead")]
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public Renderer renderer
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
        {
            get { return renderTarget; }
            set { renderTarget = value; }
        }
        [SerializeField]
        public RenderTexture renderTexture;

        [SerializeField]
        public Renderer renderTextureRenderer;

        [SerializeField]
        public RawImage rawImage;

        //Add Renderer
        private int cubeIndex = 0;

        public void SetStartingRenderMode(NexRenderMode renderMode)
        {
            startingRenderMode = renderMode;
        }

        public void ChangeRenderMode(int renderMode)
        {
            Log($"ChangeRenderMode({renderMode}) called");

            bool bChanged = false;

            NexRenderMode nexRenderMode = (NexRenderMode)renderMode;
            switch (nexRenderMode)
            {
                case NexRenderMode.RenderTexture:
                    {
                        if (MultistreamController != null && MultistreamController.IsMultiStream())
                        {
                            MultistreamController.SetMultiStreamRender();
                        }
                        else
                        {
                            SetTargetTexture(renderTexture);
                        }

                        bChanged = true;
                    }
                    break;

                case NexRenderMode.MaterialOverride:
                    {
                        SetTargetMaterialRenderer(renderTarget);

                        bChanged = true;
                    }
                    break;

                case NexRenderMode.RawImage:
                    {
                        if (MultistreamController != null && MultistreamController.IsMultiStream())
                        {
                            MultistreamController.SetMultiStreamRender();
                        }
                        else
                        {
                            SetTargetRawImage(rawImage);
                        }

                        bChanged = true;
                    }
                    break;

                default:
                    break;
            }

            if (bChanged)
            {
                SetRenderMode((NexRenderMode)renderMode);

                //Update the renders / Enable / Disable
                if (renderTextureRenderer != null)
                {
                    renderTextureRenderer.enabled = GetRenderMode() == NexRenderMode.RenderTexture;
                }

                if (rawImage != null)
                {
                    rawImage.enabled = GetRenderMode() == NexRenderMode.RawImage;
                }

                if (renderTarget != null)
                {
                    renderTarget.enabled = GetRenderMode() == NexRenderMode.MaterialOverride;
                }
            }
        }

        public void AddRenderer()
        {
            if (cubeIndex < 3 && cubeIndex >= 0)
            {
                cubeIndex++;
                AddMaterialRenderer("Cube" + cubeIndex, GameObject.FindGameObjectWithTag("Cube" + cubeIndex).GetComponent<Renderer>());
            }
        }

        public void RemoveRenderer()
        {
            if (cubeIndex < 4 && cubeIndex > 0)
            {
                RemoveMaterialRenderer("Cube" + cubeIndex);
                cubeIndex--;
            }
        }
    }
}