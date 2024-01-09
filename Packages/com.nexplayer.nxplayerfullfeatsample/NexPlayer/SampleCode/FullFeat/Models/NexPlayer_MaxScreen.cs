using NexPlayerAPI;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace NexPlayerSample
{
    public partial class NexPlayer
    {
        bool isMaximized = false;
        int prevRendMode = 0;

        //Screen maximization
        public void MaximizeScreen()
        {
            NexPlayerMaxScreenController.MaximizeScreen(this, ref isMaximized, ref prevRendMode).material = MaxScreenMaterial;
        }
    }
}
