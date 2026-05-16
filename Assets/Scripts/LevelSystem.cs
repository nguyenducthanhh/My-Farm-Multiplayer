using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
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
            Debug.LogError("❌ LevelConfig not assigned!");
        else
        {
            // ✅ THÊM: Khởi tạo experienceToNextLevel đúng từ LevelConfig
            experienceToNextLevel = GetExperienceForLevel(currentLevel + 1);
            Debug.Log($"✅ Initialized experienceToNextLevel: {experienceToNextLevel}");
        }
        LoadLevelDataFromFirebase();

    }

    public void AddExperience(int amount)
    {
        currentExperience += amount;
        Debug.Log($"📊 +{amount} XP (Total: {currentExperience}/{experienceToNextLevel})");

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

        // ✅ Lấy XP requirement từ config
        experienceToNextLevel = GetExperienceForLevel(currentLevel + 1);

        Debug.Log($"🎉 LÊN CẤP {currentLevel}! EXP: {currentExperience}/{experienceToNextLevel}");

        CheckUnlockedItems();
    }

    //private void CheckUnlockedItems()
    //{
    //    if (levelConfig == null) return;

    //    foreach (var unlock in levelConfig.levelUnlocks)
    //    {
    //        if (currentLevel >= unlock.requiredLevel && !unlockedItems.Contains(unlock.unlockedItemName))
    //        {
    //            unlockedItems.Add(unlock.unlockedItemName);
    //            Debug.Log($"🔓 UNLOCK: {unlock.unlockedItemDescription} (Cấp {unlock.requiredLevel})");

    //            // ✅ Broadcast unlock event
    //            OnItemUnlocked?.Invoke(unlock);
    //        }
    //    }

    //    SaveLevelDataToFirebase();
    //}
    private void CheckUnlockedItems()
    {
        if (levelConfig == null) return;

        Debug.Log($"🔍 Checking unlocks - Current Level: {currentLevel}, Unlocked Items: {string.Join(", ", unlockedItems)}");

        foreach (var unlock in levelConfig.levelUnlocks)
        {
            Debug.Log($"   ├─ Checking {unlock.unlockedItemName}: Level {unlock.requiredLevel}, Current: {currentLevel}");

            if (currentLevel >= unlock.requiredLevel && !unlockedItems.Contains(unlock.unlockedItemName))
            {
                unlockedItems.Add(unlock.unlockedItemName);
                Debug.Log($"🔓 UNLOCK: {unlock.unlockedItemDescription} (Cấp {unlock.requiredLevel})");

                // ✅ Broadcast unlock event
                OnItemUnlocked?.Invoke(unlock);
            }
        }

        SaveLevelDataToFirebase();
    }

    // ✅ Event khi item được mở khóa
    public delegate void OnUnlockHandler(LevelConfig.LevelUnlock unlockedItem);
    public static event OnUnlockHandler OnItemUnlocked;

    // ✅ Kiểm tra item có được mở khóa
    public bool IsItemUnlocked(string itemName)
    {
        return unlockedItems.Contains(itemName);
    }

    // ✅ Lấy level yêu cầu để mở khóa item
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

    // ✅ THÊM: Getter cho LevelData (để save)


    public int GetCurrentLevel() => currentLevel;
    public int GetCurrentExperience() => currentExperience;
    public int GetExperienceToNextLevel() => experienceToNextLevel;
    public float GetExperienceProgress() => (float)currentExperience / experienceToNextLevel;
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
                if (task.IsCompleted && task.Result.Value != null)
                {
                    try
                    {
                        string json = task.Result.GetRawJsonValue();
                        var levelData = JsonConvert.DeserializeObject<LevelData>(json);

                        if (levelData != null)
                        {
                            currentLevel = levelData.currentLevel;
                            currentExperience = levelData.currentExperience;
                            experienceToNextLevel = levelData.experienceToNextLevel;
                            unlockedItems = levelData.unlockedItems;

                            Debug.Log($"✅ Loaded Level: {currentLevel}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error loading level data: {e.Message}");
                    }
                }
                else
                {
                    Debug.Log("No level data found");
                    SaveLevelDataToFirebase();
                }
                CheckUnlockedItems();
            });
    }

    private void SaveLevelDataToFirebase()
    {
        if (LoadDataManager.firebaseUser == null) return;

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