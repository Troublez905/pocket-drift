using UnityEngine;

public class KnockbackCollisionHandler : MonoBehaviour
{
    [Header("References")]
    public ArcadeDriftController myDriftController;
    public Rigidbody mySphereRb;

    [Header("Knockback Physics Tuning")]
    public float baseKnockbackForce = 12f;
    public float boostRamMultiplier = 1.85f;
    public float springBumperMultiplier = 1.65f;
    public float upwardPopForce = 3.2f;
    public float collisionCooldown = 0.25f;

    [HideInInspector] public bool hasSpringBumperActive;
    public ArcadeDriftController LastAttacker { get; private set; }
    public float LastHitTimestamp { get; private set; }

    private float lastCollisionTime;

    private void Awake()
    {
        if (mySphereRb == null) mySphereRb = GetComponent<Rigidbody>();
        if (myDriftController == null) myDriftController = GetComponentInParent<ArcadeDriftController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastCollisionTime < collisionCooldown) return;

        KnockbackCollisionHandler otherCar = collision.collider.GetComponent<KnockbackCollisionHandler>();
        if (otherCar == null || otherCar == this) return;

        lastCollisionTime = Time.time;

        Vector3 myVel = mySphereRb != null ? mySphereRb.linearVelocity : Vector3.zero;
        Vector3 otherVel = otherCar.mySphereRb != null ? otherCar.mySphereRb.linearVelocity : Vector3.zero;

        float myMomentum = myVel.magnitude * (myDriftController != null && myDriftController.IsBoosting ? boostRamMultiplier : 1f);
        if (hasSpringBumperActive) myMomentum *= springBumperMultiplier;

        float otherMomentum = otherVel.magnitude * (otherCar.myDriftController != null && otherCar.myDriftController.IsBoosting ? boostRamMultiplier : 1f);

        if (myMomentum >= otherMomentum)
        {
            Vector3 pushDir = (otherCar.transform.position - transform.position).normalized;
            pushDir.y = 0f;
            float forceMag = baseKnockbackForce + Mathf.Max(0f, myMomentum - otherMomentum) * 0.75f;

            otherCar.ReceiveKnockbackImpulse(pushDir * forceMag + Vector3.up * upwardPopForce, myDriftController);
        }
    }

    public void ReceiveKnockbackImpulse(Vector3 impulse, ArcadeDriftController attacker)
    {
        if (mySphereRb == null) return;
        mySphereRb.AddForce(impulse, ForceMode.VelocityChange);
        LastAttacker = attacker;
        LastHitTimestamp = Time.time;
    }
}
