using UnityEngine;

public class EMPMine : MonoBehaviour, IWeaponPickup
{
    public string WeaponName => "9V EMP Mine";
    public WeaponSlot Slot => WeaponSlot.Rear;

    [Header("Detonation Tuning")]
    public float armingDelay = 0.5f;
    public float blastRadius = 4.5f;
    public float stunDuration = 1.8f;
    public float upwardBlastImpulse = 4f;
    public GameObject blastVFXPrefab;

    private ArcadeDriftController ownerCar;
    private bool isArmed = false;
    private bool hasDetonated = false;

    public void Initialize(ArcadeDriftController owner)
    {
        ownerCar = owner;
    }

    public void ActivateWeapon()
    {
        // Detach from car root and drop onto workbench
        transform.parent = null;
        if (ownerCar != null && ownerCar.sphereRb != null)
        {
            transform.position = ownerCar.transform.position - (ownerCar.transform.forward * 0.8f);
        }
        Invoke(nameof(ArmMine), armingDelay);
    }

    private void ArmMine()
    {
        isArmed = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isArmed || hasDetonated) return;

        ArcadeDriftController victim = other.GetComponentInParent<ArcadeDriftController>();
        if (victim != null && victim != ownerCar)
        {
            Detonate();
        }
    }

    public void Detonate()
    {
        if (hasDetonated) return;
        hasDetonated = true;

        if (blastVFXPrefab != null)
        {
            Instantiate(blastVFXPrefab, transform.position, Quaternion.identity);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (var hit in hits)
        {
            ArcadeDriftController victim = hit.GetComponentInParent<ArcadeDriftController>();
            if (victim != null)
            {
                victim.ApplyEMPStun(stunDuration);

                if (victim.sphereRb != null)
                {
                    Vector3 pushDir = (victim.transform.position - transform.position).normalized;
                    pushDir.y = 0.5f;
                    victim.sphereRb.AddForce(pushDir * upwardBlastImpulse, ForceMode.Impulse);
                }
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, blastRadius);
    }
}
