using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIMovement : MonoBehaviour
{
    public GameObject destinations;
    NavMeshAgent agent;
    private int baseSpeed;

    private Transform currentDestination;
    private bool hasBegunRoute;

    private List<Transform> stationList;

    private List<Transform> destinationList;
    private int currentDestinationIndex;

    private List<List<float>> transitionMatrix;

    void Start()
    {
        hasBegunRoute = false;
        baseSpeed = 5;
        agent = GetComponent<NavMeshAgent>();

        // TODO assert size of this array to number of destinations in editor
        transitionMatrix = new List<List<float>>
        {
            new List<float> // probability of A => (A,B,C,D)
            {
                0f, 0.2f, 0.3f, 0.5f
            },
            new List<float> // probability of B => (A,B,C,D)
            {
                0.2f, 0, 0.3f, 0.5f
            },
            new List<float> // probability of C => (A,B,C,D)
            {
                0.3f, 0.2f, 0, 0.5f
            },
            new List<float> // probability of D => (A,B,C,D)
            {
                0.2f, 0.3f, 0.5f, 0
            }
        };

        stationList = new List<Transform>();
        destinationList = new List<Transform>();

        foreach (Transform destinations in destinations.transform)
        {
            stationList.Add(destinations);
        }

        int numberOfPaths = 7;

        Transform destination = stationList[0];
        destinationList.Add(destination);

        for (int i = 0; i < numberOfPaths - 1; i++)
        {
            Transform newDestination = GetNextDestFromCurrentDest(destination);
            destinationList.Add(newDestination);
            destination = newDestination;
        }

        string listString = "";
        foreach (Transform item in destinationList)
        {
            listString += (item.gameObject.name + ", ");
        }
        Debug.Log("Final Destination List: " + listString);

        currentDestinationIndex = 0;
        currentDestination = destinationList[currentDestinationIndex];
        agent.destination = currentDestination.position;
        Debug.Log("Pathing to First Destination (Destination " + destinationList[currentDestinationIndex].name + ")");
        agent.isStopped = false;
    }

    Transform GetNextDestFromCurrentDest(Transform currentTransform)
    {
        Debug.Log(currentTransform.GetSiblingIndex());
        List<float> cumulativeTransitionList = ToCumulativeTransitionList(transitionMatrix[currentTransform.GetSiblingIndex()]);
        
        
        string listString = "";
        transitionMatrix[currentTransform.GetSiblingIndex()].ForEach(item => listString += (item + ", "));

        Debug.Log("list: " + listString);

        string listString2 = "";
        cumulativeTransitionList.ForEach(item => listString2 += (item + ", "));

        Debug.Log("cumu list: " + listString2);

        float randNum = Random.Range(0.0001f, 1f); // our random number used to choose which destination next (we make sure it's not 0)
        Debug.Log(randNum);

        int indexToChoose = 0;

        // choose station based on weighted average in transition matrix
        for (int i = 0; i < cumulativeTransitionList.Count; i++)
        {
            if (randNum == cumulativeTransitionList[i])
            {
                indexToChoose = i;
                break;
            }
            else if (randNum > cumulativeTransitionList[i])
            {
            }
            else if (randNum < cumulativeTransitionList[i])
            {
                indexToChoose = i;
                break;
            }
        }

        Debug.Log("index: " + indexToChoose);


        Transform nextDestination = stationList[indexToChoose];

        return nextDestination;
    }
    
    List<float> ToCumulativeTransitionList(List<float> transitionList)
    {
        List<float> cumulativeTransitionList = new List<float>();

        for (int i = 0; i < transitionList.Count; i++)
        {
            cumulativeTransitionList.Insert(i, 0.0f);
            for (int j = 0; j <= i; j++)
            {
                cumulativeTransitionList[i] += transitionList[j];
            }
        }

        return cumulativeTransitionList;
    }

    // Update is called once per frame
    void Update()
    {
        if (agent.speed != 0 && agent.remainingDistance != 0) hasBegunRoute = true;

        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(currentDestination.position, path);

        // if destination is blocked, then stop the agent
        if (!agent.isStopped && path.status != NavMeshPathStatus.PathComplete)
        {
            agent.ResetPath();
            agent.isStopped = true;

            Debug.Log("Destination Blocked");
        }

        // if destination has become unblocked, then start the agent
        else if (agent.isStopped && path.status == NavMeshPathStatus.PathComplete)
        {
            agent.destination = currentDestination.position;
            //agent.SetPath(path);
            Debug.Log("Destination Found");
            agent.isStopped = false;
        }

        float dist = agent.remainingDistance;
        if (hasBegunRoute && dist != Mathf.Infinity && agent.pathStatus == NavMeshPathStatus.PathComplete && dist == 0)
        {
            hasBegunRoute = false;
            currentDestinationIndex++;
            if (currentDestinationIndex < destinationList.Count)
            {
                currentDestination = destinationList[currentDestinationIndex];
                agent.destination = currentDestination.position;
                Debug.Log("Pathing to Next Destination (Destination " + destinationList[currentDestinationIndex].name + ")");
                agent.isStopped = false;
            }
            else
            {
                Debug.Log("Path complete, stations visited: " + currentDestinationIndex);
                //agent.isStopped = true;
            }
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
