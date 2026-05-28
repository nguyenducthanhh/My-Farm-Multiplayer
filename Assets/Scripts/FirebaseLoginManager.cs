using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirebaseLoginManager : MonoBehaviour
{
    [Header("Register")]
    public InputField ipRegisterEmail;
    public InputField ipRegisterPassword;
    public Button buttonRegister;

    [Header("Sign In")]
    public InputField ipLoginEmail;
    public InputField ipLoginPassword;
    public Button buttonLogin;
    public string gold;

    [Header("Switch form")]
    public Button buttonMoveToSignIn;
    public Button buttonMoveToRegister;

    public GameObject registerForm;
    public GameObject loginForm;

    private FirebaseAuth auth;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        buttonRegister.onClick.AddListener(RegisterAccountWithFirebase);
        buttonLogin.onClick.AddListener(SigninAccountWithFirebase);

        buttonMoveToRegister.onClick.AddListener(ShowRegisterForm);
        buttonMoveToSignIn.onClick.AddListener(ShowLoginForm);

        ShowLoginForm();
    }

    public void ShowLoginForm()
    {
        loginForm.SetActive(true);
        registerForm.SetActive(false);
    }
     
    public void ShowRegisterForm()
    {
        loginForm.SetActive(false);
        registerForm.SetActive(true);
    }

    public void RegisterAccountWithFirebase()
    {
        string email = ipRegisterEmail.text;
        string password = ipRegisterPassword.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Email or password is empty!");
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Đăng ký bị hủy");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log("Đăng ký thất bại: " + task.Exception?.Message);
                return;
            }
            if (task.IsCompleted)
            {
                Debug.Log("Đăng ký thành công");
                FirebaseUser firebaseUser = task.Result.User;

                CreateUserWithSeparateFields(firebaseUser.UserId).ContinueWithOnMainThread(initTask =>
                {
                    if (initTask.IsCompleted && !initTask.IsFaulted && !initTask.IsCanceled)
                    {
                        LoadingManager.NEXT_SCENE = ("PlayScene");
                        SceneManager.LoadScene("LoadingScene");
                    }
                    else
                    {
                        Debug.LogError("Không thể tạo dữ liệu mặc định cho tài khoản: " + initTask.Exception);
                    }
                });
            }
        });
    }

    private Task CreateUserWithSeparateFields(string userId)
    {
        var userRef = FirebaseDatabase.DefaultInstance.GetReference("Users").Child(userId);
        List<Task> createTasks = new List<Task>();

        createTasks.Add(userRef.Child("Name").SetValueAsync(value: ""));
        createTasks.Add(userRef.Child("Gold").SetValueAsync(gold));

        createTasks.Add(CreateAndSaveDefaultMap(userRef));

        List<InventoryItems> emptyInventory = new List<InventoryItems>();
        string inventoryJson = JsonConvert.SerializeObject(emptyInventory);
        createTasks.Add(userRef.Child("Inventory").SetRawJsonValueAsync(inventoryJson));

        List<PlantTileData> emptyPlants = new List<PlantTileData>();
        string plantsJson = JsonConvert.SerializeObject(emptyPlants);
        createTasks.Add(userRef.Child("Plants").SetRawJsonValueAsync(plantsJson));

        createTasks.Add(CreateAndSaveDefaultLevel(userRef));

        createTasks.Add(CreateAndSaveDefaultPosition(userRef));

        var dailyReward = new User.DailyRewardData
        {
            lastDailyClaimDate = "",
            lastDailyClaimGold = 0,
            hasClaimedDailyToday = false,

            lastRankingRewardDate = "",
            lastRankingRewardGold = 0,
            hasClaimedRankingToday = false,

            lastSavedSnapshotDate = "",

            accountCreatedDate = System.DateTime.UtcNow.ToString("yyyy-MM-dd")
        };

        string rewardJson = JsonConvert.SerializeObject(dailyReward);
        createTasks.Add(userRef.Child("DailyReward").SetRawJsonValueAsync(rewardJson));

        UpdateLeaderboardForNewAccount(userId, "");

        return Task.WhenAll(createTasks);
    }

    private void UpdateLeaderboardForNewAccount(string userId, string playerName)
    {
        string today = System.DateTime.UtcNow.ToString("yyyy-MM-dd");

        var levelRankEntry = new LeaderboardManager.LeaderboardRankEntry
        {
            rank = 0,
            playerName = string.IsNullOrEmpty(playerName) ? "Unknown" : playerName,
            value = 1,
            timestamp = System.DateTime.UtcNow.ToString("O")
        };

        FirebaseDatabase.DefaultInstance
            .GetReference("Leaderboard/CurrentDaily")
            .Child(today)
            .Child("levelRanking")
            .Child(userId)
            .SetRawJsonValueAsync(JsonConvert.SerializeObject(levelRankEntry))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log($"Account added to level leaderboard");
                }
                else
                {
                    Debug.LogError($"Failed to update level leaderboard: {task.Exception}");
                }
            });

        var questRankEntry = new LeaderboardManager.LeaderboardRankEntry
        {
            rank = 0,
            playerName = string.IsNullOrEmpty(playerName) ? "Unknown" : playerName,
            value = 0,
            timestamp = System.DateTime.UtcNow.ToString("O")
        };

        FirebaseDatabase.DefaultInstance
            .GetReference("Leaderboard/CurrentDaily")
            .Child(today)
            .Child("questRanking")
            .Child(userId)
            .SetRawJsonValueAsync(JsonConvert.SerializeObject(questRankEntry))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log($"Account added to quest leaderboard");
                }
                else
                {
                    Debug.LogError($"Failed to update quest leaderboard: {task.Exception}");
                }
            });
    }

    private Task CreateAndSaveDefaultPosition(DatabaseReference userRef)
    {
        var playerPosition = new User.PlayerPosition
        {
            x = 8.34855f,
            y = 0,        
            z = 0f        
        };

        string positionJson = JsonConvert.SerializeObject(playerPosition);

        Debug.Log($"Default spawn position created: ({playerPosition.x}, {playerPosition.y})");
        return userRef.Child("LastPosition").SetRawJsonValueAsync(positionJson);
    }

    private Task CreateAndSaveDefaultLevel(DatabaseReference userRef)
    {
        var levelData = new LevelSystem.LevelData
        {
            currentLevel = 1,
            currentExperience = 0,
            experienceToNextLevel = 30,
            unlockedItems = new List<string>()
        };

        string levelJson = JsonConvert.SerializeObject(levelData);

        Debug.Log($"Default level created - Level: 1, EXP: 0");
        return userRef.Child("Level").SetRawJsonValueAsync(levelJson);
    }

    private Task CreateAndSaveDefaultMap(DatabaseReference userRef)
    {
        List<TilemapDetail> tilemaps = new List<TilemapDetail>();

        for (int x = -3; x <= 2; x++)
        {
            for (int y = -4; y <= 3; y++)
            {
                TilemapDetail tm_detail = new TilemapDetail(x, y, TileMapState.Grass, System.DateTime.Now);
                tilemaps.Add(tm_detail);
            }
        }

        Map defaultMap = new Map(tilemaps);
        string mapJson = JsonConvert.SerializeObject(defaultMap);

        Debug.Log($"Default map created with {tilemaps.Count} tiles");
        return userRef.Child("MapInGame").SetRawJsonValueAsync(mapJson);
    }

    public void SigninAccountWithFirebase()
    {
        string email = ipLoginEmail.text;
        string password = ipLoginPassword.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Email or password is empty!");
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Đăng nhập bị hủy");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.Log("Đăng nhập thất bại: " + task.Exception?.Message);
                return;
            }

            if (task.IsCompleted)
            {
                Debug.Log("Đăng nhập thành công");
                FirebaseUser user = task.Result.User;

                VerifyUserRecordExists(user.UserId).ContinueWithOnMainThread(initTask =>
                {
                    if (initTask.IsCompleted && !initTask.IsFaulted && !initTask.IsCanceled)
                    {
                        LoadingManager.NEXT_SCENE = ("PlayScene");
                        SceneManager.LoadScene("LoadingScene");
                    }
                    else
                    {
                        Debug.LogError("Không thể kiểm tra dữ liệu người chơi: " + initTask.Exception);
                    }
                });
            }
        });
    }

    private async Task VerifyUserRecordExists(string userId)
    {
        var userRef = FirebaseDatabase.DefaultInstance.GetReference("Users").Child(userId);
        var snapshot = await userRef.GetValueAsync();

        if (snapshot != null && snapshot.Exists)
        {
            Debug.Log($"User record exists: Users/{userId}");
            return;
        }

        throw new Exception($"User record missing at Users/{userId}. Login was stopped to avoid resetting saved data.");
    }
}
