using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class LoadDataManager : MonoBehaviour
{
    public static FirebaseUser firebaseUser;
    public static User userInGame;
    public static bool IsUserDataLoaded { get; private set; }
    public static bool HasUserRecord { get; private set; }
    public static bool UserDataLoadFailed { get; private set; }
    public static bool LastPositionWasRepaired { get; private set; }
    public static event Action OnUserDataLoaded;

    private DatabaseReference reference;
    private bool hasCompletedCurrentLoad;
    private const float DefaultSpawnX = 8.34855f;
    private const float DefaultSpawnY = 0f;
    private const float DefaultSpawnZ = 0f;

    private void Awake()
    {
        FirebaseApp app = FirebaseApp.DefaultInstance;
        reference = FirebaseDatabase.DefaultInstance.RootReference;
        firebaseUser = FirebaseAuth.DefaultInstance.CurrentUser;

        if (firebaseUser != null)
        {
            GetUserInGame();
        }
        else
        {
            Debug.LogError("Firebase user is null in Awake!");
        }
    }

    public void GetUserInGame()
    {
        if (firebaseUser == null)
        {
            Debug.LogError("Firebase user is null!");
            return;
        }

        Debug.Log($"Loading user data for: {firebaseUser.UserId}");

        IsUserDataLoaded = false;
        HasUserRecord = false;
        UserDataLoadFailed = false;
        LastPositionWasRepaired = false;
        hasCompletedCurrentLoad = false;

        userInGame = new User();

        LoadUserDataParts();
    }

 
    private void LoadUserDataParts()
    {
        var userRef = reference.Child("Users").Child(firebaseUser.UserId);

        userRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                MarkUserDataLoadFailed($"Failed to check user record: {task.Exception}");
                return;
            }

            if (!task.IsCompleted || task.Result == null || !task.Result.Exists)
            {
                MarkUserDataLoadFailed($"User record not found at Users/{firebaseUser.UserId}. Refusing to load defaults or save over Firebase.");
                return;
            }

            HasUserRecord = true;
            RepairUserRecordIfNeeded(userRef, task.Result).ContinueWithOnMainThread(repairTask =>
            {
                if (repairTask.IsFaulted || repairTask.IsCanceled)
                {
                    MarkUserDataLoadFailed($"Failed to repair user schema: {repairTask.Exception}");
                    return;
                }

                LoadExistingUserDataParts(userRef);
            });
        });
    }

    private async Task RepairUserRecordIfNeeded(DatabaseReference userRef, DataSnapshot userSnapshot)
    {
        Dictionary<string, object> updates = new Dictionary<string, object>();

        string existingName = userSnapshot.Child("Name").Value?.ToString()?.Trim();
        if (string.IsNullOrEmpty(existingName) || existingName == "Unknown" || existingName.StartsWith("Player_"))
        {
            string recoveredName = await TryRecoverNameFromLeaderboard(firebaseUser.UserId);
            if (!string.IsNullOrEmpty(recoveredName))
            {
                updates["Name"] = recoveredName;
                Debug.Log($"Repair: Name restored from leaderboard: {recoveredName}");
            }
            else if (userSnapshot.Child("Name").Value == null)
            {
                updates["Name"] = "";
                Debug.Log("Repair: Name missing, created empty Name field.");
            }
        }

        if (userSnapshot.Child("Gold").Value == null)
        {
            updates["Gold"] = 100;
            Debug.LogWarning("Repair: Gold missing, created default Gold = 100.");
        }

        if (userSnapshot.Child("MapInGame").Value == null)
        {
            updates["MapInGame"] = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(CreateDefaultMap()));
            Debug.LogWarning("Repair: MapInGame missing, created default map.");
        }

        if (userSnapshot.Child("Inventory").Value == null)
        {
            updates["Inventory"] = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(new List<InventoryItems>()));
            Debug.LogWarning("Repair: Inventory missing, created empty inventory.");
        }

        if (userSnapshot.Child("Plants").Value == null)
        {
            updates["Plants"] = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(new List<PlantTileData>()));
            Debug.LogWarning("Repair: Plants missing, created empty plants list.");
        }

        if (userSnapshot.Child("DailyReward").Value == null)
        {
            updates["DailyReward"] = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(new User.DailyRewardData()));
            Debug.LogWarning("Repair: DailyReward missing, created default daily reward data.");
        }

        if (userSnapshot.Child("Level").Value == null)
        {
            int recoveredLevel = await TryRecoverLevelFromLeaderboard(firebaseUser.UserId);
            var levelData = CreateLevelData(Mathf.Max(1, recoveredLevel));

            updates["Level"] = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(levelData));
            Debug.LogWarning(recoveredLevel > 1
                ? $"Repair: Level missing, restored level {recoveredLevel} from leaderboard with EXP reset to 0."
                : "Repair: Level missing, created default level.");
        }
        else
        {
            try
            {
                var existingLevel = JsonConvert.DeserializeObject<LevelSystem.LevelData>(userSnapshot.Child("Level").GetRawJsonValue());
                int currentStoredLevel = Mathf.Max(1, existingLevel?.currentLevel ?? 1);
                int recoveredLevel = await TryRecoverLevelFromLeaderboard(firebaseUser.UserId);

                if (recoveredLevel > currentStoredLevel)
                {
                    var repairedLevel = CreateLevelData(recoveredLevel);
                    repairedLevel.unlockedItems = existingLevel?.unlockedItems ?? new List<string>();
                    updates["Level"] = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(repairedLevel));
                    Debug.LogWarning($"Repair: Level looked stale/default. Restored level {recoveredLevel} from leaderboard with EXP reset to 0.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Repair: Could not inspect existing Level data: {e.Message}");
            }
        }

        if (userSnapshot.Child("LastPosition").Value == null)
        {
            var playerPosition = CreateDefaultSpawnPosition();
            updates["LastPosition"] = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(playerPosition));
            LastPositionWasRepaired = true;
            Debug.LogWarning($"Repair: LastPosition missing, created default spawn position ({playerPosition.x}, {playerPosition.y}, {playerPosition.z}).");
        }

        if (updates.Count == 0)
            return;

        await userRef.UpdateChildrenAsync(updates);
        Debug.Log($"User schema repair completed with {updates.Count} missing field(s).");
    }

    private async Task<string> TryRecoverNameFromLeaderboard(string userId)
    {
        string[] rankTypes = { "levelRanking", "questRanking" };
        string[] dates =
        {
            DateTime.UtcNow.ToString("yyyy-MM-dd"),
            DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
        };

        foreach (string date in dates)
        {
            foreach (string rankType in rankTypes)
            {
                var snapshot = await FirebaseDatabase.DefaultInstance
                    .GetReference("Leaderboard/CurrentDaily")
                    .Child(date)
                    .Child(rankType)
                    .Child(userId)
                    .Child("playerName")
                    .GetValueAsync();

                string playerName = snapshot?.Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(playerName) &&
                    playerName != "Unknown" &&
                    !playerName.StartsWith("Player_"))
                {
                    return playerName;
                }
            }
        }

        return null;
    }

    private async Task<int> TryRecoverLevelFromLeaderboard(string userId)
    {
        string[] dates =
        {
            DateTime.UtcNow.ToString("yyyy-MM-dd"),
            DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
        };

        foreach (string date in dates)
        {
            var snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference("Leaderboard/CurrentDaily")
                .Child(date)
                .Child("levelRanking")
                .Child(userId)
                .Child("value")
                .GetValueAsync();

            if (snapshot?.Value != null && int.TryParse(snapshot.Value.ToString(), out int level))
                return Mathf.Max(1, level);
        }

        return 1;
    }

    private LevelSystem.LevelData CreateLevelData(int level)
    {
        return new LevelSystem.LevelData
        {
            currentLevel = Mathf.Max(1, level),
            currentExperience = 0,
            experienceToNextLevel = 30,
            unlockedItems = new List<string>()
        };
    }

    private void LoadExistingUserDataParts(DatabaseReference userRef)
    {
        int loadedPartsCount = 0;
        int totalParts = 6; // DailyReward, Name, Gold, LastPosition, MapInGame, Inventory

        userRef.Child("DailyReward").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && task.Result.Value != null)
            {
                try
                {
                    string rewardJson = task.Result.GetRawJsonValue();
                    userInGame.DailyReward = JsonConvert.DeserializeObject<User.DailyRewardData>(rewardJson);
                    Debug.Log($"   DailyReward loaded:");
                    Debug.Log($"   Last Daily Claim: {userInGame.DailyReward.lastDailyClaimDate}");
                    Debug.Log($"   Last Ranking Claim: {userInGame.DailyReward.lastRankingRewardDate}");
                    Debug.Log($"   Last Saved Snapshot: {userInGame.DailyReward.lastSavedSnapshotDate}");
                }
                catch (Exception e)
                {
                    Debug.LogError($" Error parsing DailyReward: {e.Message}");
                    userInGame.DailyReward = new User.DailyRewardData();
                }
            }
            else if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($" Failed to load DailyReward, keeping local default and not saving over Firebase: {task.Exception}");
            }
            else
            {
                Debug.Log(" DailyReward not found, using default");
                userInGame.DailyReward = new User.DailyRewardData();
            }

            loadedPartsCount++;
            CheckLoadComplete(loadedPartsCount, totalParts);
        });

        userRef.Child("Name").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && task.Result.Value != null)
            {
                userInGame.Name = task.Result.Value.ToString();
                Debug.Log($" Name loaded: '{userInGame.Name}'");
            }
            else if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($" Failed to load Name, keeping local default and not saving over Firebase: {task.Exception}");
            }
            else
            {
                userInGame.Name = "";
                Debug.Log(" Name not found, using default empty string");
            }

            loadedPartsCount++;
            CheckLoadComplete(loadedPartsCount, totalParts);
        });

        userRef.Child("Gold").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && task.Result.Value != null)
            {
                userInGame.Gold = Convert.ToInt32(task.Result.Value);
                Debug.Log($" Gold loaded: {userInGame.Gold}");
            }
            else if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($" Failed to load Gold, keeping local default and not saving over Firebase: {task.Exception}");
            }
            else
            {
                userInGame.Gold = 100;
                Debug.Log(" Gold not found, using default 100");
            }

            loadedPartsCount++;
            CheckLoadComplete(loadedPartsCount, totalParts);
        });

        userRef.Child("LastPosition").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && task.Result.Value != null)
            {
                try
                {
                    string posJson = task.Result.GetRawJsonValue();
                    userInGame.LastPosition = JsonConvert.DeserializeObject<User.PlayerPosition>(posJson);
                    userInGame.LastPosition ??= CreateDefaultSpawnPosition();
                    Debug.Log($" LastPosition loaded: ({userInGame.LastPosition.x}, {userInGame.LastPosition.y}, {userInGame.LastPosition.z})");
                }
                catch (Exception e)
                {
                    Debug.LogError($" Error parsing LastPosition: {e.Message}");
                    userInGame.LastPosition = CreateDefaultSpawnPosition();
                    LastPositionWasRepaired = true;
                }
            }
            else if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($" Failed to load LastPosition, keeping local default and not saving over Firebase: {task.Exception}");
            }
            else
            {
                Debug.Log(" LastPosition not found, using default spawn position");
                userInGame.LastPosition = CreateDefaultSpawnPosition();
                LastPositionWasRepaired = true;
            }

            loadedPartsCount++;
            CheckLoadComplete(loadedPartsCount, totalParts);
        });

        userRef.Child("MapInGame").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && task.Result.Value != null)
            {
                try
                {
                    string mapJson = task.Result.GetRawJsonValue();
                    userInGame.MapInGame = JsonConvert.DeserializeObject<Map>(mapJson);
                    Debug.Log($" Map loaded with {userInGame.MapInGame?.lstTilemapDetail?.Count ?? 0} tiles");
                }
                catch (Exception e)
                {
                    Debug.LogError($" Error parsing MapInGame: {e.Message}");
                    userInGame.MapInGame = CreateDefaultMap();
                }
            }
            else if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($" Failed to load MapInGame, keeping local default and not saving over Firebase: {task.Exception}");
            }
            else
            {
                Debug.Log(" MapInGame not found, creating default");
                userInGame.MapInGame = CreateDefaultMap();
            }

            loadedPartsCount++;
            CheckLoadComplete(loadedPartsCount, totalParts);
        });

        userRef.Child("Inventory").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            userInGame.Inventory = new List<InventoryItems>();

            if (task.IsCompleted && !task.IsFaulted && task.Result.Value != null)
            {
                try
                {
                    string inventoryJson = task.Result.GetRawJsonValue();
                    Debug.Log($"Raw Inventory JSON: {inventoryJson}");

                    if (inventoryJson.StartsWith("["))
                    {
                        userInGame.Inventory = JsonConvert.DeserializeObject<List<InventoryItems>>(inventoryJson);
                        Debug.Log($" Inventory loaded as array with {userInGame.Inventory?.Count ?? 0} items");
                    }
                    else if (inventoryJson.StartsWith("{"))
                    {
                        Debug.LogWarning(" Inventory is JSON Object, not auto-fixing Firebase during load.");
                        var dictInventory = JsonConvert.DeserializeObject<Dictionary<string, object>>(inventoryJson);
                        Debug.Log($"Inventory object keys: {string.Join(", ", dictInventory.Keys)}");
                    }
                    else
                    {
                        Debug.LogWarning($" Unknown inventory format: {inventoryJson}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($" Error parsing Inventory: {e.Message}");
                    Debug.LogError($"Inventory JSON causing error: {task.Result.GetRawJsonValue()}");
                    Debug.LogWarning(" Inventory was not fixed automatically to avoid overwriting existing Firebase data during load.");
                }
            }
            else if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($" Failed to load Inventory, keeping local default and not saving over Firebase: {task.Exception}");
            }
            else
            {
                Debug.Log(" Inventory not found, using empty list");
            }

            loadedPartsCount++;
            CheckLoadComplete(loadedPartsCount, totalParts);
        });
    }

    // Method mới để fix inventory format trên Firebase
    private void FixInventoryOnFirebase()
    {
        Debug.Log("🔧 Fixing Inventory format on Firebase...");

        List<InventoryItems> emptyInventory = new List<InventoryItems>();
        string correctJson = JsonConvert.SerializeObject(emptyInventory);

        reference.Child("Users").Child(firebaseUser.UserId)
            .Child("Inventory")
            .SetRawJsonValueAsync(correctJson)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log(" Inventory format fixed on Firebase");
                }
                else
                {
                    Debug.LogError(" Failed to fix inventory format: " + task.Exception);
                }
            });
    }


    private void CheckLoadComplete(int loadedCount, int totalCount)
    {
        if (loadedCount >= totalCount && !hasCompletedCurrentLoad)
        {
            hasCompletedCurrentLoad = true;
            IsUserDataLoaded = true;

            Debug.Log(" ALL USER DATA LOADED SUCCESSFULLY!");
            Debug.Log($"Final User Data - Name: '{userInGame.Name}', Gold: {userInGame.Gold}");
            Debug.Log($"Map: {(userInGame.MapInGame != null ? "Available" : "NULL")}");
            Debug.Log($"Inventory: {userInGame.Inventory?.Count ?? 0} items");

            OnUserDataLoaded?.Invoke();
        }
    }

    private void MarkUserDataLoadFailed(string message)
    {
        UserDataLoadFailed = true;
        IsUserDataLoaded = false;
        HasUserRecord = false;
        hasCompletedCurrentLoad = true;
        Debug.LogError(message);
    }

    private Map CreateDefaultMap()
    {
        List<TilemapDetail> tilemaps = new List<TilemapDetail>();

        // Tạo default map giống như trong FirebaseLoginManager
        for (int x = -3; x <= 2; x++)
        {
            for (int y = -4; y <= 3; y++)
            {
                TilemapDetail tm_detail = new TilemapDetail(x, y, TileMapState.Grass, DateTime.Now);
                tilemaps.Add(tm_detail);
            }
        }

        return new Map(tilemaps);
    }

    private User.PlayerPosition CreateDefaultSpawnPosition()
    {
        return new User.PlayerPosition
        {
            x = DefaultSpawnX,
            y = DefaultSpawnY,
            z = DefaultSpawnZ
        };
    }
}
