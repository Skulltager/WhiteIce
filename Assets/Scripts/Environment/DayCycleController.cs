using UnityEngine;

public class DayCycleController : MonoBehaviour
{
    [SerializeField] private Light sunLight = default;
    [SerializeField] private Light moonLight = default;
    [SerializeField] private Vector3 startDirectionSun = default;
    [SerializeField] private Vector3 rotationAngle = default;

    [SerializeField] private float dayLengthInSeconds = default;

    private float dayTimeSpend;
    private float moonBaseIntensity;
    private float sunBaseIntensity;

    private void Awake()
    {
        moonBaseIntensity = moonLight.intensity;
        sunBaseIntensity = sunLight.intensity;
        sunLight.transform.LookAt(sunLight.transform.position - startDirectionSun);
        moonLight.transform.LookAt(moonLight.transform.position + startDirectionSun);
        dayTimeSpend = 0;
    }

    private void FixedUpdate()
    {
        dayTimeSpend += Time.fixedDeltaTime;
        Vector3 sunDirection = Quaternion.Euler(rotationAngle * dayTimeSpend / dayLengthInSeconds) * startDirectionSun;
        sunLight.transform.LookAt(sunLight.transform.position - sunDirection);
        moonLight.transform.LookAt(moonLight.transform.position + sunDirection);
        sunLight.intensity = Mathf.Clamp01(Vector3.Dot(Vector3.up, sunDirection) * 3) * sunBaseIntensity;
        moonLight.intensity = Mathf.Clamp01(Vector3.Dot(Vector3.down, sunDirection) * 3) * moonBaseIntensity;
    }
}
