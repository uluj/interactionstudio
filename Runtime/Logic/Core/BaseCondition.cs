using UnityEngine;

/// <summary>
/// Tüm "Koşul" (IF) ScriptableObject'lerinin miras alacağı
/// temel soyut sınıftır.
/// </summary>
public abstract class BaseCondition : ScriptableObject
{
    [Header("Condition Logic")]
    [Tooltip("If true, the result of this condition will be inverted (NOT).")]
    [SerializeField]
    private bool InvertCondition = false; // "NOT" (Değil) mantığı

    /// <summary>
    /// BaseTrigger bu metodu çağırır. 'Invert' mantığını otomatik yönetir.
    /// </summary>
    public bool CheckCondition(GameObject contextOwner, GameObject otherObject)
    {
        bool result = OnCheck(contextOwner, otherObject);
        return InvertCondition ? !result : result;
    }

    /// <summary>
    /// Her yeni Koşul (Condition) script'inin yazmak zorunda olduğu ana mantık.
    /// </summary>
    /// <returns>Koşul geçerse True, geçmezse False.</returns>
    protected abstract bool OnCheck(GameObject contextOwner, GameObject otherObject);
}