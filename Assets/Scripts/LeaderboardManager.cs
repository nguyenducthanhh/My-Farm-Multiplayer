using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
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

        // ✅ Setup button listeners
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

        // ✅ Tạo sẵn 10 entry UI cho bảng cấp độ
        for (int i = 0; i < maxLeaderboardEntries; i++)
        {
            var entry = Instantiate(leaderboardEntryPrefab, levelContentTransform);
            levelDisplayedEntries.Add(entry);
        }

        // ✅ Tạo sẵn 10 entry UI cho bảng nhiệm vụ
        for (int i = 0; i < maxLeaderboardEntries; i++)
        {
            var entry = Instantiate(leaderboardEntryPrefab, questContentTransform);
            questDisplayedEntries.Add(entry);
        }
    }

    public void OpenLeaderboard()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);

        ShowLevelLeaderboard();
    }
    private bool hasLoadedLeaderboard = false;

    public void CloseLeaderboard()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        // ✅ THÊM: Reset flag khi đóng bảng
        hasLoadedLeaderboard = false;
        Debug.Log("🔄 Leaderboard closed - flag reset");
    }

    // ✅ THÊM: Flag để track lần đầu mở bảng

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

        // ✅ SỬA: Reset scroll khi chuyển sang tab Level
        if (levelScrollRect != null)
        {
            levelScrollRect.verticalNormalizedPosition = 1f;
            Debug.Log("🔼 Reset level scroll to top");
        }

        LoadAndDisplayLevelLeaderboard();
    }

    // ✅ Hiển thị bảng xếp hạng nhiệm vụ
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

        // ✅ SỬA: Reset scroll khi chuyển sang tab Quest
        if (questScrollRect != null)
        {
            questScrollRect.verticalNormalizedPosition = 1f;
            Debug.Log("🔼 Reset quest scroll to top");
        }

        LoadAndDisplayQuestLeaderboard();
    }

    // ✅ SỬA: Load và hiển thị bảng xếp hạng cấp độ
    private void LoadAndDisplayLevelLeaderboard()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Value != null)
                {
                    try
                    {
                        var userSnapshots = task.Result.Children;

                        // ✅ DEBUG: Kiểm tra số lượng user
                        Debug.Log($"═══════════════════════════════════════");
                        Debug.Log($"🔍 FIREBASE USERS: {userSnapshots.Count()} người");
                        Debug.Log($"═══════════════════════════════════════");

                        List<(string userId, string name, int level)> allPlayers =
                            new List<(string, string, int)>();

                        int index = 0;
                        foreach (var userSnapshot in userSnapshots)
                        {
                            index++;
                            string userId = userSnapshot.Key;

                            // Lấy Name
                            var nameValue = userSnapshot.Child("Name").Value;
                            string playerName = nameValue != null ? nameValue.ToString() : "Unknown";

                            if (string.IsNullOrEmpty(playerName))
                                playerName = $"Player_{userId.Substring(0, 6)}";

                            // Lấy Level
                            int playerLevel = 1;
                            var levelSnapshot = userSnapshot.Child("Level");

                            if (levelSnapshot.Value != null)
                            {
                                try
                                {
                                    string levelJson = levelSnapshot.GetRawJsonValue();
                                    var levelData = JsonConvert.DeserializeObject<
                                        Dictionary<string, object>>(levelJson);

                                    if (levelData != null && levelData.ContainsKey("currentLevel"))
                                    {
                                        playerLevel = int.Parse(levelData["currentLevel"].ToString());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogWarning($"⚠️ Error parsing level: {ex.Message}");
                                }
                            }

                            allPlayers.Add((userId, playerName, playerLevel));

                            // ✅ DEBUG: Log từng user
                            Debug.Log($"   [{index}] ✅ {playerName} → Level {playerLevel}");
                        }

                        Debug.Log($"═══════════════════════════════════════");
                        Debug.Log($"📊 TỔNG NGƯỜI CHƠI ĐƯỢC THÊM: {allPlayers.Count}");
                        Debug.Log($"═══════════════════════════════════════");

                        // ✅ Sort theo level (giảm dần) và lấy top 10
                        var sortedPlayers = allPlayers
                            .OrderByDescending(p => p.level)
                            .Take(maxLeaderboardEntries)
                            .ToList();

                        // ✅ DEBUG: Log sau khi sort
                        Debug.Log($"🏆 BẢNG XẾP HẠNG (SAU KHI SORT):");
                        foreach (var p in sortedPlayers)
                        {
                            int rank = sortedPlayers.IndexOf(p) + 1;
                            Debug.Log($"   #{rank}: {p.name} → Level {p.level}");
                        }

                        // ✅ Hiển thị lên UI
                        DisplayLevelLeaderboard(sortedPlayers.Select((p, idx) =>
                            new LeaderboardEntry(p.userId, p.name, p.level, idx + 1)).ToList());

                        Debug.Log($"✅ Loaded {sortedPlayers.Count} level leaderboard entries");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"❌ Error loading level leaderboard: {e.Message}");
                        Debug.LogError($"Stack trace: {e.StackTrace}");
                    }
                }
                else
                {
                    Debug.LogError("❌ No users data found in Firebase");
                }
            });
    }

    // ✅ SỬA: Load và hiển thị bảng xếp hạng nhiệm vụ
    // ✅ Load và hiển thị bảng xếp hạng nhiệm vụ
    private void LoadAndDisplayQuestLeaderboard()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Value != null)
                {
                    try
                    {
                        var userSnapshots = task.Result.Children;
                        List<(string userId, string name, int completedQuestCount)> allPlayers =
                            new List<(string, string, int)>();

                        Debug.Log($"🔍 Total users in Firebase: {userSnapshots.Count()}");

                        foreach (var userSnapshot in userSnapshots)
                        {
                            string userId = userSnapshot.Key;

                            // Lấy Name
                            var nameValue = userSnapshot.Child("Name").Value;
                            string playerName = nameValue != null ? nameValue.ToString() : "Unknown";

                            if (string.IsNullOrEmpty(playerName))
                                playerName = $"Player_{userId.Substring(0, 6)}";

                            // ✅ Lấy tổng số nhiệm vụ đã hoàn thành
                            int completedQuestCount = 0;
                            var questSnapshot = userSnapshot.Child("Quests");

                            if (questSnapshot.Value != null)
                            {
                                try
                                {
                                    string questJson = questSnapshot.GetRawJsonValue();
                                    var questData = JsonConvert.DeserializeObject<
                                        Dictionary<string, object>>(questJson);

                                    if (questData != null && questData.ContainsKey("completedQuestIds"))
                                    {
                                        var completedList = questData["completedQuestIds"] as Newtonsoft.Json.Linq.JArray;

                                        // ✅ Lấy tổng số phần tử trong list
                                        completedQuestCount = (completedList != null) ? completedList.Count : 0;

                                        Debug.Log($"   📊 {playerName}: Completed {completedQuestCount} quests");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogWarning($"⚠️ Error parsing quest for {userId}: {ex.Message}");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"⚠️ No Quest data for {userId}, using 0 completed quests");
                            }

                            allPlayers.Add((userId, playerName, completedQuestCount));
                            Debug.Log($"   ✅ Added: {playerName} (Completed Quests: {completedQuestCount})");
                        }

                        // ✅ Sort theo tổng số nhiệm vụ hoàn thành (giảm dần) và lấy top 10
                        var sortedPlayers = allPlayers
                            .OrderByDescending(p => p.completedQuestCount)
                            .Take(maxLeaderboardEntries)
                            .ToList();

                        Debug.Log($"🏆 BẢNG XẾP HẠNG NHIỆM VỤ (SAU KHI SORT):");
                        foreach (var p in sortedPlayers)
                        {
                            int rank = sortedPlayers.IndexOf(p) + 1;
                            Debug.Log($"   #{rank}: {p.name} → {p.completedQuestCount} quests");
                        }

                        // ✅ Hiển thị lên UI
                        DisplayQuestLeaderboard(sortedPlayers.Select((p, idx) =>
                            new LeaderboardEntry(p.userId, p.name, p.completedQuestCount, idx + 1)).ToList());

                        Debug.Log($"✅ Loaded {sortedPlayers.Count} quest leaderboard entries");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"❌ Error loading quest leaderboard: {e.Message}");
                        Debug.LogError($"Stack trace: {e.StackTrace}");
                    }
                }
                else
                {
                    Debug.LogError("❌ No users data found in Firebase");
                }
            });
    }

    private void DisplayLevelLeaderboard(List<LeaderboardEntry> entries)
    {
        // ✅ XÓA: Không reset scroll ở đây
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
        // ✅ XÓA: Không reset scroll ở đây
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
}