using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using NativeWebSocket;
using System.Diagnostics;


public class EasyVizARWebSocketConnection : MonoBehaviour
{
    [SerializeField]
    private string _webSocketURL = "ws://easyvizar.wings.cs.wisc.edu:5000/ws";
    public string WebSocketURL
    {
        get { return _webSocketURL; }
        set { _webSocketURL = value; }
    }

    [SerializeField]
    string _locationId = "69e92dff-7138-4091-89c4-ed073035bfe6";
    public string LocationID
    {
        get { return _locationId; }
        set { _locationId = value; }
    }

    public GameObject featureManager = null;
    public GameObject headsetManager = null;
    public GameObject map_parent;

    [SerializeField]
    float _updateInterval = 0.2f;
    public bool post_data = false;

    [SerializeField]
    bool _echoMessages = true;

    private WebSocket _ws = null;
    private bool isConnected = false;
    
    private Camera _mainCamera;
    private float _lastUpdated = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (!featureManager)
        {
            featureManager = GameObject.Find("FeatureManager");
        }
        
        if (!headsetManager)
        {
            headsetManager = GameObject.Find("EasyVizARHeadsetManager");
        }

        _mainCamera = Camera.main;

        // Otherwise, wait for a QR code to be scanned.
        QRScanner.Instance.LocationChanged += async (o, ev) =>
        {
            if (isConnected)
            {
                await _ws.Close();
                isConnected = false;
            }
            _locationId = ev.LocationID;
            string scheme = ev.UseHTTPS ? "wss" : "ws";
            _webSocketURL = string.Format("{0}://{1}/ws", scheme, ev.Server);
            _ws = initializeWebSocket();

            // Connect returns a Task that only completes after the connection closes.
            // If we 'await' it here, it might block other code that needs to respond to the LocationChanged event.
            var _ = _ws.Connect();
        };
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

        if (isConnected && post_data)
        {
            float t = UnityEngine.Time.time;
            if (t - _lastUpdated > _updateInterval)
            {
                var pos = _mainCamera.transform.position;
                var rot = _mainCamera.transform.rotation;

                object[] parts = { "move", pos[0], pos[1], pos[2], rot[0], rot[1], rot[2], rot[3] };
                string message = string.Join(" ", parts);

                Task.Run(() => _ws.SendText(message));
                _lastUpdated = t;
            }
        }
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
        var headers = new Dictionary<string, string>
        {
            { "Authorization", EasyVizARServer.Instance.GetAuthorizationHeader() },
            { "X-Ignore", "ignore" }
        };

        // Request "json-with-header-v2" subprotocol from the server. This results in received messages
        // having a useful header before the JSON body so that we know how to deserialize and
        // route the message.
        //
        // v2 simplified the message format for improved performance.
        string subprotocol = "json-with-header-v2";

        var ws = new WebSocket(_webSocketURL, subprotocol, headers);

        ws.OnError += (error) =>
        {
            UnityEngine.Debug.Log("WS Error: " + error);
        };

        ws.OnOpen += this.onConnected;
        ws.OnMessage += this.onMessage;

