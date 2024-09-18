using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Location Model Viewer
 * 
 * Attach this script to a game object to render a location volumetric map.
 * The script will automatically keep the model up-to-date via events from
 * the Location Model Loader script. The general idea is to have one authoritative
 * model managed by the Location Model Loader and potentially multiple viewers
 * with different scale, rotation, material, or other viewing parameters.
 * 
 * There might be slightly different behavior if the LocationModelViewer
 * is active at application startup vs. created later after a model has been
 * loaded. Both use cases should work, though.
 * 
 * Changing location or trying to load multiple different models during the
 * lifetime of the application is not well supported at this time. Expected
 * problems include mingling of meshes and missed updates for some of the models.
 */
public class LocationModelViewer : MonoBehaviour
{
    // Store reference to newest GameObject for each surface, keyed on surface ID.
    private Dictionary<string, GameObject> surfaces = new();

    void Start()
    {
        var model = LocationModelLoader.Instance.GetModel();
        CloneModelComponents(model);

        LocationModelLoader.Instance.ModelImported += (o, ev) =>
        {
            CloneModelComponents(ev.model);
        };
    }

    private void CloneModelComponents(GameObject model)
    {
        var clone = Instantiate(model, this.transform);

        // Iterate over the components of the object and save a reference to each.
        // For location models, this iterates the individual surfaces, which each have their own surface ID.
        foreach (Transform tf in clone.transform)
        {
            if (surfaces.ContainsKey(tf.name))
            {
                Debug.Log("Destroy object " + tf.name);
                Destroy(surfaces[tf.name]);
            }
            surfaces[tf.name] = tf.gameObject;
            tf.gameObject.SetActive(true);
        }

        // Also create a reference to the parent object. This ensures that for individually loaded
        // surfaces, we store a reference to the top-level game object, rather than its child.
        surfaces[model.name] = clone;

        clone.name = model.name;
        clone.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
