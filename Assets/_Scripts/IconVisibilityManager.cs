using UnityEngine;

public class IconVisibilityManager : MonoBehaviour
{
    [SerializeField] private GameObject clipping_sphere; // Reference to the collider you want to check against
    private GameObject icon_parent;

    private void Start()
    {
        icon_parent = this.gameObject;

        // Invoke the IconChildVisibility method every .5 seconds
        InvokeRepeating("IconChildVisibility", 0, 0.5f);
    }

    ////Iterate though all the children of the icon parent and add them to the icon_objects array if they are tagged icon
    //private void IconChildVisibility()
    //{
    //    foreach (Transform child in icon_parent.transform)
    //    {
    //        if (child.CompareTag("Icon"))
    //        {
    //            if (IsObjectInsideCollider(child.gameObject))
    //            {
    //                child.gameObject.SetActive(true); // Enable the icon object if it's inside the collider
    //            }
    //            else
    //            {
    //                child.gameObject.SetActive(false); // Set the icon object as inactive if it's outside the collider
    //            }
    //        }
    //    }
    //}

    //Iterate though all the children of the icon parent and disable them if they are further away from the target collider than the x scale of the target collider
    private void IconChildVisibility()
    {
        foreach (Transform child in icon_parent.transform)
        {
            if (child.CompareTag("Icon"))
            {
                float distance = Vector3.Distance(clipping_sphere.transform.position, child.position);

                //Local scale is divided by 2 because the scale is the diameter of the sphere
                if (distance < clipping_sphere.transform.localScale.x/2)
                {
                    child.gameObject.SetActive(true); // Enable the icon object if it's inside the collider
                }
                else
                {
                    child.gameObject.SetActive(false); // Set the icon object as inactive if it's outside the collider
                }
            }
        }
    }

    // Check if the object is inside the collider

    //private bool IsObjectInsideCollider(GameObject icon)
    //{
    //    // Check if the object's bounds intersect with the collider's bounds
    //    //return target_collider.x.Intersects(icon.GetComponent<Collider>().bounds);
    //    return false;
    //}
}
