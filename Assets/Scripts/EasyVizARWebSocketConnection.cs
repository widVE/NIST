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

    [SerializeField]
    string _locationId = "66a4e9f2-e978-4405-988e-e168a9429030";
    public string LocationID
    {
        get { return _locationId; }
        set { _locationId = value; }
    }

    public GameObject featureManager = null;
    public GameObject headsetManager = null;

    // Attach QRScanner GameObject so we can listen for location change events.
    [SerializeField]
    GameObject _qrScanner = null;

    private WebSocket _ws = null;
    private bool isConnected = false;

    // Start is called before the first frame update
    async void Start()
    {
        if (!featureManager)
        {
            featureManager = GameObject.Find("HandMenu_Large_AutoWorldLock_On_HandDrop_Marker_Array");
        }
        
        if (!headsetManager)
        {
            headsetManager = GameObject.Find("EasyVizARHeadsetManager");
        }

        // Otherwise, wait for a QR code to be scanned.
        if (_qrScanner)
        {
            var scanner = _qrScanner.GetComponent<QRScanner>();
            scanner.LocationChanged += async (o, ev) =>
            {
                if (isConnected)
                {
                    await _ws.Close();
                    isConnected = false;
                }
                _locationId = ev.LocationID;
                _webSocketURL = string.Format("ws://{0}/ws", ev.Server);
                _ws = initializeWebSocket();
                await _ws.Connect();
            };
        }

#if UNITY_EDITOR
        // In editor mode, use the default server and location ID.
        _ws = initializeWebSocket();

        // Warning: this does not return until the connection eventually closes, I think.
        // Do not put important code after the connect call!
        await _ws.Connect();
#endif
    }

    // Update is called once per frame
    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (_ws is not null)
        {
            _ws.DispatchMessageQueue();
        }
#endif
    }

    private async void OnApplicationQuit()
    {
        if (isConnected)
        {
            await _ws.Close();
            isConnected = false;
        }
    }

    private WebSocket initializeWebSocket()
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

        var ws = new WebSocket(_webSocketURL, subprotocol, headers);

        ws.OnError += (error) =>
        {
            Debug.Log("WS Error: " + error);
        };

        ws.OnOpen += this.onConnected;
        ws.OnMessage += this.onMessage;

        return ws;
    }

    private async void onConnected()
    {
        Debug.Log("WS Connected: " + _webSocketURL);

        if (headsetManager)
        {
            await _ws.SendText("subscribe headsets:created");
            await _ws.SendText("subscribe headsets:updated");
            await _ws.SendText("subscribe headsets:deleted");
        }

        if (featureManager) {
            await _ws.SendText(string.Format("subscribe features:created /locations/{0}/*", _locationId));
            await _ws.SendText(string.Format("subscribe features:updated /locations/{0}/*", _locationId));
            await _ws.SendText(string.Format("subscribe features:deleted /locations/{0}/*", _locationId));
        }

        isConnected = true;
    }

    private void onMessage(byte[] data)
    {
        // Message is in json-with-header format.
        // <event description> <URI> <JSON body>
        // Example:
        // headsets:updated /headsets/123 {"current": {...}, "previous": {...}}
        var message = System.Text.Encoding.UTF8.GetString(data);
        var parts = message.Split(" ", 3);
        if (parts.Length < 3)
        {
            Debug.Log("Warning: received malformed websocket message from server");
            return;
        }

        string event_type = parts[0];
        string event_uri = parts[1];
        string event_body = parts[2];

        switch (event_type)
        {
            case "headsets:created":
                {
                    HeadsetsEvent ev = JsonUtility.FromJson<HeadsetsEvent>(event_body);
                    headsetManager.GetComponent<EasyVizARHeadsetManager>().CreateRemoteHeadset(ev.current);
                    break;
                }
            case "headsets:updated":
                {
                    HeadsetsEvent ev = JsonUtility.FromJson<HeadsetsEvent>(event_body);
                    headsetManager.GetComponent<EasyVizARHeadsetManager>().UpdateRemoteHeadset(ev.previous.name, ev.current);
                    break;
                }
            case "headsets:deleted":
                {
                    HeadsetsEvent ev = JsonUtility.FromJson<HeadsetsEvent>(event_body);
                    headsetManager.GetComponent<EasyVizARHeadsetManager>().DeleteRemoteHeadset(ev.previous.name);
                    break;
                }
            case "features:created":
                {
                    FeaturesEvent ev = JsonUtility.FromJson<FeaturesEvent>(event_body);
                    featureManager.GetComponent<FeatureManager>().AddFeatureFromServer(ev.current);
                    break;
                }
            case "features:updated":
                {
                    FeaturesEvent ev = JsonUtility.FromJson<FeaturesEvent>(event_body);
                    featureManager.GetComponent<FeatureManager>().UpdateFeatureFromServer(ev.current);
                    break;
                }
            case "features:deleted":
                {
                    FeaturesEvent ev = JsonUtility.FromJson<FeaturesEvent>(event_body);
                    featureManager.GetComponent<FeatureManager>().DeleteFeatureFromServer(ev.previous.id);
                    break;
                }
            default:
                {
                    Debug.Log("Event: " + event_type + " " + event_uri);
                    break;
                }
        }
    }
}
