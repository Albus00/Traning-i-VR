using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
// Make an enemyHandler class that delegates actions to enemies. 
// example: 
// loop through list of alive enemies => give each enemy an action based on how many enemies can do the same action at the same time
// if 10 enemies alive -> 6 move, 4 attack
// if close to player then attack = melee, if far away then attack = throw.
// this action can be called every X beats
// How to make it so that all enemies dont perform actions at the same time?
// enemy1 does action on beat X+1
// enemy2 does action on beat X+3 etc...
public class BehaviourTest : MonoBehaviour
{
    // PS: remember to set BPM based on the song. 
    // --------- Enemy State --------- //
    private enum EnemyState
    {
        Idle,
        Running,
        Dash,
        Ragdoll
        
    }
    private EnemyState currentState = EnemyState.Idle;

    // --------- Limb Handling --------- //
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] colliders;
    private Rigidbody hitRigidbody;


    // --------- Movement handling --------- //
    private Transform target;
    private float distanceToTarget;
    public float movementSpeed;
    // ---- Dashing ---- //
    private float dashDistance;
    public float dashSpeed;
    Vector3 dashTarget;
    public float dashVariationFactor;
    public GameObject testBox;

    // --------- Components --------- //
    private Animator animator;
    private CharacterController characterController;
    

    // --------- Action Timing --------- //
    public float bpm;
    private float secPerBeat;
    public float beatsPerAction; // decides how many beats have to occur for an action to be allowed
    private float actionTimer; // decides when an action can be done
    private float timer;
    private bool startAllowingActions; // toggles when do action is allowed
    private bool doAction; // is set to true -> do something -> is set to false

    void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        target = GameObject.FindWithTag("player").transform;
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        DisableRagdoll(); // No ragdoll as long as enemy isnt dead.
    }
    private void Start()
    {
        startAllowingActions = false;
        bpm = 110f;
        secPerBeat = 60f / bpm;
        beatsPerAction = 1f;
        //firstCall = true;
        dashDistance = 2.0f;
        dashSpeed = 15f;
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                IdleBehaviour(); 
                break;
            case EnemyState.Running:
                RunningBehaviour();
                break;
            case EnemyState.Ragdoll:
                RagdollBehaviour();
                break;
            case EnemyState.Dash:
                DashBehaviour();
                break;
            
        }
        HandleBehaviour();
    }
    // ------------------------------------ IDLE ------------------------------------ //
    private void IdleBehaviour()
    {
        gameObject.transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
    }


    // ------------------------------------ RUNNING ------------------------------------ //
    private void RunningBehaviour()
    {
        distanceToTarget = (target.position - gameObject.transform.position).magnitude;
        if (distanceToTarget > 1)
        {
            //Vector3 targetDirection = target.position - gameObject.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(target.position.x, transform.position.y, target.position.z), movementSpeed * Time.deltaTime);
        }


        gameObject.transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        //Debug.Log("Running");
        // debug for running direction
        //Debug.DrawLine(transform.position, transform.forward.normalized, new Color(1.0f, 0.0f, 0.0f));
    }


    // ------------------------------------ RAGDOLL ------------------------------------ //
    private void RagdollBehaviour()
    {
        // Nothing for now
    }
    private void DisableRagdoll()
    {
        foreach (var rigidbody in ragdollRigidbodies)
        {
            rigidbody.isKinematic = true; // stops physics from affecting the rigidbodies in the child objects like arms, legs etc
        }
        foreach (var collider in colliders)
        {
            collider.isTrigger = true;
        }
        animator.enabled = true;
        
    }
    private void EnableRagdoll() // when die, go ragdoll
    {
        foreach (var collider in colliders)
        {
            collider.isTrigger = false;
        }
        foreach (var rigidbody in ragdollRigidbodies)
        {
            rigidbody.isKinematic = false; // enables physics  affecting the rigidbodies in the child objects like arms, legs etc
        }
        animator.enabled = false;
        this.enabled = false;
        Object.Destroy(gameObject,3f);
    }


    // ------------------------------------ DASHING ------------------------------------ //
    // check this later: https://answers.unity.com/questions/1716253/how-to-move-towards-a-random-position-higher-than.html

    private void StartDash()
    {
        Vector3 direction = (target.position - transform.position).normalized;

        // Add random variation in the X and Z directions
        Vector3 randomVariation = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        dashVariationFactor = 0.7f; // Adjust this value to control the amount of variation

        Vector3 newDirection = Vector3.Lerp(direction, randomVariation, dashVariationFactor);
        float dotProduct = Vector3.Dot(direction, newDirection);

        // If the dot product is negative, it means the new direction is pointing away from the player. sets it to -itself to always dash towards the player.
        if (dotProduct < 0)
        {
            newDirection = Vector3.Lerp(direction, -randomVariation, dashVariationFactor);
        }

        dashTarget = transform.position + newDirection * dashDistance;
        dashTarget.y = 0f;


        //Instantiate(testBox, dashTarget, Quaternion.identity); // instantiates a box for debugging purposes
    }

    private void DashBehaviour()
    {
        transform.position = Vector3.MoveTowards(transform.position, dashTarget, dashSpeed * Time.deltaTime);
        //Debug.Log("In DashBehaviour: " + dashTarget);
        //Debug.Log("Dashing");
        // play animation

        if (transform.position == dashTarget)
        {
            EndDash();
        }
        // revert to base Behaviour

    }
    private void EndDash()
    {
        //Debug.Log("Dash ended");
        currentState = EnemyState.Idle;
    }

    
    public void projectileCollisionDetected(Collider limbCollider, Vector3 projectilePosition)
    {
        EnableRagdoll();
        currentState = EnemyState.Ragdoll;

        // Find the rigidbody that corresponds to the limb collider
        hitRigidbody = limbCollider.attachedRigidbody;
        Debug.Log("Hit rigidbody: " + hitRigidbody);

        if (hitRigidbody != null)
        {
            // Enable the rigidbody and collider components
            hitRigidbody.isKinematic = false;
            limbCollider.enabled = true;

            // Apply a force to the rigidbody in the direction of the projectile
            Vector3 forceDirection = limbCollider.transform.position - projectilePosition;
            float forceMagnitude = 10000f; // Adjust this value to control the strength of the force
            hitRigidbody.AddForce(forceDirection.normalized * forceMagnitude);
        }

    }


    // Do a check for distance from enemy to player and change the probablility of an action accordingly. for example so the enemy doesnt dash past the player.
    private void HandleBehaviour() 
    {
        timer += Time.deltaTime; // keeps time
        if (timer >= secPerBeat * beatsPerAction)
        {
            if (doAction) {
                timer = 0f;
                doAction = false;
                StartDash();
                currentState = EnemyState.Dash;
            }
        }
    }

    public void BeatReceiver() // turns doAction to true when the soundDetection script detects an amplitude spike at selected frequency band.
    {
        doAction = true;
    }
}
