using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NpcChefController : MonoBehaviour
{
    [Header("Chef Settings")]
    [SerializeField] private string chefId = "chef_1";

    [Header("Recipes")]
    [SerializeField] private List<RecipeButton> recipeButtons = new List<RecipeButton>();

    [Header("UI References")]
    [SerializeField] private Button interactButton;
    [SerializeField] private GameObject chefMenuPanel;
    [SerializeField] private Button closeMenuButton;      // ← THÊM: Nút đóng menu
    [SerializeField] private Text goldDisplayText;        // ← THÊM: Text hiển thị gold

    [Header("Player References")]
    [SerializeField] private RecyclableInventory playerInventory;

    private bool playerInRange = false;
    private Dictionary<RecipeButton, CookingSlot> cookingSlots = new Dictionary<RecipeButton, CookingSlot>();

    [System.Serializable]
    public class RecipeButton
    {
        public string dishName;                           // "Thịt lợn nướng"
        public string dishItemName;                       // "grilled_pork" (KHÔNG cần _item)
        public List<IngredientRequirement> ingredients = new List<IngredientRequirement>();
        public float cookingDuration;
        public Button cookButton;                         // Button "Nấu"
        public Button collectButton;                      // Button "Thu thập"
        public Text statusText;                           // Text hiển thị trạng thái
    }

    [System.Serializable]
    public class IngredientRequirement
    {
        public string itemName;
        public int quantity = 1;
    }

    [System.Serializable]
    public class CookingSlot
    {
        public RecipeButton recipe;
        public bool isCooking;
        public long nextCookingTimeTicks;
        public bool canCollect;
    }

    [System.Serializable]
    public class ChefData
    {
        public string chefId;
        public List<CookingSlotData> cookingSlots = new List<CookingSlotData>();
    }

    [System.Serializable]
    public class CookingSlotData
    {
        public int recipeIndex;
        public bool isCooking;
        public long nextCookingTimeTicks;
        public bool canCollect;
    }

    private void Start()
    {
        if (interactButton != null)
            interactButton.onClick.AddListener(OpenChefMenu);

        // ✅ THÊM: Nút đóng menu
        if (closeMenuButton != null)
            closeMenuButton.onClick.AddListener(CloseChefMenu);

        chefMenuPanel?.SetActive(false);

        // ✅ Khởi tạo cooking slots và button listeners
        for (int i = 0; i < recipeButtons.Count; i++)
        {
            var recipe = recipeButtons[i];
            cookingSlots[recipe] = new CookingSlot { recipe = recipe };

            // ✅ Gắn listeners cho buttons
            if (recipe.cookButton != null)
            {
                int index = i; // Capture for closure
                recipe.cookButton.onClick.AddListener(() => OnCookButtonClicked(index));
            }

            if (recipe.collectButton != null)
            {
                int index = i;
                recipe.collectButton.onClick.AddListener(() => OnCollectButtonClicked(index));
            }
        }

        LoadChefDataFromFirebase();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            if (interactButton != null)
                interactButton.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactButton != null)
                interactButton.gameObject.SetActive(false);
            CloseChefMenu();
        }
    }

    private void Update()
    {
        if (!playerInRange) return;

        // ✅ Kiểm tra tất cả recipes đang nấu
        foreach (var recipe in recipeButtons)
        {
            if (cookingSlots[recipe].isCooking && DateTime.UtcNow >= new DateTime(cookingSlots[recipe].nextCookingTimeTicks, DateTimeKind.Utc))
            {
                cookingSlots[recipe].isCooking = false;
                cookingSlots[recipe].canCollect = true;

                SaveChefDataToFirebase();
            }
        }

        // ✅ Cập nhật UI mỗi frame
        if (chefMenuPanel != null && chefMenuPanel.activeInHierarchy)
        {
            RefreshUI();
        }
    }

    private void OpenChefMenu()
    {
        if (playerInRange && chefMenuPanel != null)
        {
            chefMenuPanel.SetActive(true);
            RefreshUI();
            UpdateGoldDisplay();  // ← THÊM: Cập nhật gold khi mở menu
        }
    }

    private void CloseChefMenu()
    {
        if (chefMenuPanel != null)
            chefMenuPanel.SetActive(false);
    }

    // ✅ THÊM: Method cập nhật gold display
    private void UpdateGoldDisplay()
    {
        if (goldDisplayText != null && LoadDataManager.userInGame != null)
        {
            goldDisplayText.text = $"Gold: {LoadDataManager.userInGame.Gold}";
        }
    }

    private void RefreshUI()
    {
        // ✅ THÊM: Cập nhật gold display
        UpdateGoldDisplay();

        foreach (var recipe in recipeButtons)
        {
            var slot = cookingSlots[recipe];

            if (slot.canCollect)
            {
                // ✅ Sẵn sàng thu thập
                recipe.cookButton?.gameObject.SetActive(false);
                recipe.collectButton?.gameObject.SetActive(true);

                // ✅ THAY ĐỔI: Ẩn statusText khi xong
                if (recipe.statusText != null)
                    recipe.statusText.gameObject.SetActive(false);
            }
            else if (slot.isCooking)
            {
                // ✅ Đang nấu
                float timeRemaining = (float)(new DateTime(slot.nextCookingTimeTicks, DateTimeKind.Utc) - DateTime.UtcNow).TotalSeconds;
                timeRemaining = Mathf.Max(0, timeRemaining);

                recipe.cookButton?.gameObject.SetActive(false);
                recipe.collectButton?.gameObject.SetActive(false);

                // ✅ THAY ĐỔI: Hiển thị statusText khi đang nấu
                if (recipe.statusText != null)
                {
                    recipe.statusText.gameObject.SetActive(true);
                    recipe.statusText.text = $"Đang làm: {timeRemaining:F0}s";
                }
            }
            else
            {
                // ✅ Sẵn sàng để nấu (chưa nấu)
                recipe.cookButton?.gameObject.SetActive(true);
                recipe.collectButton?.gameObject.SetActive(false);

                // ✅ THAY ĐỔI: Ẩn statusText khi chưa nấu
                if (recipe.statusText != null)
                    recipe.statusText.gameObject.SetActive(false);
            }
        }
    }

    private void OnCookButtonClicked(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeButtons.Count) return;

        var recipe = recipeButtons[recipeIndex];

        // ✅ DEBUG: In ra tất cả ingredients và số lượng
        Debug.Log($"🔍 === Checking ingredients for {recipe.dishName} ===");

        foreach (var ingredient in recipe.ingredients)
        {
            int quantity = playerInventory.GetItemQuantity(ingredient.itemName);
            Debug.Log($"   Ingredient: '{ingredient.itemName}'");
            Debug.Log($"   Need: {ingredient.quantity}, Have: {quantity}");
        }

        Debug.Log($"📊 Can Cook? {CanCook(recipe)}");

        if (!CanCook(recipe))
        {
            Debug.Log($"❌ Không đủ nguyên liệu cho {recipe.dishName}!");
            return;
        }

        // ✅ DEBUG: Trước khi trừ
        Debug.Log($"📍 Before removing items:");
        foreach (var item in playerInventory._invenItems)
        {
            if (item.name.Contains("grape"))
                Debug.Log($"   {item.name}: {item.quantity}");
        }

        // ✅ Trừ tất cả nguyên liệu
        foreach (var ingredient in recipe.ingredients)
        {
            playerInventory.RemoveInventoryItem(ingredient.itemName, ingredient.quantity);
            Debug.Log($"   Removed {ingredient.quantity}x {ingredient.itemName}");
        }

        // ✅ DEBUG: Sau khi trừ
        Debug.Log($"📍 After removing items:");
        foreach (var item in playerInventory._invenItems)
        {
            if (item.name.Contains("grape"))
                Debug.Log($"   {item.name}: {item.quantity}");
        }

        // ✅ Bắt đầu nấu
        var slot = cookingSlots[recipe];
        slot.isCooking = true;
        slot.canCollect = false;
        slot.nextCookingTimeTicks = DateTime.UtcNow.AddSeconds(recipe.cookingDuration).Ticks;

        Debug.Log($"✅ Bắt đầu nấu {recipe.dishName}");

        SaveChefDataToFirebase();
    }
    private void OnCollectButtonClicked(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeButtons.Count) return;

        var recipe = recipeButtons[recipeIndex];
        var slot = cookingSlots[recipe];

        if (!slot.canCollect)
        {
            Debug.Log("❌ Chưa nấu xong!");
            return;
        }

        // ✅ Tạo món ăn và thêm vào inventory
        // ✅ QUAN TRỌNG: Sử dụng dishItemName TRỰC TIẾP (KHÔNG thêm _item)
        InventoryItems dish = new InventoryItems(
            recipe.dishItemName,    // "grilled_pork" (không phải "grilled_pork_item")
            recipe.dishName,
            1
        );

        playerInventory.AddInventoryItem(dish);

        // ✅ Reset slot
        slot.isCooking = false;
        slot.canCollect = false;

        Debug.Log($"✅ Thu thập được 1 {recipe.dishName}");

        SaveChefDataToFirebase();
    }

    private bool CanCook(RecipeButton recipe)
    {
        if (playerInventory == null) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            int quantity = playerInventory.GetItemQuantity(ingredient.itemName);
            if (quantity < ingredient.quantity)
            {
                return false;
            }
        }

        return true;
    }

    private void LoadChefDataFromFirebase()
    {
        if (LoadDataManager.firebaseUser == null) return;

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Chef")
            .Child(chefId)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Value != null)
                {
                    try
                    {
                        string json = task.Result.GetRawJsonValue();
                        var chefData = JsonConvert.DeserializeObject<ChefData>(json);

                        if (chefData != null)
                        {
                            foreach (var slotData in chefData.cookingSlots)
                            {
                                if (slotData.recipeIndex < recipeButtons.Count)
                                {
                                    var recipe = recipeButtons[slotData.recipeIndex];
                                    cookingSlots[recipe].isCooking = slotData.isCooking;
                                    cookingSlots[recipe].nextCookingTimeTicks = slotData.nextCookingTimeTicks;
                                    cookingSlots[recipe].canCollect = slotData.canCollect;
                                }
                            }

                            Debug.Log($"✅ Loaded chef data");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error loading chef data: {e.Message}");
                    }
                }
            });
    }

    private void SaveChefDataToFirebase()
    {
        if (LoadDataManager.firebaseUser == null) return;

        var chefData = new ChefData { chefId = this.chefId };

        for (int i = 0; i < recipeButtons.Count; i++)
        {
            var slot = cookingSlots[recipeButtons[i]];
            chefData.cookingSlots.Add(new CookingSlotData
            {
                recipeIndex = i,
                isCooking = slot.isCooking,
                nextCookingTimeTicks = slot.nextCookingTimeTicks,
                canCollect = slot.canCollect
            });
        }

        string json = JsonConvert.SerializeObject(chefData);

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Chef")
            .Child(chefId)
            .SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompleted)
                    Debug.LogError($"Failed to save chef data: {task.Exception}");
            });
    }
}
