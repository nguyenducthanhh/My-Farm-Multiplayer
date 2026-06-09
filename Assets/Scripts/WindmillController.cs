using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using UnityEngine;
using UnityEngine.UI;

public class WindmillController : MonoBehaviour
{
    [Header("Windmill Settings")]
    [SerializeField] private string millId = "windmill_1";
    [SerializeField] private int grainRequiredPerMill = 2;             
    [SerializeField] private string grainItemType = "Paddy";         
    [SerializeField] private float millingDuration = 180f;           
    [SerializeField] private string flourProductType = "flour";       

    [Header("UI References")]
    [SerializeField] private Button millButton;
    [SerializeField] private Button collectButton;
    [SerializeField] private GameObject uiPanel;

    [Header("Player References")]
    [SerializeField] private RecyclableInventory playerInventory;

    private bool playerInRange = false;
    private bool isMilling = false;
    private bool canCollect = false;
    private DateTime nextMillingTime = DateTime.MinValue;

    [System.Serializable]
    public class MillData
    {
        public string millId;
        public bool isMilling;
        public long nextMillingTimeTicks;
        public bool canCollect;
    }

    private void Start()
    {
        if (millButton != null)
            millButton.onClick.AddListener(OnMillButtonClicked);

        if (collectButton != null)
            collectButton.onClick.AddListener(OnCollectButtonClicked);

        uiPanel?.SetActive(false);

        LoadMillDataFromFirebase();
        CheckMillingStatus();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            ShowUI();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            HideUI();
        }
    }

    private void Update()
    {
        if (!playerInRange) return;

        UpdateUIStatus();

        if (isMilling && DateTime.UtcNow >= nextMillingTime)
        {
            isMilling = false;
            canCollect = true;

            SaveMillDataToFirebase();
            UpdateUIStatus();
        }
    }

    private void ShowUI()
    {
        if (uiPanel != null)
            uiPanel.SetActive(true);

        UpdateUIStatus();
    }

    private void HideUI()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);
    }

    private void UpdateUIStatus()
    {
        if (canCollect)
        {
            if (millButton != null)
                millButton.gameObject.SetActive(false);

            if (collectButton != null)
                collectButton.gameObject.SetActive(true);
        }
        else if (isMilling)
        {
            if (millButton != null)
                millButton.gameObject.SetActive(false);

            if (collectButton != null)
                collectButton.gameObject.SetActive(false);
        }
        else
        {
            if (millButton != null)
                millButton.gameObject.SetActive(true);

            if (collectButton != null)
                collectButton.gameObject.SetActive(false);
        }
    }

    private void OnMillButtonClicked()
    {
        if (!CanMill())
        {
            Debug.Log($" Không đủ {grainItemType}! Cần {grainRequiredPerMill}, hiện có {GetGrainQuantity()}");
            NotificationManager.ShowReward($"Không đủ Lúa! Cần {grainRequiredPerMill}, hiện có {GetGrainQuantity()}");
            return;
        }

        playerInventory.RemoveInventoryItem($"{grainItemType}_fruit", grainRequiredPerMill);

        isMilling = true;
        canCollect = false;
        nextMillingTime = DateTime.UtcNow.AddSeconds(millingDuration);

        Debug.Log($" Cối xay đang xay {grainRequiredPerMill} lúa, sẽ xong sau {millingDuration}s");

        SaveMillDataToFirebase();
        UpdateUIStatus();
    }

    private void OnCollectButtonClicked()
    {
        if (!canCollect)
        {
            Debug.Log(" Chưa có bột mì để thu thập!");
            return;
        }

        string flourItemName = $"{flourProductType}_item";
        string flourDescription = "Bột mì";

        InventoryItems flour = new InventoryItems(
            flourItemName,
            flourDescription,
            1
        );

        playerInventory.AddInventoryItem(flour);

        canCollect = false;
        isMilling = false;

        Debug.Log($" Thu thập được 1 bột mì");
        NotificationManager.ShowReward("Bạn thu thập được 1 bột mì!");
        SaveMillDataToFirebase();
        UpdateUIStatus();
    }

    private bool CanMill()
    {
        if (playerInventory == null) return false;

        string grainItemName = $"{grainItemType}_fruit";
        int quantity = playerInventory.GetItemQuantity(grainItemName);

        return quantity >= grainRequiredPerMill;
    }

    private int GetGrainQuantity()
    {
        if (playerInventory == null) return 0;

        string grainItemName = $"{grainItemType}_fruit";
        return playerInventory.GetItemQuantity(grainItemName);
    }

    private void CheckMillingStatus()
    {
        if (isMilling && DateTime.UtcNow >= nextMillingTime)
        {
            isMilling = false;
            canCollect = true;
        }
    }

    private void LoadMillDataFromFirebase()
    {
        if (LoadDataManager.firebaseUser == null)
        {
            Debug.LogError("Firebase user not logged in!");
            return;
        }

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Windmills")
            .Child(millId)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Value != null)
                {
                    try
                    {
                        string json = task.Result.GetRawJsonValue();
                        var millData = JsonConvert.DeserializeObject<MillData>(json);

                        if (millData != null)
                        {
                            isMilling = millData.isMilling;
                            nextMillingTime = new DateTime(millData.nextMillingTimeTicks, DateTimeKind.Utc);
                            canCollect = millData.canCollect;

                            Debug.Log($" Loaded mill data: isMilling={isMilling}, canCollect={canCollect}");

                            CheckMillingStatus();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error loading mill data: {e.Message}");
                    }
                }
                else
                {
                    Debug.Log($"No mill data found for {millId}");
                }
            });
    }

    private void SaveMillDataToFirebase()
    {
        if (LoadDataManager.firebaseUser == null)
        {
            Debug.LogError("Firebase user not logged in!");
            return;
        }

        var millData = new MillData
        {
            millId = this.millId,
            isMilling = this.isMilling,
            nextMillingTimeTicks = this.nextMillingTime.Ticks,
            canCollect = this.canCollect
        };

        string json = JsonConvert.SerializeObject(millData);

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Windmills")
            .Child(millId)
            .SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($" Saved mill data: {millId}");
                }
                else
                {
                    Debug.LogError($"Failed to save mill data: {task.Exception}");
                }
            });
    }
}
