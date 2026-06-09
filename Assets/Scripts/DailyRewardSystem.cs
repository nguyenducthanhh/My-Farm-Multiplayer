using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyRewardSystem : MonoBehaviour
{
    private static DailyRewardSystem instance;

    [Header("Base Daily Reward")]
    [SerializeField] private int baseDailyGold = 100;

    [Header("Rank Bonus (Top Players)")]
    [SerializeField] private int rank1BonusLevel = 500;
    [SerializeField] private int rank2BonusLevel = 300;
    [SerializeField] private int rank3BonusLevel = 200;

    [SerializeField] private int rank1BonusQuest = 400;
    [SerializeField] private int rank2BonusQuest = 250;
    [SerializeField] private int rank3BonusQuest = 150;

    [Header("Daily Reward Time (UTC)")]
    [SerializeField] private int rewardHour = 0;
    [SerializeField] private int rewardMinute = 0;
    [SerializeField] private int rewardSecond = 0;
    [SerializeField] private int checkIntervalSeconds = 60;

    private float timeSinceLastCheck = 0f;
    private string lastCheckedDate = "";
    private bool hasCheckedLoginToday = false;

    private RankingSnapshot cachedSnapshot = null;
    private string cachedSnapshotDate = "";
    private bool isCheckingSnapshot = false;
    private bool isClaimingRankingReward = false;

    private int cachedPlayerLevelRank = -1;
    private int cachedPlayerQuestRank = -1;

    public static DailyRewardSystem Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<DailyRewardSystem>();
            return instance;
        }
    }

    private void Start()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        lastCheckedDate = GetTodayDate();

        Debug.Log($" DailyRewardSystem initialized. Today: {lastCheckedDate}");
    }

    private void Update()
    {
        timeSinceLastCheck += Time.deltaTime;

        if (timeSinceLastCheck >= checkIntervalSeconds)
        {
            timeSinceLastCheck = 0f;
            CheckAndClaimDailyReward();
        }
    }

    public void CheckRewardOnLogin()
    {
        if (!LoadDataManager.IsUserDataLoaded || !LoadDataManager.HasUserRecord)
        {
            Debug.LogWarning("Daily reward check skipped because user data is not validly loaded.");
            return;
        }

        StartCoroutine(CheckRewardAfterLeaderboardLoaded());
    }

    private void ResetDailyFlagsIfNewDay()
    {
        if (LoadDataManager.userInGame?.DailyReward == null)
            return;

        string today = GetTodayDate();
        var rewardData = LoadDataManager.userInGame.DailyReward;

        rewardData.hasClaimedDailyToday = rewardData.lastDailyClaimDate == today;
        rewardData.hasClaimedRankingToday = rewardData.lastRankingRewardDate == today;

    }

    private IEnumerator CheckRewardAfterLeaderboardLoaded()
    {
        string today = GetTodayDate();

        ResetDailyFlagsIfNewDay();

        // Đợi cho đến khi LeaderboardManager có dữ liệu
        float waitTime = 0f;
        float maxWaitTime = 10f;

        while (waitTime < maxWaitTime)
        {
            if (LeaderboardManager.Instance != null &&
                LeaderboardManager.Instance.GetLevelLeaderboard().Count > 0 &&
                LeaderboardManager.Instance.GetQuestLeaderboard().Count > 0)
            {
                Debug.Log($" Leaderboard loaded! Checking daily reward...");
                break;
            }

            Debug.Log($" Waiting for leaderboard to load... ({waitTime:F1}s)");
            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;
        }

        if (waitTime >= maxWaitTime)
        {
            Debug.LogWarning(" Leaderboard took too long to load, checking with current leaderboard");
        }

        if (hasCheckedLoginToday && lastCheckedDate == today)
        {
            Debug.Log($" Already checked login reward today ({today})");
            yield break;
        }

        Debug.Log($" Checking daily reward on login... (Date: {today})");

        if (LoadDataManager.userInGame?.DailyReward == null)
        {
            Debug.LogWarning(" DailyReward data is null");
            yield break;
        }

        // Snapshot ranking là dữ liệu toàn cục theo ngày, không phụ thuộc trạng thái từng user.
        if (IsRewardTimePassed())
        {
            yield return StartCoroutine(EnsureRankingSnapshotExistsCoroutine(today));
        }

        if (IsRewardTimePassed())
        {
            //  Claim quà hàng ngày
            if (!LoadDataManager.userInGame.DailyReward.hasClaimedDailyToday)
            {
                Debug.Log($" Claiming daily reward on login...");
                AutoClaimDailyReward();
            }
            else
            {
                Debug.Log($" Already claimed daily reward today ({today})");
            }

            //  Claim quà ranking (riêng biệt)
            if (!LoadDataManager.userInGame.DailyReward.hasClaimedRankingToday)
            {
                Debug.Log($" Claiming ranking reward on login...");
                yield return StartCoroutine(ClaimRankingRewardCoroutine(today));
            }
        }
        else
        {
            Debug.Log($" Reward time has not passed yet. Daily and ranking rewards are not claimable.");
        }

        hasCheckedLoginToday = true;
        lastCheckedDate = today;
    }

    private IEnumerator EnsureRankingSnapshotExistsCoroutine(string date)
    {
        while (isCheckingSnapshot)
        {
            yield return null;
        }

        if (cachedSnapshotDate == date && cachedSnapshot != null)
            yield break;

        bool isLoaded = false;
        bool snapshotExists = false;

        FirebaseDatabase.DefaultInstance
            .GetReference("DailyRewardRanking")
            .Child(date)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && task.Result.Value != null)
                {
                    try
                    {
                        cachedSnapshot = JsonConvert.DeserializeObject<RankingSnapshot>(task.Result.GetRawJsonValue());
                        cachedSnapshotDate = date;
                        snapshotExists = cachedSnapshot != null;
                        Debug.Log($" Global ranking snapshot already exists for {date}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($" Error parsing existing ranking snapshot: {e.Message}");
                    }
                }

                isLoaded = true;
            });

        yield return new WaitUntil(() => isLoaded);

        if (snapshotExists)
            yield break;

        if (LeaderboardManager.Instance != null)
        {
            yield return StartCoroutine(LeaderboardManager.Instance.RefreshLeaderboardFromUsers());
        }

        yield return StartCoroutine(SaveRankingSnapshotCoroutine(date));
    }

    private IEnumerator SaveRankingSnapshotCoroutine(string date)
    {
        if (isCheckingSnapshot)
        {
            Debug.Log(" Already saving snapshot, skipping...");
            yield break;
        }

        if (LeaderboardManager.Instance == null)
        {
            Debug.LogWarning(" LeaderboardManager not initialized, cannot save snapshot");
            yield break;
        }

        var levelLeaderboard = LeaderboardManager.Instance.GetLevelLeaderboard();
        var questLeaderboard = LeaderboardManager.Instance.GetQuestLeaderboard();

        if (levelLeaderboard.Count == 0 || questLeaderboard.Count == 0)
        {
            Debug.LogWarning(" Leaderboard is empty, cannot save snapshot");
            yield break;
        }

        isCheckingSnapshot = true;

        // Tạo snapshot data
        var rankingSnapshot = new RankingSnapshot
        {
            snapshotDate = date,
            levelRanking = new Dictionary<string, int>(),
            questRanking = new Dictionary<string, int>()
        };

        // Ghi rank cho từng player (Level)
        for (int i = 0; i < levelLeaderboard.Count; i++)
        {
            rankingSnapshot.levelRanking[levelLeaderboard[i].playerId] = i + 1;
        }

        // Ghi rank cho từng player (Quest)
        for (int i = 0; i < questLeaderboard.Count; i++)
        {
            rankingSnapshot.questRanking[questLeaderboard[i].playerId] = i + 1;
        }

        // Lưu vào Firebase
        string snapshotJson = JsonConvert.SerializeObject(rankingSnapshot);

        bool isSaved = false;
        FirebaseDatabase.DefaultInstance
            .GetReference("DailyRewardRanking")
            .Child(date)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && task.Result.Value != null)
                {
                    Debug.Log($" Ranking snapshot for {date} already exists, keeping original reward ranks.");
                    try
                    {
                        cachedSnapshot = JsonConvert.DeserializeObject<RankingSnapshot>(task.Result.GetRawJsonValue());
                        cachedSnapshotDate = date;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($" Error parsing existing ranking snapshot: {e.Message}");
                    }

                    isSaved = true;
                    return;
                }

                FirebaseDatabase.DefaultInstance
                    .GetReference("DailyRewardRanking")
                    .Child(date)
                    .SetRawJsonValueAsync(snapshotJson)
                    .ContinueWithOnMainThread(task =>
                    {
                        if (task.IsCompleted && !task.IsFaulted)
                        {
                            cachedSnapshot = rankingSnapshot;
                            cachedSnapshotDate = date;
                            Debug.Log($"   Ranking snapshot saved for {date}");
                            Debug.Log($"   Level Rankings: {rankingSnapshot.levelRanking.Count} players");
                            Debug.Log($"   Quest Rankings: {rankingSnapshot.questRanking.Count} players");
                            isSaved = true;
                        }
                        else
                        {
                            Debug.LogError($" Failed to save ranking snapshot: {task.Exception}");
                            isSaved = true;
                        }
                    });
            });

        //  Đợi cho đến khi save xong
        yield return new WaitUntil(() => isSaved);

        if (LoadDataManager.userInGame?.DailyReward != null)
        {
            LoadDataManager.userInGame.DailyReward.lastSavedSnapshotDate = date;

            //  Lưu vào Firebase
            string rewardJson = JsonConvert.SerializeObject(LoadDataManager.userInGame.DailyReward);
            FirebaseDatabase.DefaultInstance
                .GetReference("Users")
                .Child(LoadDataManager.firebaseUser.UserId)
                .Child("DailyReward")
                .SetRawJsonValueAsync(rewardJson)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted && !task.IsFaulted)
                    {
                        Debug.Log($" lastSavedSnapshotDate updated to {date}");
                    }
                    else
                    {
                        Debug.LogError($" Failed to update lastSavedSnapshotDate: {task.Exception}");
                    }
                });
        }

        isCheckingSnapshot = false;
    }

    private IEnumerator LoadRankingSnapshotCoroutine(string date)
    {
        //  Nếu đã cache rồi, dùng cache
        if (cachedSnapshotDate == date && cachedSnapshot != null)
        {
            Debug.Log($" Using cached snapshot for {date}");
            ExtractPlayerRankFromSnapshot(cachedSnapshot);
            yield break;
        }

        bool isLoaded = false;
        FirebaseDatabase.DefaultInstance
            .GetReference("DailyRewardRanking")
            .Child(date)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Value != null)
                {
                    try
                    {
                        cachedSnapshot = JsonConvert.DeserializeObject<RankingSnapshot>(task.Result.GetRawJsonValue());
                        cachedSnapshotDate = date;

                        ExtractPlayerRankFromSnapshot(cachedSnapshot);

                        Debug.Log($" Snapshot loaded for {date}");
                        if (cachedSnapshot != null)
                        {
                            Debug.Log($"   Level Rankings: {cachedSnapshot.levelRanking.Count}");
                            Debug.Log($"   Quest Rankings: {cachedSnapshot.questRanking.Count}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($" Error parsing snapshot: {e.Message}");
                        cachedPlayerLevelRank = -1;
                        cachedPlayerQuestRank = -1;
                    }
                }
                else
                {
                    Debug.LogWarning($" No snapshot found for {date}");
                    cachedPlayerLevelRank = -1;
                    cachedPlayerQuestRank = -1;
                }

                isLoaded = true;
            });

        //  Đợi cho đến khi load xong
        yield return new WaitUntil(() => isLoaded);
    }

    private void ExtractPlayerRankFromSnapshot(RankingSnapshot snapshot)
    {
        if (snapshot == null || LoadDataManager.firebaseUser == null)
        {
            cachedPlayerLevelRank = -1;
            cachedPlayerQuestRank = -1;
            return;
        }

        string userId = LoadDataManager.firebaseUser.UserId;

        if (snapshot.levelRanking.TryGetValue(userId, out int levelRank))
        {
            cachedPlayerLevelRank = levelRank;
            Debug.Log($" Player level rank: {levelRank}");
        }
        else
        {
            cachedPlayerLevelRank = -1;
            Debug.Log($" Player not in level top rankings");
        }

        if (snapshot.questRanking.TryGetValue(userId, out int questRank))
        {
            cachedPlayerQuestRank = questRank;
            Debug.Log($" Player quest rank: {questRank}");
        }
        else
        {
            cachedPlayerQuestRank = -1;
            Debug.Log($" Player not in quest top rankings");
        }
    }

    private int GetPlayerRankLevelFromSnapshot()
    {
        return cachedPlayerLevelRank;
    }

    private int GetPlayerRankQuestFromSnapshot()
    {
        return cachedPlayerQuestRank;
    }


    private bool IsRewardTimePassed()
    {
        DateTime now = DateTime.UtcNow;

        DateTime targetTime = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            rewardHour,
            rewardMinute,
            rewardSecond
        );

        return now >= targetTime;
    }

    private void CheckAndClaimDailyReward()
    {
        if (!LoadDataManager.IsUserDataLoaded || !LoadDataManager.HasUserRecord)
        {
            return;
        }

        if (LoadDataManager.userInGame?.DailyReward == null)
        {
            Debug.Log($"DailyReward data is null");
            return;
        }

        string today = GetTodayDate();
        ResetDailyFlagsIfNewDay();

        if (!IsRewardTimePassed())
        {
            return;
        }

        Debug.Log($" Reward time passed! Checking status...");
        lastCheckedDate = today;

        if (LoadDataManager.userInGame != null && LoadDataManager.firebaseUser != null)
        {
            StartCoroutine(EnsureRankingSnapshotExistsCoroutine(today));

            //  Claim quà hàng ngày nếu chưa claim
            if (!LoadDataManager.userInGame.DailyReward.hasClaimedDailyToday)
            {
                Debug.Log($" Claiming daily reward at reward time...");
                AutoClaimDailyReward();
            }

            //  Claim quà ranking nếu chưa claim
            if (!LoadDataManager.userInGame.DailyReward.hasClaimedRankingToday)
            {
                Debug.Log($" Claiming ranking reward at reward time...");
                StartCoroutine(ClaimRankingRewardCoroutine(today));
            }
        }
    }

    private void AutoClaimDailyReward()
    {
        string today = GetTodayDate();

        if (LoadDataManager.userInGame.DailyReward != null &&
            LoadDataManager.userInGame.DailyReward.hasClaimedDailyToday)
        {
            Debug.Log($" Already claimed daily reward today ({today})");
            return;
        }

        Debug.Log($" Auto-claiming daily reward for {today}...");

        int dailyBaseReward = baseDailyGold;
        LoadDataManager.userInGame.Gold += dailyBaseReward;
        LoadDataManager.userInGame.DailyReward.lastDailyClaimDate = today;
        LoadDataManager.userInGame.DailyReward.lastDailyClaimGold = dailyBaseReward;
        LoadDataManager.userInGame.DailyReward.hasClaimedDailyToday = true;

        NotificationManager.ShowReward($"Phần thưởng hàng ngày: +{dailyBaseReward} Vàng");

        Debug.Log($" Daily Base Reward Claimed: {dailyBaseReward} Gold");

        SaveDailyRewardToFirebase();

        UsernameWizard.UpdateGoldDisplay();

        Debug.Log($" Daily reward auto-claimed: {dailyBaseReward} Gold");
    }

    private IEnumerator ClaimRankingRewardCoroutine(string today)
    {
        if (isClaimingRankingReward)
        {
            Debug.Log(" Already claiming ranking reward, skipping duplicate request...");
            yield break;
        }

        if (LoadDataManager.userInGame.DailyReward != null &&
            LoadDataManager.userInGame.DailyReward.lastRankingRewardDate == today)
        {
            LoadDataManager.userInGame.DailyReward.hasClaimedRankingToday = true;
            Debug.Log($" Already claimed ranking reward today ({today})");
            yield break;
        }

        isClaimingRankingReward = true;

        yield return StartCoroutine(EnsureRankingSnapshotExistsCoroutine(today));

        yield return StartCoroutine(LoadRankingSnapshotCoroutine(today));

        if (LoadDataManager.userInGame.DailyReward != null &&
            (LoadDataManager.userInGame.DailyReward.hasClaimedRankingToday ||
             LoadDataManager.userInGame.DailyReward.lastRankingRewardDate == today))
        {
            LoadDataManager.userInGame.DailyReward.hasClaimedRankingToday = true;
            Debug.Log($"✅ Already claimed ranking reward today ({today})");
            isClaimingRankingReward = false;
            yield break;
        }

        int rankingBonus = 0;

        int playerRankLevel = GetPlayerRankLevelFromSnapshot();
        int levelBonus = GetRankBonusLevel(playerRankLevel);
        rankingBonus += levelBonus;

        int playerRankQuest = GetPlayerRankQuestFromSnapshot();
        int questBonus = GetRankBonusQuest(playerRankQuest);
        rankingBonus += questBonus;

        if (rankingBonus > 0)
        {
            LoadDataManager.userInGame.Gold += rankingBonus;
            LoadDataManager.userInGame.DailyReward.lastRankingRewardDate = today;
            LoadDataManager.userInGame.DailyReward.lastRankingRewardGold = rankingBonus;
            LoadDataManager.userInGame.DailyReward.hasClaimedRankingToday = true;

            Debug.Log($"   Ranking Reward Claimed: {rankingBonus} Gold");
            Debug.Log($"   Level Rank: {playerRankLevel} → Bonus: {levelBonus}");
            Debug.Log($"   Quest Rank: {playerRankQuest} → Bonus: {questBonus}");

            SaveDailyRewardToFirebase();
            UsernameWizard.UpdateGoldDisplay();

            NotificationManager.ShowReward($"Bảng xếp hạng: +{rankingBonus} Vàng (Cấp {playerRankLevel}, Quest {playerRankQuest})");

            Debug.Log($" Ranking reward auto-claimed: {rankingBonus} Gold");

        }
        else
        {
            Debug.Log($" Player not in top rankings, no ranking reward");
            LoadDataManager.userInGame.DailyReward.lastRankingRewardDate = today;
            LoadDataManager.userInGame.DailyReward.lastRankingRewardGold = 0;
            LoadDataManager.userInGame.DailyReward.hasClaimedRankingToday = true;
            SaveDailyRewardToFirebase();
        }

        isClaimingRankingReward = false;
    }


    private void SaveDailyRewardToFirebase()
    {
        if (LoadDataManager.firebaseUser == null || !LoadDataManager.IsUserDataLoaded || !LoadDataManager.HasUserRecord)
            return;

        var userRef = FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId);

        userRef.Child("Gold").SetValueAsync(LoadDataManager.userInGame.Gold)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log($" Gold saved: {LoadDataManager.userInGame.Gold}");
                }
                else
                {
                    Debug.LogError($" Failed to save Gold: {task.Exception}");
                }
            });

        string rewardJson = JsonConvert.SerializeObject(LoadDataManager.userInGame.DailyReward);
        userRef.Child("DailyReward").SetRawJsonValueAsync(rewardJson)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log($" DailyReward data saved");
                }
                else
                {
                    Debug.LogError($" Failed to save DailyReward: {task.Exception}");
                }
            });
    }

    private int GetRankBonusLevel(int rank)
    {
        return rank switch
        {
            1 => rank1BonusLevel,
            2 => rank2BonusLevel,
            3 => rank3BonusLevel,
            _ => 0
        };
    }

    private int GetRankBonusQuest(int rank)
    {
        return rank switch
        {
            1 => rank1BonusQuest,
            2 => rank2BonusQuest,
            3 => rank3BonusQuest,
            _ => 0
        };
    }

    private string GetTodayDate()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd");
    }

   
}