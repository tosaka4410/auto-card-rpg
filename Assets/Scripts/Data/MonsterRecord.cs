using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MonsterRecord
{
    // ===== Meta =====
    public string recordId;          // GUID
    public string displayName;       // 任意
    public DateTime createdAt;
    public int dataVersion = 1;

    // ===== Core Stats =====
    public int maxHp;

    // SkillData は直接持たない（参照切れ防止）
    public List<string> skillIds = new();   // SkillData.skillId

    // ===== Build Summary (Cache) =====
    public Dictionary<SkillType, int> typeCount = new();
    public int stableCount;
    public int cooldownCount;

    // ===== Training Info =====
    public int finalShopGrade;
    public int totalBattles;
    public int totalTurns;

    // ===== PvP Record (Phase1) =====
    public int pvpWin;
    public int pvpLose;

    public MonsterRecord()
    {
        recordId = Guid.NewGuid().ToString();
        createdAt = DateTime.Now;
    }
}
