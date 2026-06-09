using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    private static LeaderboardManager instance;

    [Header("Main Panel")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Button levelLeaderboardButton;
    [SerializeField] private Button questLeaderboardButton;
    [SerializeField] private Button closeButton1;
    [SerializeField] private Button closeButton2;

    [Header("Level Leaderboard")]
    [SerializeField] private GameObject levelLeaderboardContent;
    [SerializeField] private ScrollRect levelScrollRect;
    [SerializeField] private Transform levelContentTransform;

    [Header("Quest Leaderboard")]
    [SerializeField] private GameObject questLeaderboardContent;
    [SerializeField] private ScrollRect questScrollRect;
    [SerializeField] private Transform questContentTransform;

    [Header("Prefab")]
    [SerializeField] private Transform leaderboardEntryPrefab;

    [Header("Settings")]
    [SerializeField] private int maxLeaderboardEntries = 10;

    private List<Transform> levelDisplayedEntries = new List<Transform>();
    private List<Transform> questDisplayedEntries = new List<Transform>();
    private bool isLevelLeaderboardActive = true;
    private bool isRefreshingLeaderboard = false;

    //  Cache leaderboard data
    private List<LeaderboardEntry> cachedLevelLeaderboard = new List<LeaderboardEntry>();
    private List<LeaderboardEntry> cachedQuestLeaderboard = new List<LeaderboardEntry>();

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string playerId;
        public string playerName;
        public int value;
        public int rank;
        public LeaderboardEntry(string playerId, string playerName, int value, int rank)
        {
            this.playerId = playerId;
            this.playerName = playerName;
            this.value = value;
            this.rank = rank;
        }
    }

    [System.Serializable]
    public class LeaderboardSnapshot
    {
        public string snapshotDate;
        public string timestamp;
        public Dictionary<string, LeaderboardRankEntry> levelRanking = new Dictionary<string, LeaderboardRankEntry>();
        public Dictionary<string, LeaderboardRankEntry> questRanking = new Dictionary<string, LeaderboardRankEntry>();
    }

    [System.Serializable]
    public class LeaderboardRankEntry
    {
        public int rank;
        public string playerName;
        public int value;
        public string timestamp;
    }

    public static LeaderboardManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<LeaderboardManager>();
            return instance;
        }
    }

    private void Start()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        //  Setup button listeners
        if (levelLeaderboardButton != null)
            levelLeaderboardButton.onClick.AddListener(ShowLevelLeaderboard);
        if (questLeaderboardButton != null)
            questLeaderboardButton.onClick.AddListener(ShowQuestLeaderboard);
        if (closeButton1 != null)
            closeButton1.onClick.AddListener(CloseLeaderboard);
        if (closeButton2 != null)
            closeButton2.onClick.AddListener(CloseLeaderboard);

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        //  Tạo sẵn entry UI
        for (int i = 0; i < maxLeaderboardEntries; i++)
        {
            var entry = Instantiate(leaderboardEntryPrefab, levelContentTransform);
            levelDisplayedEntries.Add(entry);
        }

        for (int i = 0; i < maxLeaderboardEntries; i++)
        {
            var entry = Instantiate(leaderboardEntryPrefab, questContentTransform);
            questDisplayedEntries.Add(entry);
        }

        StartCoroutine(LoadAndCacheLeaderboardFromFirebase());
    }

    public void OpenLeaderboard()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);

        ShowLevelLeaderboard();
        StartCoroutine(RefreshLeaderboardFromUsers());
    }

    public void CloseLeaderboard()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }

    private void ShowLevelLeaderboard()
    {
        isLevelLeaderboardActive = true;

        if (levelLeaderboardButton != null)
            levelLeaderboardButton.interactable = false;
        if (questLeaderboardButton != null)
            questLeaderboardButton.interactable = true;

        if (levelLeaderboardContent != null)
            levelLeaderboardContent.SetActive(true);
        if (questLeaderboardContent != null)
            questLeaderboardContent.SetActive(false);

        if (levelScrollRect != null)
            levelScrollRect.verticalNormalizedPosition = 1f;

        DisplayLevelLeaderboard(cachedLevelLeaderboard);
    }

    private void ShowQuestLeaderboard()
    {
        if (isLevelLeaderboardActive == false) return;

        isLevelLeaderboardActive = false;

        if (levelLeaderboardButton != null)
            levelLeaderboardButton.interactable = true;
        if (questLeaderboardButton != null)
            questLeaderboardButton.interactable = false;

        if (levelLeaderboardContent != null)
            levelLeaderboardContent.SetActive(false);
        if (questLeaderboardContent != null)
            questLeaderboardContent.SetActive(true);

        if (questScrollRect != null)
            questScrollRect.verticalNormalizedPosition = 1f;

        DisplayQuestLeaderboard(cachedQuestLeaderboard);
    }

    private IEnumerator LoadAndCacheLeaderboardFromFirebase()
    {
        string today = System.DateTime.UtcNow.ToString("yyyy-MM-dd");

        Debug.Log($" Loading leaderboard from Firebase for {today}...");

        yield return StartCoroutine(LoadLeaderboardFromFirebase(today, "levelRanking",
            (entries) => cachedLevelLeaderboard = entries));

        yield return StartCoroutine(LoadLeaderboardFromFirebase(today, "questRanking",
            (entries) => cachedQuestLeaderboard = entries));

        Debug.Log($" Leaderboard cached successfully!");
    }


    private IEnumerator LoadLeaderboardFromFirebase(string date, string rankType, System.Action<List<LeaderboardEntry>> callback)
    {
        bool isLoaded = false;
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        FirebaseDatabase.DefaultInstance
            .GetReference("Leaderboard/CurrentDaily")
            .Child(date)
            .Child(rankType)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Value != null)
                {
                    try
                    {
                        var snapshotData = JsonConvert.DeserializeObject<Dictionary<string, LeaderboardRankEntry>>(
                            task.Result.GetRawJsonValue()
                        );

                        foreach (var kvp in snapshotData)
                        {
                            string playerId = kvp.Key;
                            var rankEntry = kvp.Value;

                            var entry = new LeaderboardEntry(
                                playerId,
                                rankEntry.playerName,
                                rankEntry.value,
                                rankEntry.rank > 0 ? rankEntry.rank : 0
                            );
                            entries.Add(entry);
                        }
                        // sắp xếp lại
                        entries = entries
                            .OrderByDescending(e => e.value)
                            .ThenBy(e => e.playerName)
                            .Take(maxLeaderboardEntries)
                            .ToList();

                        for (int i = 0; i < entries.Count; i++)
                        {
                            entries[i].rank = i + 1;
                        }

                        Debug.Log($" {rankType} loaded from Firebase: {entries.Count} entries");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($" Error parsing {rankType}: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($" No {rankType} found in Firebase for {date}, will load from users...");
                }

                isLoaded = true;
            });

        yield return new WaitUntil(() => isLoaded);
        callback?.Invoke(entries);
    }


    public IEnumerator SaveLeaderboardToFirebase()
    {
        string today = System.DateTime.UtcNow.ToString("yyyy-MM-dd");

        Debug.Log($" Saving current leaderboard to Firebase for {today}...");

        var leaderboardSnapshot = new LeaderboardSnapshot
        {
            snapshotDate = today,
            timestamp = System.DateTime.UtcNow.ToString("O"),
            levelRanking = new Dictionary<string, LeaderboardRankEntry>(),
            questRanking = new Dictionary<string, LeaderboardRankEntry>()
        };

        for (int i = 0; i < cachedLevelLeaderboard.Count; i++)
        {
            var entry = cachedLevelLeaderboard[i];
            leaderboardSnapshot.levelRanking[entry.playerId] = new LeaderboardRankEntry
            {
                rank = i + 1,
                playerName = entry.playerName,
                value = entry.value,
                timestamp = System.DateTime.UtcNow.ToString("O")
            };
        }

        for (int i = 0; i < cachedQuestLeaderboard.Count; i++)
        {
            var entry = cachedQuestLeaderboard[i];
            leaderboardSnapshot.questRanking[entry.playerId] = new LeaderboardRankEntry
            {
                rank = i + 1,
                playerName = entry.playerName,
                value = entry.value,
                timestamp = System.DateTime.UtcNow.ToString("O")
            };
        }

        string json = JsonConvert.SerializeObject(leaderboardSnapshot);
        bool isSaved = false;

        FirebaseDatabase.DefaultInstance
            .GetReference("Leaderboard/CurrentDaily")
            .Child(today)
            .SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log($" Leaderboard saved to Firebase for {today}");
                    isSaved = true;
                }
                else
                {
                    Debug.LogError($" Failed to save leaderboard: {task.Exception}");
                    isSaved = true;
                }
            });

        yield return new WaitUntil(() => isSaved);
    }


    public IEnumerator RefreshLeaderboardFromFirebase()
    {
        Debug.Log($" Refreshing leaderboard from Firebase...");
        yield return StartCoroutine(LoadAndCacheLeaderboardFromFirebase());

        // Update UI display
        if (isLevelLeaderboardActive)
            DisplayLevelLeaderboard(cachedLevelLeaderboard);
        else
            DisplayQuestLeaderboard(cachedQuestLeaderboard);
    }


    public IEnumerator RefreshLeaderboardFromUsers()
    {
        if (isRefreshingLeaderboard)
            yield break;

        isRefreshingLeaderboard = true;
        bool isLoaded = false;

        List<LeaderboardEntry> levelEntries = new List<LeaderboardEntry>();
        List<LeaderboardEntry> questEntries = new List<LeaderboardEntry>();

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && task.Result.Value != null)
                {
                    foreach (var userSnapshot in task.Result.Children)
                    {
                        string userId = userSnapshot.Key;
                        string playerName = GetUserName(userSnapshot, userId);
                        int currentLevel = GetUserLevel(userSnapshot);
                        int completedQuestCount = GetCompletedQuestCount(userSnapshot);

                        levelEntries.Add(new LeaderboardEntry(userId, playerName, currentLevel, 0));
                        questEntries.Add(new LeaderboardEntry(userId, playerName, completedQuestCount, 0));
                    }

                    cachedLevelLeaderboard = BuildRankedEntries(levelEntries);
                    cachedQuestLeaderboard = BuildRankedEntries(questEntries);

                    Debug.Log($" Live leaderboard refreshed from Users: Level={cachedLevelLeaderboard.Count}, Quest={cachedQuestLeaderboard.Count}");
                }
                else
                {
                    Debug.LogError($" Failed to refresh leaderboard from Users: {task.Exception}");
                }

                isLoaded = true;
            });

        yield return new WaitUntil(() => isLoaded);

        if (isLevelLeaderboardActive)
            DisplayLevelLeaderboard(cachedLevelLeaderboard);
        else
            DisplayQuestLeaderboard(cachedQuestLeaderboard);

        yield return StartCoroutine(SaveLeaderboardToFirebase());
        isRefreshingLeaderboard = false;
    }

    private List<LeaderboardEntry> BuildRankedEntries(List<LeaderboardEntry> entries)
    {
        var rankedEntries = entries
            .OrderByDescending(e => e.value)
            .ThenBy(e => e.playerName)
            .Take(maxLeaderboardEntries)
            .ToList();

        for (int i = 0; i < rankedEntries.Count; i++)
        {
            rankedEntries[i].rank = i + 1;
        }

        return rankedEntries;
    }

    private string GetUserName(DataSnapshot userSnapshot, string userId)
    {
        string playerName = userSnapshot.Child("Name").Value?.ToString();

        if (string.IsNullOrWhiteSpace(playerName))
        {
            string shortId = userId.Length > 6 ? userId.Substring(0, 6) : userId;
            return $"Player_{shortId}";
        }

        return playerName.Trim();
    }

    private int GetUserLevel(DataSnapshot userSnapshot)
    {
        try
        {
            var levelSnapshot = userSnapshot.Child("Level");
            if (levelSnapshot.Value == null)
                return 1;

            var levelData = JsonConvert.DeserializeObject<LevelSystem.LevelData>(levelSnapshot.GetRawJsonValue());
            return Mathf.Max(1, levelData?.currentLevel ?? 1);
        }
        catch (Exception e)
        {
            Debug.LogWarning($" Error parsing user level: {e.Message}");
            return 1;
        }
    }

    private int GetCompletedQuestCount(DataSnapshot userSnapshot)
    {
        try
        {
            var questSnapshot = userSnapshot.Child("Quests");
            if (questSnapshot.Value == null)
                return 0;

            var questData = JsonConvert.DeserializeObject<QuestSystem.QuestData>(questSnapshot.GetRawJsonValue());
            return questData?.completedQuestIds?.Count ?? 0;
        }
        catch (Exception e)
        {
            Debug.LogWarning($" Error parsing user quests: {e.Message}");
            return 0;
        }
    }

    private void DisplayLevelLeaderboard(List<LeaderboardEntry> entries)
    {
        for (int i = 0; i < levelDisplayedEntries.Count; i++)
        {
            if (i < entries.Count)
            {
                var entry = entries[i];
                SetEntryUI(levelDisplayedEntries[i], entry);
            }
            else
            {
                levelDisplayedEntries[i].gameObject.SetActive(false);
            }
        }
    }

    private void DisplayQuestLeaderboard(List<LeaderboardEntry> entries)
    {
        for (int i = 0; i < questDisplayedEntries.Count; i++)
        {
            if (i < entries.Count)
            {
                var entry = entries[i];
                SetEntryUI(questDisplayedEntries[i], entry);
            }
            else
            {
                questDisplayedEntries[i].gameObject.SetActive(false);
            }
        }
    }

    private void SetEntryUI(Transform entryTransform, LeaderboardEntry entry)
    {
        entryTransform.gameObject.SetActive(true);

        var texts = entryTransform.GetComponentsInChildren<Text>();

        if (texts.Length >= 3)
        {
            texts[0].text = $"#{entry.rank}";
            texts[1].text = entry.playerName;
            texts[2].text = entry.value.ToString();
        }
    }

    public List<LeaderboardEntry> GetLevelLeaderboard()
    {
        return new List<LeaderboardEntry>(cachedLevelLeaderboard);
    }
    public List<LeaderboardEntry> GetQuestLeaderboard()
    {
        return new List<LeaderboardEntry>(cachedQuestLeaderboard);
    }
}
