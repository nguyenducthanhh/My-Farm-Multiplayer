using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NpcQuestGiver : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject questMenu;          
    [SerializeField] private Button interactButton;        
    [SerializeField] private Button closeMenuButton;       
    [Header("Quest Container")]
    [SerializeField] private GameObject questSlotPrefab;   
    [SerializeField] private Transform questContainer;     

    [Header("Player References")]
    [SerializeField] private RecyclableInventory playerInventory;

    private bool playerInRange = false;
    private List<QuestSlotUI> questSlots = new List<QuestSlotUI>();

    private void Start()
    {
        if (closeMenuButton != null)
            closeMenuButton.onClick.AddListener(CloseQuestMenu);

        questMenu?.SetActive(false);

        CreateQuestSlots();

        Debug.Log(" NpcQuestGiver initialized");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerInRange = true;
            interactButton.gameObject.SetActive(true);
            Debug.Log(" Player entered quest giver area");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerInRange = false;
            interactButton.gameObject.SetActive(false);
            CloseQuestMenu();
            Debug.Log(" Player left quest giver area");
        }
    }

    public void OpenQuestMenu()
    {
        if (!playerInRange) return;

        questMenu.SetActive(true);
        UpdateQuestDisplay();
        Debug.Log(" Opening quest menu");
    }

    public void CloseQuestMenu()
    {
        questMenu.SetActive(false);
        Debug.Log(" Closing quest menu");
    }

    private void CreateQuestSlots()
    {
        if (questSlotPrefab == null || questContainer == null)
        {
            Debug.LogError(" Quest slot prefab or container not assigned!");
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            GameObject slotGO = Instantiate(questSlotPrefab, questContainer);
            slotGO.name = $"QuestSlot_{i}";

            QuestSlotUI slotUI = slotGO.GetComponent<QuestSlotUI>();
            if (slotUI != null)
            {
                questSlots.Add(slotUI);
                Debug.Log($" Created quest slot {i}");
            }
            else
            {
                Debug.LogError($" QuestSlotUI component not found on prefab!");
            }
        }
    }

    private void Update()
    {
        if (playerInRange && questMenu.activeInHierarchy)
        {
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($" Active quests: {QuestSystem.Instance.GetActiveQuests().Count}");
            }
            UpdateQuestDisplay();
        }
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
}