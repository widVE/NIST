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


    [SerializeField] private bool isDrawing = false;
    [SerializeField] private GameObject cursor = null;
    [SerializeField] private Vector3 startPosition = new();
    [SerializeField] private NavMeshPath navMeshPath;
    [SerializeField] private GameObject user_avatar;

    private void Awake()
    {
        navMeshPath = new();
    }

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void SetUser(GameObject user)
    { 
        user_avatar = user;
    }

    public void StartPreview(GameObject cursor, GameObject user)
    {
        isDrawing = true;
        this.cursor = cursor;
        user_avatar = user;
        startPosition = user_avatar.transform.localPosition;
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
