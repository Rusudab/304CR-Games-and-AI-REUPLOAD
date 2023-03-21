using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Reference for the TakeDamage function
// Reference: https://www.youtube.com/watch?v=THnivyG0Mvo

/*
 * How the AI is supposed to work is that he can come close to the player, which makes the player take damage.
 * After he attacks, the enemy will run into cover. After arriving into cover, the enemy will stay there for a second,
 * and if he fulfills some requirements, he will start attacking the player.
 * 
 * The total amount of states that the AI has are:
 * 
 * Idle, Patrol, Attack, LookForCover, StopLookingForCover, Retreat, Regenerate
 * 
 * All of these states are being explained on what they do lower into the script.
 * These states are being controlled with the fear of the enemy, which means how fearful the enemy is, and the health of the enemy
 * I wanted to implement an intelligence factor as well, but did not have enough time.
 * 
 * I will be reffering to the AI as the "enemy" throughout this script
 */

public class Behaviour : MonoBehaviour
{
    public NavMeshAgent navAgent;
    public EnemyLineOfSightChecker enemySightSystem;

    public AnimationCurve healthFactor;
    public AnimationCurve fearFactor;
    public AnimationCurve intelligenceFactor;

    public GameObject player;
    public GameObject remapObj;

    float distanceToPlayer;
    float enemySpeed = 5.0f;

    public float enemyHealth = 100.0f;
    // Intelligence can go from 1-10, 1 meaning very unintelligent, 10 meaning close to genius. The intelligence is designed as the default value being randomized
    [Range(0f, 10f)]
    public float enemyIntelligence;
    // Fear is a value that can also go from 1-10, where 1 means very fearless, 10 meaning very fearful. The fear is designed as 4 being the average intelligence
    [Range(0f, 10f)]
    public float enemyFear;

    /* 
     * These next 3 variables are being used each frame to store the remapped value from the health, fear, and intelligence variables
     * These variables are being used everywhere for the fuzzy logic AI, in order to evaluate their value to their specific animation curves
     */
    float remappedFear;
    float remappedHealth;
    float remappedIntelligence;

    bool justGotShot;
    bool isReadyToAttack;

    public enum decisions
    {
        IDLE,
        PATROL,
        ATTACK,
        LOOKFORCOVER,
        STOPLOOKINGFORCOVER,
        RETREAT,
        REGENERATE
    }

    public decisions currentDecision;

    // Start is called before the first frame update
    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        enemySightSystem = GetComponentInChildren<EnemyLineOfSightChecker>();

        enemyHealth = 100f;
        enemyFear = 4f;
        enemyIntelligence = Random.Range(1f, 10f);

        remappedFear = remapAnything(enemyFear, 1f, 10f, 0f, 1f);
        remappedHealth = remapAnything(enemyHealth, 1f, 100f, 0f, 1f);
        remappedIntelligence = remapAnything(enemyIntelligence, 1f, 10f, 0f, 1f);

        StartCoroutine(decide());
        StartCoroutine(BehaviourChange());
        StartCoroutine(WhileInBehaviour());

