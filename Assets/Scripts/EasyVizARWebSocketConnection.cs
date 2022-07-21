using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NativeWebSocket;

[System.Serializable]
public class FeaturesEvent
{
    public EasyVizAR.Feature previous;
    public EasyVizAR.Feature current;
}

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

    public GameObject featureManager = null;
    public GameObject headsetManager = null;

    private WebSocket _ws = null;

    // Start is called before the first frame update
    async void Start()
    {
        if (!featureManager)
        {
            featureManager = GameObject.Find("FeatureManager");
        }
        
        if (!headsetManager)
        {
            headsetManager = GameObject.Find("EasyVizARHeadsetManager");
        }

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
            if (headsetManager) {
                await _ws.SendText("subscribe headsets:created");
                await _ws.SendText("subscribe headsets:updated");
                await _ws.SendText("subscribe headsets:deleted");
                Debug.Log("Subscribed to headset events.");
            }
            if (featureManager) {
                await _ws.SendText("subscribe features:created");
                await _ws.SendText("subscribe features:updated");
                await _ws.SendText("subscribe features:deleted");
                Debug.Log("Subscribed to feature events.");
            }
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

            switch(parts[0]) {
                case "headsets:created":
                    {
                        HeadsetsEvent ev = JsonUtility.FromJson<HeadsetsEvent>(parts[2]);
                        headsetManager.GetComponent<EasyVizARHeadsetManager>().CreateRemoteHeadset(ev.current);
                        break;
                    }
                case "headsets:updated":
                    {
                        HeadsetsEvent ev = JsonUtility.FromJson<HeadsetsEvent>(parts[2]);
                        headsetManager.GetComponent<EasyVizARHeadsetManager>().UpdateRemoteHeadset(ev.previous.name, ev.current);
                        break;
                    }
                case "headsets:deleted":
                    {
                        HeadsetsEvent ev = JsonUtility.FromJson<HeadsetsEvent>(parts[2]);
                        headsetManager.GetComponent<EasyVizARHeadsetManager>().DeleteRemoteHeadset(ev.previous.name);
                        break;
                    }
                case "features:created":
                    {
                        FeaturesEvent ev = JsonUtility.FromJson<FeaturesEvent>(parts[2]);
                        featureManager.GetComponent<FeatureManager>().AddFeatureFromServer(ev.current);
                        break;
                    }
                case "features:updated":
                    {
                        FeaturesEvent ev = JsonUtility.FromJson<FeaturesEvent>(parts[2]);
                        featureManager.GetComponent<FeatureManager>().UpdateFeatureFromServer(ev.current);
                        break;
                    }
                case "features:deleted":
                    {
                        FeaturesEvent ev = JsonUtility.FromJson<FeaturesEvent>(parts[2]);
                        featureManager.GetComponent<FeatureManager>().DeleteFeatureFromServer(ev.previous.id);
                        break;
                    }
                default:
                    {
                        Debug.Log("Event: " + parts[0] + " " + parts[1]);
                        break;
                    }
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
