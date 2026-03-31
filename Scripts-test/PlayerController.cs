using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("Main Settings")]
    public CharacterStats characterStats;

    public float lookSpeed = 2.0f;
    public float gravity = -9.81f;
    public float crouchHeight = 0.5f;

    // --- НОВОЕ ПОЛЕ ---
    [HideInInspector]
    public bool canRotateCamera = true; // Если false, мышь не вращает камеру
    // ------------------

    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private float yVelocity = 0f;
    private float originalHeight;

    [Header("Movement Tuning")]
    public float acceleration = 15f;
    public float deceleration = 20f;
    private Vector3 currentMoveVelocity;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    private float xRotation = 0f;

    [Header("Camera Effects")]
    public float cameraTiltAmount = 3f;
    public float cameraTiltSpeed = 10f;
    private float currentCameraTilt = 0f;

    [Header("Camera Bobbing")]
    public float cameraBobFrequency = 2f;
    public float cameraBobAmplitude = 0.05f;
    public AudioClip[] footstepSounds;
    public AudioSource audioSource;

    private Vector3 initialCameraPosition;
    private float bobTimer = 0f;
    private bool isStepSoundPlayed = false;

    private float lastGroundedTime;
    private float jumpGracePeriod = 0.2f;

    void Start()
    {
        if (characterStats == null)
        {
            Debug.LogError("CharacterStats не назначен в инспекторе!");
            this.enabled = false;
            return;
        }

        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        if (cameraTransform != null)
            initialCameraPosition = cameraTransform.localPosition;

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        HandleMovement();
        HandleCameraBob();
        RotateCamera();
    }

    private void HandleMovement()
    {
        if (controller.isGrounded)
        {
            lastGroundedTime = Time.time;
            if (yVelocity < 0) yVelocity = -2f;
        }

        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        float currentSpeed = characterStats.speed;
        if (Input.GetKey(KeyCode.LeftShift)) currentSpeed *= characterStats.sprintSpeedMultiplier;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            controller.height = Mathf.Lerp(controller.height, crouchHeight, Time.deltaTime * 10f);
            currentSpeed *= characterStats.crouchSpeedMultiplier;
        }
        else
        {
            controller.height = Mathf.Lerp(controller.height, originalHeight, Time.deltaTime * 10f);
        }

        Vector3 targetVelocity = (transform.right * moveHorizontal + transform.forward * moveVertical) * currentSpeed;
        float smoothRate = (targetVelocity.magnitude > 0.1f) ? acceleration : deceleration;
        currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, Time.deltaTime * smoothRate);

        moveDirection.x = currentMoveVelocity.x;
        moveDirection.z = currentMoveVelocity.z;

        if (Time.time - lastGroundedTime <= jumpGracePeriod)
        {
            if (Input.GetButtonDown("Jump"))
            {
                yVelocity = Mathf.Sqrt(characterStats.jumpHeight * -2f * gravity);
                lastGroundedTime = -1f;
            }
        }

        yVelocity += gravity * Time.deltaTime;
        moveDirection.y = yVelocity;

        controller.Move(moveDirection * Time.deltaTime);
    }

    private void HandleCameraBob()
    {
        if (controller.isGrounded && currentMoveVelocity.magnitude > 0.1f)
        {
            float currentBobFrequency = Input.GetKey(KeyCode.LeftShift) ? cameraBobFrequency * 1.5f : cameraBobFrequency;

            bobTimer += Time.deltaTime * currentBobFrequency;
            float bobOffset = Mathf.Sin(bobTimer) * cameraBobAmplitude;

            if (Mathf.Sin(bobTimer) < -0.9f && !isStepSoundPlayed)
            {
                PlayFootstepSound();
                isStepSoundPlayed = true;
            }
            else if (Mathf.Sin(bobTimer) > -0.1f)
            {
                isStepSoundPlayed = false;
            }

            cameraTransform.localPosition = initialCameraPosition + new Vector3(0, bobOffset, 0);
        }
        else
        {
            bobTimer = 0f;
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, initialCameraPosition, Time.deltaTime * 10f);
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, footstepSounds.Length);
            audioSource.PlayOneShot(footstepSounds[randomIndex]);
        }
    }

    private void RotateCamera()
    {
        // --- ИЗМЕНЕНИЕ: Проверяем флаг ---
        if (!canRotateCamera) return;
        // ---------------------------------

        if (Cursor.lockState != CursorLockMode.Locked) return;

        float rotateHorizontal = Input.GetAxis("Mouse X") * lookSpeed;
        float rotateVertical = Input.GetAxis("Mouse Y") * lookSpeed;

        transform.Rotate(Vector3.up * rotateHorizontal);

        xRotation -= rotateVertical;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (cameraTransform != null)
        {
            float targetTilt = -Input.GetAxis("Horizontal") * cameraTiltAmount;
            currentCameraTilt = Mathf.Lerp(currentCameraTilt, targetTilt, Time.deltaTime * cameraTiltSpeed);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, currentCameraTilt);
        }
    }
}