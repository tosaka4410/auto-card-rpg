using System.Collections.Generic;
using UnityEngine;

public class MonsterRecordRepository : MonoBehaviour
{
    public static MonsterRecordRepository I { get; private set; }

    private readonly List<MonsterRecord> records = new();
    public IReadOnlyList<MonsterRecord> Records => records;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Add(MonsterRecord r) => records.Add(r);

    public void Remove(MonsterRecord r) => records.Remove(r);
}
