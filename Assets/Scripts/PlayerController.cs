/**
// File Name : PlayerController.cs
// Author : Jack P. Fisher
// Creation Date : March 24, 2025
//
// Brief Description : This script has the code for the player movement, dash, ground slam, jump, and turns the character with the camera. It also allows the player to shoot
// and controls the UI elements corresponding to player health and damage.
**/
using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

namespace Q3Movement
{

    [RequireComponent(typeof(CharacterController))]
    public class Q3PlayerController : MonoBehaviour
    {
        [System.Serializable]
        public class MovementSettings
        {
            public float MaxSpeed;
            public float Acceleration;
            public float Deceleration;

            public MovementSettings(float maxSpeed, float accel, float decel)
            {
                MaxSpeed = maxSpeed;
                Acceleration = accel;
                Deceleration = decel;
            }
        }

        //sound effect for firing
        public AudioClip GunSound;

        


        private Vector3 addedVelocity;

        [Header("Aiming")]
        [SerializeField] private Camera m_Camera;
        [SerializeField] private MouseLook m_MouseLook = new MouseLook();

        [Header("Movement")]
        //I want the player gravity and friction to be low so the jump is floaty and you don't lose speed while on the ground
        [SerializeField] private float playerFriction = 6;
        [SerializeField] private float playerGravity = 5;
        [SerializeField] private float jumpForce = 15;

        [SerializeField] private bool queuingJump = false;
        [SerializeField] private float m_AirControl = 0.3f;
        [SerializeField] private MovementSettings m_GroundSettings = new MovementSettings(7, 14, 10);
        [SerializeField] private MovementSettings m_AirSettings = new MovementSettings(7, 2, 2);
        [SerializeField] private MovementSettings m_StrafeSettings = new MovementSettings(1, 50, 50);
        [SerializeField] private float dashSpeed;
        [SerializeField] private float dashCooldown;
        private bool CanDash;
        private float SlamCooldown;

        //variables for pausing and unpausing
        public GameObject pauseMenu;
        private bool isPaused;

        //tutorial popups
        public GameObject Tutorial1;
        public GameObject Tutorial2;
        public GameObject Tutorial3;
        public GameObject Tutorial4;
        public GameObject Tutorial5;

        //move direction for the dash input to make you dash in the direction you are moving
        private Vector3 moveDirection;

        //variables for firing the gun
        public Transform BulletSpawnPoint;
        public GameObject BulletPrefab;
        public float bulletSpeed;

        /// Returns player's current speed. This will be useful when I make a UI element that tracks player speed later. 
        public float Speed { get { return playerCharacter.velocity.magnitude; } }

        private CharacterController playerCharacter;
        private Vector3 normalizedMoveDirection = Vector3.zero;
        private Vector3 playerVelocity = Vector3.zero;

        // Used to queue the next jump just before hitting the ground.
        private bool jumpQueued = false;


        // Used to display real time friction values.
        private float m_PlayerFriction = 0;

        private Vector3 m_MoveInput;
        private Transform playerTransform;
        private Transform m_CamTran;

        //booleans to make enemy damage change work
        public bool isDamage1;
        public bool isDamage2;
        public bool isDamage3;
        
        private InputAction dash;
        private InputAction restart;
        private InputAction quit;
        
        //UI elements
        public RawImage Damage1;
        public RawImage Damage2;
        public RawImage Damage3;

        //Health and text to display health
        private int Health;
        public TMP_Text healthText;

        //used to set firerate
        private bool canFire;

        private void Awake()
        {
            isDamage1 = true;
            isDamage2 = false;
            isDamage3 = false;

            isPaused = false;
        }
        private void Start()
        {
            isDamage1 = true;
            isDamage2 = false;
            isDamage3 = false;
            Health = 100;
            healthText.text = "Health: " + Health.ToString();
            CanDash = true;
            playerTransform = transform;
            playerCharacter = GetComponent<CharacterController>();

            if (!m_Camera)
                m_Camera = Camera.main;

            m_CamTran = m_Camera.transform;

            isPaused = false;
            canFire = true;


        }

        

        //changes the ui to reflect how much damage the player is doing based on their current speed
        public void HandleUI()
        {
            if(Speed <= 7)
            {
                Damage2.enabled = false;
                Damage3.enabled = false;
                Damage1.enabled = true;

                //I need these because otherwise the enemy damage changing breaks and this is my solution
                isDamage1 = true;
                isDamage2 = false;
                isDamage3 = false;
            }
            if (Speed >= 8)
            {
                Damage2.enabled = true;
                Damage3.enabled = false;
                Damage1.enabled = false;

                isDamage1 = false;
                isDamage2 = true;
                isDamage3 = false;
            }
            if (Speed >= 12)
            {
                Damage2.enabled = false;
                Damage3.enabled = true;
                Damage1.enabled = false;

                isDamage1 = false;
                isDamage2 = false;
                isDamage3 = true;
            }
        }


