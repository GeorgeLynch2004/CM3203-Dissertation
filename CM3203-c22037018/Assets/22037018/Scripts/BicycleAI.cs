using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;

public class BicycleAI : MonoBehaviour
{
    // Events
    [SerializeField] private UnityEvent currentAIDirective;
    [SerializeField] private UnityEvent neutralActions;
    [SerializeField] private UnityEvent offenceActions;
    [SerializeField] private UnityEvent defenceActions;

    // Navmesh
    [SerializeField] private NavMeshAgent navmeshAgent;
    [SerializeField] private Transform carrotOnTheStick;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        currentAIDirective.Invoke();
        var path = new NavMeshPath();
        navmeshAgent.CalculatePath(carrotOnTheStick.position, path);
    }

    public void increasePace()
    {

    }

    public void maintainPace()
    {
        navmeshAgent.SetDestination(carrotOnTheStick.position);
    }

    public void decreasePace()
    {

    }

    public void rotatePaceline()
    {

    }
}
