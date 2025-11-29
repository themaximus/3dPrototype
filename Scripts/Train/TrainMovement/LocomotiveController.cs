using UnityEngine;

[RequireComponent(typeof(TrainBogie))]
public class LocomotiveController : MonoBehaviour
{
    [Header("Controls")]
    public float currentSpeed = 0f;
    public float maxSpeed = 15f;
    public float acceleration = 5f;
    public float brakeForce = 10f;

    [Header("Input")]
    [Range(-1, 1)] public int throttleInput = 0;

    private TrainBogie bogie;

    void Awake()
    {
        bogie = GetComponent<TrainBogie>();
        bogie.isLocomotive = true;
    }

    void Update()
    {
        if (throttleInput != 0)
        {
            currentSpeed += throttleInput * acceleration * Time.deltaTime;
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, brakeForce * Time.deltaTime);
        }

        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);

        bogie.distanceOnRail += currentSpeed * Time.deltaTime;
        bogie.UpdatePosition();
    }

    // --- НОВЫЙ МЕТОД: ЭКСТРЕННАЯ ОСТАНОВКА ---
    public void EmergencyStop()
    {
        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            Debug.Log("🛑 АВАРИЙНАЯ ОСТАНОВКА! Столкновение с вагоном.");
            currentSpeed = 0;
            throttleInput = 0;
        }
    }
}