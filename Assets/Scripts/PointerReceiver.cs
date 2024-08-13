using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointerReceiver : MonoBehaviour, IMixedRealityPointerHandler
{
    public UnityEngine.Events.UnityAction<Vector3, Vector3, Quaternion> pointerClickedHanler;

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        if (pointerClickedHanler != null)
        {
            var result = eventData.Pointer.Result;
            var spawnPosition = result.Details.Point;
            var spawnRotation = Quaternion.LookRotation(-result.Details.Normal);
            pointerClickedHanler(eventData.Pointer.Position, spawnPosition, spawnRotation);
        }
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        //throw new System.NotImplementedException();
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
       // throw new System.NotImplementedException();
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        //throw new System.NotImplementedException();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
