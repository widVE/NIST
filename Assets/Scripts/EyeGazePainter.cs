// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos.EyeTracking
{
    /// <summary>
    /// Sample for allowing a GameObject to follow the user's eye gaze
    /// at a given distance of "DefaultDistanceInMeters".
    /// </summary>
    [AddComponentMenu("Scripts/MRTK/Examples/FollowEyeGaze")]
    public class EyeGazePainter : MonoBehaviour
    {
        [Tooltip("Display the game object along the eye gaze ray at a default distance (in meters).")]
        [SerializeField]
        private float defaultDistanceInMeters = 2f;

        public GameObject particle_prefab;

        private void Update()
        {
            var eyeGazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
            if (eyeGazeProvider != null)
            {
                
                gameObject.transform.position = eyeGazeProvider.GazeOrigin + eyeGazeProvider.GazeDirection.normalized * defaultDistanceInMeters;

                Ray ray_to_gaze = new Ray(CameraCache.Main.transform.position, eyeGazeProvider.GazeDirection.normalized);
                RaycastHit hitInfo;
                UnityEngine.Physics.Raycast(ray_to_gaze, out hitInfo);

                gameObject.transform.position = hitInfo.point;

                SpawnParticle(position: hitInfo.point, Quaternion.FromToRotation(Vector3.back, hitInfo.normal));
            }
        }

        void SpawnParticle(Vector3 position, Quaternion rotation)
        {
            // Instantiate a particle prefab at the specified position
            Instantiate(particle_prefab, position, rotation);
        }

        //Generate a coroutine that calls itself every 0.1 seconds
 
        IEnumerator SpawnParticleCoroutine()
        {


            //Wait for 0.1 seconds
            yield return new WaitForSeconds(0.1f);

            //Call the coroutine again
            StartCoroutine(SpawnParticleCoroutine());
        }


    }


}
