using UnityEngine;

public enum WeaponSlot
{
    Roof,
    Rear
}

public interface IWeaponPickup
{
    string WeaponName { get; }
    WeaponSlot Slot { get; }
    void Initialize(ArcadeDriftController owner);
    void ActivateWeapon();
}
