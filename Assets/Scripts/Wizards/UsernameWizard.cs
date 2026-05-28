using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;

public class UsernameWizard : MonoBehaviour
{
    public GameObject usernameWizard;
    public GameObject storageBox;
    public Button buttonOk;
    public InputField inputUsername;
    [SerializeField] GameObject seedSlot;

    public Text usernameProfile;
    public Text usernameDisplay;
    public Text gold;

    public static bool IsEnteringUsername { get; private set; } = false;

    public static UsernameWizard Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(WaitForUserDataAndSetup());
    }

    private IEnumerator WaitForUserDataAndSetup()
    {
        while (LoadDataManager.userInGame == null || !LoadDataManager.IsUserDataLoaded)
        {
            if (LoadDataManager.UserDataLoadFailed)
            {
                Debug.LogError("UsernameWizard setup skipped because user data failed to load.");
                yield break;
            }

            Debug.Log("UsernameWizard: Waiting for user data to load...");
            yield return new WaitForSeconds(0.1f);
        }

        if (DailyRewardSystem.Instance != null)
        {
            DailyRewardSystem.Instance.CheckRewardOnLogin();
            Debug.Log("Daily reward check triggered on login");
        }

        // Bây giờ mới setup UI
        SetupUI();

        StartCoroutine(AutoUpdateGold());
    }

    private void SetupUI()
    {
        // KIỂM TRA NULL TRƯỚC KHI SỬ DỤNG
        if (LoadDataManager.userInGame == null)
        {
            Debug.LogError("LoadDataManager.userInGame is still null!");
            return;
        }

        string currentName = LoadDataManager.userInGame.Name;

        // ✅ DEBUG: Log để check tên hiện tại
        Debug.Log($"🔍 SetupUI - Current Name: '{currentName}' - Length: {currentName?.Length}");

        // ✅ SỬA: Kiểm tra Name có phải rỗng hoặc tên mặc định không
        // Nếu Name rỗng hoặc bắt đầu với "Player_" → hiển thị wizard
        bool needsUsername = string.IsNullOrEmpty(currentName) ||
                            currentName.StartsWith("Player_");

        if (needsUsername)
        {
            // ✅ Hiển thị bảng nhập tên
            Debug.Log("📝 Showing username wizard - name is empty or default");

            usernameWizard.SetActive(true);
            storageBox.SetActive(false);
            seedSlot.SetActive(false);

            IsEnteringUsername = true;
        }
        else
        {
            // ✅ Ẩn bảng nhập tên - user đã có tên thực
            Debug.Log($"✅ Username already set: '{currentName}'");

            usernameWizard.SetActive(false);
            storageBox.SetActive(true);
            seedSlot.SetActive(true);
            IsEnteringUsername = false;

            if (usernameProfile != null)
                usernameProfile.text = currentName;
            if (usernameDisplay != null)
                usernameDisplay.text = currentName;
        }

        RefreshGold();

        if (buttonOk != null)
            buttonOk.onClick.AddListener(SetNewUsername);
    }

    public void RefreshGold()
    {
        if (gold != null && LoadDataManager.userInGame != null)
        {
            gold.text = LoadDataManager.userInGame.Gold.ToString();
        }
    }

    private IEnumerator AutoUpdateGold()
    {
        while (true)
        {
            RefreshGold();
            yield return new WaitForSeconds(1f);
        }
    }

    public static void UpdateGoldDisplay()
    {
        if (Instance != null)
        {
            Instance.RefreshGold();
        }
    }

    public void SetNewUsername()
    {
        if (LoadDataManager.userInGame == null)
        {
            Debug.LogError("❌ Cannot set username - user data is null!");
            return;
        }

        if (inputUsername == null || string.IsNullOrEmpty(inputUsername.text))
        {
            Debug.LogWarning("⚠️ Username input is empty!");
            return;
        }

        string newUsername = inputUsername.text.Trim();
        if (string.IsNullOrEmpty(newUsername))
        {
            Debug.LogWarning("⚠️ Username input is empty!");
            return;
        }

        Debug.Log($"💾 Saving username to Firebase: '{newUsername}'");

        // ✅ DISABLE button khi saving
        if (buttonOk != null)
            buttonOk.interactable = false;
        if (inputUsername != null)
            inputUsername.interactable = false;

        StartCoroutine(SaveUsernameIfUniqueCoroutine(newUsername));
    }

    private IEnumerator SaveUsernameIfUniqueCoroutine(string newUsername)
    {
        bool isChecked = false;
        bool isDuplicate = false;
        bool checkFailed = false;
        string currentUserId = LoadDataManager.firebaseUser.UserId;

        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && task.Result.Value != null)
                {
                    foreach (var userSnapshot in task.Result.Children)
                    {
                        if (userSnapshot.Key == currentUserId)
                            continue;

                        string existingName = userSnapshot.Child("Name").Value?.ToString();
                        if (string.IsNullOrWhiteSpace(existingName))
                            continue;

                        if (string.Equals(existingName.Trim(), newUsername, StringComparison.OrdinalIgnoreCase))
                        {
                            isDuplicate = true;
                            break;
                        }
                    }
                }
                else if (task.IsFaulted)
                {
                    Debug.LogError($"❌ Failed to check username uniqueness: {task.Exception}");
                    checkFailed = true;
                }

                isChecked = true;
            });

        yield return new WaitUntil(() => isChecked);

        if (checkFailed)
        {
            if (buttonOk != null)
                buttonOk.interactable = true;
            if (inputUsername != null)
                inputUsername.interactable = true;
            yield break;
        }

        if (isDuplicate)
        {
            Debug.LogWarning($"⚠️ Username already exists: '{newUsername}'");
            if (buttonOk != null)
                buttonOk.interactable = true;
            if (inputUsername != null)
                inputUsername.interactable = true;
            yield break;
        }

        LoadDataManager.userInGame.Name = newUsername;

        // ✅ CẬP NHẬT: Dùng UpdateChildrenAsync để chỉ update field Name
        var userRef = FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId);

        var updateData = new Dictionary<string, object>
        {
            { "Name", newUsername }
        };

        userRef.UpdateChildrenAsync(updateData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log($"✅ Username saved successfully: '{newUsername}'");

                    // ✅ THÊM: Update tên trong Leaderboard
                    StartCoroutine(UpdateLeaderboardUsernameCoroutine(newUsername));

                    // Update UI
                    if (usernameProfile != null)
                        usernameProfile.text = newUsername;
                    if (usernameDisplay != null)
                        usernameDisplay.text = newUsername;

                    usernameWizard.SetActive(false);
                    storageBox.SetActive(true);
                    seedSlot.SetActive(true);

                    IsEnteringUsername = false;
                    Debug.Log("✅ Username setup completed");

                    // ✅ THÊM: Enable controls lại
                    if (buttonOk != null)
                        buttonOk.interactable = true;
                    if (inputUsername != null)
                        inputUsername.interactable = true;
                }
                else
                {
                    Debug.LogError($"❌ Failed to save username: {task.Exception?.Message}");

                    // ✅ THÊM: Enable controls lại nếu lỗi
                    if (buttonOk != null)
                        buttonOk.interactable = true;
                    if (inputUsername != null)
                        inputUsername.interactable = true;
                }
            });
    }

    /// <summary>
    /// ✅ THÊM: Update tên trong Leaderboard và refresh ngay
    /// </summary>
    private IEnumerator UpdateLeaderboardUsernameCoroutine(string newUsername)
    {
        string today = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
        string userId = LoadDataManager.firebaseUser.UserId;

        Debug.Log($"🔄 Updating leaderboard username to: '{newUsername}'");

        // ✅ Update Level Leaderboard
        bool levelUpdated = false;
        FirebaseDatabase.DefaultInstance
            .GetReference("Leaderboard/CurrentDaily")
            .Child(today)
            .Child("levelRanking")
            .Child(userId)
            .Child("playerName")
            .SetValueAsync(newUsername)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log($"✅ Level leaderboard username updated");
                }
                else
                {
                    Debug.LogError($"❌ Failed to update level leaderboard: {task.Exception}");
                }
                levelUpdated = true;
            });

        // ✅ Update Quest Leaderboard
        bool questUpdated = false;
        FirebaseDatabase.DefaultInstance
            .GetReference("Leaderboard/CurrentDaily")
            .Child(today)
            .Child("questRanking")
            .Child(userId)
            .Child("playerName")
            .SetValueAsync(newUsername)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log($"✅ Quest leaderboard username updated");
                }
                else
                {
                    Debug.LogError($"❌ Failed to update quest leaderboard: {task.Exception}");
                }
                questUpdated = true;
            });

        // ✅ Đợi cả 2 update xong
        yield return new WaitUntil(() => levelUpdated && questUpdated);

        // ✅ THÊM: Reload Leaderboard ngay sau khi update tên
        Debug.Log("📊 Reloading leaderboard after username update...");
        if (LeaderboardManager.Instance != null)
        {
            yield return StartCoroutine(LeaderboardManager.Instance.RefreshLeaderboardFromFirebase());
            Debug.Log("✅ Leaderboard refreshed with new username!");
        }
    }
}