        justGotShot = false;
        isReadyToAttack = false;
    }

    void Update()
    {
        distanceToPlayer = Vector3.Distance(this.transform.position, player.transform.position);
        // Calculatin the remapped values of fear, health, and intelligence each frame
        remappedFear = remapAnything(enemyFear, 1f, 10f, 0f, 1f);
        remappedHealth = remapAnything(enemyHealth, 1f, 100f, 0f, 1f);
        remappedIntelligence = remapAnything(enemyIntelligence, 1f, 10f, 0f, 1f);
    }

    /*
     * This is the finite state machine, it has a switch statement that goes through the states of the enemy
     */
    IEnumerator decide()
    {
        switch (currentDecision)
        {
            // In the idle state, the enemy just stays idling
            case decisions.IDLE:
                enemySightSystem.lookForCoverIndefinitely = false;
                enemySightSystem.isLookingForCover = false;

                navAgent.speed = enemySpeed;
                navAgent.SetDestination(transform.position);

                yield return new WaitForSeconds(2f);
                break;

            // In the patrol state, the enemy will patrol in a random area
            case decisions.PATROL:
                enemySightSystem.lookForCoverIndefinitely = false;
                enemySightSystem.isLookingForCover = false;

                navAgent.speed = enemySpeed;
                if (navAgent.destination != transform.position)
                {
                    float rangingx = Random.Range(-10.0f, 10.0f);
                    float rangingz = Random.Range(-10.0f, 10.0f);
                    Vector3 range = new Vector3(transform.position.x + rangingx, transform.position.y, transform.position.z + rangingz);
                    navAgent.SetDestination(range);
                }

                yield return new WaitForSeconds(0.5f);
                break;

            // In the attack state, the enemy will follow the player and when he gets close, a debug.log is being called showing that the player got hit
            case decisions.ATTACK:
                enemySightSystem.lookForCoverIndefinitely = false;
                enemySightSystem.isLookingForCover = false;

                navAgent.destination = player.transform.position;
                navAgent.speed = navAgent.speed + distanceToPlayer / 10;
                if (distanceToPlayer < 5.0f)
                {
                    Debug.Log("Player took 10 damage");
                }
                if (isReadyToAttack && distanceToPlayer < 4.9f)
                {
                    currentDecision = decisions.LOOKFORCOVER;
                    isReadyToAttack = false;
                }

                yield return new WaitForSeconds(1f);
                break;
            
            // In the lookforcover state, the enemy will find a position to hide away from the player
            case decisions.LOOKFORCOVER:
                navAgent.speed = enemySpeed;
                enemySightSystem.isLookingForCover = true;

                yield return new WaitForSeconds(0.5f);
                break;

            // In the stoplookingforcover state, the enemy will no longer look for cover
            case decisions.STOPLOOKINGFORCOVER:
                navAgent.speed = enemySpeed;
                enemySightSystem.isLookingForCover = false;
                enemySightSystem.lookForCoverIndefinitely = false;

                yield return new WaitForSeconds(0.5f);
                break;

            // In the retreat state, the enemy will run away from the player when the health goes underneath a certain point
            case decisions.RETREAT:
                navAgent.speed = enemySpeed*2;
                enemySightSystem.isLookingForCover = false;
                enemySightSystem.lookForCoverIndefinitely = false;

                Debug.Log("Is Retreating");
                StartCoroutine(FleeTimer());

                yield return new WaitForSeconds(0.5f);
                break;

            // In the regenerate state, the enemy will slowly regenerate its health
            case decisions.REGENERATE:
                navAgent.speed = enemySpeed;
                enemySightSystem.isLookingForCover = false;
                enemySightSystem.lookForCoverIndefinitely = false;

                StartCoroutine(RegenTimer());

                yield return new WaitForSeconds(0.5f);
                break;

            default:
                Debug.Log("Default Call");
                yield return new WaitForSeconds(0.1f);
                break;
        }

        StartCoroutine(decide());
    }

    // This enumerator is made to change the behaviour of the enemy, based on if statements.
    IEnumerator BehaviourChange()
    {
        // If the player gets to a distance over 50, the enemy will just start patrolling
        if (distanceToPlayer > 50.0f)
        {
            currentDecision = decisions.PATROL;
            yield return new WaitForSeconds(5f);
        }

        // This will change to the idle state if the enemy is in the patrol state if the player's distance from the enemy is 50
        if (distanceToPlayer > 50.0f && currentDecision == decisions.PATROL)
        {
            currentDecision = decisions.IDLE;
            yield return new WaitForSeconds(5f);
        }

        // This will detect the player if his distance is 30 from the enemy, which then changes the state to attacking
        if (distanceToPlayer < 30.0f && (currentDecision == decisions.PATROL || currentDecision == decisions.IDLE))
        {
            currentDecision = decisions.ATTACK;
            isReadyToAttack = true;
        }

        // This will trigger the enemy to look for cover, if it is being shot by the player
        if (justGotShot == true)
        {
            currentDecision = decisions.LOOKFORCOVER;
            enemySightSystem.lookForCoverIndefinitely = true;
            justGotShot = false;
        }

        // This checks to see if the enemy has arrived to the destination of his cover
        // Condition for this if to be true: if the player's health is not low, and his fear level is not too high
        if(currentDecision == decisions.LOOKFORCOVER && 
            new Vector3(navAgent.destination.x, 0.0f, navAgent.destination.z) == new Vector3(navAgent.transform.position.x, 0.0f, navAgent.transform.position.z) && 
            healthFactor.Evaluate(remappedHealth) >= 0.3f && fearFactor.Evaluate(remappedFear) <= 0.85f)
        {
            isReadyToAttack = true;
            currentDecision = decisions.ATTACK;
        }

        // This checks to see if the enemy's health is under a certain amount
        // Which then prompts the enemy to go hide and regenerate health while standing still
        // If the player approaches the enemy, he will get triggered to go hide away from the player again and keep regenerating
        if (currentDecision == decisions.LOOKFORCOVER && 
            new Vector3 (navAgent.destination.x, 0.0f, navAgent.destination.z) == new Vector3 (navAgent.transform.position.x, 0.0f, navAgent.transform.position.z) && 
            healthFactor.Evaluate(remappedHealth) <= 0.3f )
        {
            if (distanceToPlayer > 6.0f)
            {
                currentDecision = decisions.REGENERATE;
            }
            else
            {
                currentDecision = decisions.LOOKFORCOVER;
            }
        }

        // This checks to see if the enemy's health is very low, with his fear very high
        // Which then prompts the enemy to retreat and run away from the player, and then regenerate
        // If the player is at a certain distance from the enemy.
        if (healthFactor.Evaluate(remappedHealth) <= 0.15f && fearFactor.Evaluate(remappedFear) >= 0.9f)
        {
            if (distanceToPlayer < 12.0f)
            {
                currentDecision = decisions.RETREAT;
            }
            else
            {
                currentDecision = decisions.REGENERATE;
            }
        }
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(BehaviourChange());
    }

    // This enumerator controls the basic values of fear and intelligence that the enemy has
    IEnumerator WhileInBehaviour()
    {

        // This is set to decrease fear, as the enemy does nothing stimulating, making him calm down
        // As well as intelligence by a small margin, due to the situation not being intellectually stimulating to the enemy
        if (currentDecision == decisions.IDLE || currentDecision == decisions.PATROL)
        {
            enemyFear -= 0.3f;
            enemyIntelligence -= 0.05f;
            yield return new WaitForSeconds(5f);
        }

        // This increases the enemy's fear and intelligence if he is in combat, or retreating
        if (currentDecision == decisions.ATTACK || currentDecision == decisions.RETREAT)
        {
            enemyIntelligence += 0.1f;
            enemyFear += 0.2f;
            yield return new WaitForSeconds(5f);
        }

        // If the enemy gets to a high fear value, but not on low health
        // He will run away, hide, and wait to decrease his fear value, before starting to attack again
        if((currentDecision == decisions.LOOKFORCOVER || currentDecision == decisions.REGENERATE) && 
            new Vector3(navAgent.destination.x, 0.0f, navAgent.destination.z) == new Vector3(navAgent.transform.position.x, 0.0f, navAgent.transform.position.z))
        {
            enemyFear -= 0.4f;
            yield return new WaitForSeconds(0.5f);
        }

        // These next 4 if statements are made so the fear or the intelligence value can not go under 0 nor over 10
        if (enemyFear <= 0.0f)
        {
            enemyFear = 0.0f;
        }
        if (enemyIntelligence <= 0.0f)
        {
            enemyIntelligence = 0.0f;
        }
        if (enemyFear >= 10.0f)
        {
            enemyFear = 10.0f;
        }
        if (enemyIntelligence >= 10.0f)
        {
            enemyIntelligence = 10.0f;
        }

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(WhileInBehaviour());
    }

    // This enumerator was made so the player is regenerating a certain amount of time,
    // Which in this case, it is 5 seconds, afterwards he starts attacking
    IEnumerator RegenTimer()
    {
        enemyHealth += 2f;
        yield return new WaitForSeconds(5f);
        isReadyToAttack = true;
        currentDecision = decisions.ATTACK;
    }

    // This calculates the fleeing destination of the enemy
    IEnumerator FleeTimer()
    {
        Vector3 directionToPlayer = navAgent.transform.position - player.transform.position;
        Vector3 newDestionation = navAgent.transform.position + directionToPlayer;

        navAgent.destination = newDestionation;
        yield return new WaitForSeconds(2f);

        isReadyToAttack = true;
        currentDecision = decisions.ATTACK;
    }

    // With this function, I am calling the script Remap, to do the remapping for me
    float remapAnything(float value, float from1, float to1, float from2, float to2)
    {
        return remapObj.GetComponent<Remap>().RemapFunction(value, from1, to1, from2, to2);
    }

    // Part of this function was created with the help of the tutorial referenced at the top of the Gun.cs script
    // This script also increases the enemy's fear if he is being shot.
    public void TakeDamage(float dmg)
    {
        enemyHealth -= dmg;
        justGotShot = true;

        enemyFear += 0.4f;
        if(enemyFear >= 10.0f)
        {
            enemyFear = 10.0f;
        }
        if(enemyHealth < 0.0f)
        {
            Destroy(this.gameObject);
        }
    }
}