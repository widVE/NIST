using System;
using UnityEngine;
using NexPlayerAPI;

namespace NexPlayerSample
{
    public partial class NexPlayer
    {
        public void MonitorRendererSizeChange()
        {
            Log("MonitorRendererSizeChange() called");

            if (!CanResize())
            {
                currentRendererSize = Vector2.zero;
                Log("Avoiding resizing");
                return;
            }

            Vector2 rendererSize = videoCanvasHelper.GetRendererSize();
            if (rendererSize != Vector2.zero && currentRendererSize != rendererSize)
            {
                currentRendererSize = rendererSize;
                SetPlayerOutputPosition(ratio, currentRendererSize);
            }

        }

        public void ChangeAspectRatio(VideoAspectRatio aspectRatio)
        {
            NexPlayerResizeController.ChangeAspectRatio(this, aspectRatio);
        }

        private bool CanResize()
        {
            if (UnityEngine.XR.XRSettings.enabled)
            {
                return false;
            }

            return IsPlayerCreated() && videoCanvasHelper != null
                && !(Application.platform == RuntimePlatform.Android && GetRenderMode() == NexRenderMode.MaterialOverride)
                && !(Application.platform == RuntimePlatform.IPhonePlayer && GetRenderMode() == NexRenderMode.RenderTexture);
        }
    }
}
