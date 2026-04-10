//using Firebase;
//using Firebase.Auth;
//using Firebase.Database;
//using Firebase.Extensions;
//using Newtonsoft.Json;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEditor.PackageManager;
//using UnityEngine;

//public class LoadDataManager : MonoBehaviour
//{
//    public static FirebaseUser firebaseUser;
//    public static User userInGame;

//    private DatabaseReference reference;
//    private void Awake()
//    {
//        FirebaseApp app = FirebaseApp.DefaultInstance;
//        reference = FirebaseDatabase.DefaultInstance.RootReference;
//        firebaseUser = FirebaseAuth.DefaultInstance.CurrentUser;
//        GetUserInGame();

//    }
//    void Start()
//    {
//    }

//    void Update()
//    {

//    }



//    public void GetUserInGame()
//    {

//        reference.Child("Users").Child(firebaseUser.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
//        {
//            if (task.IsCompleted)
//            {
//                DataSnapshot snapshot = task.Result;
//                userInGame = JsonConvert.DeserializeObject<User>(snapshot.Value.ToString());
//                Debug.Log("User in game" + userInGame.ToString());
//            }
//            else
//            {
//                Debug.Log("Doc du lieu that bai" + task.Exception);
//            }
//        });

//    }
//}
// 27/3

using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadDataManager : MonoBehaviour
{
    public static FirebaseUser firebaseUser;
    public static User userInGame;

    private DatabaseReference reference;

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

        // Khởi tạo User object trống trước
        userInGame = new User();

        // Load từng field riêng biệt
        LoadUserDataParts();
    }

    //private void LoadUserDataParts()
    //{
    //    var userRef = reference.Child("Users").Child(firebaseUser.UserId);
    //    int loadedPartsCount = 0;
    //    int totalParts = 4; // Name, Gold, MapInGame, Inventory

    //    // Load Name
    //    userRef.Child("Name").GetValueAsync().ContinueWithOnMainThread(task =>
    //    {
    //        if (task.IsCompleted && task.Result.Value != null)
    //        {
    //            userInGame.Name = task.Result.Value.ToString();
    //            Debug.Log($"✅ Name loaded: '{userInGame.Name}'");
    //        }
    //        else
    //        {
    //            userInGame.Name = "";
    //            Debug.Log("⚠️ Name not found, using default empty string");
    //        }

    //        loadedPartsCount++;
    //        CheckLoadComplete(loadedPartsCount, totalParts);
    //    });

    //    // Load Gold
    //    userRef.Child("Gold").GetValueAsync().ContinueWithOnMainThread(task =>
    //    {
    //        if (task.IsCompleted && task.Result.Value != null)
    //        {
    //            userInGame.Gold = Convert.ToInt32(task.Result.Value);
    //            Debug.Log($"✅ Gold loaded: {userInGame.Gold}");
    //        }
    //        else
    //        {
    //            userInGame.Gold = 100;
    //            Debug.Log("⚠️ Gold not found, using default 100");
    //        }

    //        loadedPartsCount++;
    //        CheckLoadComplete(loadedPartsCount, totalParts);
    //    });

    //    // Load MapInGame
    //    userRef.Child("MapInGame").GetValueAsync().ContinueWithOnMainThread(task =>
    //    {
    //        if (task.IsCompleted && task.Result.Value != null)
    //        {
    //            try
    //            {
    //                string mapJson = task.Result.GetRawJsonValue();
    //                userInGame.MapInGame = JsonConvert.DeserializeObject<Map>(mapJson);
    //                Debug.Log($"✅ Map loaded with {userInGame.MapInGame?.lstTilemapDetail?.Count ?? 0} tiles");
    //            }
    //            catch (Exception e)
    //            {
    //                Debug.LogError($"❌ Error parsing MapInGame: {e.Message}");
    //                userInGame.MapInGame = CreateDefaultMap();
    //            }
    //        }
    //        else
    //        {
    //            Debug.Log("⚠️ MapInGame not found, creating default");
    //            userInGame.MapInGame = CreateDefaultMap();
    //        }

    //        loadedPartsCount++;
    //        CheckLoadComplete(loadedPartsCount, totalParts);
    //    });

    //    // Load Inventory
    //    userRef.Child("Inventory").GetValueAsync().ContinueWithOnMainThread(task =>
    //    {
    //        if (task.IsCompleted && task.Result.Value != null)
    //        {
    //            try
    //            {
    //                string inventoryJson = task.Result.GetRawJsonValue();
    //                userInGame.Inventory = JsonConvert.DeserializeObject<List<InventoryItems>>(inventoryJson);
    //                Debug.Log($"✅ Inventory loaded with {userInGame.Inventory?.Count ?? 0} items");
    //            }
    //            catch (Exception e)
    //            {
    //                Debug.LogError($"❌ Error parsing Inventory: {e.Message}");
    //                userInGame.Inventory = new List<InventoryItems>();
    //            }
    //        }
    //        else
    //        {
    //            Debug.Log("⚠️ Inventory not found, using empty list");
    //            userInGame.Inventory = new List<InventoryItems>();
    //        }

    //        loadedPartsCount++;
    //        CheckLoadComplete(loadedPartsCount, totalParts);
    //    });
    //}

    private void LoadUserDataParts()
    {
        var userRef = reference.Child("Users").Child(firebaseUser.UserId);
        int loadedPartsCount = 0;
        int totalParts = 4; // Name, Gold, MapInGame, Inventory

        // Load Name
        userRef.Child("Name").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Value != null)
            {
                userInGame.Name = task.Result.Value.ToString();
                Debug.Log($"✅ Name loaded: '{userInGame.Name}'");
            }
            else
            {
                userInGame.Name = "";
                Debug.Log("⚠️ Name not found, using default empty string");
            }

            loadedPartsCount++;
            CheckLoadComplete(loadedPartsCount, totalParts);
        });

        // Load Gold
        userRef.Child("Gold").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Value != null)
            {
                userInGame.Gold = Convert.ToInt32(task.Result.Value);
                Debug.Log($"✅ Gold loaded: {userInGame.Gold}");
            }
            else
            {
                userInGame.Gold = 100;
                Debug.Log("⚠️ Gold not found, using default 100");
            }

            loadedPartsCount++;
            CheckLoadComplete(loadedPartsCount, totalParts);
        });

        // Load MapInGame
        userRef.Child("MapInGame").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Value != null)
            {
                try
                {
                    string mapJson = task.Result.GetRawJsonValue();
                    userInGame.MapInGame = JsonConvert.DeserializeObject<Map>(mapJson);
                    Debug.Log($"✅ Map loaded with {userInGame.MapInGame?.lstTilemapDetail?.Count ?? 0} tiles");
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Error parsing MapInGame: {e.Message}");
                    userInGame.MapInGame = CreateDefaultMap();
                }
            }
            else
            {
                Debug.Log("⚠️ MapInGame not found, creating default");
                userInGame.MapInGame = CreateDefaultMap();
            }

            loadedPartsCount++;
            CheckLoadComplete(loadedPartsCount, totalParts);
        });

        // Load Inventory - SỬA ĐỂ HANDLE CẢ 2 FORMAT
        userRef.Child("Inventory").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            userInGame.Inventory = new List<InventoryItems>(); // Default empty list

            if (task.IsCompleted && task.Result.Value != null)
            {
                try
                {
                    string inventoryJson = task.Result.GetRawJsonValue();
                    Debug.Log($"Raw Inventory JSON: {inventoryJson}");

                    // KIỂM TRA FORMAT CỦA JSON
                    if (inventoryJson.StartsWith("["))
                    {
                        // Đây là JSON Array - format đúng
                        userInGame.Inventory = JsonConvert.DeserializeObject<List<InventoryItems>>(inventoryJson);
                        Debug.Log($"✅ Inventory loaded as array with {userInGame.Inventory?.Count ?? 0} items");
                    }
                    else if (inventoryJson.StartsWith("{"))
                    {
                        // Đây là JSON Object - cần xử lý đặc biệt
                        Debug.LogWarning("⚠️ Inventory is JSON Object, attempting to parse...");

                        // Thử parse như Dictionary để debug
                        var dictInventory = JsonConvert.DeserializeObject<Dictionary<string, object>>(inventoryJson);
                        Debug.Log($"Inventory object keys: {string.Join(", ", dictInventory.Keys)}");

                        // Reset về empty list và fix trên Firebase
                        userInGame.Inventory = new List<InventoryItems>();
                        FixInventoryOnFirebase();
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Unknown inventory format: {inventoryJson}");
                        userInGame.Inventory = new List<InventoryItems>();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Error parsing Inventory: {e.Message}");
                    Debug.LogError($"Inventory JSON causing error: {task.Result.GetRawJsonValue()}");
                    userInGame.Inventory = new List<InventoryItems>();

                    // Fix inventory trên Firebase
                    FixInventoryOnFirebase();
                }
            }
            else
            {
                Debug.Log("⚠️ Inventory not found, using empty list");
                userInGame.Inventory = new List<InventoryItems>();
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
                    Debug.Log("✅ Inventory format fixed on Firebase");
                }
                else
                {
                    Debug.LogError("❌ Failed to fix inventory format: " + task.Exception);
                }
            });
    }


    private void CheckLoadComplete(int loadedCount, int totalCount)
    {
        if (loadedCount >= totalCount)
        {
            Debug.Log("🎉 ALL USER DATA LOADED SUCCESSFULLY!");
            Debug.Log($"Final User Data - Name: '{userInGame.Name}', Gold: {userInGame.Gold}");
            Debug.Log($"Map: {(userInGame.MapInGame != null ? "Available" : "NULL")}");
            Debug.Log($"Inventory: {userInGame.Inventory?.Count ?? 0} items");
        }
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
}

