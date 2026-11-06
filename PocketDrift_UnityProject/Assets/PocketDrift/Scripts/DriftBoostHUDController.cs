using UnityEngine;
using UnityEngine.UI;

public class DriftBoostHUDController : MonoBehaviour
{
    [Header("Target Player Car")]
    public ArcadeDriftController playerCar;

    [Header("Optional UI References")]
    public Image boostFillBar;
    public Text speedText;
    public Text boostTierText;

    [Header("Boost Tier Colors")]
    public Color baseColor = new Color(0.2f, 0.8f, 1f);
    public Color tier1Yellow = new Color(1f, 0.85f, 0.1f);
    public Color tier2Orange = new Color(1f, 0.45f, 0.05f);
    public Color tier3Purple = new Color(0.85f, 0.15f, 1f);

    private void Start()
    {
        if (playerCar == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerCar = playerObj.GetComponent<ArcadeDriftController>();
        }
    }

    private void Update()
    {
        if (playerCar == null) return;

        if (boostFillBar != null)
        {
            boostFillBar.fillAmount = playerCar.NormalizedBoostCharge;
            switch (playerCar.CurrentBoostTier)
            {
                case 1: boostFillBar.color = tier1Yellow; break;
                case 2: boostFillBar.color = tier2Orange; break;
                case 3: boostFillBar.color = tier3Purple; break;
                default: boostFillBar.color = baseColor; break;
            }
        }

        if (speedText != null)
        {
            int scaleMph = Mathf.RoundToInt(playerCar.CurrentSpeed * 4.5f);
            speedText.text = $"{scaleMph} MPH";
        }

        if (boostTierText != null)
        {
            boostTierText.text = playerCar.IsDrifting && playerCar.CurrentBoostTier > 0
                ? $"OVERDRIVE TIER {playerCar.CurrentBoostTier}!"
                : (playerCar.IsBoosting ? "BOOSTING!" : "");
        }
    }
}
