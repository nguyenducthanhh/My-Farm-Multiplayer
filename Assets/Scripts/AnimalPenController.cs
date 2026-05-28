using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimalPenController : MonoBehaviour
{
    [Header("Animal Pen Settings")]
    [SerializeField] private string penId;
    [SerializeField] private AnimalType animalType;
    [SerializeField] private int feedQuantityRequired = 1;
    [SerializeField] private string feedItemType = "paddy";
    [SerializeField] private float feedDuration = 60f;
    [SerializeField] private string productItemType = "egg";

    [Header("Animation - Multiple Animals")]
    [SerializeField] private List<Animator> animalAnimators = new List<Animator>();  // ← List of Animators
    [SerializeField] private string isEatingParameterName = "IsEating";

    [Header("UI References")]
    [SerializeField] private Button feedButton;
    [SerializeField] private Button collectButton;
    [SerializeField] private GameObject uiPanel;

    [Header("Player References")]
    [SerializeField] private RecyclableInventory playerInventory;

    [Header("Lock Overlay Squares")]
    [SerializeField] private GameObject lockOverlay1;  // Square lock phía trên trái
    [SerializeField] private GameObject lockOverlay2;
    [SerializeField] private GameObject lockOverlay3;
    private bool playerInRange = false;
    private bool isFeeding = false;
    private bool canCollect = false;
    private DateTime nextFeedTime = DateTime.MinValue;
    private bool penUnlocked = false;
    public enum AnimalType
    {
        Chicken,
        Pig,
        Cow
    }

    [System.Serializable]
    public class PenData
    {
        public string penId;
        public AnimalType animalType;
        public bool isFeeding;
        public long nextFeedTimeTicks;
        public bool canCollect;
    }

    private void Start()
    {
        // ✅ DELAY: Chờ LevelSystem load xong trước khi kiểm tra
        StartCoroutine(CheckPenUnlockAfterDelay());

        if (feedButton != null)
            feedButton.onClick.AddListener(OnFeedButtonClicked);

        if (collectButton != null)
            collectButton.onClick.AddListener(OnCollectButtonClicked);

        uiPanel?.SetActive(false);

        // ✅ Auto-find Animators nếu list trống
        if (animalAnimators.Count == 0)
        {
            AutoFindAnimators();
        }

        // ✅ Kiểm tra Animators
        if (animalAnimators.Count == 0)
        {
            Debug.LogError($"❌ No Animators found on {gameObject.name}!");
        }
        else
        {
            Debug.Log($"✅ Found {animalAnimators.Count} Animators");
        }

        LoadPenDataFromFirebase();
        CheckFeedingStatus();

        SetAllAnimalsState(false);
    }

    // ✅ THÊM: Coroutine chờ LevelSystem load xong
    // ✅ THÊM: Coroutine chờ LevelSystem load xong
    private IEnumerator CheckPenUnlockAfterDelay()
    {
        // Chờ 0.5 giây để LevelSystem load xong
        yield return new WaitForSeconds(0.5f);

        string penUnlockName = GetPenUnlockName();
        Debug.Log($"🐷 Checking pen unlock: {penUnlockName}");

        if (LevelSystem.Instance == null)
        {
            Debug.LogError("❌ LevelSystem.Instance is NULL!");
            yield break;
        }

        if (!LevelSystem.Instance.IsItemUnlocked(penUnlockName))
        {
            int requiredLevel = LevelSystem.Instance.GetRequiredLevelForItem(penUnlockName);
            Debug.Log($"🔒 Chuồng chưa mở khóa! Cần cấp {requiredLevel}");

            // ✅ Disable chuồng
            ShowLockOverlays();
            penUnlocked = false;
            //gameObject.SetActive(false);
            yield break;  // ← Thay return bằng yield break
        }

        Debug.Log($"✅ Chuồng '{penUnlockName}' đã mở khóa!");
        HideLockOverlays();
        penUnlocked = true;
    }
    private void ShowLockOverlays()
    {
        if (lockOverlay1 != null)
            lockOverlay1.SetActive(true);

        if (lockOverlay2 != null)
            lockOverlay2.SetActive(true);
        if (lockOverlay3 != null)
            lockOverlay3.SetActive(true);

        Debug.Log("🔒 Lock overlays shown");
    }

    // ✅ THÊM: Ẩn lock overlays
    private void HideLockOverlays()
    {
        if (lockOverlay1 != null)
            lockOverlay1.SetActive(false);

        if (lockOverlay2 != null)
            lockOverlay2.SetActive(false);

        if (lockOverlay3 != null)
            lockOverlay3.SetActive(false);
        Debug.Log("🔓 Lock overlays hidden");
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
        UpdatePenUnlockStatus();

        // ✅ LUÔN kiểm tra xem ăn xong chưa (bất kể player ở đâu)
        if (isFeeding && DateTime.UtcNow >= nextFeedTime)
        {
            isFeeding = false;
            canCollect = true;

            // ✅ Set tất cả con vật về Sleeping ngay (không cần player ở gần)
            SetAllAnimalsState(false);

            SavePenDataToFirebase();
        }

        // ✅ CHỈ update UI khi player ở gần
        if (playerInRange)
        {
            UpdateUIStatus();
        }
    }

    private void UpdatePenUnlockStatus()
    {
        if (LevelSystem.Instance == null)
            return;

        string penUnlockName = GetPenUnlockName();
        bool isCurrentlyUnlocked = LevelSystem.Instance.IsItemUnlocked(penUnlockName);

        // Nếu vừa mở khóa (từ false → true)
        if (isCurrentlyUnlocked && !penUnlocked)
        {
            Debug.Log($"🎉 {penUnlockName} vừa được mở khóa!");
            penUnlocked = true;
            HideLockOverlays();
        }
        // Nếu vừa bị khóa lại (từ true → false)
        else if (!isCurrentlyUnlocked && penUnlocked)
        {
            Debug.Log($"🔒 {penUnlockName} bị khóa lại!");
            penUnlocked = false;
            ShowLockOverlays();
        }
    }

    private string GetPenUnlockName()
    {
        return penId;
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
        // ✅ THÊM: Kiểm tra xem chuồng đã mở khóa chưa
        if (!penUnlocked)
        {
            // ❌ Chưa mở khóa - ẩn tất cả button
            if (feedButton != null)
                feedButton.gameObject.SetActive(false);

            if (collectButton != null)
                collectButton.gameObject.SetActive(false);

            Debug.Log("🔒 Buttons hidden - Pen not unlocked");
            return;  // ← Dừng lại, không tiếp tục
        }

        // ✅ Đã mở khóa - tiếp tục logic bình thường
        if (canCollect)
        {
            if (feedButton != null)
                feedButton.gameObject.SetActive(false);

            if (collectButton != null)
                collectButton.gameObject.SetActive(true);
        }
        else if (isFeeding)
        {
            float timeRemaining = (float)(nextFeedTime - DateTime.UtcNow).TotalSeconds;
            timeRemaining = Mathf.Max(0, timeRemaining);

            if (feedButton != null)
                feedButton.gameObject.SetActive(false);

            if (collectButton != null)
                collectButton.gameObject.SetActive(false);
        }
        else
        {
            if (feedButton != null)
                feedButton.gameObject.SetActive(true);

            if (collectButton != null)
                collectButton.gameObject.SetActive(false);
        }
    }

    private void OnFeedButtonClicked()
    {

        // ✅ Kiểm tra xem chuồng đã mở khóa chưa (QUAN TRỌNG)
        if (!penUnlocked)
        {
            string penUnlockName = GetPenUnlockName();
            int requiredLevel = LevelSystem.Instance.GetRequiredLevelForItem(penUnlockName);
            int currentLevel = LevelSystem.Instance.GetCurrentLevel();

            Debug.LogWarning($"❌ Chuồng chưa mở khóa! Cần cấp {requiredLevel}, hiện tại cấp {currentLevel}");
            return;  // ← KHÔNG cho ăn nếu chưa mở khóa
        }

        // ✅ Kiểm tra xem có đủ thức ăn không
        if (!CanFeed())
        {
            Debug.Log($"❌ Không đủ {GetFeedItemName()}!");
            return;
        }

        playerInventory.RemoveInventoryItem($"{feedItemType}_fruit", feedQuantityRequired);

        isFeeding = true;
        canCollect = false;
        nextFeedTime = DateTime.UtcNow.AddSeconds(feedDuration);

        SetAllAnimalsState(true);

        Debug.Log($"✅ {GetAnimalName()} bắt đầu ăn, sẽ xong sau {feedDuration}s");

        SavePenDataToFirebase();
        UpdateUIStatus();
    }

    private void OnCollectButtonClicked()
    {
 
        // ✅ Kiểm tra xem chuồng đã mở khóa chưa (QUAN TRỌNG)
        if (!penUnlocked)
        {
            string penUnlockName = GetPenUnlockName();
            int requiredLevel = LevelSystem.Instance.GetRequiredLevelForItem(penUnlockName);
            int currentLevel = LevelSystem.Instance.GetCurrentLevel();

            Debug.LogWarning($"❌ Chuồng chưa mở khóa! Cần cấp {requiredLevel}, hiện tại cấp {currentLevel}");
            return;  // ← KHÔNG thu hoạch nếu chưa mở khóa
        }

        if (!canCollect)
        {
            Debug.Log("❌ Chưa có sản phẩm để thu thập!");
            return;
        }

        string productName = $"{productItemType}_item";
        string productDescription = GetProductDescription();

        InventoryItems product = new InventoryItems(
            productName,
            productDescription,
            1
        );

        playerInventory.AddInventoryItem(product);

        canCollect = false;
        isFeeding = false;

        SetAllAnimalsState(false);

        Debug.Log($"✅ Thu thập được 1 {productDescription}");

        SavePenDataToFirebase();
        UpdateUIStatus();
    }

    // ✅ THÊM: Auto-find Animators từ children
    private void AutoFindAnimators()
    {
        var animators = GetComponentsInChildren<Animator>();

        Debug.Log($"🔍 Found {animators.Length} Animators in children");

        animalAnimators.Clear();

        foreach (var animator in animators)
        {
            // Bỏ qua Animator của chính chuồng (nếu có)
            if (animator.gameObject != gameObject)
            {
                animalAnimators.Add(animator);
                Debug.Log($"✅ Added animator: {animator.gameObject.name}");
            }
        }

        Debug.Log($"📊 Total animals: {animalAnimators.Count}");
    }

    // ✅ THÊM: Set animation state cho TẤT CẢ con vật
    private void SetAllAnimalsState(bool isEating)
    {
        if (animalAnimators == null || animalAnimators.Count == 0)
        {
            Debug.LogError("❌ No Animators in list!");
            return;
        }

        Debug.Log($"🎬 SetAllAnimalsState: IsEating = {isEating} for {animalAnimators.Count} animals");

        foreach (var animator in animalAnimators)
        {
            if (animator != null)
            {
                animator.SetBool(isEatingParameterName, isEating);
                Debug.Log($"   ✅ {animator.gameObject.name}: IsEating = {isEating}");
            }
            else
            {
                Debug.LogWarning("❌ Animator is NULL in list!");
            }
        }
    }

    private bool CanFeed()
    {
        if (playerInventory == null) return false;

        string feedItemName = $"{feedItemType}_fruit";
        int quantity = playerInventory.GetItemQuantity(feedItemName);

        return quantity >= feedQuantityRequired;
    }

    private void CheckFeedingStatus()
    {
        if (isFeeding && DateTime.UtcNow >= nextFeedTime)
        {
            isFeeding = false;
            canCollect = true;
        }
    }

    private void LoadPenDataFromFirebase()
    {
        if (LoadDataManager.firebaseUser == null)
        {
            Debug.LogError("Firebase user not logged in!");
            return;
        }

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("AnimalPens")
            .Child(penId)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Value != null)
                {
                    try
                    {
                        string json = task.Result.GetRawJsonValue();
                        var penData = JsonConvert.DeserializeObject<PenData>(json);

                        if (penData != null)
                        {
                            isFeeding = penData.isFeeding;
                            nextFeedTime = new DateTime(penData.nextFeedTimeTicks, DateTimeKind.Utc);
                            canCollect = penData.canCollect;

                            Debug.Log($"✅ Loaded pen data: isFeeding={isFeeding}, canCollect={canCollect}");

                            CheckFeedingStatus();

                            // ✅ Set animation based on state
                            SetAllAnimalsState(isFeeding);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error loading pen data: {e.Message}");
                    }
                }
                else
                {
                    Debug.Log($"No pen data found for {penId}");
                }
            });
    }

    private void SavePenDataToFirebase()
    {
        if (LoadDataManager.firebaseUser == null)
        {
            Debug.LogError("Firebase user not logged in!");
            return;
        }

        var penData = new PenData
        {
            penId = this.penId,
            animalType = this.animalType,
            isFeeding = this.isFeeding,
            nextFeedTimeTicks = this.nextFeedTime.Ticks,
            canCollect = this.canCollect
        };

        string json = JsonConvert.SerializeObject(penData);

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("AnimalPens")
            .Child(penId)
            .SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"✅ Saved pen data: {penId}");
                }
                else
                {
                    Debug.LogError($"Failed to save pen data: {task.Exception}");
                }
            });
    }

    private string GetAnimalName()
    {
        return animalType switch
        {
            AnimalType.Chicken => "Gà",
            AnimalType.Pig => "Lợn",
            AnimalType.Cow => "Bò",
            _ => "Động vật"
        };
    }

    private string GetFeedItemName()
    {
        return feedItemType switch
        {
            "paddy" => "Lúa",
            "corn" => "Ngô",
            _ => "Thức ăn"
        };
    }

    private string GetProductDescription()
    {
        return productItemType switch
        {
            "egg" => "Trứng gà",
            "meat" => "Thịt lợn",
            "milk" => "Sữa bò",
            _ => "Sản phẩm"
        };
    }
}
