using System;
using UnityEngine;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    [RequireComponent(typeof (AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private int m_JumpCount;
        [SerializeField] private float m_ClimbSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.
        [SerializeField] private AudioClip m_KnockBackArrowSound;
        [SerializeField] private AudioClip m_ShootSound;
        [SerializeField] private AudioClip m_BoostJumpSound;
        [SerializeField] private AudioClip m_PulledBackFull;
        [SerializeField] Animator m_Animator;
        [SerializeField] Animator m_BowAnimator;

        private Camera m_Camera;
        private bool m_Jump;
        private int m_JumpHold;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero, m_PrevMoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition, newMoveDirection;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping, m_HasJumpedOffWall, m_OnWall, m_Falling, playedAudioOnce;
        private AudioSource m_AudioSource;
        private Collider prevCollider = null;
        private float m_HeightOfCollider = 0;
        [SerializeField]
        private LayerMask mask;
        private bool disableNormalMovement;
        

        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = GetComponentInChildren<Camera>();
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;
            m_JumpHold = m_JumpCount;
            m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);

        }


        // Update is called once per frame
        private void Update()
        {
            
            RotateView();

            // check input for animations
            if (Input.GetKey("w"))
            {
                m_Animator.SetBool("Walk", true);

                if (Input.GetKey("left shift"))
                    m_Animator.SetBool("Run", true);
                else
                    m_Animator.SetBool("Run", false);
            }
            else
            {
                m_Animator.SetBool("Walk", false);
                m_Animator.SetBool("Run", false);
            }

            if (Input.GetKey("s"))
            {
                print("Backward");
                m_Animator.SetBool("Backward", true);
            }
            else
                m_Animator.SetBool("Backward", false);

            if (Input.GetKeyDown("space"))
                m_Animator.SetBool("Jump", true);

            if (Input.GetKeyUp("space"))
                m_Animator.SetBool("Jump", false);

            if (m_Jumping)
                m_Animator.SetBool("Jumping", true);
            else
                m_Animator.SetBool("Jumping", false);


            if (Input.GetMouseButtonDown(0))
                m_BowAnimator.SetBool("Pull", true);
            else
                m_BowAnimator.SetBool("Pull", false);

            if (Input.GetMouseButton(0))
                m_BowAnimator.SetBool("Hold", true);
            else
                m_BowAnimator.SetBool("Hold", false);

            if (Input.GetMouseButtonUp(0))
                m_BowAnimator.SetBool("Release", true);
            else
                m_BowAnimator.SetBool("Release", false);

            

            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump && m_JumpHold > 0)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
                if (m_Jump)
                {
                    m_Falling = false;
                    m_JumpHold -= 1;
                }
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                m_JumpHold = m_JumpCount;
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
                m_Falling = false;
                m_HasJumpedOffWall = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                print("falling!");
                m_Falling = true;
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;
        }

        public void PlayKnockbackShootSound()
        {
            m_AudioSource.PlayOneShot(m_KnockBackArrowSound);
        }
        public void PlayShootSound()
        {
            m_AudioSource.PlayOneShot(m_ShootSound);
        }
        public void PlayPulledBackFull()
        {
            m_AudioSource.PlayOneShot(m_PulledBackFull);
        }


        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate()
        {
            bool wallClimbing = false, wallRunning = false;
            float speed;
            GetInput(out speed);

            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;
            

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
            
            m_MoveDir.x = desiredMove.x*speed;
            m_MoveDir.z = desiredMove.z*speed;
            if (m_Jumping && !m_Falling)
            {
                RaycastHit hit;
                // Checks if touching wall forward
                if (Physics.Raycast(transform.position, transform.forward, out hit, .75f))
                {
                    // If you have tried to climb the same wall - dont allow
                    if (hit.collider == prevCollider && m_HasJumpedOffWall)
                    {
                        m_OnWall = false;
                    }
                    else
                    {
                        m_MoveDir = new Vector3(0, m_ClimbSpeed, 0);
                        if (m_HasJumpedOffWall)
                            m_JumpHold = 2;
                        m_HasJumpedOffWall = false;
                        m_OnWall = true;
                        wallClimbing = true;
                    }
                    prevCollider = hit.collider;
                }
                // Checks if touching wall on the right
                else if (Physics.Raycast(transform.position, transform.right, out hit, .75f))
                {
                    // If you have tried to run on the same wall - dont allow
                    if (hit.collider == prevCollider && m_HasJumpedOffWall)
                    {
                        m_OnWall = false;
                    }
                    else
                    {
                        m_MoveDir = new Vector3(Mathf.Round(m_MoveDir.x), desiredMove.y*speed, Mathf.Round(m_MoveDir.z));
                        if (m_HasJumpedOffWall)
                            m_JumpHold = 2;
                        m_HasJumpedOffWall = false;
                        m_OnWall = true;
                        wallRunning = true;
                    }
                    prevCollider = hit.collider;
                }
                // Checks if touching wall on the left
                else if (Physics.Raycast(transform.position, -transform.right, out hit, .75f))
                {
                    // If you have tried to run on the same wall - dont allow
                    if (hit.collider == prevCollider && m_HasJumpedOffWall)
                    {
                        m_OnWall = false;
                    }
                    else
                    {
                        m_MoveDir = new Vector3(Mathf.Round(m_MoveDir.x), desiredMove.y*speed, Mathf.Round(m_MoveDir.z));
                        if (m_HasJumpedOffWall)
                            m_JumpHold = 2;
                        m_HasJumpedOffWall = false;
                        m_OnWall = true;
                        wallRunning = true;
                    }
                    prevCollider = hit.collider;
                }
                else
                {
                    m_OnWall = false;
                    m_HasJumpedOffWall = true;
                }
            }
            if (m_Jump && m_JumpHold > -1)
            {
                if (m_CharacterController.isGrounded)
                    m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                    if (m_OnWall)
                    {
                        m_OnWall = false;
                        m_HasJumpedOffWall = true;
                    }
                }
            }
            else if (!m_OnWall)
            {
                m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
            }
            if ((transform.position.x > 28 && transform.position.x < 32) &&
            (transform.position.z > -42 && transform.position.z < -38) &&
            (transform.position.y > 1.5f && transform.position.y < 15))
            {
                m_JumpHold = 0;
                m_MoveDir.y = m_JumpSpeed * 4;
                if (!playedAudioOnce)
                {
                    m_AudioSource.PlayOneShot(m_BoostJumpSound);
                    playedAudioOnce = true;
                }
            }
            else
                playedAudioOnce = false;
            if (disableNormalMovement)
            {
                m_MoveDir = newMoveDirection;
                newMoveDirection += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);
            if (m_CharacterController.isGrounded)
                disableNormalMovement = false;

            m_PrevMoveDir = m_MoveDir;

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

            m_MouseLook.UpdateCursorLock();

        }
        public void SetSens(float value)
        {
            m_MouseLook.XSensitivity = value/5;
            m_MouseLook.YSensitivity = value/5;
        }
        public void SetMoveDirectionForPowerUp(Vector3 direction)
        {
            print(direction);
            newMoveDirection = direction * 25;
            if (m_CharacterController.isGrounded && direction.y < .2f)
                newMoveDirection.y = m_JumpSpeed * 2;
            disableNormalMovement = true;
            StartCoroutine(SetBoolForNewMoveDirection());
        }
        IEnumerator SetBoolForNewMoveDirection()
        {
            yield return new WaitForSeconds(10f);
            disableNormalMovement = false;
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift) || (horizontal > .5f || horizontal < -.5f);
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }


        private void RotateView()
        {
            m_MouseLook.LookRotation (transform, m_Camera.transform);
        }
        public Quaternion GetCamRotation()
        {
            return (m_Camera.transform.rotation);
        }

      
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                RaycastHit _hit;
                if (!Physics.Raycast(transform.position, -transform.up, out _hit, 1.1f, mask))
                {
                    print("EDGEEE");
                    m_StickToGroundForce = 0;
                }
                else
                {
                    m_StickToGroundForce = 10;
                }
                prevCollider = null;
                return;
            }
            disableNormalMovement = false;
            if (m_CollisionFlags == CollisionFlags.Sides || (m_CollisionFlags & CollisionFlags.Sides) != 0)
            {
                //prevCollider = hit.collider;
            }
            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
