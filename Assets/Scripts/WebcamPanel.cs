using UnityEngine;
using System.Collections;
using System.Linq;


public class WebcamPanel : MonoBehaviour
{
    const string THERMAL_CAMERA_NAME = "PureThermal (fw:v1.3.0)";

    UnityEngine.Windows.WebCam.PhotoCapture photoCaptureObject = null;
    Texture2D targetTexture = null;

    public bool showAnyCamera = false;
    public string preferredCamera = "PureThermal (fw:v1.3.0)";

    // Use this for initialization
    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        WebCamTexture webcamTexture = new WebCamTexture();
        bool foundPreferredCamera = false;

        if (devices.Length > 0)
        {
            webcamTexture.deviceName = devices[0].name;
        }

        for (int i = 0; i < devices.Length; i++)
        {   
            if (devices[i].name == preferredCamera)
            {
                Debug.Log(string.Format("Found camera '{0}' (preferred camera)", devices[i].name));
                webcamTexture.deviceName = devices[i].name;
                foundPreferredCamera = true;
            }
            else
            {
                Debug.Log(string.Format("Found camera '{0}'", devices[i].name));
            }
        }

        if (foundPreferredCamera || (showAnyCamera && devices.Length > 0))
        {
            // Create a GameObject to which the texture can be applied
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Renderer quadRenderer = quad.GetComponent<Renderer>() as Renderer;

            var shader = Shader.Find("Unlit/Texture");
            if (shader != null)
            {
                quadRenderer.material = new Material(shader);
            }

            quad.transform.parent = this.transform;
            quad.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);

            quadRenderer.material.SetTexture("_MainTex", webcamTexture);

            webcamTexture.Play();
        }
    }
}