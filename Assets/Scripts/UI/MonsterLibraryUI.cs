using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MonsterLibraryUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject libraryPanel;

    [Header("List")]
    [SerializeField] private Transform listContent;
    [SerializeField] private RecordRowView rowPrefab;

    [Header("Detail")]
    [SerializeField] private InputField nameInput;
    [SerializeField] private Text summaryText;
    [SerializeField] private Text skillsText;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button useAsMyButton;
    [SerializeField] private Button useAsEnemyButton;
    [SerializeField] private Button deleteButton;

    private readonly List<RecordRowView> rows = new();
    private MonsterRecord current;

    public MonsterRecord SelectedMy { get; private set; }
    public MonsterRecord SelectedEnemy { get; private set; }

    void Start()
    {
        backButton.onClick.AddListener(Hide);

        useAsMyButton.onClick.AddListener(() =>
        {
            if (current == null) return;
            SelectedMy = current;
            RefreshDetail();
            RefreshList();
        });

        useAsEnemyButton.onClick.AddListener(() =>
        {
            if (current == null) return;
            SelectedEnemy = current;
            RefreshDetail();
            RefreshList();
        });

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(() =>
            {
                if (current == null) return;
                MonsterRecordRepository.I.Remove(current);
                current = null;
                RefreshList();
                ClearDetail();
            });
        }

        nameInput.onEndEdit.AddListener((txt) =>
        {
            if (current == null) return;
            current.displayName = txt;
            RefreshList();
            RefreshDetail();
        });
    }

    public void Show()
    {
        libraryPanel.SetActive(true);
        RefreshList();
        ClearDetail();
    }

    public void Hide()
    {
        libraryPanel.SetActive(false);
    }

    private void RefreshList()
    {
        ClearRows();

        foreach (var r in MonsterRecordRepository.I.Records)
        {
            var row = Instantiate(rowPrefab, listContent);
            rows.Add(row);
            row.Bind(r, OnSelectRecord);
        }
    }

    private void OnSelectRecord(MonsterRecord r)
    {
        current = r;
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (current == null)
        {
            ClearDetail();
            return;
        }

        nameInput.text = string.IsNullOrEmpty(current.displayName)
            ? AutoName(current)
            : current.displayName;

        string myMark = SelectedMy == current ? " [MY]" : "";
        string enemyMark = SelectedEnemy == current ? " [ENEMY]" : "";

        summaryText.text =
            $"ID: {current.recordId.Substring(0, 8)}{myMark}{enemyMark}\n" +
            $"HP: {current.maxHp}\n" +
            $"ShopGrade: {current.finalShopGrade}\n" +
            $"Battles: {current.totalBattles}  Turns: {current.totalTurns}\n" +
            $"Stable: {current.stableCount}  Cooldown: {current.cooldownCount}\n" +
            $"PvP W/L: {current.pvpWin}/{current.pvpLose}\n" +
            $"Types: {FormatTypeCount(current)}";

        skillsText.text =
            "SKILLS:\n" +
            string.Join("\n", current.skillIds.Select((id, i) => $"{i + 1}. {id}"));
    }

    private void ClearDetail()
    {
        nameInput.text = "";
        summaryText.text = "Select a record...";
        skillsText.text = "";
    }

    private void ClearRows()
    {
        foreach (var r in rows)
        {
            if (r != null) Destroy(r.gameObject);
        }
        rows.Clear();
    }

    private static string AutoName(MonsterRecord r)
        => $"Monster-{r.recordId.Substring(0, 4)}";

    private static string FormatTypeCount(MonsterRecord r)
    {
        if (r.typeCount == null || r.typeCount.Count == 0)
            return "(none)";

        return string.Join(", ",
            r.typeCount.Select(kv => $"{kv.Key}:{kv.Value}"));
    }
}
