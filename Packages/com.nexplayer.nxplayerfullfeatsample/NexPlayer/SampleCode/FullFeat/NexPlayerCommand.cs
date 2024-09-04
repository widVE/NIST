using System;
using UnityEngine;

namespace NexPlayerAPI
{
    public abstract class NexPlayerCommand : MonoBehaviour
    {
        public abstract void Execute(NexPlayerBehaviour nexPlayer, Action action);
    }
}