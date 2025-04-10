/**
// File Name : PlayerController.cs
// Author : Jack P. Fisher
// Creation Date : March 24, 2025
//
// Brief Description : This script has the code for the player movement, dash, ground slam, jump, and turns the character with the camera. 
A lot of things need changed still like the controls need to be refined and I need to figure out if I want the ground slam on spacebar or a different button.
I followed a guide to recreate the movement of Quake, an arena shooter, in my game which will also be an arena shooter. 
I don't really like the movement sytstem though so I may go back and change it into something else when I feel better 
(I have been sick after catching something from my partner Autumn who I visited on Thursday). 
**/
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;

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


        //move direction for the dash input to make you dash in the direction you are moving
        private Vector3 moveDirection;
        


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


        public PlayerInput playerInput;
        private InputAction dash;
        private InputAction restart;
        private InputAction quit;
        

        private void Start()
        {
            CanDash = true;
            playerTransform = transform;
            playerCharacter = GetComponent<CharacterController>();

            if (!m_Camera)
                m_Camera = Camera.main;

            m_CamTran = m_Camera.transform;

            playerInput.currentActionMap.Enable();
            
            restart = playerInput.currentActionMap.FindAction("Restart");
            quit = playerInput.currentActionMap.FindAction("Quit");
            dash = playerInput.currentActionMap.FindAction("Dash");
            

            dash.started += Dash_started;
            restart.started += Restart_started;
            quit.started += Quit_started;

        }

        private void Quit_started(InputAction.CallbackContext context)
        {
            SceneManager.LoadScene(0);
        }

        private void Restart_started(InputAction.CallbackContext context)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.buildIndex);
        }

        private void Dash_started(InputAction.CallbackContext context)
        {
            StartCoroutine(Dash());
        }



        private void Update()
        {
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

            // I am using the old input system because I am not quite sure what I want the controls to be yet, and I am sick and short on time. This will be updated later.
            
            
            if (jumpQueued && Input.GetKey(KeyCode.Space))
            {
                StartCoroutine(Slam());
            }
            
        }

        //this function allows the player to slam back down to the ground after jumping
        IEnumerator Slam()
        {
            playerVelocity = new Vector3(0f, transform.up.y * -dashSpeed, 0f);
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
        //for now this just loads the win scene since the checklist for the alpha says the player needs an objective.
        //In the future this will progress the player to the next level.
        private void OnTriggerEnter(Collider collision)
        {
            if (collision.tag == "Collectible")
            {
                SceneManager.LoadScene(2);
            }
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