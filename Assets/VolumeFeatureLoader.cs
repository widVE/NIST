using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeFeatureLoader : MonoBehaviour
{

    public EasyVizAR.FeatureList feature_list;

    public FeatureManager featureManager_reference;


    private void Awake()
    {
        featureManager_reference = GameObject.Find("FeatureManager").GetComponent<FeatureManager>();
        feature_list = featureManager_reference.feature_list;
 
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach (EasyVizAR.Feature feature in feature_list.features)
        {
            // This will add the feature if it is new or update an existing one.
            //if (!feature_dictionary.ContainsValue(feature))
            featureManager_reference.UpdateFeatureFromServer(feature);
        }

    }

}
