using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public float speed = 5.0f;
    public float sprintSpeedMultiplier = 1.5f;
    public float lookSpeed = 2.0f;
    public float jumpHeight = 1.0f;
    public float gravity = -9.81f;
    public float crouchHeight = 0.5f;
    public float crouchSpeedMultiplier = 0.5f;

    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private float yVelocity = 0f;
    private float originalHeight;

    [Header("Camera Settings")]
    public Transform cameraTransform; // Установить через инспектор
    private float xRotation = 0f;

    [Header("Camera Bobbing")]
    public float cameraBobFrequency = 2f;
    public float cameraBobAmplitude = 0.05f;
    public AudioClip[] footstepSounds; // Массив звуков шагов
    public AudioSource audioSource; // Аудиоисточник для воспроизведения звуков

    private Vector3 initialCameraPosition;
    private float bobTimer = 0f;
    private bool isStepSoundPlayed = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;

        // Если камера не назначена вручную, ищем Main Camera
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            else
            {
                Debug.LogError("Камера не найдена! Убедитесь, что Main Camera существует или назначьте её вручную.");
            }
        }

        Cursor.lockState = CursorLockMode.Locked; // Блокировка курсора
        if (cameraTransform != null)
        {
            initialCameraPosition = cameraTransform.localPosition;
        }

        // Проверка наличия аудиоисточника
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        HandleMovement();
        HandleCameraBob();
        RotateCamera();
    }

    private void HandleMovement()
    {
        // Проверяем, на земле ли персонаж
        if (controller.isGrounded)
        {
            // Получение ввода для перемещения
            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");

            // Вычисляем скорость
            float currentSpeed = speed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed *= sprintSpeedMultiplier;
            }
            if (Input.GetKey(KeyCode.LeftControl))
            {
                controller.height = Mathf.Lerp(controller.height, crouchHeight, Time.deltaTime * 10f);
                currentSpeed *= crouchSpeedMultiplier;
            }
            else
            {
                controller.height = Mathf.Lerp(controller.height, originalHeight, Time.deltaTime * 10f);
            }

            // Вычисляем движение относительно локальных осей игрока
            moveDirection = transform.right * moveHorizontal + transform.forward * moveVertical;
            moveDirection *= currentSpeed;

            // Прыжок
            if (Input.GetButtonDown("Jump"))
            {
                yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            // Если персонаж в воздухе, движение по XZ продолжается с последним направлением
            moveDirection = new Vector3(moveDirection.x, 0, moveDirection.z);
        }

        // Применяем гравитацию
        yVelocity += gravity * Time.deltaTime;
        moveDirection.y = yVelocity;

        // Двигаем персонажа
        controller.Move(moveDirection * Time.deltaTime);
    }

    private void HandleCameraBob()
    {
        if (controller.isGrounded && (moveDirection.x != 0 || moveDirection.z != 0))
        {
            // Проверка на спринт
            float currentBobFrequency = Input.GetKey(KeyCode.LeftShift) ? cameraBobFrequency * 1.5f : cameraBobFrequency;

            // Камера движется вверх-вниз, основываясь на таймере и амплитуде
            bobTimer += Time.deltaTime * currentBobFrequency;
            float bobOffset = Mathf.Sin(bobTimer) * cameraBobAmplitude;

            // Воспроизведение звука шага в нижней точке синусоиды
            if (Mathf.Sin(bobTimer) < -0.9f && !isStepSoundPlayed)
            {
                PlayFootstepSound();
                isStepSoundPlayed = true;
            }
            else if (Mathf.Sin(bobTimer) > -0.1f)
            {
                isStepSoundPlayed = false;
            }

            // Обновляем позицию камеры
            cameraTransform.localPosition = initialCameraPosition + new Vector3(0, bobOffset, 0);
        }
        else
        {
            // Возвращаем камеру в изначальное положение
            bobTimer = 0f;
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, initialCameraPosition, Time.deltaTime * 10f);
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, footstepSounds.Length);
            AudioClip footstep = footstepSounds[randomIndex];
            audioSource.PlayOneShot(footstep);
        }
    }

    private void RotateCamera()
    {
        float rotateHorizontal = Input.GetAxis("Mouse X") * lookSpeed;
        float rotateVertical = Input.GetAxis("Mouse Y") * lookSpeed;

        // Вращаем персонажа по горизонтали
        transform.Rotate(Vector3.up * rotateHorizontal);

        // Ограничиваем вращение камеры по вертикали
        xRotation -= rotateVertical;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Вращаем камеру
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
}
