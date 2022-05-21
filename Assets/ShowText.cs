using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowText : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 parentPos = transform.parent.position;
		GetComponent<TMPro.TextMeshPro>().text = parentPos.ToString("F4");
    }
}
