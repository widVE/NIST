using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NativeWebSocket;

[System.Serializable]
public class HeadsetsEvent
{
    public EasyVizAR.Headset previous;
    public EasyVizAR.Headset current;
}

public class EasyVizARWebSocketConnection : MonoBehaviour
{
    [SerializeField]
    private string _webSocketURL = "ws://halo05.wings.cs.wisc.edu:5000/ws";
    public string WebSocketURL
    {
        get { return _webSocketURL; }
        set { _webSocketURL = value; }
    }

    private WebSocket _ws = null;

    // Start is called before the first frame update
    async void Start()
    {
        // This is just a placeholder. We may need to set authorization headers later on.
        var headers = new Dictionary<string, string>
        {
            { "X-Ignore", "ignore" }
        };

        // Request "json-with-header" subprotocol from the server. This results in received messages
        // having a useful header before the JSON body so that we know how to deserialize and
        // route the message.
        string subprotocol = "json-with-header";

        _ws = new WebSocket(_webSocketURL, subprotocol, headers);

        _ws.OnOpen += async () =>
        {
            Debug.Log("WS Connected: " + _webSocketURL);
            await _ws.SendText("subscribe headsets:updated");
            await _ws.SendText("subscribe features:created");
        };

        _ws.OnError += (error) =>
        {
            Debug.Log("WS Error: " + error);
        };

        _ws.OnMessage += (data) =>
        {
            // Message is in json-with-header format.
            // <event description> <URI> <JSON body>
            // Example:
            // headsets:updated /headsets/123 {"current": {...}, "previous": {...}}
            var message = System.Text.Encoding.UTF8.GetString(data);
            var parts = message.Split(" ", 3);
            
            if (parts[0] == "headsets:updated")
            {
                HeadsetsEvent ev = JsonUtility.FromJson<HeadsetsEvent>(parts[2]);
                Debug.Log("Headset updated: " + ev.current.name);
            }
            else
            {
                Debug.Log("Event: " + parts[0] + " " + parts[1]);
            }
        };

        await _ws.Connect();
    }

    // Update is called once per frame
    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _ws.DispatchMessageQueue();
#endif
    }

    private async void OnApplicationQuit()
    {
        await _ws.Close();
    }
}
