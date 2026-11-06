using UnityEngine;

public class OilSlickHazard : MonoBehaviour, IWeaponPickup
{
    public string WeaponName => "Iridescent Oil Slick";
    public WeaponSlot Slot => WeaponSlot.Rear;

    [Header("Hazard Tuning")]
    public float lifetime = 10f;
    public float slipperyDragMultiplier = 0.2f;

    private ArcadeDriftController ownerCar;
    private bool isPlaced = false;

    public void Initialize(ArcadeDriftController owner)
    {
        ownerCar = owner;
    }

    public void ActivateWeapon()
    {
        transform.parent = null;
        if (ownerCar != null)
        {
            transform.position = ownerCar.transform.position - (ownerCar.transform.forward * 0.9f) + (Vector3.up * 0.02f);
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
        isPlaced = true;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isPlaced) return;

        ArcadeDriftController victim = other.GetComponentInParent<ArcadeDriftController>();
        if (victim != null)
        {
            victim.SetSurfaceSpeedMultiplier(slipperyDragMultiplier);

            // Spin out visual root slightly for arcade feel
            if (victim.visualRoot != null)
            {
                victim.visualRoot.Rotate(0f, 280f * Time.deltaTime, 0f, Space.Self);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isPlaced) return;

        ArcadeDriftController victim = other.GetComponentInParent<ArcadeDriftController>();
        if (victim != null)
        {
            victim.SetSurfaceSpeedMultiplier(1f);
        }
    }
}
