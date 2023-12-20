using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NexPlayerAPI
{
    public class NexPlayerRenderController : NexPlayerRenderControllerInternal
    {
        [System.Obsolete("Use the Init method instead")]
        public NexPlayerRenderController(NexPlayerBehaviour player) : base(player)
        {
        }
    }
}
