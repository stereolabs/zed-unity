using UnityEngine;

public class SunMaterialManager : MonoBehaviour
{
    void Awake()
    {
        if (UpgradePluginToSRP.UpgradePlanetariumToSRP(gameObject))
        {
            Debug.Log("Upgraded sun to SRP.");
        }
    }
}