        private void Update()
        {
            Debug.Log(Speed);
            HandleUI();
            m_MoveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
           
            QueueJump();

            // Change between grounded and air movement
            if (playerCharacter.isGrounded)
            {
                GroundMove();
            }
            else
            {
                AirMove();
            }

            

            // Move the character.
            playerCharacter.Move(playerVelocity * Time.deltaTime);

            // I am using the old input system because the new one won't work, and I can't figure out how to fix it
            
            //Ground Slam
            if (jumpQueued && Input.GetKey(KeyCode.Space))
            {
                StartCoroutine(Slam());
                
            }

            //Restart
            if (Input.GetKey(KeyCode.R))
            {
                Scene currentScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(currentScene.buildIndex);
            }
            //Quit to main menu
            if (Input.GetKey(KeyCode.Q))
            {
                SceneManager.LoadScene(0);
            }
            //Dash
            if (Input.GetKey(KeyCode.LeftShift))
            {
                StartCoroutine(Dash());
            }
            //Shoot Gun
            if (Input.GetKeyDown(KeyCode.Mouse0) && canFire)
            {
                AudioSource.PlayClipAtPoint(GunSound, transform.position);
                var Bullet = Instantiate(BulletPrefab, BulletSpawnPoint.position, BulletSpawnPoint.rotation);
                Bullet.GetComponent<Rigidbody>().velocity = BulletSpawnPoint.forward * bulletSpeed;
                canFire = false;

                
            }
            if (canFire == false)
            {
                StartCoroutine(FireRate());
            }
            //Pause menu
            if (Input.GetKeyDown(KeyCode.P) && isPaused == false)
            {
                
                    pauseMenu.SetActive(true);
                    isPaused = true;
                    Time.timeScale = 0;
                
                
            }
            if (Input.GetKeyDown(KeyCode.O) && isPaused == true)
            {

                pauseMenu.SetActive(false);
                isPaused = false;
                Time.timeScale = 1;


            }
        }

        

        //this function allows the player to slam back down to the ground after jumping
        IEnumerator Slam()
        {
            addedVelocity = new Vector3(0f, transform.up.y * -dashSpeed, 0f);
            CanDash = true;
            playerVelocity = playerVelocity + addedVelocity;
            yield return new WaitForSeconds(SlamCooldown);

        }


        




