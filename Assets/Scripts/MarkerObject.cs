using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerObject : MonoBehaviour
{
    //public EasyVizAR.Feature feature;
    //public GameObject marker;
    public int feature_ID = -1;
    public FeatureManager manager_script = null;


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
