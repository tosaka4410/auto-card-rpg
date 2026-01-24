// Assets/Scripts/GameController.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameUI ui;

    // B: Library UI（旧UI版）
    [SerializeField] private MonsterLibraryUI libraryUI;

    [Header("Pools")]
    public List<SkillData> skillPool;

    [Header("Enemy Presets (Training)")]
    public List<EnemyPreset> enemyPresets;

    [Header("Config")]
    [SerializeField] private int playerBaseHp = 40;

    private Monster player;
    private readonly Shop shop = new();

    // ショップグレード（BG準拠で提示数増）
    private int shopGrade = 1;

    private List<SkillData> currentOffers = new();

    // Skill辞書（Record -> Monster変換用）
    private Dictionary<string, SkillData> skillDict;

    // 疑似PvP用の選択（LibraryUI側が持っているが、参照しやすくする）
    private MonsterRecord selectedMy;
    private MonsterRecord selectedEnemy;

    // 戦闘ログ用カウンタ（Recordに入れる想定）
    private int totalBattles;
    private int totalTurns;

    // Cooldown管理：前ターンに抽選された cooldown 技ID（次ターン除外）
    private readonly HashSet<string> cooldownBlockedNextTurn = new();

    void Start()
    {
        player = new Monster(playerBaseHp);

        // Skill辞書作成
        skillDict = BuildSkillDict(skillPool);

        // 次へボタン：バトル終了→ショップへ
        ui.BindNextButton(() => OpenShop());

        // Library UIがある場合、最初は閉じておく（Panel側で非表示でもOK）
        if (libraryUI != null)
            libraryUI.Hide();

        OpenShop();
    }

    // ===== Shop =====

    void OpenShop()
    {
        int offerCount = 2 + shopGrade; // v1.3/v1.4: 2 + grade
        currentOffers = shop.Offer(skillPool, offerCount);

        ui.ShowShop(
            shopGrade,
            currentOffers,
            player.skills,
            onPickOffer: (idx) => TakeSkill(currentOffers[idx]),
            onReroll: () =>
            {
                currentOffers = shop.Offer(skillPool, offerCount);
                ui.ShowShop(
                    shopGrade,
                    currentOffers,
                    player.skills,
                    (i) => TakeSkill(currentOffers[i]),
                    () => Reroll(offerCount),
                    StartBattle
                );
            },
            onStartBattle: StartBattle
        );
    }

    void Reroll(int offerCount)
    {
        currentOffers = shop.Offer(skillPool, offerCount);
        ui.ShowShop(
            shopGrade,
            currentOffers,
            player.skills,
            (i) => TakeSkill(currentOffers[i]),
            () => Reroll(offerCount),
            StartBattle
        );
    }

    void TakeSkill(SkillData skill)
    {
        if (skill == null) return;

        if (player.skills.Count >= 7)
            player.skills.RemoveAt(0);

        player.skills.Add(skill);

        // 取得後はショップ継続（プロトタイプ仕様）
        OpenShop();
    }

    // ===== Library (B) =====

    // Shop画面のボタン等から呼べるように public
    public void OpenLibrary()
    {
        if (libraryUI == null)
        {
            Debug.LogWarning("libraryUI が未設定です。MonsterLibraryUI をアサインしてください。");
            return;
        }

        libraryUI.Show();
    }

    // Libraryから戻るなどでショップへ戻す用（必要ならUIボタンに紐づけ）
    public void CloseLibraryAndBackToShop()
    {
        if (libraryUI != null) libraryUI.Hide();
        OpenShop();
    }

    // ===== Battle Entry =====

    void StartBattle()
    {
        // Libraryで MY/ENEMY が両方選ばれていれば、それで疑似PvP
        PullSelectedRecordsFromLibrary();

        if (selectedMy != null && selectedEnemy != null)
        {
            StartPseudoPvp(selectedMy, selectedEnemy);
            return;
        }

        // そうでなければ Training 戦（EnemyPreset）
        StartTrainingBattle();
    }

    void PullSelectedRecordsFromLibrary()
    {
        if (libraryUI == null) return;

        selectedMy = libraryUI.SelectedMy;
        selectedEnemy = libraryUI.SelectedEnemy;
    }

    void StartPseudoPvp(MonsterRecord my, MonsterRecord enemyRec)
    {
        // MY側は「今の育成中プレイヤー」ではなく、Recordから生成（PvP検証用）
        var myMonster = Monster.CreateMonsterFromRecord(my, skillDict);
        var enemyMonster = Monster.CreateMonsterFromRecord(enemyRec, skillDict);

        // UI表示
        ui.ShowBattleStart($"PVP: {GetDisplayName(enemyRec)}", myMonster.hp, enemyMonster.hp);

        // 以降のループを Record戦向けに
        StartCoroutine(BattleLoop(myMonster, enemyMonster, isPvp: true, myRecord: my, enemyRecord: enemyRec));
    }

    void StartTrainingBattle()
    {
        if (enemyPresets == null || enemyPresets.Count == 0)
        {
            Debug.LogError("enemyPresets が空です。EnemyPreset を1つ以上アサインしてください。");
            return;
        }

        var preset = enemyPresets[Random.Range(0, enemyPresets.Count)];
        var enemy = Monster.FromPreset(preset);

        // プレイヤーHPリセット（毎戦）
        player.hp = player.maxHp;

        ui.ShowBattleStart(preset.enemyName, player.hp, enemy.hp);

        StartCoroutine(BattleLoop(player, enemy, isPvp: false, myRecord: null, enemyRecord: null));
    }

    // ===== Battle Loop =====

    IEnumerator BattleLoop(Monster myMonster, Monster enemy, bool isPvp, MonsterRecord myRecord, MonsterRecord enemyRecord)
    {
        totalBattles++;
        int turn = 0;

        // Cooldown除外の管理は「各陣営ごと」に持つのが理想だが、
        // プロトタイプでは 2つ持つ（プレイヤー側/敵側）
        var myCooldownBlocked = new HashSet<string>();
        var enemyCooldownBlocked = new HashSet<string>();

        while (myMonster.hp > 0 && enemy.hp > 0)
        {
            turn++;
            totalTurns++;

            var p = ResolveTurn(myMonster, enemy, turn, myCooldownBlocked);
            var e = ResolveTurn(enemy, myMonster, turn, enemyCooldownBlocked);

            ui.UpdateBattleTurn(
                turn,
                myMonster.hp,
                enemy.hp,
                p.pickedNames,
                p.atk,
                p.def,
                e.pickedNames,
                e.atk,
                e.def,
                p.fatigue,     // 同ターン同値なので片側でOK
                p.damage,      // MY→ENEMY
                e.damage       // ENEMY→MY
            );

            yield return new WaitForSeconds(0.35f);
        }

        bool win = myMonster.hp > 0;
        ui.ShowBattleEnd(win);

        if (!isPvp)
        {
            // Training: 勝ったらgrade上げる（上限6）
            if (win) shopGrade = Mathf.Min(6, shopGrade + 1);

            // Training勝利時：Record保存（まずは勝利時のみでOK）
            if (win)
            {
                var record = MonsterRecordFactory.CreateRecordFromMonster(
                    player,
                    finalShopGrade: shopGrade,
                    totalBattles: totalBattles,
                    totalTurns: totalTurns
                );

                // Repositoryがある前提（Bで作成）
                if (MonsterRecordRepository.I != null)
                {
                    MonsterRecordRepository.I.Add(record);
                }
                else
                {
                    Debug.LogWarning("MonsterRecordRepository が見つかりません。Sceneに追加してください。");
                }
            }
        }
        else
        {
            // 疑似PvP戦績更新（MY/ENEMYどちらを増やすかは好み）
            if (myRecord != null)
            {
                if (win) myRecord.pvpWin++;
                else myRecord.pvpLose++;
            }
            if (enemyRecord != null)
            {
                if (win) enemyRecord.pvpLose++;
                else enemyRecord.pvpWin++;
            }
        }
    }

    // ===== Turn Resolution =====

    // cooldownBlockedNextTurn：前ターンで抽選されたCooldown技（次ターン抽選対象外）
    private (string pickedNames, int atk, int def, int fatigue, int damage) ResolveTurn(
        Monster atkM,
        Monster defM,
        int turn,
        HashSet<string> cooldownBlockedNextTurn
    )
    {
        int n = atkM.skills.Count;
        int k = Mathf.CeilToInt(n / 2f);

        // 1) 候補プール：所持技から Cooldown除外を取り除く
        var pool = new List<SkillData>();
        foreach (var s in atkM.skills)
        {
            if (s == null) continue;

            // Cooldown(1)：前ターンで抽選されたら、次ターンは候補外
            if (s.tag == SkillTag.Cooldown && !string.IsNullOrEmpty(s.skillId) && cooldownBlockedNextTurn.Contains(s.skillId))
                continue;

            pool.Add(s);
        }

        // 2) Stable優先
        var picked = new List<SkillData>();
        foreach (var s in pool)
        {
            if (s.tag == SkillTag.Stable && picked.Count < k)
                picked.Add(s);
        }

        // 3) 残りをランダム
        var rest = pool.Except(picked).ToList();
        while (picked.Count < k && rest.Count > 0)
        {
            int idx = Random.Range(0, rest.Count);
            picked.Add(rest[idx]);
            rest.RemoveAt(idx);
        }

        // 4) 集計
        int atk = 0;
        int def = 0;

        foreach (var s in picked)
        {
            atk += s.attack;
            def += s.block;
        }

        int fatigue = Mathf.Max(0, turn - 4);
        int damage = Mathf.Max(0, atk - def) + fatigue;

        defM.hp -= damage;

        // 5) 次ターン除外を更新：このターン引いたCooldown技を「次ターンブロック」にする
        cooldownBlockedNextTurn.Clear();
        foreach (var s in picked)
        {
            if (s.tag == SkillTag.Cooldown && !string.IsNullOrEmpty(s.skillId))
                cooldownBlockedNextTurn.Add(s.skillId);
        }

        string pickedNames = string.Join(", ", picked.Select(x => x.skillName));
        return (pickedNames, atk, def, fatigue, damage);
    }

    // ===== Helpers =====

    private Dictionary<string, SkillData> BuildSkillDict(List<SkillData> allSkills)
    {
        var dict = new Dictionary<string, SkillData>();

        foreach (var s in allSkills)
        {
            if (s == null) continue;

            if (string.IsNullOrEmpty(s.skillId))
            {
                // プロトタイプでは警告だけ出す（後で必須化）
                Debug.LogWarning($"SkillData '{s.name}' の skillId が空です。Record保存/PvPで困ります。");
                continue;
            }

            dict[s.skillId] = s;
        }

        return dict;
    }

    private string GetDisplayName(MonsterRecord r)
    {
        if (r == null) return "(null)";
        return string.IsNullOrEmpty(r.displayName) ? $"Monster-{r.recordId.Substring(0, 4)}" : r.displayName;
    }
}