        //this function launches the player forward and then starts a cooldown to make the player wait before dashing
        IEnumerator Dash()
        {
            float startTime = Time.time;

            if (CanDash == true)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");

                Vector3 inputDir = new Vector3(h, 0f, v).normalized;

                // Transform input relative to camera
                moveDirection = Camera.main.transform.TransformDirection(inputDir);
                moveDirection.y = 0f;
                moveDirection.Normalize();
                
                playerVelocity = new Vector3(moveDirection.x * dashSpeed, 0f, moveDirection.z * dashSpeed);
                CanDash = false;
                yield return new WaitForSeconds(dashCooldown);
                CanDash = true;
            }
            


            
        }
        //used to stop the player from firing the gun constantly
        IEnumerator FireRate()
        {
            yield return new WaitForSeconds(0.9f);
            canFire = true;
        }
        //Triggers for taking damage, gaining health, and progressing to the next level
        private void OnTriggerEnter(Collider collision)
        {
            if (collision.tag == "Goal")
            {
                Debug.Log("I touched the goal");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1); 
            }
            if (collision.tag == "Health")
            {
                Health += 50;
                healthText.text = "Health: " + Health.ToString();
            }
            if(collision.tag == "EnemyBullet")
            {
                
                Health -= 10;
                healthText.text = "Health: " + Health.ToString();
                if (Health <= 0)
                {
                    Scene currentScene = SceneManager.GetActiveScene();
                    SceneManager.LoadScene(currentScene.buildIndex);
                }
            }
            if (collision.tag == "Tutorial1")
            {
                Tutorial1.SetActive(true);
                
                StartCoroutine(RemoveTutorials(10f));
            }
            if (collision.tag == "Tutorial2")
            {
                Tutorial2.SetActive(true);
                
                StartCoroutine(RemoveTutorials(6f));
            }
            if (collision.tag == "Tutorial3")
            {
                Tutorial3.SetActive(true);
                
                StartCoroutine(RemoveTutorials(8f));
            }
            if (collision.tag == "Tutorial4")
            {
                Tutorial4.SetActive(true);
                
                StartCoroutine(RemoveTutorials(8f));
            }
            if (collision.tag == "Tutorial5")
            {
                Tutorial5.SetActive(true);
                
                StartCoroutine(RemoveTutorials(6f));
            }

        }
    //tutorials will disappear after a few seconds
    private IEnumerator RemoveTutorials(float duration)
    {
        yield return new WaitForSeconds(duration);
            
            Tutorial1.SetActive(false);
            Tutorial2.SetActive(false);
            Tutorial3.SetActive(false);
            Tutorial4.SetActive(false);
            Tutorial5.SetActive(false);
    }
    // Queues the next jump.
    private void QueueJump()
        {
            if (queuingJump)
            {
                jumpQueued = Input.GetButton("Jump");
                return;
            }

            if (Input.GetButtonDown("Jump") && !jumpQueued)
            {
                jumpQueued = true;
            }

            if (Input.GetButtonUp("Jump"))
            {
                jumpQueued = false;
            }
        }

        // Handle air movement. 
        private void AirMove()
        {
            float accel;

            var wishdir = new Vector3(m_MoveInput.x, 0, m_MoveInput.z);
            wishdir = playerTransform.TransformDirection(wishdir);

            float wishspeed = wishdir.magnitude;
            wishspeed *= m_AirSettings.MaxSpeed;

            wishdir.Normalize();
            normalizedMoveDirection = wishdir;

            // CPM Air control.
            float wishspeed2 = wishspeed;
            if (Vector3.Dot(playerVelocity, wishdir) < 0)
            {
                accel = m_AirSettings.Deceleration;
            }
            else
            {
                accel = m_AirSettings.Acceleration;
            }

            // If the player is ONLY strafing left or right
            if (m_MoveInput.z == 0 && m_MoveInput.x != 0)
            {
                if (wishspeed > m_StrafeSettings.MaxSpeed)
                {
                    wishspeed = m_StrafeSettings.MaxSpeed;
                }

                accel = m_StrafeSettings.Acceleration;
            }

            Accelerate(wishdir, wishspeed, accel);
            if (m_AirControl > 0)
            {
                AirControl(wishdir, wishspeed2);
            }

            // Apply gravity
            playerVelocity.y -= playerGravity * Time.deltaTime;
        }

        // Air control occurs when the player is in the air, it allows players to move side 
        // to side much faster rather than being 'sluggish' when it comes to cornering.
        private void AirControl(Vector3 targetDir, float targetSpeed)
        {
            // Only control air movement when moving forward or backward.
            if (Mathf.Abs(m_MoveInput.z) < 0.001 || Mathf.Abs(targetSpeed) < 0.001)
            {
                return;
            }

            float zSpeed = playerVelocity.y;
            playerVelocity.y = 0;
            /* Next two lines are equivalent to idTech's VectorNormalize() */
            float speed = playerVelocity.magnitude;
            playerVelocity.Normalize();

            float dot = Vector3.Dot(playerVelocity, targetDir);
            float k = 32;
            k *= m_AirControl * dot * dot * Time.deltaTime;

            // Change direction while slowing down.
            if (dot > 0)
            {
                playerVelocity.x *= speed + targetDir.x * k;
                playerVelocity.y *= speed + targetDir.y * k;
                playerVelocity.z *= speed + targetDir.z * k;

                playerVelocity.Normalize();
                normalizedMoveDirection = playerVelocity;
            }

            playerVelocity.x *= speed;
            playerVelocity.y = zSpeed; // Note this line
            playerVelocity.z *= speed;
        }

        // Handle ground movement.
        private void GroundMove()
        {
            // Do not apply friction if the player is queueing up the next jump
            if (!jumpQueued)
            {
                ApplyFriction(1.0f);
            }
            else
            {
                ApplyFriction(0);
            }

            var wishdir = new Vector3(m_MoveInput.x, 0, m_MoveInput.z);
            wishdir = playerTransform.TransformDirection(wishdir);
            wishdir.Normalize();
            normalizedMoveDirection = wishdir;

            var wishspeed = wishdir.magnitude;
            wishspeed *= m_GroundSettings.MaxSpeed;

            Accelerate(wishdir, wishspeed, m_GroundSettings.Acceleration);

            // Reset the gravity velocity
            playerVelocity.y = -playerGravity * Time.deltaTime;

            if (jumpQueued)
            {
                playerVelocity.y = jumpForce;
                jumpQueued = false;
            }
        }

        private void ApplyFriction(float t)
        {
            // Equivalent to VectorCopy();
            Vector3 vec = playerVelocity;
            vec.y = 0;
            float speed = vec.magnitude;
            float drop = 0;

            // Only apply friction when grounded.
            if (playerCharacter.isGrounded)
            {
                float control = speed < m_GroundSettings.Deceleration ? m_GroundSettings.Deceleration : speed;
                drop = control * playerFriction * Time.deltaTime * t;
            }

            float newSpeed = speed - drop;
            m_PlayerFriction = newSpeed;
            if (newSpeed < 0)
            {
                newSpeed = 0;
            }

            if (speed > 0)
            {
                newSpeed /= speed;
            }

            playerVelocity.x *= newSpeed;
            // playerVelocity.y *= newSpeed;
            playerVelocity.z *= newSpeed;
        }

        // Calculates acceleration based on desired speed and direction.
        private void Accelerate(Vector3 targetDir, float targetSpeed, float accel)
        {
            float currentspeed = Vector3.Dot(playerVelocity, targetDir);
            float addspeed = targetSpeed - currentspeed;
            if (addspeed <= 0)
            {
                return;
            }

            float accelspeed = accel * Time.deltaTime * targetSpeed;
            if (accelspeed > addspeed)
            {
                accelspeed = addspeed;
            }

            playerVelocity.x += accelspeed * targetDir.x;
            playerVelocity.z += accelspeed * targetDir.z;
        }
    }
}