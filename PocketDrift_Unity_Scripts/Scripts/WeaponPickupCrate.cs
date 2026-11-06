using UnityEngine;

public class WeaponPickupCrate : MonoBehaviour
{
    [Header("Weapon Arsenal Pool")]
    public GameObject[] availableWeaponPrefabs;

    [Header("Visual Juice")]
    public float rotationSpeed = 90f;
    public float bobFrequency = 2f;
    public float bobHeight = 0.25f;
    public float respawnTime = 4.5f;
    public ParticleSystem pickupVFX;

    private Vector3 basePosition;
    private Collider triggerCollider;
    private MeshRenderer[] renderers;
    private float respawnTimer;

    private void Start()
    {
        basePosition = transform.position;
        triggerCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<MeshRenderer>();
    }

    private void Update()
    {
        if (respawnTimer > 0f)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                SetPickupActive(true);
            }
            return;
        }

        // Rotate and bob
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
        float newY = basePosition.y + Mathf.Sin(Time.time * bobFrequency) * bobHeight;
        transform.position = new Vector3(basePosition.x, newY, basePosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (respawnTimer > 0f) return;

        WeaponInventoryManager inventory = other.GetComponentInParent<WeaponInventoryManager>();
        if (inventory != null && availableWeaponPrefabs != null && availableWeaponPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, availableWeaponPrefabs.Length);
            inventory.EquipWeapon(availableWeaponPrefabs[randomIndex]);

            if (pickupVFX != null)
            {
                pickupVFX.transform.position = transform.position;
                pickupVFX.Play();
            }

            SetPickupActive(false);
            respawnTimer = respawnTime;
        }
    }

    private void SetPickupActive(bool active)
    {
        if (triggerCollider != null) triggerCollider.enabled = active;
        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = active;
            }
        }
    }
}
