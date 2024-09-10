using Dummiesman;
using System.IO;
using System.Text;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    void Start()
    {
        //make www
        var www = new WWW("https://easyvizar.wings.cs.wisc.edu/locations/bec66948-ab50-421a-9d12-281a77f228e3/model");
        while (!www.isDone)
            System.Threading.Thread.Sleep(1);

        //create stream and load
        var textStream = new MemoryStream(Encoding.UTF8.GetBytes(www.text));
        var loadedObj = new OBJLoader().Load(textStream);
    }
}
