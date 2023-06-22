using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class Sc_AIStatesManagerHierarchical : MonoBehaviour
{
    private Trait aiTrait;

    //All first layer states. First layer reperesents the bigger states that may contain multiple smaller states
    [HideInInspector]
    public Sc_AIBaseParentState currentFLState;
    [HideInInspector]
    public Sc_CombatFLState combatFLState = new Sc_CombatFLState();
    [HideInInspector]
    public Sc_AlertFLState alertParentState = new Sc_AlertFLState();
    [HideInInspector]
    public Sc_NonCombatFLState nonCombatParentState = new Sc_NonCombatFLState();

    //All the current state the AI can be in
    [HideInInspector]
    public Sc_AIBaseState currentSLState;
    [HideInInspector]
    public Sc_AttackState attackState = new Sc_AttackState();
    [HideInInspector]
    public Sc_IdleState idleState = new Sc_IdleState();
    [HideInInspector]
    public Sc_PatrolState patrolState = new Sc_PatrolState();
    [HideInInspector]
    public Sc_AggressionState aggressionDesicionState = new Sc_AggressionState();
    [HideInInspector]
    public Sc_CoverState coverState = new Sc_CoverState();
    [HideInInspector]
    public Sc_SearchState searchState = new Sc_SearchState();

    //A script that contains a set of common methods that multiple states can call on
    [SerializeField]
    private Sc_CommonMethods commonMethods;

    //The navigation agent of the AI
    [SerializeField]
    private NavMeshAgent navMeshAgent;

    //Director AI that controls all of the AI
    [SerializeField]
    private Sc_AIDirector directorAI;

    //The player game object and weather they have been spotted
    [SerializeField]
    private GameObject player;
    [HideInInspector]
    public bool playerNoticed;

    [SerializeField]
    private float visionRange, visionConeAngle, audioRange, alertedTimer, decisionTimer, idleTimer;
    private float distPlayer, angleToPlayer;

    //The value that the AI determines if they should go and attack the player or go to cover
    private float decisionValue = 0;

    //Variables that are important for the patrol state
    [Header("Patroling")]
    //All the patrol points the AI can walk to and from
    [SerializeField]
    private GameObject[] patrolPoints;

    //All variables related to the attack state the player
    [Header("Attacking/Chasing")]
    //Current weapon gameobject
    [SerializeField]
    private GameObject currentWeapon;

    //Variables important to the cover state
    [Header("Cover")]
    //All cover positions that the player can use
    [SerializeField]
    private GameObject[] cover;
    //How far the AI is willing to run to cover
    [SerializeField]
    private float coverDistance;

    //Animation information
    private bool isAttacking, isReloading, isIdling, isWalking;

    //To check if the player is being blocked by some objects.
    // Bit shift the index of the layer (9) to get a bit mask
    private int layerMask = 1 << 9;

    private RaycastHit hit;

    //UI variables that appears on top of each AI that helps explain what their current actions are. Helps with debuging each individual AI better
    [Header("UI State Text")]
    [SerializeField]
    private bool showActions;
    [SerializeField]
    private GameObject actionUIText;
    [SerializeField]
    private TextMeshProUGUI stateText;
    public string currentAction;

    // Start is called before the first frame update
    void Start()
    {
        //Set first layer state to Non-Combate
        currentFLState = nonCombatParentState;
        //Sets starting state to patroling
        currentSLState = patrolState;

        currentFLState.EnterState();
        currentSLState.EnterState(playerNoticed);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwitchFLState(Sc_AIBaseParentState state)
    {
        currentFLState = state;
        currentFLState.EnterState();
    }

    public void SwitchSLState(Sc_AIBaseState state)
    {
        currentSLState = state;
        currentSLState.EnterState(playerNoticed);
    }

    public void SetUpTraits(Trait newAITrait, AudioClip[] audioClips)
    {
        this.aiTrait = newAITrait;
        //this.aiAudioClips = audioClips;

        commonMethods.SetUpTrait(aiTrait);
        aggressionDesicionState.SetUpTrait(aiTrait);
    }

    public void RecentlyHit()
    {
        if (currentFLState != combatFLState)
        {
            SwitchFLState(combatFLState);
            SwitchSLState(aggressionDesicionState);
        }
    }

    //Sets the AIs decision value
    public void SetDecisionValue(float value)
    {
        decisionValue = value;
    }

    //returns the AIs decision value
    public float ReturnDecisionValue()
    {
        return decisionValue;
    }

    //Current action which is used for some UI so that the user can better determine what each individual AI is doing.
    public void SetCurrentAction(string action)
    {
        currentAction = action;
    }

    //Sets if the AI is attacking the player
    public void SetIsAttacking(bool isAttacking)
    {
        this.isAttacking = isAttacking;
    }

    //Returns if the AI is attacking
    public bool ReturnIsAttacking()
    {
        return isAttacking;
    }

    public void SetIsReloading(bool isReloading)
    {
        this.isReloading = isReloading;
    }

    public bool ReturnIsReloading()
    {
        return isReloading;
    }

    //Setsif the AI is idling
    public void SetIsIdling(bool isIdling)
    {
        this.isIdling = isIdling;
    }

    //Returns if the AI is idling
    public bool ReturnIsIdling()
    {
        return isIdling;
    }

    //Sets if the AI is walking
    public void SetIsWalking(bool isWalking)
    {
        this.isWalking = isWalking;
    }

    //Returns if the AI is walking
    public bool ReturnIsWalking()
    {
        return isWalking;
    }
}
