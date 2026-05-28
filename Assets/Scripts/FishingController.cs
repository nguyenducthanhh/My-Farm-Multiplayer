using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class FishingController : MonoBehaviour
{
    [Header("Fishing Settings")]
    [SerializeField] private string fishingSpotId = "bridge_1";
    [SerializeField] private float fishingDuration = 10f;              // 10 giây
    [SerializeField] private int baitRequiredPerFish = 1;              // 1 mồi/lần

    [Header("Animation")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string castAnimationName = "CastTrigger";        // Thả câu
    [SerializeField] private string reelAnimationName = "ReelTrigger";        // Giật cá

    [Header("Fish Catch Rates")]
    [SerializeField] private List<FishCatchData> fishCatchRates = new List<FishCatchData>();

    [Header("UI References")]
    [SerializeField] private Button fishButton;
    [SerializeField] private Button reelButton;        
    //[SerializeField] private Text statusText;
    [SerializeField] private GameObject fishingUIPanel;

    [Header("Player References")]
    [SerializeField] private RecyclableInventory playerInventory;
    [SerializeField] private Transform playerTransform;

    private bool isOnBridge = false;
    private bool isFishing = false;
    private DateTime fishingEndTime = DateTime.MinValue;
    private bool canReel = false;

    [System.Serializable]
    public class FishCatchData
    {
        public string fishType;      // "crab", "lobster", etc.
        public float catchRate;  // 50% tỉ lệ câu được
    }

    [System.Serializable]
    public class FishingData
    {
        public string fishingSpotId;
        public bool isFishing;
        public long fishingEndTimeTicks;
        public bool canReel;
    }

    private void Start()
    {
        if (fishButton != null)
            fishButton.onClick.AddListener(OnFishButtonClicked);
        if (reelButton != null)
            reelButton.onClick.AddListener(OnReelButtonClicked);

        fishingUIPanel?.SetActive(false);

        if (playerAnimator == null)
            playerAnimator = playerTransform?.GetComponent<Animator>();

        LoadFishingDataFromFirebase();
    }

    private void Update()
    {
        if (!isOnBridge) return;

        UpdateUIStatus();

        if (isFishing && DateTime.UtcNow >= fishingEndTime)
        {
            isFishing = false;
            canReel = true;

            SaveFishingDataToFirebase();
            UpdateUIStatus();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isOnBridge = true;
            Debug.Log("🎣 Player entered fishing area");
            ShowFishingUI();
        }
    }

    // ✅ THÊM: OnTriggerExit2D
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isOnBridge = false;
            Debug.Log("🎣 Player left fishing area");
            HideFishingUI();

            if (isFishing)
            {
                CancelFishing();
            }
        }
    }
    private void ShowFishingUI()
    {
        if (fishingUIPanel != null)
            fishingUIPanel.SetActive(true);

        UpdateUIStatus();
    }

    private void HideFishingUI()
    {
        if (fishingUIPanel != null)
            fishingUIPanel.SetActive(false);
    }

    private void UpdateUIStatus()
    {
        if (canReel)
        {
            // ✅ Sẵn sàng thu cần - hiển thị nút "Thu cần"
            fishButton?.gameObject.SetActive(false);
            reelButton?.gameObject.SetActive(true);

            //if (statusText != null)
            //    statusText.text = "🎣 Thu cần!";
        }
        else if (isFishing)
        {
            // ✅ Đang câu cá
            float timeRemaining = (float)(fishingEndTime - DateTime.UtcNow).TotalSeconds;
            timeRemaining = Mathf.Max(0, timeRemaining);

            fishButton?.gameObject.SetActive(false);
            reelButton?.gameObject.SetActive(false);

            //if (statusText != null)
            //    statusText.text = "⏳ Đang câu cá: {timeRemaining:F0}s";
        }
        else
        {
            // ✅ Sẵn sàng để câu
            fishButton?.gameObject.SetActive(true);
            reelButton?.gameObject.SetActive(false);

            //if (statusText != null)
            //    statusText.text = "🎣 Nhấn để câu cá";
        }
    }

    private void OnFishButtonClicked()
    {
        StartFishing();
    }
    private void OnReelButtonClicked()
    {
        ReelAndCollectFish();
    }
    private void StartFishing()
    {
        // ✅ Kiểm tra có đủ mồi không
        if (!HasBait())
        {
            Debug.Log("❌ Không có mồi câu!");
            return;
        }

        // ✅ Trừ mồi
        playerInventory.RemoveInventoryItem("worm_bait", baitRequiredPerFish);

        // ✅ Bắt đầu câu cá
        isFishing = true;
        canReel = false;
        fishingEndTime = DateTime.UtcNow.AddSeconds(fishingDuration);

        // ✅ Disable player movement
        SetPlayerMovementEnabled(false);

        // ✅ THÊM: Set Speed = 0 để Animator không chạy Move animation
        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", 0f);
            Debug.Log("🎬 Set Speed = 0");
        }

        // ✅ Thực hiện animation thả câu
        PlayCastAnimation();
        AudioManager.Instance.PlayCastSound();

        Debug.Log($"✅ Bắt đầu câu cá, sẽ xong sau {fishingDuration}s");

        SaveFishingDataToFirebase();
        UpdateUIStatus();
    }

    private void ReelAndCollectFish()
    {
        if (!canReel)
        {
            Debug.Log("❌ Chưa đến lúc thu cần!");
            return;
        }

        canReel = false;

        // ✅ Thực hiện animation thu cần
        PlayReelAnimation();
        AudioManager.Instance.PlayReelSound();

        // ✅ Random cá
        string caughtFish = GetRandomFish();

        if (string.IsNullOrEmpty(caughtFish))
        {
            Debug.Log("❌ Câu không được cá!");
            isFishing = false;
            SetPlayerMovementEnabled(true);

            SaveFishingDataToFirebase();
            UpdateUIStatus();
            return;
        }

        // ✅ Thêm cá vào inventory
        InventoryItems fish = new InventoryItems(
            caughtFish,
            playerInventory.itemDatabase.GetDescription(caughtFish),
            1
        );

        playerInventory.AddInventoryItem(fish);

        isFishing = false;
        SetPlayerMovementEnabled(true);


        SetPlayerMovementEnabled(true);

        Debug.Log($"✅ Thu thập được 1 {playerInventory.itemDatabase.GetDescription(caughtFish)}");
        NotificationManager.ShowReward($"Câu được: {playerInventory.itemDatabase.GetDescription(caughtFish)}");
        SaveFishingDataToFirebase();
        UpdateUIStatus();
    }


    private void CancelFishing()
    {
        isFishing = false;
        canReel = false;
        SetPlayerMovementEnabled(true);
        Debug.Log("❌ Hủy câu cá");
    }

    private bool HasBait()
    {
        return playerInventory.GetItemQuantity("worm_bait") >= baitRequiredPerFish;
    }

    private string GetRandomFish()
    {
        // ✅ Tính tổng tỉ lệ
        float totalRate = 100;
        //foreach (var fish in fishCatchRates)
        //{
        //    totalRate += fish.catchRate;
        //}

        // ✅ Random số từ 0 đến totalRate
        float randomValue = UnityEngine.Random.Range(0, totalRate);
        Debug.Log($"🎲 Random value for fishing: {randomValue:F2} (Total Rate: {totalRate:F2})");
        // ✅ Tìm loại cá dựa trên random value
        float currentRate = 0;
        foreach (var fish in fishCatchRates)
        {
            currentRate += fish.catchRate;
            if (randomValue <= currentRate)
            {
                Debug.Log($"🎣 Câu được: {fish.fishType} (Random: {randomValue:F2}/{totalRate:F2})");
                return fish.fishType;
            }
        }

        return null;
    }

    private void PlayCastAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(castAnimationName);
            Debug.Log($"🎬 Playing cast animation: {castAnimationName}");
        }
    }

    private void PlayReelAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(reelAnimationName);
            Debug.Log($"🎬 Playing reel animation: {reelAnimationName}");
        }
    }

    private void SetPlayerMovementEnabled(bool enabled)
    {
        // ✅ THÊM: Debug để kiểm tra
        if (playerTransform == null)
        {
            Debug.LogError("❌ playerTransform is NULL!");
            return;
        }

        var playerMovement = playerTransform.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError($"❌ PlayerMovement component not found on {playerTransform.gameObject.name}!");
            return;
        }

        playerMovement.enabled = enabled;
        Debug.Log($"🚶 PlayerMovement: {(enabled ? "ENABLED ✅" : "DISABLED ❌")}");
    }

    private void LoadFishingDataFromFirebase()
    {
        if (LoadDataManager.firebaseUser == null) return;

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Fishing")
            .Child(fishingSpotId)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Value != null)
                {
                    try
                    {
                        string json = task.Result.GetRawJsonValue();
                        var fishingData = JsonConvert.DeserializeObject<FishingData>(json);

                        if (fishingData != null)
                        {
                            isFishing = fishingData.isFishing;
                            fishingEndTime = new DateTime(fishingData.fishingEndTimeTicks, DateTimeKind.Utc);
                            canReel = fishingData.canReel;

                            Debug.Log($"✅ Loaded fishing data");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error loading fishing data: {e.Message}");
                    }
                }
            });
    }

    private void SaveFishingDataToFirebase()
    {
        if (LoadDataManager.firebaseUser == null) return;

        var fishingData = new FishingData
        {
            fishingSpotId = this.fishingSpotId,
            isFishing = this.isFishing,
            fishingEndTimeTicks = this.fishingEndTime.Ticks,
            canReel = this.canReel
        };

        string json = JsonConvert.SerializeObject(fishingData);

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Fishing")
            .Child(fishingSpotId)
            .SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompleted)
                    Debug.LogError($"Failed to save fishing data: {task.Exception}");
            });
    }
}
