using UnityEngine;

public enum HazardEffectType
{
    Damage,
    InstantKill,
    ControlLoss,
    Slow
}

[CreateAssetMenu(fileName ="NewHazardData", menuName= "HazardLane/Hazard Data")]
public class HazardData : ScriptableObject
{
    [Header("Basic Info")]
    public string hazardName = "New Hazard";
    [Header("Prefabs")]
    public GameObject WarningVisualPrefab;
    public GameObject HazardPrefab;
    [Header("Timing")]
    public float WarningTime= 2f;
    [Header("Gameplay Effect")]
    public HazardEffectType EffectType = HazardEffectType.Damage;

    public float EffectValue = 10f;
    [Header("Special Rules")]
    public bool RequiresBraking = false;


}
