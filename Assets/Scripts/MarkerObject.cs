using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerObject : MonoBehaviour
{
    //public EasyVizAR.Feature feature;
    //public GameObject marker;
    public int feature_ID = -1;
    public FeatureManager manager_script = null;

    // Notify the Feature Manager that this marker has changed so that it can then notify the server.
    public void UpdatedWithID()
    {
        //StartCoroutine(waiter());
        manager_script.UpdateFeature(feature_ID);
    }

    IEnumerator waiter()
    {
      
        //Wait for 2 seconds
        yield return new WaitForSeconds(10);

    }


}
