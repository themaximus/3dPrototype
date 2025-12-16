using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Ссылка на компонент 'Light' (луч фонарика)")]
    public Light flashlightBeam; // Перетащи сюда свой Spot Light

    [Tooltip("Звук включения")]
    public AudioClip clickOnSound;

    [Tooltip("Звук выключения")]
    public AudioClip clickOffSound;

    [Header("Input")]
    [Tooltip("Кнопка для включения/выключения")]
    public KeyCode toggleKey = KeyCode.Mouse0; // ЛКМ по умолчанию

    private bool isOn = false;
    private AudioSource audioSource;

    void Awake()
    {
        // Настраиваем AudioSource (он нужен для звуков)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (flashlightBeam == null)
        {
            Debug.LogError("FlashlightController: 'flashlightBeam' не назначен!", this);
            this.enabled = false;
            return;
        }

        // Убедимся, что фонарик выключен при старте
        flashlightBeam.enabled = false;
        isOn = false;
    }

    // Update "слушает" нажатия, только когда этот
    // префаб существует в сцене (т.е. когда он в руках)
    void Update()
    {
        // Если нажата наша кнопка
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFlashlight();
        }
    }

    private void ToggleFlashlight()
    {
        // Меняем состояние
        isOn = !isOn;

        // Включаем/выключаем свет
        flashlightBeam.enabled = isOn;

        // Проигрываем звук
        if (isOn && clickOnSound != null)
        {
            audioSource.PlayOneShot(clickOnSound);
        }
        else if (!isOn && clickOffSound != null)
        {
            audioSource.PlayOneShot(clickOffSound);
        }
    }
}