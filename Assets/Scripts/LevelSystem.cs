using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSystem : MonoBehaviour
{
    [SerializeField] private LevelConfig levelConfig;

    private static LevelSystem instance;
    private int currentLevel = 1;
    private int currentExperience = 0;
    private int experienceToNextLevel;
    private List<string> unlockedItems = new List<string>();

    [System.Serializable]
    public class LevelData
    {
        public int currentLevel;
        public int currentExperience;
        public int experienceToNextLevel;
        public List<string> unlockedItems = new List<string>();
    }

    public static LevelSystem Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<LevelSystem>();
            return instance;
        }
    }
    public LevelConfig LevelConfigData
    {
        get { return levelConfig; }
    }
    private void Start()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        if (levelConfig == null)
            Debug.LogError(" LevelConfig not assigned!");

        RecalculateExperienceToNextLevel();
        Debug.Log($" Initialized experienceToNextLevel: {experienceToNextLevel}");

        StartCoroutine(WaitForUserDataAndLoadLevel());

    }

    private IEnumerator WaitForUserDataAndLoadLevel()
    {
        while (LoadDataManager.firebaseUser == null ||
               LoadDataManager.userInGame == null ||
               !LoadDataManager.IsUserDataLoaded)
        {
            yield return new WaitForSeconds(0.1f);
        }

        LoadLevelDataFromFirebase();
    }

    public void AddExperience(int amount)
    {
        RecalculateExperienceToNextLevel();

        currentExperience += amount;
        Debug.Log($" +{amount} XP (Total: {currentExperience}/{experienceToNextLevel})");

        while (currentExperience >= experienceToNextLevel)
        {
            LevelUp();
        }

        SaveLevelDataToFirebase();
    }

    private void LevelUp()
    {
        currentExperience -= experienceToNextLevel;
        currentLevel++;

        RecalculateExperienceToNextLevel();

        Debug.Log($" LÊN CẤP {currentLevel}! EXP: {currentExperience}/{experienceToNextLevel}");
        NotificationManager.ShowReward($"LÊN CẤP {currentLevel}!");
        CheckUnlockedItems();
    }

    private void CheckUnlockedItems()
    {
        if (levelConfig == null) return;

        Debug.Log($" Checking unlocks - Current Level: {currentLevel}, Unlocked Items: {string.Join(", ", unlockedItems)}");

        foreach (var unlock in levelConfig.levelUnlocks)
        {
            Debug.Log($" Checking {unlock.unlockedItemName}: Level {unlock.requiredLevel}, Current: {currentLevel}");

            if (currentLevel >= unlock.requiredLevel && !unlockedItems.Contains(unlock.unlockedItemName))
            {
                unlockedItems.Add(unlock.unlockedItemName);
                Debug.Log($" UNLOCK: {unlock.unlockedItemDescription} (Cấp {unlock.requiredLevel})");

                OnItemUnlocked?.Invoke(unlock);
            }
        }

        SaveLevelDataToFirebase();
    }

    //  Event khi item được mở khóa
    public delegate void OnUnlockHandler(LevelConfig.LevelUnlock unlockedItem);
    public static event OnUnlockHandler OnItemUnlocked;

    //  Kiểm tra item có được mở khóa
    public bool IsItemUnlocked(string itemName)
    {
        return unlockedItems.Contains(itemName);
    }

    //  Lấy level yêu cầu để mở khóa item
    public int GetRequiredLevelForItem(string itemName)
    {
        if (levelConfig == null) return -1;

        foreach (var unlock in levelConfig.levelUnlocks)
        {
            if (unlock.unlockedItemName == itemName)
                return unlock.requiredLevel;
        }
        return -1;
    }

    private int GetExperienceForLevel(int level)
    {
        if (levelConfig == null) return 100 + (level * 50);

        var threshold = levelConfig.levelThresholds.Find(t => t.level == level);
        return threshold != null ? threshold.experienceRequired : 100 + (level * 50);
    }

    private void RecalculateExperienceToNextLevel()
    {
        experienceToNextLevel = Mathf.Max(1, GetExperienceForLevel(currentLevel + 1));
    }

    private void NormalizeLevelProgressFromConfig()
    {
        RecalculateExperienceToNextLevel();

        int guard = 0;
        while (currentExperience >= experienceToNextLevel && guard < 100)
        {
            currentExperience -= experienceToNextLevel;
            currentLevel++;
            RecalculateExperienceToNextLevel();
            guard++;
        }

        if (guard >= 100)
        {
            Debug.LogWarning(" Level normalization stopped after 100 level-ups. Check LevelConfig thresholds.");
        }
    }


    public int GetCurrentLevel() => currentLevel;
    public int GetCurrentExperience() => currentExperience;
    public int GetExperienceToNextLevel() => experienceToNextLevel;
    public float GetExperienceProgress() => (float)currentExperience / Mathf.Max(1, experienceToNextLevel);
    public List<string> GetUnlockedItems() => unlockedItems;
    private void LoadLevelDataFromFirebase()
    {
        if (LoadDataManager.firebaseUser == null) return;

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Level")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && task.Result.Value != null)
                {
                    try
                    {
                        string json = task.Result.GetRawJsonValue();
                        var levelData = JsonConvert.DeserializeObject<LevelData>(json);

                        if (levelData != null)
                        {
                            currentLevel = Mathf.Max(1, levelData.currentLevel);
                            currentExperience = Mathf.Max(0, levelData.currentExperience);
                            unlockedItems = levelData.unlockedItems ?? new List<string>();
                            NormalizeLevelProgressFromConfig();

                            Debug.Log($" Loaded Level: {currentLevel}, EXP: {currentExperience}/{experienceToNextLevel}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error loading level data: {e.Message}");
                    }
                }
                else if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"Failed to load level data. Keeping local defaults and not saving over Firebase: {task.Exception}");
                    return;
                }
                else
                {
                    Debug.LogWarning("No level data found. Keeping local defaults and not saving automatically.");
                    currentLevel = 1;
                    currentExperience = 0;
                    unlockedItems = new List<string>();
                    RecalculateExperienceToNextLevel();
                }
                CheckUnlockedItems();
            });
    }

    private void SaveLevelDataToFirebase()
    {
        if (LoadDataManager.firebaseUser == null || !LoadDataManager.IsUserDataLoaded || !LoadDataManager.HasUserRecord) return;

        var levelData = new LevelData
        {
            currentLevel = this.currentLevel,
            currentExperience = this.currentExperience,
            experienceToNextLevel = this.experienceToNextLevel,
            unlockedItems = this.unlockedItems
        };

        string json = JsonConvert.SerializeObject(levelData);

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Level")
            .SetRawJsonValueAsync(json);
    }
}
