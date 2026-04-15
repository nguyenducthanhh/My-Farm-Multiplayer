//using UnityEngine;
//using UnityEngine.UI;

//public class QuestSlotUI : MonoBehaviour
//{
//    [SerializeField] private Text questNameText;
//    [SerializeField] private Text questDescriptionText;
//    [SerializeField] private Text requirementsText;
//    [SerializeField] private Button completeButton;
//    [SerializeField] private Text rewardText;

//    private QuestConfig.Quest currentQuest;
//    private RecyclableInventory inventory;

//    public void SetQuest(QuestConfig.Quest quest, RecyclableInventory inv)
//    {
//        currentQuest = quest;
//        inventory = inv;

//        gameObject.SetActive(true);

//        questNameText.text = quest.questName;
//        questDescriptionText.text = quest.questDescription;
//        rewardText.text = $"⭐ +{quest.rewardExperience} XP | 💰 +{quest.rewardGold}";

//        // ✅ Hiển thị yêu cầu
//        string reqText = "Yêu cầu:\n";
//        foreach (var req in quest.requirements)
//        {
//            int have = inventory.GetItemQuantity(req.itemName);
//            reqText += $"• {req.itemName} x{req.quantity} ({have}/{req.quantity})\n";
//        }
//        requirementsText.text = reqText;

//        // ✅ Update button
//        if (completeButton != null)
//        {
//            completeButton.onClick.RemoveAllListeners();
//            completeButton.onClick.AddListener(OnCompleteButtonClicked);
//            completeButton.interactable = QuestSystem.Instance.CanCompleteQuest(quest, inventory);
//        }
//    }

//    public void HideQuest()
//    {
//        gameObject.SetActive(false);
//    }

//    private void OnCompleteButtonClicked()
//    {
//        if (currentQuest == null) return;

//        if (!QuestSystem.Instance.CanCompleteQuest(currentQuest, inventory))
//        {
//            Debug.Log("❌ Không đủ items để hoàn thành quest!");
//            return;
//        }

//        QuestSystem.Instance.CompleteQuest(currentQuest, inventory);
//    }
//}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestSlotUI : MonoBehaviour
{
    [SerializeField] private Text questNameText;
    [SerializeField] private Text questDescriptionText;
    [SerializeField] private Text requirementsText;
    [SerializeField] private Button completeButton;
    [SerializeField] private Text rewardText;

    private QuestConfig.Quest currentQuest;
    private RecyclableInventory inventory;

    public void SetQuest(QuestConfig.Quest quest, RecyclableInventory inv)
    {
        currentQuest = quest;
        inventory = inv;

        gameObject.SetActive(true);

        questNameText.text = quest.questName;
        questDescriptionText.text = quest.questDescription;
        rewardText.text = $"⭐ +{quest.rewardExperience} XP | 💰 +{quest.rewardGold}";
        

        // ✅ Debug: Kiểm tra requirements
        Debug.Log($"🎯 Setting quest: {quest.questName}");
        Debug.Log($"   Requirements count: {(quest.requirements?.Count ?? 0)}");

        // ✅ Hiển thị yêu cầu
        string reqText = "Yêu cầu:\n";

        if (quest.requirements != null && quest.requirements.Count > 0)
        {
            foreach (var req in quest.requirements)
            {
                int have = inventory.GetItemQuantity(req.itemName);
                reqText += $"• {req.itemDescription} x{req.quantity} ({have}/{req.quantity})\n";

                // Debug từng requirement
                Debug.Log($"   ├─ Item: {req.itemName}, Need: {req.quantity}, Have: {have}");
            }
        }
        else
        {
            reqText += "Không có yêu cầu";
            Debug.LogWarning("⚠️ Quest không có requirements!");
        }

        if (requirementsText != null)
        {
            requirementsText.text = reqText;
            Debug.Log($"✅ requirementsText updated");
        }
        else
        {
            Debug.LogError("❌ requirementsText is NULL!");
        }

        // ✅ Update button
        if (completeButton != null)
        {
            completeButton.onClick.RemoveAllListeners();
            completeButton.onClick.AddListener(OnCompleteButtonClicked);
            completeButton.interactable = QuestSystem.Instance.CanCompleteQuest(quest, inventory);
        }
    }

    public void HideQuest()
    {
        gameObject.SetActive(false);
    }

    private void OnCompleteButtonClicked()
    {
        if (currentQuest == null) return;

        if (!QuestSystem.Instance.CanCompleteQuest(currentQuest, inventory))
        {
            Debug.Log("❌ Không đủ items để hoàn thành quest!");
            return;
        }

        QuestSystem.Instance.CompleteQuest(currentQuest, inventory);
    }
}
