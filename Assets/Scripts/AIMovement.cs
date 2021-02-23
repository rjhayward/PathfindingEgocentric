using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;
public class AIMovement : MonoBehaviour
{
    public ThirdPersonCharacter character;
    public Transform head;

    public GameObject destinations;
    NavMeshAgent agent;
    private int baseSpeed;

    private Transform currentDestination;
    private bool hasBegunRoute;

    private List<Transform> stationList;

    private List<Transform> destinationList;
    private int currentDestinationIndex;

    private List<List<float>> transitionMatrix;

    public int lengthOfPath;

    private float TransitionStartTime;
    private bool IsInTransition;

    private Timer timer;

    private float TimeElapsed;

    private bool isStopped;
    private int transitionPhase;

    

    List<List<float>> CreateUniformTransitionMatrix(int numPoints)
    {
        // if there is only 1 point (or no points) we cannot create a useful transition matrix
        if (numPoints <= 1) return null;

        // create transition matrix
        List<List<float>> TransitionMatrix = new List<List<float>>();

        // populate each row with a list of floats all the same, except if col == row it is populated with a 0 
        // float to insert = 1/(numPoints-1) -> will add up to approximately 1
        for (int row = 0; row < numPoints; row++)
        {
            TransitionMatrix.Add(new List<float>());
            //TransitionMatrix[row] = new List<float>();
            for (int col = 0; col < numPoints; col++)
            {
                if (col != row)
                {
                    TransitionMatrix[row].Add(1.0f/(numPoints-1));
                }
                else
                {
                    TransitionMatrix[row].Add(0f);
                }
            }
        }

        return TransitionMatrix;

        //return new List<List<float>>
        //{
        //    new List<float> // probability of A => (A,B,C,D)
        //    {
        //        0f, 0.2f, 0.3f, 0.5f
        //    },
        //    new List<float> // probability of B => (A,B,C,D)
        //    {
        //        0.2f, 0, 0.3f, 0.5f
        //    },
        //    new List<float> // probability of C => (A,B,C,D)
        //    {
        //        0.3f, 0.2f, 0, 0.5f
        //    },
        //    new List<float> // probability of D => (A,B,C,D)
        //    {
        //        0.3f, 0.2f, 0.5f, 0
        //    },
        //    new List<float> // probability of E => (A,B,C,D)
        //    {
        //        0.3f, 0.2f, 0, 0.5f
        //    },
        //    new List<float> // probability of F => (A,B,C,D)
        //    {
        //        0.2f, 0.3f, 0.5f, 0
        //    }
        //};
    }

    void Start()
    {
        transitionPhase = 0;
        isStopped = false;
        TransitionStartTime = 0f;
        TimeElapsed = 0f;
        IsInTransition = false;
        hasBegunRoute = false;
        baseSpeed = 5;
        agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = false;
        agent.velocity = Vector3.zero;
        
        stationList = new List<Transform>();
        destinationList = new List<Transform>();

        foreach (Transform dest in destinations.transform)
        {
            stationList.Add(dest);
        }

        // TODO assert size of this array to number of destinations in editor
        transitionMatrix = CreateUniformTransitionMatrix(stationList.Count);

        Transform destination = stationList[0];
        destinationList.Add(destination);

        for (int i = 0; i < lengthOfPath - 1; i++)
        {
            Transform newDestination = GetNextDestFromCurrentDest(destination);
            destinationList.Add(newDestination);
            destination = newDestination;
        }

        string listString = "";
        foreach (Transform item in destinationList)
        {
            listString += (item.name + ", ");
        }
        Debug.Log("Final Destination List: " + listString);

        currentDestinationIndex = 0;
        currentDestination = destinationList[currentDestinationIndex];
        agent.destination = currentDestination.position;
        Debug.Log("Pathing to First Destination (Destination " + destinationList[currentDestinationIndex].name + ")");
        isStopped = false;
    }

