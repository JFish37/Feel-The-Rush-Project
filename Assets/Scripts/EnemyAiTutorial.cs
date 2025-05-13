/**
// File Name : PlayerController.cs
// Author : Jack P. Fisher
// Creation Date : April 21, 2025
//
// Brief Description : This script controls enemy movement and attacks, and makes enemies take more damage depending on player speed.
**/
using Q3Movement;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemyAiTutorial : MonoBehaviour
{
    public NavMeshAgent agent;

    public Transform player;
    private Q3PlayerController playerScript;

    public LayerMask whatIsGround, whatIsPlayer;
    
    public float health;
    private int Damage;

    public Transform bulletSpawnPoint;

    

    

    //enemy wandering variables
    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange;

    //Attacking
    public float timeBetweenAttacks;
    bool alreadyAttacked;
    public GameObject bullet;

    //States
    public float sightRange, attackRange;
    public bool playerInSightRange, playerInAttackRange;

    //on death enemy will open door to next room
    public GameObject Door;
    private void Start()
    {
        playerScript = FindObjectOfType<Q3PlayerController>();
        
        player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        //Check for sight and attack range
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        if (!playerInSightRange && !playerInAttackRange) Wander();
        if (playerInSightRange && !playerInAttackRange) ChasePlayer();
        if (playerInAttackRange && playerInSightRange) AttackPlayer();

        //this changes the damage enemies take depending on the player's speed, which is the main mechanic of the game.
        if (playerScript != null)
        {
            if (playerScript.isDamage1)
            {
                Damage = 10;
            }
            else if (playerScript.isDamage2)
            {
                Damage = 20;
            }
            else if (playerScript.isDamage3)
            {
                Damage = 25;
            }
        }
        else
        {
            Debug.LogWarning("playerScript is null; cannot set Damage.");
        }

    }

    private void Wander()
    {
        if (!walkPointSet) SearchWalkPoint();

        if (walkPointSet)
            agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        //Walkpoint reached
        if (distanceToWalkPoint.magnitude < 1f)
            walkPointSet = false;
    }
    private void SearchWalkPoint()
    {
        //Calculate random point in range
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
            walkPointSet = true;
    }

    private void ChasePlayer()
    {
        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        //Make sure enemy doesn't move
        agent.SetDestination(transform.position);

        transform.LookAt(player);

        if (!alreadyAttacked)
        {
            ///Attack code here
            Rigidbody rb = Instantiate(bullet, bulletSpawnPoint.position, Quaternion.identity).GetComponent<Rigidbody>();
            rb.AddForce(transform.forward * 32f, ForceMode.Impulse);
            

            ///End of attack code

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }
    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    
    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    //when enemy is shot, will take damage and eventually die and open door
    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "PlayerBullet")
        {
            
            health -= Damage;

            if (health <= 0)
            {
                Door.transform.position = new Vector3( transform.position.x, transform.position.y + 50, transform.position.z);
                Invoke(nameof(DestroyEnemy), 0.5f);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}
