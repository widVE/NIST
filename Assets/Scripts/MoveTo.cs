// MoveTo.cs
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class MoveTo : MonoBehaviour
{

    public Transform goal;
    Vector3 destination;
    private NavMeshAgent agent;

    

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        destination = agent.destination;
    }

    void Update()
    {
        //if the goal hasn't moved, don't update the path
        //if (agent.destination != goal.position)
        //{
        //    agent.destination = goal.position;
        //}

        // Update destination if the target moves one unit
        if (Vector3.Distance(destination, goal.position) > 1.0f)
        {
            destination = goal.position;
            agent.destination = destination;
        }
    }
}