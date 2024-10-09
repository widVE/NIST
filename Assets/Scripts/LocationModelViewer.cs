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
    private GameObject modelParent = null;

    // Store reference to newest GameObject for each surface, keyed on surface ID.
    private Dictionary<string, GameObject> surfaces = new();

    void Start()
    {
        modelParent = new GameObject();
        modelParent.name = "model";
        modelParent.transform.parent = transform;
        modelParent.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        modelParent.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

        var model = LocationModelLoader.Instance.GetModel();
        CloneModelComponents(model);

        LocationModelLoader.Instance.ModelImported += (o, ev) =>
        {
            CloneModelComponents(ev.model);
        };
    }

    private void CloneModelComponents(GameObject model)
    {
        // Two cases: we are passed a GameObject that contains the MeshFilter directly,
        // or we are passed a GameObject which is a container for other GameObjects.
        if (model.GetComponent<MeshFilter>() != null)
        {
            var clone = Instantiate(model, modelParent.transform);
            clone.name = model.name;

            if (surfaces.ContainsKey(model.name))
            {
                Destroy(surfaces[model.name]);
            }
            surfaces[model.name] = clone;
        }
        else
        {
            // Iterate over the components of the object and save a reference to each.
            // For location models, this iterates the individual surfaces, which each have their own surface ID.
            foreach (Transform tf in model.transform)
            {
                CloneModelComponents(tf.gameObject);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
