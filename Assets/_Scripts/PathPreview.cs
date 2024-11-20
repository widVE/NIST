using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(LineRenderer))]
public class PathPreview : MonoBehaviour
{
    public float updateInterval = 0.1f;

    private LineRenderer lineRenderer;

    private bool isDrawing = false;
    private GameObject cursor = null;
    private Vector3 startPosition = new();
    private NavMeshPath navMeshPath;

    private void Awake()
    {
        navMeshPath = new();
    }

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void StartPreview(GameObject cursor)
    {
        isDrawing = true;
        this.cursor = cursor;
        startPosition = cursor.transform.localPosition;
        StartCoroutine(UpdatePathCoroutine());
    }

    public void StopPreview()
    {
        isDrawing = false;
		lineRenderer.positionCount = 0;
    }

    private IEnumerator UpdatePathCoroutine()
    {
        while (isDrawing)
        {
            var found = NavMesh.CalculatePath(startPosition, cursor.transform.localPosition, NavMesh.AllAreas, navMeshPath);
            if (found)
            {
                lineRenderer.positionCount = navMeshPath.corners.Length;
                lineRenderer.SetPositions(navMeshPath.corners);
            }
            else
            {
                lineRenderer.positionCount = 0;
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }
}
