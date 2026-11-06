using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(ArcadeDriftController))]
public class WeaponInventoryManager : MonoBehaviour
{
    [Header("Mount Slots on Car Visual Root")]
    public Transform roofMountPoint;
    public Transform rearMountPoint;

    [Header("Current Inventory")]
    public GameObject currentWeaponPrefab;
    public UnityEvent<string> onWeaponEquipped;
    public UnityEvent onWeaponFired;

    private ArcadeDriftController ownerController;
    private GameObject activeWeaponInstance;
    private IWeaponPickup activeWeaponInterface;

    private void Awake()
    {
        ownerController = GetComponent<ArcadeDriftController>();
    }

    public bool HasWeapon => activeWeaponInterface != null;

    public void EquipWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab == null) return;

        // Clean up previously equipped visual item
        if (activeWeaponInstance != null)
        {
            Destroy(activeWeaponInstance);
        }

        currentWeaponPrefab = weaponPrefab;
        IWeaponPickup weaponComp = weaponPrefab.GetComponent<IWeaponPickup>();
        if (weaponComp == null)
        {
            Debug.LogWarning("[WeaponInventory] Prefab does not implement IWeaponPickup.");
            return;
        }

        Transform targetMount = (weaponComp.Slot == WeaponSlot.Roof) ? roofMountPoint : rearMountPoint;
        if (targetMount == null) targetMount = transform;

        activeWeaponInstance = Instantiate(weaponPrefab, targetMount);
        activeWeaponInstance.transform.localPosition = Vector3.zero;
        activeWeaponInstance.transform.localRotation = Quaternion.identity;

        activeWeaponInterface = activeWeaponInstance.GetComponent<IWeaponPickup>();
        activeWeaponInterface.Initialize(ownerController);

        onWeaponEquipped?.Invoke(activeWeaponInterface.WeaponName);
    }

    public void FireCurrentWeapon()
    {
        if (activeWeaponInterface == null) return;

        activeWeaponInterface.ActivateWeapon();
        onWeaponFired?.Invoke();

        // Single-use weapon expended
        if (activeWeaponInstance != null)
        {
            Destroy(activeWeaponInstance);
        }
        activeWeaponInterface = null;
        currentWeaponPrefab = null;
    }
}
