using UnityEngine;
using Cinemachine;
namespace AdrianWez
{
    namespace Controller
    {
        // Basic Third Person Controller
        [RequireComponent(typeof(CharacterController))]
        [RequireComponent(typeof(Animator))]
        public class ThirdPerson : MonoBehaviour
        {
            [Header("Player")]
            [Tooltip("Move speed of the character in m/s")]
            [SerializeField] private float MoveSpeed = 2.0f;

            [Tooltip("Sprint speed of the character in m/s")]
            [SerializeField] private float SprintSpeed = 5.335f;
            [SerializeField] private float _horizontalRotation = 2.5f;
            [SerializeField] private float _verticalRotation = 2;

            [Tooltip("How fast the character turns to face movement direction")]
            [Range(0.0f, 0.3f)]
            [SerializeField] private float RotationSmoothTime = 0.12f;

            [SerializeField] private AudioClip LandingAudioClip;
            [SerializeField] private AudioClip[] FootstepAudioClips;
            [Range(0, 1)][SerializeField] private float FootstepAudioVolume = 0.5f;

            [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
            [SerializeField] private float Gravity = -15.0f;

            [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
            [SerializeField] private float FallTimeout = 0.15f;


            [Tooltip("Useful for rough ground")]
            [SerializeField] private float GroundedOffset = -0.14f;

            [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
            [SerializeField] private float GroundedRadius = 0.28f;

            [Tooltip("What layers the character uses as ground")]
            public LayerMask GroundLayers;

            [Header("Camera")]
            [SerializeField] private Camera _mainCamera;
            [SerializeField] private CinemachineVirtualCamera _followCamera;
            private Cinemachine3rdPersonFollow _3rdPersonFollow { get => _followCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>(); }
            [SerializeField] private float _maxFollowDistance = 2.4f;
            [SerializeField] private float _minFollowDistance = 1.5f;
            [SerializeField] private Vector2 _followVerticalClamp = new(70, -30);

            [SerializeField] private CinemachineVirtualCamera _focusCamera;

            [Tooltip("How far in degrees can you move the camera (Up, Down)")]
            [SerializeField] private Vector2 _focusVerticalClamp = new(25, -20);
            [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
            [SerializeField] private GameObject CinemachineCameraTarget;

            [Tooltip("For locking the camera position on all axis")]
            [SerializeField] private bool LockCameraPosition = false;

            // cinemachine
            private float _cinemachineTargetYaw;
            private float _cinemachineTargetPitch;

            // player
            private float _speed;
            private float _targetSpeed;
            private Vector3 _targetDirection;
            private float _rotation;
            private float _FocusingRotationClamp;
            private float _targetRotation = 0.0f;
            private float _rotationVelocity;
            private float _verticalVelocity;
            private float _terminalVelocity = 53.0f;
            private Vector3 _spherePosition;            // Sphere used to check if player is grounded

            // timeout deltatime
            private float _fallTimeoutDelta;

            // animation IDs
            private int _animIDSpeed;
            private int _animIDGrounded;
            private int _animIDFreeFall;
            private int _animIDXAxis;
            private int _animIDZAxis;

            // animations parameters
            private float _baseParameter;
            private Vector3 _focusingParameterXZ;
            [SerializeField] private float _animationBlendRate = 6f;

            // hidden refs
            private Animator _animator { get => GetComponent<Animator>(); }
            private CharacterController _controller { get => GetComponent<CharacterController>(); }

            private Vector2 _moveInput { get => new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")); }
            private Vector2 _lookInput { get => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); }
            private Vector3 _normalizedMove;
            // states
            public bool Grounded { get; private set; }      // Used to check if player is touching the ground
            public bool Focusing { get; private set; }       // Also known as aiming (default pattern is used by hold Right Mouse)
            public bool Moving { get; private set; }        // is player moving horizontally
            public bool Sprinting { get; private set; }      // Move speed acceleration 

            private void Start()
            {
                // preventing main camera's null return
                if (_mainCamera == null) GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

                _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

                AssignAnimationIDs();

                // reset our timeouts on start
                _fallTimeoutDelta = FallTimeout;

                // disabling this cocksuckers
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            private void Update()
            {

                _focusCamera.enabled = Focusing ? true : false;

                States();
                JumpAndGravity();
                Move();
            }

            private void LateUpdate() => CameraProperties();

            private void AssignAnimationIDs()
            {
                _animIDSpeed = Animator.StringToHash("Speed");
                _animIDXAxis = Animator.StringToHash("XAxis");
                _animIDZAxis = Animator.StringToHash("ZAxis");
                _animIDGrounded = Animator.StringToHash("Grounded");
                _animIDFreeFall = Animator.StringToHash("FreeFall");
            }

            private void CameraProperties()
            {
                // if there is an input and camera position is not fixed
                if (_lookInput != Vector2.zero && !LockCameraPosition)
                {
                    _cinemachineTargetYaw += _lookInput.x * _horizontalRotation;
                    _cinemachineTargetPitch -= _lookInput.y * _verticalRotation;
                }

                _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
                // clamping up and down accordingly to the player's current state
                if (Focusing) _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, _focusVerticalClamp.y, _focusVerticalClamp.x);
                else _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, _followVerticalClamp.y, _followVerticalClamp.x);

                // Cinemachine will follow this target
                CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);

                // focus camera
                _focusCamera.gameObject.SetActive(Focusing);
                
                // smooth transition to sprint
                _3rdPersonFollow.CameraDistance = Sprinting? Mathf.Lerp(_3rdPersonFollow.CameraDistance, _maxFollowDistance, Time.deltaTime * _animationBlendRate) : Mathf.Lerp(_3rdPersonFollow.CameraDistance, _minFollowDistance, Time.deltaTime * _animationBlendRate);

            }

            private void States()
            {
                // set sphere position, with offset
                _spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
                Grounded = Physics.CheckSphere(_spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

                // update animator
                _animator.SetBool(_animIDGrounded, Grounded);

                if (Focusing)
                {
                    Sprinting = Grounded && Input.GetKeyDown(KeyCode.LeftShift) && _moveInput != Vector2.zero;
                    Focusing = Grounded && !Sprinting && Input.GetKey(KeyCode.Mouse1);
                }
                else
                {
                    Sprinting = Grounded && Input.GetKey(KeyCode.LeftShift) && _moveInput != Vector2.zero;
                    Focusing = Grounded && Input.GetKeyDown(KeyCode.Mouse1) && !Sprinting;
                }

                Moving = new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude != .0f;
            }

            private void Move()
            {
                // set target speed based on move speed, sprint speed and if sprint is pressed
                _targetSpeed = Sprinting ? SprintSpeed : MoveSpeed;

                // if there is no input, set the target speed to 0
                if (_moveInput == Vector2.zero) _targetSpeed = 0.0f;
                _speed = _moveInput.magnitude * _targetSpeed;

                // normalise input direction
                _normalizedMove = new Vector3(_moveInput.x, 0.0f, _moveInput.y).normalized;

                // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
                // if there is a move input rotate player when the player is moving
                if (_moveInput != Vector2.zero && !Focusing)
                {
                    _targetRotation = Mathf.Atan2(_normalizedMove.x, _normalizedMove.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                    _rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                    // rotate to face input direction relative to camera position
                    transform.rotation = Quaternion.Euler(0.0f, _rotation, 0.0f);
                }
                if (Focusing)
                {
                    _rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _mainCamera.transform.eulerAngles.y, ref _rotationVelocity, RotationSmoothTime/2);
                    // rotate to face input direction relative to camera position
                    transform.rotation = Quaternion.Euler(0.0f, _rotation, 0.0f);
                }

                _targetDirection = Focusing ? transform.right * _moveInput.x + transform.forward * _moveInput.y : Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

                // move the player
                _controller.Move(_targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

                // updating animator
                if (Focusing)
                {
                    _baseParameter = Mathf.Clamp01(_baseParameter + Time.deltaTime * _animationBlendRate);
                    _focusingParameterXZ = Vector3.Lerp(_focusingParameterXZ, new Vector3(_moveInput.x, 0, _moveInput.y) * _speed, Time.deltaTime * _animationBlendRate);
                }
                else
                {
                    _baseParameter = Mathf.Clamp01(_baseParameter - Time.deltaTime * _animationBlendRate);
                    _focusingParameterXZ = Vector3.Lerp(_focusingParameterXZ, Vector3.forward * _speed, Time.deltaTime * _animationBlendRate);
                }
                _animator.SetFloat(_animIDSpeed, _baseParameter);
                _animator.SetFloat(_animIDXAxis, Mathf.Round(_focusingParameterXZ.x * 100) / 100);
                _animator.SetFloat(_animIDZAxis, Mathf.Round(_focusingParameterXZ.z * 100) / 100);
            }

            private void JumpAndGravity()
            {
                if (Grounded)
                {
                    // reset the fall timeout timer
                    _fallTimeoutDelta = FallTimeout;

                    // update animator if using character
                    _animator.SetBool(_animIDFreeFall, false);

                    // stop our velocity dropping infinitely when grounded
                    if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;

                }
                else
                {
                    // fall timeout
                    if (_fallTimeoutDelta >= 0.0f) _fallTimeoutDelta -= Time.deltaTime;
                    // update animator
                    else _animator.SetBool(_animIDFreeFall, true);
                }

                // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
                if (_verticalVelocity < _terminalVelocity)
                    _verticalVelocity += Gravity * Time.deltaTime;
            }

            private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
            {
                if (lfAngle < -360f) lfAngle += 360f;
                if (lfAngle > 360f) lfAngle -= 360f;
                return Mathf.Clamp(lfAngle, lfMin, lfMax);
            }

            // Gizmos
            private void OnDrawGizmosSelected()
            {
                Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
                Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

                if (Grounded) Gizmos.color = transparentGreen;
                else Gizmos.color = transparentRed;

                // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
                Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
            }

            // Audios
            private void OnFootstep(AnimationEvent animationEvent)
            {
                if (animationEvent.animatorClipInfo.weight > 0.5f)
                {
                    if (FootstepAudioClips.Length > 0)
                    {
                        var index = Random.Range(0, FootstepAudioClips.Length);
                        AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                    }
                }
            }

            private void OnLand(AnimationEvent animationEvent)
            {
                if (animationEvent.animatorClipInfo.weight > 0.5f)
                {
                    AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }
    }
}
