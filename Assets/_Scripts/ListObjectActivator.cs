using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ListObjectActivator : MonoBehaviour
{
    public List<GameObject> activated_list;
    public int active_index = 0;
    //public GameObject[] ;

    // Activates the next item in the list when called, and loops back through
    [ContextMenu("Cycle Active")]
    public void cycleActiveItem()
    {
        
        
        //Disable all the list objects
        foreach (GameObject item in activated_list)
        {
            if (item != null)
            {
                item.SetActive(false);
            }
        }

        //If we are in bounds activate the item at our current index
        //otherwise we need to rest to the beginning of the list
        //what I really want is a looped list, so that might be possible
        //to just use for each on
        if (active_index < activated_list.Count)
        {
            activated_list[active_index].SetActive(true);
        }
        else
        {
            active_index = 0;
            activated_list[active_index].SetActive(true);
        }
        
        active_index++;
        
        // This looks like it goes out of bounds because we see it go 1,2,3 in the editor, but the 3 is never used
        //as an index because it leades to the else branch i htink
    }
}
