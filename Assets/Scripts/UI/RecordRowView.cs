using System;
using UnityEngine;
using UnityEngine.UI;

public class RecordRowView : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private Button button;

    private MonsterRecord record;

    public void Bind(MonsterRecord r, Action<MonsterRecord> onClick)
    {
        record = r;

        string name = string.IsNullOrEmpty(r.displayName)
            ? AutoName(r)
            : r.displayName;

        label.text =
            $"{name} | HP:{r.maxHp} | " +
            $"Stable:{r.stableCount} CD:{r.cooldownCount} | " +
            $"W:{r.pvpWin} L:{r.pvpLose}";

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(record));
    }

    private string AutoName(MonsterRecord r)
        => $"Monster-{r.recordId.Substring(0, 4)}";
}
