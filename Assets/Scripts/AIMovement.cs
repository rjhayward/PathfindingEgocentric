using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIMovement : MonoBehaviour
{
    public Transform destination;
    NavMeshAgent agent;
    private int baseSpeed;

    void Start()
    {
        baseSpeed = 5;
        agent = GetComponent<NavMeshAgent>();
        agent.destination = destination.position;
        agent.isStopped = false;

    }

    // Update is called once per frame
    void Update()
    {
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(destination.position, path);

        // if destination is blocked, then stop the agent
        if (!agent.isStopped && path.status != NavMeshPathStatus.PathComplete)
        {
            agent.ResetPath();
            agent.isStopped = true;

            Debug.Log("Destination Blocked");
        }
        else if (agent.isStopped && path.status == NavMeshPathStatus.PathComplete)
        {
            agent.destination = destination.position;
            //agent.SetPath(path);
            Debug.Log("Destination Found");
            agent.isStopped = false;
        }

        NavMeshHit navHit;
        agent.SamplePathPosition(NavMesh.AllAreas, 0.0f, out navHit);

        // sets the speed based on the cost of the terrain beneath agent
        agent.speed = baseSpeed * 1 / agent.GetAreaCost(GetAreaListFromNavMeshHit(navHit)[0]);

    }

    // function which takes our navhit and produces a list of area indices based on its area mask
    List<int> GetAreaListFromNavMeshHit(NavMeshHit navHit)
    {
        List<int> areaList = new List<int>();

        int maskValue = navHit.mask;
        //Debug.Log(maskValue);

        int areaIndex = 0;

        while (maskValue >= 1)
        {
            if (maskValue % 2 == 1)
            {
                areaList.Add(areaIndex);
            }

            areaIndex++;
            maskValue >>= 1;
        }

        return areaList;
    }

}
