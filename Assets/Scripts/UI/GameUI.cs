using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject battlePanel;

    [Header("Shop")]
    [SerializeField] private Transform offersRoot;
    [SerializeField] private Button offerButtonPrefab;
    [SerializeField] private Text ownedSkillsText;
    [SerializeField] private Text shopInfoText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button startBattleButton;

    [Header("Battle")]
    [SerializeField] private Text statusText;
    [SerializeField] private Text turnDetailText;
    [SerializeField] private Text logText;
    [SerializeField] private ScrollRect logScroll;
    [SerializeField] private Button nextButton;

    private readonly List<Button> spawnedOfferButtons = new();

    public void ShowShop(
        int shopGrade,
        IReadOnlyList<SkillData> offers,
        IReadOnlyList<SkillData> ownedSkills,
        Action<int> onPickOffer,
        Action onReroll,
        Action onStartBattle
    )
    {
        shopPanel.SetActive(true);
        battlePanel.SetActive(false);

        if (shopInfoText != null)
            shopInfoText.text = $"SHOP GRADE: {shopGrade} / OFFERS: {offers.Count}";

        if (ownedSkillsText != null)
            ownedSkillsText.text = "OWNED (max 7):\n- " + string.Join("\n- ", SkillsToNames(ownedSkills));

        ClearOfferButtons();
        for (int i = 0; i < offers.Count; i++)
        {
            int index = i;

            var btn = Instantiate(offerButtonPrefab, offersRoot);
            spawnedOfferButtons.Add(btn);

            var label = btn.GetComponentInChildren<Text>();
            if (label != null) label.text = FormatSkillLine(offers[i]);

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onPickOffer?.Invoke(index));
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(() => onReroll?.Invoke());
        }

        if (startBattleButton != null)
        {
            startBattleButton.onClick.RemoveAllListeners();
            startBattleButton.onClick.AddListener(() => onStartBattle?.Invoke());
        }

        if (nextButton != null) nextButton.interactable = false;
    }

    public void ShowBattleStart(string enemyName, int playerHp, int enemyHp)
    {
        shopPanel.SetActive(false);
        battlePanel.SetActive(true);

        if (statusText != null)
            statusText.text = $"ENEMY: {enemyName}\nP HP: {playerHp} / E HP: {enemyHp}";

        if (turnDetailText != null) turnDetailText.text = "";
        if (logText != null) logText.text = "=== BATTLE START ===\n";

        ScrollToBottom();
        if (nextButton != null) nextButton.interactable = false;
    }

    public void UpdateBattleTurn(
        int turn,
        int playerHp,
        int enemyHp,
        string playerPicked,
        int playerAtk,
        int playerDef,
        string enemyPicked,
        int enemyAtk,
        int enemyDef,
        int fatigue,
        int damageToEnemy,
        int damageToPlayer
    )
    {
        if (statusText != null)
            statusText.text = $"Turn: {turn}\nP HP: {playerHp} / E HP: {enemyHp}";

        if (turnDetailText != null)
        {
            turnDetailText.text =
                $"[PLAYER] {playerPicked}\nATK:{playerAtk} DEF:{playerDef}  dmg->E:{damageToEnemy}\n" +
                $"[ENEMY ] {enemyPicked}\nATK:{enemyAtk} DEF:{enemyDef}  dmg->P:{damageToPlayer}\n" +
                $"FATIGUE:{fatigue}";
        }

        AppendLog(
            $"T{turn} | P({playerAtk}-{playerDef}) -> E:{damageToEnemy} | " +
            $"E({enemyAtk}-{enemyDef}) -> P:{damageToPlayer} | FAT:{fatigue}\n"
        );
    }

    public void ShowBattleEnd(bool playerWin)
    {
        AppendLog(playerWin ? "\n=== WIN ===\n" : "\n=== LOSE ===\n");
        if (nextButton != null) nextButton.interactable = true;
    }

    public void BindNextButton(Action onNext)
    {
        if (nextButton == null) return;
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(() => onNext?.Invoke());
    }

    private void AppendLog(string s)
    {
        if (logText != null) logText.text += s;
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (logScroll == null) return;
        Canvas.ForceUpdateCanvases();
        logScroll.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }

    private void ClearOfferButtons()
    {
        foreach (var b in spawnedOfferButtons)
        {
            if (b != null) Destroy(b.gameObject);
        }
        spawnedOfferButtons.Clear();
    }

    private static List<string> SkillsToNames(IReadOnlyList<SkillData> skills)
    {
        var list = new List<string>();
        foreach (var s in skills) list.Add(s != null ? s.skillName : "(null)");
        return list.Count == 0 ? new List<string> { "(none)" } : list;
    }

    private static string FormatSkillLine(SkillData s)
    {
        if (s == null) return "(null)";
        string tag = s.tag == SkillTag.None ? "" : $" [{s.tag}]";
        return $"{s.skillName}  A:{s.attack} B:{s.block}{tag}";
    }
}
