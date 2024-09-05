using Dummiesman;
using System.IO;
using System.Text;
using UnityEngine;

public class ObjFromStream : MonoBehaviour {

    public GameObject spawn_parent;
    //awake
    void Awake () {
        //make www
        var www = new WWW("https://easyvizar.wings.cs.wisc.edu/locations/70fc63b6-61ca-4a2f-8e0f-7b2c265a9c9f/model");
        while (!www.isDone)
            System.Threading.Thread.Sleep(1);
        
        //create stream and load
        var textStream = new MemoryStream(Encoding.UTF8.GetBytes(www.text));

        GameObject loadedObj = new OBJLoader().Load(textStream);

        //set loadeObj position to 0 to its parent moveable map
        if (loadedObj != null && spawn_parent != null)
        {
            loadedObj.transform.SetParent(spawn_parent.transform);
            spawn_parent.transform.localScale = new Vector3(1f, 1f, 1f);
            loadedObj.transform.localScale = new Vector3(-1f, 1f, 1f);
            loadedObj.transform.localPosition = Vector3.zero;
        }



    }
}