    Transform GetNextDestFromCurrentDest(Transform currentTransform)
    {
        //Debug.Log(currentTransform.GetSiblingIndex());
        List<float> cumulativeTransitionList = ToCumulativeTransitionList(transitionMatrix[currentTransform.GetSiblingIndex()]);
        
        string listString = "";
        transitionMatrix[currentTransform.GetSiblingIndex()].ForEach(item => listString += (item + ", "));

        //Debug.Log("list: " + listString);

        string listString2 = "";
        cumulativeTransitionList.ForEach(item => listString2 += (item + ", "));

        //Debug.Log("cumu list: " + listString2);

        float randNum = Random.Range(0.0001f, 1f); // our random number used to choose which destination next (we make sure it's not 0)
        //Debug.Log(randNum);

        int indexToChoose = 0;

        // choose station based on weighted average in transition matrix
        for (int i = 0; i < cumulativeTransitionList.Count; i++)
        {
            if (randNum == cumulativeTransitionList[i])
            {
                indexToChoose = i;
                break;
            }
            //else if (randNum > cumulativeTransitionList[i])
            //{
            //}
            else if (randNum < cumulativeTransitionList[i])
            {
                indexToChoose = i;
                break;
            }
        }

        //Debug.Log("index: " + indexToChoose);

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
        TimeElapsed += Time.deltaTime;
        if (agent.speed != 0 && agent.remainingDistance != 0) hasBegunRoute = true;

        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(currentDestination.position, path);

        // if destination is blocked, then stop the agent
        if (!isStopped && path.status != NavMeshPathStatus.PathComplete)
        {
            agent.ResetPath();
            isStopped = true;
            Debug.Log("Destination Blocked");
        }

        // if destination has become unblocked, then start the agent
        else if (isStopped && path.status == NavMeshPathStatus.PathComplete)
        {
            agent.destination = currentDestination.position;
            //agent.SetPath(path);
            Debug.Log("Destination Found");
            isStopped = false;
        }

        float dist = agent.remainingDistance;



        float timeToWait = 4f;

        if (hasBegunRoute && dist != Mathf.Infinity && agent.pathStatus == NavMeshPathStatus.PathComplete && dist == 0)
        {
            hasBegunRoute = false;
            currentDestinationIndex++;
            if (currentDestinationIndex < destinationList.Count)
            {
                // TODO use expected time for each destination type

                if (!IsInTransition)
                {
                    Debug.Log("Waiting " + timeToWait + " seconds, time elapsed: " + TimeElapsed);
                    IsInTransition = true;
                    TransitionStartTime = TimeElapsed;
                }
            }
            else
            {
                Debug.Log("Path complete, stations visited: " + currentDestinationIndex);
            }
        }


        if (IsInTransition && (TimeElapsed < (TransitionStartTime + timeToWait / 4f)))
        {
            //agent.destination = agent.transform.position + 0.1f*(agent.transform.right + agent.transform.forward);
            head.transform.Rotate(transform.up, 100f * Time.deltaTime);
        }
        else if (IsInTransition && (TimeElapsed < (TransitionStartTime + 3 * timeToWait / 4)))
        {
            //agent.destination = agent.transform.position + 0.1f * (agent.transform.right - agent.transform.forward);
            head.transform.Rotate(transform.up, -100f * Time.deltaTime);
        }
        else if(IsInTransition && (TimeElapsed < (TransitionStartTime + timeToWait)))
        {
            //agent.destination = agent.transform.position + 0.1f * (-agent.transform.forward);
            head.transform.Rotate(transform.up, 100f * Time.deltaTime);
        }
        if (IsInTransition && (TimeElapsed >= (TransitionStartTime + timeToWait)))
        {
            transitionPhase = 0;
            IsInTransition = false;
            currentDestination = destinationList[currentDestinationIndex];
            agent.destination = currentDestination.position;
            Debug.Log("Pathing to Next Destination (Destination " + destinationList[currentDestinationIndex].name + ")");
            isStopped = false;
        }

        NavMeshHit navHit;
        agent.SamplePathPosition(NavMesh.AllAreas, 0.0f, out navHit);

        if (!isStopped || IsInTransition)
        {
            character.Move(agent.desiredVelocity, false, false);
        }
        else if (!IsInTransition)
        {
            character.Move(Vector3.zero, false, false);
        }

        // sets the speed based on the cost of the terrain beneath agent
        //agent.speed = baseSpeed * 2 / agent.GetAreaCost(GetAreaListFromNavMeshHit(navHit)[0]);

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
