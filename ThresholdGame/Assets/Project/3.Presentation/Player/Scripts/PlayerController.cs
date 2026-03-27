using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    #region Variables: Movement

    private Vector2 _input;
    private CharacterController _characterController;

    private Vector3 _direction;       // incluye Y (gravedad)
    private Vector3 _moveDirection;   // solo XZ (movimiento)

    [SerializeField] private float speed;

    [SerializeField] private Transform cameraTransform; // <-- arrastra aquí la Main Camera (la que tiene CinemachineBrain)

    #endregion

    #region Variables: Rotation

    [SerializeField] private float smoothTime = 0.8f;
    private float _currentVelocity;

    #endregion

    #region Variables: Gravity

    private float _gravity = -9.81f;
    [SerializeField] private float gravityMultiplier = 3.0f;
    private float _velocity;

    #endregion

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        ApplyGravity();
        ApplyRotation();
        ApplyMovement();
    }

    private void ApplyGravity()
    {
        if (_characterController.isGrounded && _velocity < 0.0f)
            _velocity = -1.0f;
        else
            _velocity += _gravity * gravityMultiplier * Time.deltaTime;

        _direction.y = _velocity;
    }

    private void ApplyRotation()
    {
        if (_moveDirection.sqrMagnitude == 0) return;

        var targetAngle = Mathf.Atan2(_moveDirection.x, _moveDirection.z) * Mathf.Rad2Deg;
        var angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _currentVelocity, smoothTime);
        transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
    }

    private void ApplyMovement()
    {
        _direction.x = _moveDirection.x;
        _direction.z = _moveDirection.z;

        _characterController.Move(_direction * speed * Time.deltaTime);
    }

    public void Move(InputAction.CallbackContext context)
    {
        _input = context.ReadValue<Vector2>();

        // Si no hay cámara asignada, fallback a mundo
        if (cameraTransform == null)
        {
            _moveDirection = new Vector3(_input.x, 0.0f, _input.y);
            return;
        }

        // Movimiento relativo a la cámara (plano XZ)
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f; camForward.Normalize();
        camRight.y = 0f; camRight.Normalize();

        _moveDirection = (camRight * _input.x + camForward * _input.y);
        if (_moveDirection.sqrMagnitude > 1f) _moveDirection.Normalize();
    }
}