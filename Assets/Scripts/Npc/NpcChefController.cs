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
    [SerializeField] private Button closeMenuButton;      
    [SerializeField] private Text goldDisplayText;        

    [Header("Player References")]
    [SerializeField] private RecyclableInventory playerInventory;

    private bool playerInRange = false;
    private Dictionary<RecipeButton, CookingSlot> cookingSlots = new Dictionary<RecipeButton, CookingSlot>();

    [System.Serializable]
    public class RecipeButton
    {
        public string dishName;                        
        public string dishItemName;                       
        public List<IngredientRequirement> ingredients = new List<IngredientRequirement>();
        public float cookingDuration;
        public Button cookButton;                        
        public Button collectButton;                   
        public Text statusText;                         
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

        if (closeMenuButton != null)
            closeMenuButton.onClick.AddListener(CloseChefMenu);

        chefMenuPanel?.SetActive(false);

        for (int i = 0; i < recipeButtons.Count; i++)
        {
            var recipe = recipeButtons[i];
            cookingSlots[recipe] = new CookingSlot { recipe = recipe };

            if (recipe.cookButton != null)
            {
                int index = i;
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

        foreach (var recipe in recipeButtons)
        {
            if (cookingSlots[recipe].isCooking && DateTime.UtcNow >= new DateTime(cookingSlots[recipe].nextCookingTimeTicks, DateTimeKind.Utc))
            {
                cookingSlots[recipe].isCooking = false;
                cookingSlots[recipe].canCollect = true;

                SaveChefDataToFirebase();
            }
        }

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
            UpdateGoldDisplay(); 
        }
    }

    private void CloseChefMenu()
    {
        if (chefMenuPanel != null)
            chefMenuPanel.SetActive(false);
    }

    private void UpdateGoldDisplay()
    {
        if (goldDisplayText != null && LoadDataManager.userInGame != null)
        {
            goldDisplayText.text = $"Vàng: {LoadDataManager.userInGame.Gold}";
        }
    }

    private void RefreshUI()
    {
        UpdateGoldDisplay();

        foreach (var recipe in recipeButtons)
        {
            var slot = cookingSlots[recipe];

            if (slot.canCollect)
            {
                recipe.cookButton?.gameObject.SetActive(false);
                recipe.collectButton?.gameObject.SetActive(true);

                if (recipe.statusText != null)
                    recipe.statusText.gameObject.SetActive(false);
            }
            else if (slot.isCooking)
            {
                float timeRemaining = (float)(new DateTime(slot.nextCookingTimeTicks, DateTimeKind.Utc) - DateTime.UtcNow).TotalSeconds;
                timeRemaining = Mathf.Max(0, timeRemaining);

                recipe.cookButton?.gameObject.SetActive(false);
                recipe.collectButton?.gameObject.SetActive(false);

                if (recipe.statusText != null)
                {
                    recipe.statusText.gameObject.SetActive(true);
                    recipe.statusText.text = $"Đang làm: {timeRemaining:F0}s";
                }
            }
            else
            {
                recipe.cookButton?.gameObject.SetActive(true);
                recipe.collectButton?.gameObject.SetActive(false);

                if (recipe.statusText != null)
                    recipe.statusText.gameObject.SetActive(false);
            }
        }
    }

    private void OnCookButtonClicked(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeButtons.Count) return;

        var recipe = recipeButtons[recipeIndex];

        Debug.Log($" Checking ingredients for {recipe.dishName} ===");

        foreach (var ingredient in recipe.ingredients)
        {
            int quantity = playerInventory.GetItemQuantity(ingredient.itemName);
            Debug.Log($"   Ingredient: '{ingredient.itemName}'");
            Debug.Log($"   Need: {ingredient.quantity}, Have: {quantity}");
        }

        Debug.Log($" Can Cook? {CanCook(recipe)}");

        if (!CanCook(recipe))
        {
            Debug.Log($" Không đủ nguyên liệu cho {recipe.dishName}!");
            NotificationManager.ShowReward($"Không đủ nguyên liệu cho {recipe.dishName}!", 1f);
            return;
        }

        Debug.Log($" Before removing items:");
        foreach (var item in playerInventory._invenItems)
        {
            if (item.name.Contains("grape"))
                Debug.Log($"   {item.name}: {item.quantity}");
        }

        foreach (var ingredient in recipe.ingredients)
        {
            playerInventory.RemoveInventoryItem(ingredient.itemName, ingredient.quantity);
            Debug.Log($"   Removed {ingredient.quantity}x {ingredient.itemName}");
        }

        Debug.Log($" After removing items:");
        foreach (var item in playerInventory._invenItems)
        {
            if (item.name.Contains("grape"))
                Debug.Log($"   {item.name}: {item.quantity}");
        }

        var slot = cookingSlots[recipe];
        slot.isCooking = true;
        slot.canCollect = false;
        slot.nextCookingTimeTicks = DateTime.UtcNow.AddSeconds(recipe.cookingDuration).Ticks;

        Debug.Log($" Bắt đầu nấu {recipe.dishName}");

        SaveChefDataToFirebase();
    }
    private void OnCollectButtonClicked(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipeButtons.Count) return;

        var recipe = recipeButtons[recipeIndex];
        var slot = cookingSlots[recipe];

        if (!slot.canCollect)
        {
            Debug.Log(" Chưa nấu xong!");
            return;
        }

        InventoryItems dish = new InventoryItems(
            recipe.dishItemName,   
            recipe.dishName,
            1
        );

        playerInventory.AddInventoryItem(dish);

        slot.isCooking = false;
        slot.canCollect = false;

        Debug.Log($"Thu thập được 1 {recipe.dishName}");

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

                            Debug.Log($" Loaded chef data");
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
