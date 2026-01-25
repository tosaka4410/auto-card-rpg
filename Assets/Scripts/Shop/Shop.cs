using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Shop
{
    /// <summary>
    /// ショップグレードに応じて、入手可能スキル（grade<=shopGrade & shopOnlyBase）から提示する
    /// </summary>
    public List<SkillData> Offer(List<SkillData> pool, int count, int shopGrade)
    {
        if (pool == null || pool.Count == 0) return new List<SkillData>();

        // 1) 候補：ショップに出るベーススキルのみ + grade制限
        var candidates = pool
            .Where(s => s != null
                && s.shopOnlyBase
                && (int)s.grade <= shopGrade)
            .ToList();

        if (candidates.Count == 0) return new List<SkillData>();

        // 2) 重複を避けたいので picked で管理（候補が少ない場合は途中で止まる）
        var result = new List<SkillData>();
        var picked = new HashSet<SkillData>();

        for (int i = 0; i < count; i++)
        {
            var s = WeightedPick(candidates, shopGrade, picked);
            if (s == null)
            {
                // 候補が尽きたら、残りは重複許可で埋める（詰み防止）
                s = WeightedPick(candidates, shopGrade, null);
                if (s == null) break;
            }

            result.Add(s);
            picked.Add(s);
        }

        return result;
    }

    /// <summary>
    /// shopGradeに近いほどレアにする重み付き抽選
    /// diff=shopGrade-skillGrade: 0(同グレード)が一番レア
    /// weight = 1 + diff*2 （例：diff0=1, diff1=3, diff2=5…）
    /// </summary>
    private SkillData WeightedPick(List<SkillData> candidates, int shopGrade, HashSet<SkillData> pickedOrNull)
    {
        var list = (pickedOrNull == null)
            ? candidates
            : candidates.Where(c => !pickedOrNull.Contains(c)).ToList();

        if (list.Count == 0) return null;

        int total = 0;
        var weights = new int[list.Count];

        for (int i = 0; i < list.Count; i++)
        {
            int diff = shopGrade - (int)list[i].grade; // 0..(shopGrade-1)
            int w = 1 + diff * 2;
            weights[i] = w;
            total += w;
        }

        int r = Random.Range(0, total);
        for (int i = 0; i < list.Count; i++)
        {
            r -= weights[i];
            if (r < 0) return list[i];
        }

        return list[0];
    }
}
