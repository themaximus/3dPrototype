using UnityEngine;

/// <summary>
/// Узкоспециализированный скрипт для фонарика.
/// </summary>
public class FlashlightAction : MonoBehaviour
{
    public Light flashlightBeam;
    public AudioClip clickOnSound;
    public AudioClip clickOffSound;

    private AudioSource audioSource;
    private bool isFlashlightOn = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (flashlightBeam != null)
        {
            flashlightBeam.enabled = isFlashlightOn;
        }
    }

    // Эту функцию мы будем вызывать из EquippableItemController
    public void ToggleFlashlight()
    {
        if (flashlightBeam == null) return;

        isFlashlightOn = !isFlashlightOn;
        flashlightBeam.enabled = isFlashlightOn;

        if (isFlashlightOn && clickOnSound != null)
        {
            audioSource.PlayOneShot(clickOnSound);
        }
        else if (!isFlashlightOn && clickOffSound != null)
        {
            audioSource.PlayOneShot(clickOffSound);
        }
    }
}