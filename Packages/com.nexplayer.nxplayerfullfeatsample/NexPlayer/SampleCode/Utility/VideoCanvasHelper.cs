using UnityEngine;
using NexPlayerAPI;
using NexPlayerSample;

namespace NexUtility
{
    public class VideoCanvasHelper
    {
        private NexPlayer nexPlayer;
        private Vector2 rendererSize = Vector2.zero;

        public VideoCanvasHelper(NexPlayer nexPlayer)
        {
            this.nexPlayer = nexPlayer;
        }

        public Vector2 GetRendererSize()
        {
            var renderMode = nexPlayer.GetRenderMode();

            if (renderMode == NexRenderMode.RawImage)
            {
                if (nexPlayer.rawImage != null)
                {
                    rendererSize = NexUtil.GetPixelSizeOfRawImage(nexPlayer.rawImage);
                }
            }
            else
            {
                if (Camera.main != null)
                {
                    MeshRenderer meshRenderer = null;

                    if (renderMode == NexRenderMode.MaterialOverride)
                    {
                        if (nexPlayer.renderTarget != null)
                        {
                            meshRenderer = nexPlayer.renderTarget.GetComponent<MeshRenderer>();
                        }
                    }
                    else if (renderMode == NexRenderMode.RenderTexture)
                    {
                        if (nexPlayer.renderTextureRenderer != null && nexPlayer.renderTexture != null)
                        {
                            meshRenderer = nexPlayer.renderTextureRenderer.GetComponent<MeshRenderer>();
                        }
                    }

                    if (meshRenderer != null)
                    {
                        rendererSize = NexUtil.GetPixelSizeOfMeshRenderer(meshRenderer, Camera.main);
                    }
                }
            }

            return rendererSize;
        }
    }
}