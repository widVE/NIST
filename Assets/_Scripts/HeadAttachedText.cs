using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadAttachedText : MonoBehaviour
{
    private class HeadAttachedMessage
    {
        public string Message;
        public float DurationSeconds;
        public HeadAttachedMessage(string message, float duration)
        {
            Message = message;
            DurationSeconds = duration;
        }
    }

    private Queue<HeadAttachedMessage> messageQueue = new();
    private TMPro.TMP_Text textObject;

    public static HeadAttachedText Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("Warning: multiple instances of HeadAttachedText created when there should only be one.");
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        textObject = GetComponent<TMPro.TMP_Text>();
        if (textObject)
        {
            StartCoroutine(DisplayRoutine());
        }
    }
    IEnumerator DisplayRoutine()
    {
        while(true)
        {
            if (messageQueue.TryDequeue(out HeadAttachedMessage message))
            {
                textObject.text = message.Message;
                yield return new WaitForSeconds(message.DurationSeconds);
                textObject.text = "";
            }
            else
            {
                yield return null;
            }
        }
    }
    public void EnqueueMessage(string message, float duration)
    {
        messageQueue.Enqueue(new HeadAttachedMessage(message, duration));
    }
}
