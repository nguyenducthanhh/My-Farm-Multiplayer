using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuestUI : MonoBehaviour
{
    //[SerializeField] private GameObject questSlotPrefab;
    //[SerializeField] private Transform questContainer;
    [SerializeField] private RecyclableInventory playerInventory;

    private List<QuestSlotUI> questSlots = new List<QuestSlotUI>();

    private void Start()
    {
        //// ✅ Tạo 4 slot UI
        //for (int i = 0; i < 4; i++)
        //{
        //    //GameObject slotGO = Instantiate(questSlotPrefab, questContainer);
        //    QuestSlotUI slotUI = slotGO.GetComponent<QuestSlotUI>();
        //    questSlots.Add(slotUI);
        //}

        QuestSystem.OnQuestCompleted += OnQuestCompleted;

        UpdateQuestDisplay();
    }

    private void Update()
    {
        UpdateQuestDisplay();
    }

    private void UpdateQuestDisplay()
    {
        var activeQuests = new List<QuestConfig.Quest>(QuestSystem.Instance.GetActiveQuests());

        for (int i = 0; i < questSlots.Count; i++)
        {
            if (i < activeQuests.Count)
            {
                questSlots[i].SetQuest(activeQuests[i], playerInventory);
            }
            else
            {
                questSlots[i].HideQuest();
            }
        }
    }

    private void OnQuestCompleted(QuestConfig.Quest quest)
    {
        Debug.Log($"✅ Quest completed: {quest.questName}");
        UpdateQuestDisplay();
    }

    private void OnDestroy()
    {
        QuestSystem.OnQuestCompleted -= OnQuestCompleted;
    }
}