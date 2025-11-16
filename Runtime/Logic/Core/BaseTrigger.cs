using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Bu, Runtime/Logic/Core/ altına ekleyeceğin YENİ script'tir
public abstract class BaseTrigger : MonoBehaviour
{
    [Header("Trigger Logic")]
    [SerializeField]
    private List<BaseCondition> Conditions; // Koşul (IF) listesi

    [SerializeField]
    private List<GameAction> Actions; // Eylem (THEN) listesi
    
    /// <summary>
    /// Miras alan sınıflar (örn: OnTriggerEnter)
    /// koşulların kontrol edilmesini istediklerinde bu metodu çağırır.
    /// </summary>
    protected void TryExecuteActions(GameObject otherObject)
    {
        // 1. Tüm Koşulları (IF) Kontrol Et
        foreach (var condition in Conditions)
        {
            if (condition.CheckCondition(this.gameObject, otherObject) == false)
            {
                return; // Bir koşul bile uymazsa durdur
            }
        }

        // 2. Tüm Eylemleri (THEN) Çalıştır
        // BU, YENİ MİMARİNİN KİLİT NOKTASIDIR:
        foreach (var action in Actions)
        {
            if (action.GetActionType() == GameAction.ActionType.Instant)
            {
                // Eylem anlıksa, doğrudan çalıştır
                action.Execute(this);
            }
            else
            {
                // Eylem süreliyse, bu MonoBehaviour (BaseTrigger) üzerinde bir Coroutine başlat
                StartCoroutine(action.ExecuteCoroutine(this));
            }
        }
    }
}