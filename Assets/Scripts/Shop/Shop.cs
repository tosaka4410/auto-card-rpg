using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Shop
{
    public List<SkillData> Offer(List<SkillData> pool, int count)
    {
        var temp = new List<SkillData>(pool);
        var result = new List<SkillData>();

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, temp.Count);
            result.Add(temp[idx]);
            temp.RemoveAt(idx);
        }
        return result;
    }
}
