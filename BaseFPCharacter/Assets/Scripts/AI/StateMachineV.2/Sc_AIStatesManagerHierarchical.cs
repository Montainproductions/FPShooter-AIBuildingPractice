using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class Sc_AIStatesManagerHierarchical : MonoBehaviour
{
    private Trait aiTrait;

    [SerializeField]
    private GameObject self;

    //All first layer states. First layer reperesents the bigger states that may contain multiple smaller states. This first layer is meant to represent general activites that the AI is trying to complete. The non combate state contains the patroling and idle states, the alerted first layer state contains the alerted and search states, finally the first layer combat state contains the aggression desicion, attack and cover states. 
    [HideInInspector]
    public Sc_AIBaseStateHierarchical currentFLState;
    [HideInInspector]
    public Sc_NonCombatFLState nonCombatFLState = new Sc_NonCombatFLState();
    [HideInInspector]
    public Sc_AlertFLState alertFLState = new Sc_AlertFLState();
    [HideInInspector]
    public Sc_CombatFLState combatFLState = new Sc_CombatFLState();

    //All the current states in the second layer of the HFSM which the AI can be in
    [HideInInspector]
    public Sc_AIBaseStateHierarchical currentSLState;
    [HideInInspector]
    public Sc_PatrolingSLState patrolState = new Sc_PatrolingSLState();
    [HideInInspector]
    public Sc_IdleSLState idleState = new Sc_IdleSLState();
    [HideInInspector]
    public Sc_AlertedSLState alertedState = new Sc_AlertedSLState();
    [HideInInspector]
    public Sc_SearchingSLState searchState = new Sc_SearchingSLState();
    [HideInInspector]
    public Sc_AggressionSLState aggressionDesicionState = new Sc_AggressionSLState();
    [HideInInspector]
    public Sc_ShootingSLState attackState = new Sc_ShootingSLState();
    [HideInInspector]
    public Sc_CoverSLState coverState = new Sc_CoverSLState();

    //A script that contains a set of common methods that multiple states can call on
    [SerializeField]
    private Sc_HFSMCommenMethods commenMethods;

    //The navigation agent of the AI
    [SerializeField]
    private NavMeshAgent navMeshAgent;

    //Director AI that controls all of the AI
    private Sc_AIDirector directorAI;

    //The player game object and weather they have been spotted
    private GameObject player;
    private Vector3 playerPosition;
    [HideInInspector]
    public bool playerNoticed;

    //Audio Source
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private float lastAudioTimer;

    //Timers and basic ranges
    [SerializeField]
    private float visionRange, visionConeAngle, audioRange, alertedTimer, decisionTimer;

    //The value that the AI determines if they should go and attack the player or go to cover
    private float decisionValue = 0;

    [Header("Idle")]
    [SerializeField]
    private float idleTimer;

    //All variables related to the attack state the player
    [Header("Attacking/Chasing")]
    //Current weapon gameobject
    [SerializeField]
    private GameObject currentWeapon;

    //Variables important to the cover state
    [Header("Cover")]
    //How far the AI is willing to run to cover
    [SerializeField]
    private float coverDistance;

    [Header("Foiliage")]
    private GameObject[] allFoiliage;

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
        nonCombatFLState.NonCombatSetUp(this, directorAI, commenMethods, gameObject.transform, visionRange, visionConeAngle);
        alertFLState.AlertSetUp(this, directorAI, commenMethods, player, gameObject.transform, visionRange, visionConeAngle);
        combatFLState.CombatFLSetUp(this, directorAI, commenMethods, player, gameObject.transform, visionRange, visionConeAngle);

        commenMethods.CommenMethodSetUp(navMeshAgent, gameObject, player, audioSource, allFoiliage, lastAudioTimer, decisionTimer);
        //Sending important variables and objects to all of the states
        patrolState.PatrolStartStateInfo(this, commenMethods);
        idleState.IdleStartStateInfo(this, commenMethods, idleTimer);
        aggressionDesicionState.AggressionStartStateInfo(this, commenMethods, directorAI, gameObject, player, currentWeapon, aiTrait, coverDistance);
        attackState.AttackStartStateInfo(this, commenMethods, self, player, currentWeapon);
        coverState.CoverStartStateInfo(this, commenMethods, self, player, currentWeapon);

        //The current player position for when entering a state
        playerPosition = player.transform.position;

        //Set first layer state to Non-Combate
        currentFLState = nonCombatFLState;
        //Sets starting state to patroling
        currentSLState = patrolState;

        //Starts the enter state of the current first layer and second layer of the state machine
        currentFLState.EnterState(playerPosition);
        currentSLState.EnterState(playerPosition);
    }

    // Update is called once per frame
    void Update()
    {
        currentFLState.UpdateState();
        currentSLState.UpdateState();

        Debug.Log(currentFLState + " " + currentSLState);

        //If the UI text should be updated
        if (showActions)
        {
            stateText.SetText(currentFLState.ToString() + " " + currentSLState.ToString() + "   " + currentAction);
        }
    }

    //Switches the first layer in the HFSM
    public void SwitchFLState(Sc_AIBaseStateHierarchical state)
    {
        playerPosition = player.transform.position;
        
        currentFLState = state;
        currentFLState.EnterState(playerPosition);
    }

    //Switches the second layer in the HFSM
    public void SwitchSLState(Sc_AIBaseStateHierarchical state)
    {
        playerPosition = player.transform.position;

        commenMethods.StopMovement();

        currentSLState = state;
        currentSLState.EnterState(playerPosition);
    }

    //Once the traits have been distributed and recived by the state manager then is passed to the required scripts
    public void SetUpTraits(Trait newAITrait, AudioClip[] audioClips)
    {

        this.aiTrait = newAITrait;
        //this.aiAudioClips = audioClips;

        commenMethods.SetUpTrait(aiTrait, audioClips);
        searchState.SetUpTrait(aiTrait);
        aggressionDesicionState.SetUpTrait(aiTrait);
    }

    public void SetUpInfoDirector(Sc_AIDirector directorScript, GameObject player,GameObject spawnPointsOj) 
    {
        directorAI = directorScript;

        this.player = player;
        StartCoroutine(nonCombatFLState.RecivePlayerGO(this.player));

        Sc_CoverandPatrolPoints spawnPointScript= spawnPointsOj.GetComponent<Sc_CoverandPatrolPoints>();
        StartCoroutine(patrolState.RecivePatrolPoints(spawnPointScript.ReturnPatrolPoints()));
        aggressionDesicionState.ReciveAllCoverPoints(spawnPointScript.ReturnCoverPoints());
        coverState.ReciveAllCoverPoints(spawnPointScript.ReturnCoverPoints());
    }

    //Sets the AIs decision value
    public void SetDecisionValue(float value)
    {
        decisionValue = value;
    }

    //Changes state if it was recently hit by a bullet/damaged
    public void RecentlyHit()
    {
        PlayRandomAudioOneShot(0, 2);
        //Debug.Log("Speaking");
        if (currentFLState != combatFLState)
        {
            SwitchFLState(combatFLState);
            SwitchSLState(aggressionDesicionState);
        }
    }

    public void PlayRandomAudioOneShot(int lowerLevelIncl, int higherLevelIncl)
    {
        StartCoroutine(commenMethods.PlayRandomAudioOneShot(lowerLevelIncl, higherLevelIncl));
    }

    //returns the AIs decision value
    public float ReturnDecisionValue()
    {
        return decisionValue;
    }

    ///The next section is setting up the animation system to properly transition between the various animations that exist.

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

    //Sets if the AI is currently reloading
    public void SetIsReloading(bool isReloading)
    {
        this.isReloading = isReloading;
    }

    //Returns if the AI is currently reloading
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
