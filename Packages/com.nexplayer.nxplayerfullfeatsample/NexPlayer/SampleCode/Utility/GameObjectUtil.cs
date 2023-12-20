using System;
using UnityEngine;

namespace NexUtility
{
    [Obsolete("This class has been deprecated and will be removed in the upcoming releases.")]
    public class GameObjectUtil : MonoBehaviour
    {
        [Obsolete("To toggle the active state of a GameObject, use gameObject.SetActive(!gameObject.activeSelf) instead.")]
        public void ChangeActivateGameObject(GameObject gameObject)
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}