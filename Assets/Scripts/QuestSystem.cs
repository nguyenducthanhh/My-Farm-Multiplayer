using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestSystem : MonoBehaviour
{
    [SerializeField] private QuestConfig questConfig;
    private static QuestSystem instance;

    private Queue<QuestConfig.Quest> activeQuests = new Queue<QuestConfig.Quest>();
    private List<int> completedQuestIds = new List<int>();

    [System.Serializable]
    public class QuestData
    {
        public List<int> activeQuestIds = new List<int>();
        public List<int> completedQuestIds = new List<int>();
    }

    public static QuestSystem Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<QuestSystem>();
            return instance;
        }
    }

    // ✅ Event khi quest hoàn thành
    public delegate void OnQuestCompletedHandler(QuestConfig.Quest quest);
    public static event OnQuestCompletedHandler OnQuestCompleted;

    private void Start()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        if (questConfig == null)
        {
            Debug.LogError("❌ QuestConfig NOT ASSIGNED!");
            return;
        }

        Debug.Log($"✅ QuestConfig loaded: {questConfig.allQuests.Count} quests");


        LoadQuestDataFromFirebase();
    }

    // ✅ Cấp tiến các quest (xóa quest trước, thêm quest mới)
    public void AdvanceQuests()
    {
        if (activeQuests.Count < 4)
        {
            AddNewQuest();
        }
    }

    //private void AddNewQuest()
    //{
    //    if (questConfig == null || questConfig.allQuests.Count == 0) return;

    //    // ✅ Tìm quest chưa hoàn thành và chưa active
    //    foreach (var quest in questConfig.allQuests)
    //    {
    //        if (!completedQuestIds.Contains(quest.questId) && !IsQuestActive(quest.questId))
    //        {
    //            // ✅ Kiểm tra cấp độ
    //            if (LevelSystem.Instance.GetCurrentLevel() >= quest.minimumLevel)
    //            {
    //                activeQuests.Enqueue(quest);
    //                Debug.Log($"📋 Added quest: {quest.questName}");

    //                if (activeQuests.Count < 4)
    //                    AdvanceQuests();  // ← Đệ quy thêm quest cho đủ 4 cái
    //                return;
    //            }
    //        }
    //    }
    //}
    private void AddNewQuest()
    {
        if (questConfig == null || questConfig.allQuests.Count == 0) return;

        // ✅ Tìm quest chưa hoàn thành và chưa active
        foreach (var quest in questConfig.allQuests)
        {
            if (!completedQuestIds.Contains(quest.questId) && !IsQuestActive(quest.questId))
            {
                // ✅ XÓA kiểm tra cấp độ - tất cả quest bây giờ đều có thể làm
                activeQuests.Enqueue(quest);
                Debug.Log($"📋 Added quest: {quest.questName}");

                if (activeQuests.Count < 4)
                    AdvanceQuests();  // ← Đệ quy thêm quest cho đủ 4 cái
                return;
            }
        }
    }
    private bool IsQuestActive(int questId)
    {
        foreach (var quest in activeQuests)
        {
            if (quest.questId == questId)
                return true;
        }
        return false;
    }

    // ✅ Kiểm tra quest có thể hoàn thành không
    public bool CanCompleteQuest(QuestConfig.Quest quest, RecyclableInventory inventory)
    {
        if (quest == null || inventory == null) return false;

        foreach (var requirement in quest.requirements)
        {
            int quantity = inventory.GetItemQuantity(requirement.itemName);
            if (quantity < requirement.quantity)
            {
                Debug.Log($"❌ Không đủ {requirement.itemName}! Cần {requirement.quantity}, có {quantity}");
                return false;
            }
        }

        return true;
    }

    // ✅ Hoàn thành quest
    public void CompleteQuest(QuestConfig.Quest quest, RecyclableInventory inventory)
    {
        if (!CanCompleteQuest(quest, inventory)) return;

        // ✅ Trừ items từ inventory
        foreach (var requirement in quest.requirements)
        {
            inventory.RemoveInventoryItem(requirement.itemName, requirement.quantity);
            Debug.Log($"✅ Trừ {requirement.quantity}x {requirement.itemName}");
        }

        // ✅ Trao thưởng
        if (quest.rewardGold > 0)
        {
            LoadDataManager.userInGame.Gold += quest.rewardGold;
            Debug.Log($"💰 +{quest.rewardGold} Gold");
        }

        if (quest.rewardExperience > 0)
        {
            LevelSystem.Instance.AddExperience(quest.rewardExperience);
            Debug.Log($"⭐ +{quest.rewardExperience} XP");
        }

        // ✅ Đánh dấu hoàn thành
        completedQuestIds.Add(quest.questId);

        // ✅ Xóa quest khỏi active list
        var tempQueue = new Queue<QuestConfig.Quest>();
        while (activeQuests.Count > 0)
        {
            var q = activeQuests.Dequeue();
            if (q.questId != quest.questId)
                tempQueue.Enqueue(q);
        }
        activeQuests = tempQueue;

        // ✅ Thêm quest mới
        AdvanceQuests();

        // ✅ Broadcast event
        OnQuestCompleted?.Invoke(quest);

        SaveQuestDataToFirebase();
    }

    // ✅ Getter
    public Queue<QuestConfig.Quest> GetActiveQuests() => activeQuests;
    public int GetActiveQuestCount() => activeQuests.Count;

    private void LoadQuestDataFromFirebase()
    {
        if (LoadDataManager.firebaseUser == null)
        {
            InitializeQuests();
            return;
        }

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Quests")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Value != null)
                {
                    try
                    {
                        string json = task.Result.GetRawJsonValue();
                        var questData = JsonConvert.DeserializeObject<QuestData>(json);

                        if (questData != null)
                        {
                            completedQuestIds = questData.completedQuestIds;

                            // ✅ Load active quests
                            foreach (var questId in questData.activeQuestIds)
                            {
                                var quest = questConfig.allQuests.Find(q => q.questId == questId);
                                if (quest != null)
                                    activeQuests.Enqueue(quest);
                            }

                            Debug.Log($"✅ Loaded quests: {activeQuests.Count} active, {completedQuestIds.Count} completed");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error loading quest data: {e.Message}");
                        InitializeQuests();
                    }
                }
                else
                {
                    Debug.Log("No quest data found, initializing...");
                    InitializeQuests();

                }
                Debug.Log($"📊 Active quests after load: {activeQuests.Count}");

            });
    }

    private void InitializeQuests()
    {
        // ✅ Thêm 4 quest đầu tiên
        for (int i = 0; i < 4; i++)
        {
            AdvanceQuests();
        }

        SaveQuestDataToFirebase();
    }

    private void SaveQuestDataToFirebase()
    {
        if (LoadDataManager.firebaseUser == null) return;

        var questData = new QuestData();
        foreach (var quest in activeQuests)
        {
            questData.activeQuestIds.Add(quest.questId);
        }
        questData.completedQuestIds = completedQuestIds;

        string json = JsonConvert.SerializeObject(questData);

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Quests")
            .SetRawJsonValueAsync(json);
    }
}