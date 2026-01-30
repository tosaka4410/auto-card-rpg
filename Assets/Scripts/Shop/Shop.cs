using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Shop
{
    private SkillData PickOne(List<SkillData> pool, int tier, HashSet<SkillData> avoidOrNull = null)
    {
        if (pool == null || pool.Count == 0) return null;

        var candidates = pool
            .Where(s => s != null
                && s.shopOnlyBase
                && (int)s.grade <= tier)
            .ToList();

        if (avoidOrNull != null)
            candidates = candidates.Where(s => !avoidOrNull.Contains(s)).ToList();

        if (candidates.Count == 0) return null;

        // tierに近いほどレア：diff=0 が最レア
        int total = 0;
        var weights = new int[candidates.Count];

        for (int i = 0; i < candidates.Count; i++)
        {
            int diff = tier - (int)candidates[i].grade; // 0..(tier-1)
            int w = 1 + diff * 2;                       // 0:1, 1:3, 2:5...
            weights[i] = w;
            total += w;
        }

        int r = Random.Range(0, total);
        for (int i = 0; i < candidates.Count; i++)
        {
            r -= weights[i];
            if (r < 0) return candidates[i];
        }

        return candidates[0];
    }

    /// <summary>
    /// 店の更新（＝補充はここでのみ行う）
    /// - isFrozen=true の場合は完全に何もしない（提示維持、空枠も埋めない）
    /// - isFrozen=false の場合、全スロットを引き直す（空枠も埋まる）
    /// </summary>
    public void RefreshAll(List<SkillData> pool, int tier, List<SkillData> slots, bool isFrozen)
    {
        if (slots == null || slots.Count == 0) return;
        if (isFrozen) return;

        // 同一提示を減らす（候補が少ない場合は重複する）
        var avoid = new HashSet<SkillData>();

        for (int i = 0; i < slots.Count; i++)
        {
            var s = PickOne(pool, tier, avoid);
            if (s == null) s = PickOne(pool, tier, null); // 詰み防止
            slots[i] = s;
            if (s != null) avoid.Add(s);
        }
    }
}
