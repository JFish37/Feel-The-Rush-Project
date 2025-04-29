using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float shootInterval = 2f;
    
    public float fireForce; 
    private Transform player;
    private Transform enemyTransform;
    private bool playerInRange = false;
    private float timer = 0f;

    void Update()
    {
        enemyTransform = transform; 


        if (playerInRange && player != null)
        {
            timer += Time.deltaTime;

            if (timer >= shootInterval)
            {
                ShootAtPlayer();
                timer = 0f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            player = null;
        }
    }

    void ShootAtPlayer()
    {
        GameObject projectile = Instantiate(projectilePrefab, enemyTransform.position, Quaternion.identity);
        projectile.GetComponent<Rigidbody>().AddForce(enemyTransform.position * fireForce, ForceMode.Impulse);
        
    }
}
