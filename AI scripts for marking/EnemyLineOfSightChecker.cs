using System.Collections;
using UnityEngine;

// Reference: https://www.youtube.com/watch?v=t9e2XBQY4Og

// Most of the script here is based on the tutorial referenced at the top of the script
// The only changes can be found between line 38 and line 94
[RequireComponent(typeof(SphereCollider))]
public class EnemyLineOfSightChecker : MonoBehaviour
{
    public SphereCollider Collider;
    public float FieldOfView = 360f;
    public LayerMask LineOfSightLayers;

    public GameObject player;

    public bool isLookingForCover;
    public bool lookForCoverIndefinitely;

    public delegate void GainSightEvent(Transform Target);
    public GainSightEvent OnGainSight;
    public delegate void LoseSightEvent(Transform Target);
    public LoseSightEvent OnLoseSight;

    private Coroutine CheckForLineOfSightCoroutine;

    private void Awake()
    {
        isLookingForCover = false;
        lookForCoverIndefinitely = false;
        Collider = GetComponent<SphereCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CheckLineOfSight(other.transform))
        {
            // Added another if statement so the checking of line of sight can be turned on or off based on what I want the AI to be doing
            if (isLookingForCover)
            {
                CheckForLineOfSightCoroutine = StartCoroutine(CheckForLineOfSight(other.transform));
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!CheckLineOfSight(other.transform))
        {
            // Added another if statement so the checking of line of sight can be turned on or off based on what I want the AI to be doing
            if (isLookingForCover)
            {
                CheckForLineOfSightCoroutine = StartCoroutine(CheckForLineOfSight(other.transform));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // This code is commented out because we do not want the enemy to lose the sight of the player.

        //OnLoseSight?.Invoke(other.transform);
        //if (CheckForLineOfSightCoroutine != null)
        //{
        //    StopCoroutine(CheckForLineOfSightCoroutine);
        //}
    }

    private bool CheckLineOfSight(Transform Target)
    {
        Vector3 direction = (Target.transform.position - transform.position).normalized;

        /* 
         * The dotproduct calculation and the if statement have been taken out because in this small demo
         * I do not need a line of sight for the enemy, as that would unnecessarily complicate my implementation
         */
        //float dotProduct = Vector3.Dot(transform.forward, direction);
        //if (dotProduct >= Mathf.Cos(FieldOfView))
        //{
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, Collider.radius, LineOfSightLayers))
            {
                OnGainSight?.Invoke(Target);
                return true;
            }
            /*
             * This was added here so that the enemy could always look for cover from any distance away,
             * Not only when the player is near the enemy. This way the player can shoot the enemy from a distance
             * And the enemy will look for cover
             */
            else if(lookForCoverIndefinitely)
            {
                Target = player.transform;
                OnGainSight?.Invoke(Target);
                return true;
            }
        //}

        return false;
    }

    private IEnumerator CheckForLineOfSight(Transform Target)
    {
        WaitForSeconds Wait = new WaitForSeconds(0.5f);

        while(!CheckLineOfSight(Target))
        {
            yield return Wait;
        }
    }
}