        return ws;
    }

    private async void onConnected()
    {
        UnityEngine.Debug.Log("WS Connected: " + _webSocketURL);

        // Suppress event messages that were triggered by this user.
        // Note: this feature would prevent us from receiving a notification from the server
        // when the headset navigation target should be changed.
        if (!_echoMessages)
        {
            await _ws.SendText("echo off");
        }

        // Tell the server to filter events pertaining to the current location.
        var event_uri = $"/locations/{_locationId}/*";

        if (headsetManager)
        {
            await _ws.SendText("subscribe location-headsets:created " + event_uri);
            await _ws.SendText("subscribe location-headsets:updated " + event_uri);
            await _ws.SendText("subscribe location-headsets:deleted " + event_uri);
        }

        if (featureManager) {
            await _ws.SendText("subscribe features:created " + event_uri);
            await _ws.SendText("subscribe features:updated " + event_uri);
            await _ws.SendText("subscribe features:deleted " + event_uri);
        }

        if (NavigationManager.Instance)
        {
            await _ws.SendText("subscribe map-paths:created " + event_uri);
            await _ws.SendText("subscribe map-paths:updated " + event_uri);
            await _ws.SendText("subscribe map-paths:deleted " + event_uri);
        }

        if (LocationModelLoader.Instance)
        {
            await _ws.SendText("subscribe surfaces:created " + event_uri);
            await _ws.SendText("subscribe surfaces:updated " + event_uri);
            await _ws.SendText("subscribe surfaces:deleted " + event_uri);
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
            UnityEngine.Debug.Log("Warning: received malformed websocket message from server");
            return;
        }

        string event_type = parts[0];
        string event_uri = parts[1];
        string event_body = parts[2];

        switch (event_type)
        {
            case "location-headsets:created":
                {
                    var obj = JsonUtility.FromJson<EasyVizAR.Headset>(event_body);
                    headsetManager.GetComponent<EasyVizARHeadsetManager>().CreateRemoteHeadset(obj);
                    break;
                }
            case "location-headsets:updated":
                {
                    var obj = JsonUtility.FromJson<EasyVizAR.Headset>(event_body);
                    headsetManager.GetComponent<EasyVizARHeadsetManager>().UpdateRemoteHeadset(obj.id, obj);
                    break;
                }
            case "location-headsets:deleted":
                {
                    var obj = JsonUtility.FromJson<EasyVizAR.Headset>(event_body);
                    headsetManager.GetComponent<EasyVizARHeadsetManager>().DeleteRemoteHeadset(obj.id);
                    //Destroy(map_parent.transform.Find(ev.previous.name).gameObject);
                    break;
                }
            case "features:created":
                {
                    var obj = JsonUtility.FromJson<EasyVizAR.Feature>(event_body);
                    featureManager.GetComponent<FeatureManager>().AddFeatureFromServer(obj);
                    break;
                }
            case "features:updated":
                {
                    var obj = JsonUtility.FromJson<EasyVizAR.Feature>(event_body);
                    featureManager.GetComponent<FeatureManager>().UpdateFeatureFromServer(obj);
                    break;
                }
            case "features:deleted":
                {
                    var obj = JsonUtility.FromJson<EasyVizAR.Feature>(event_body);
                    featureManager.GetComponent<FeatureManager>().DeleteFeatureFromServer(obj.id);
                    break;
                }
            case "map-paths:created":
                {
                    var obj = JsonUtility.FromJson<EasyVizAR.MapPath>(event_body);
                    NavigationManager.Instance.UpdateMapPathLineRenderers(obj);
                    break;
                }
            case "map-paths:updated":
                {
                    var obj = JsonUtility.FromJson<EasyVizAR.MapPath>(event_body);
                    NavigationManager.Instance.UpdateMapPathLineRenderers(obj);
                    break;
                }
            case "map-paths:deleted":
                {
                    var obj = JsonUtility.FromJson<EasyVizAR.MapPath>(event_body);
                    NavigationManager.Instance.DeletePathRenderers(obj.id);
                    break;
                }
            case "surfaces:created":
                {
                    var obj = JsonUtility.FromJson<EasyVizAR.Surface>(event_body);
                    LocationModelLoader.Instance.UpdateSurface(obj);
                    break;
                }
            case "surfaces:updated":
                {
                    var obj = JsonUtility.FromJson<EasyVizAR.Surface>(event_body);
                    LocationModelLoader.Instance.UpdateSurface(obj);
                    break;
                }
            case "surfaces:deleted":
                {
                    var obj = JsonUtility.FromJson<EasyVizAR.Surface>(event_body);
                    LocationModelLoader.Instance.DeleteSurface(obj.id);
                    break;
                }
            default:
                {
                    UnityEngine.Debug.Log("Event: " + event_type + " " + event_uri);
                    break;
                }
        }
    }
}
