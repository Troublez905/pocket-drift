using UnityEngine;

public class KineticGrappleWeapon : MonoBehaviour, IWeaponPickup
{
    public string WeaponName => "Kinetic Grapple Launcher";
    public WeaponSlot Slot => WeaponSlot.Roof;

    [Header("Grapple Tuning")]
    public float maxRange = 18f;
    public float pullForce = 22f;
    public float tetherDuration = 1.2f;
    public LayerMask grappleLayer = ~0;
    public LineRenderer cableLine;

    private ArcadeDriftController ownerCar;
    private Transform latchedTarget;
    private Rigidbody latchedRb;
    private float tetherTimer;
    private bool isTethered;

    public void Initialize(ArcadeDriftController owner)
    {
        ownerCar = owner;
        if (cableLine == null) cableLine = GetComponent<LineRenderer>();
        if (cableLine != null) cableLine.enabled = false;
    }

    public void ActivateWeapon()
    {
        if (ownerCar == null) return;

        Vector3 origin = transform.position;
        Vector3 forward = ownerCar.transform.forward;

        if (Physics.Raycast(origin, forward, out RaycastHit hit, maxRange, grappleLayer))
        {
            latchedTarget = hit.transform;
            latchedRb = hit.rigidbody;
            isTethered = true;
            tetherTimer = tetherDuration;

            if (cableLine != null)
            {
                cableLine.enabled = true;
            }
        }
        else
        {
            // Missed grapple
            Destroy(gameObject, 0.2f);
        }
    }

    private void Update()
    {
        if (!isTethered) return;

        tetherTimer -= Time.deltaTime;
        if (tetherTimer <= 0f || latchedTarget == null || ownerCar == null)
        {
            ReleaseGrapple();
            return;
        }

        if (cableLine != null)
        {
            cableLine.SetPosition(0, transform.position);
            cableLine.SetPosition(1, latchedTarget.position);
        }

        // Pull owner toward target
        if (ownerCar.sphereRb != null)
        {
            Vector3 pullDir = (latchedTarget.position - ownerCar.transform.position).normalized;
            ownerCar.sphereRb.AddForce(pullDir * pullForce, ForceMode.Acceleration);
        }

        // Yank rival backward if it's a dynamic car
        if (latchedRb != null && latchedRb != ownerCar.sphereRb)
        {
            Vector3 yankDir = (ownerCar.transform.position - latchedTarget.position).normalized;
            latchedRb.AddForce(yankDir * (pullForce * 0.5f), ForceMode.Acceleration);
        }
    }

    private void ReleaseGrapple()
    {
        isTethered = false;
        if (cableLine != null) cableLine.enabled = false;

        // Slingshot velocity bonus on release
        if (ownerCar != null && ownerCar.sphereRb != null)
        {
            ownerCar.sphereRb.AddForce(ownerCar.transform.forward * 8f, ForceMode.VelocityChange);
        }

        Destroy(gameObject);
    }
}
