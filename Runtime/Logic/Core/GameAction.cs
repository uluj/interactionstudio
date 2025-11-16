using UnityEngine;
using System.Collections; // Coroutine için bu GEREKLİ

/// <summary>
/// Tüm "Eylem" (THEN) ScriptableObject'lerinin miras alacağı
/// temel soyut sınıftır.
/// </summary>
public abstract class GameAction : ScriptableObject
{
    public enum ActionType { Instant, Coroutine }

    /// <summary>
    /// Eylemin "Anlık" mı (Instant) yoksa "Süreli" mi (Coroutine) olduğunu bildirir.
    /// </summary>
    public abstract ActionType GetActionType();

    /// <summary>
    /// ANLIK eylemler (ActionType.Instant) için bu metot çağrılır.
    /// </summary>
    public abstract void Execute(MonoBehaviour owner);

    /// <summary>
    /// SÜRELİ eylemler (ActionType.Coroutine) için bu metot çağrılır.
    /// </summary>
    public abstract IEnumerator ExecuteCoroutine(MonoBehaviour owner);
}