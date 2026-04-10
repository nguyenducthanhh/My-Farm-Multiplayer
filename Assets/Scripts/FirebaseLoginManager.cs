 using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
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
    public string gold = "100";
    [Header("Switch form")]
    public Button buttonMoveToSignIn;
    public Button buttonMoveToRegister;

    public GameObject registerForm;
    public GameObject loginForm;
    private FirebaseAuth auth;
    private FirebaseDatabaseManager databaseManager;
    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
       
        buttonRegister.onClick.AddListener(RegisterAccountWithFirebase);
        buttonLogin.onClick.AddListener(SigninAccountWithFirebase);
       
        buttonMoveToRegister.onClick.AddListener(SwitchForm);
        buttonMoveToSignIn.onClick.AddListener(SwitchForm);
    }

    private void Awake()
    {
        databaseManager = GetComponent<FirebaseDatabaseManager>();
    }

    //public void RegisterAccountWithFirebase()
    //{
    //    string email = ipRegisterEmail.text;
    //    string password = ipRegisterPassword.text;

    //    auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
    //    {
    //        if(task.IsCanceled)
    //        {
    //            Debug.Log("Dang ky bi huy");
    //            return;
    //        }
    //        if(task.IsFaulted)
    //        {
    //            Debug.Log("Dang ky that bai");
    //            return;
    //        }
    //        if (task.IsCompleted)
    //        {
    //            Debug.Log("Dang ky thanh cong");
    //            Map mapInGame = new Map();
    //            User userInGame = new User("", 100, mapInGame);

    //            FirebaseUser firebaseUser = task.Result.User;

    //            databaseManager.WriteDatabase("Users/" + firebaseUser.UserId, userInGame.ToString());
    //            //27/3
    //            //CreateUserWithSeparateFields(firebaseUser.UserId);
    //            LoadingManager.NEXT_SCENE =("PlayScene");
    //            SceneManager.LoadScene("LoadingScene");
    //        }
    //    });
    //}

    //27/3
    public void RegisterAccountWithFirebase()
    {
        string email = ipRegisterEmail.text;
        string password = ipRegisterPassword.text;

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Dang ky bi huy");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log("Dang ky that bai");
                return;
            }
            if (task.IsCompleted)
            {
                Debug.Log("Dang ky thanh cong");
                FirebaseUser firebaseUser = task.Result.User;

                // SỬ DỤNG CÁCH MỚI - TẠO TỪNG FIELD RIÊNG BIỆT
                CreateUserWithSeparateFields(firebaseUser.UserId);

                LoadingManager.NEXT_SCENE = ("PlayScene");
                SceneManager.LoadScene("LoadingScene");
            }
        });
    }

    private void CreateUserWithSeparateFields(string userId)
    {
        var userRef = FirebaseDatabase.DefaultInstance.GetReference("Users").Child(userId);

        // 1. Lưu Name và Gold riêng biệt
        userRef.Child("Name").SetValueAsync("");
        userRef.Child("Gold").SetValueAsync(gold);  // Tăng gold ban đầu lên 100

        // 2. Tạo và lưu Map với đầy đủ tilemap data
        CreateAndSaveDefaultMap(userRef);

        // 3. Tạo Inventory rỗng
        List<InventoryItems> emptyInventory = new List<InventoryItems>();
        string inventoryJson = JsonConvert.SerializeObject(emptyInventory);
        userRef.Child("Inventory").SetRawJsonValueAsync(inventoryJson);

        // 4. Tạo Plants rỗng
        List<PlantTileData> emptyPlants = new List<PlantTileData>();
        string plantsJson = JsonConvert.SerializeObject(emptyPlants);
        userRef.Child("Plants").SetRawJsonValueAsync(plantsJson);

        Debug.Log($"User created with separate fields - Gold: {gold}");
    }

    private void CreateAndSaveDefaultMap(DatabaseReference userRef)
    {
        // Tạo map giống như trong TileMapManager.WriteAllTileMapToFirebase()
        List<TilemapDetail> tilemaps = new List<TilemapDetail>();

        // Tạo default map bounds (có thể adjust theo game của bạn)
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
        userRef.Child("MapInGame").SetRawJsonValueAsync(mapJson);

        Debug.Log($"Default map created with {tilemaps.Count} tiles");
    }





    //private void CreateUserWithSeparateFields(string userId)
    //{
    //    var userRef = FirebaseDatabase.DefaultInstance.GetReference("Users").Child(userId);

    //    // 1. Tạo User cơ bản với Name và Gold
    //    User basicUser = new User("", 100);
    //    userRef.Child("Name").SetValueAsync("");
    //    userRef.Child("Gold").SetValueAsync(100);

    //    // 2. Tạo và lưu Map riêng
    //    Map defaultMap = new Map();
    //    string mapJson = JsonConvert.SerializeObject(defaultMap);
    //    userRef.Child("MapInGame").SetRawJsonValueAsync(mapJson);

    //    // 3. Tạo Inventory rỗng
    //    List<InventoryItems> emptyInventory = new List<InventoryItems>();
    //    string inventoryJson = JsonConvert.SerializeObject(emptyInventory);
    //    userRef.Child("Inventory").SetRawJsonValueAsync(inventoryJson);

    //    // 4. Tạo Plants rỗng
    //    List<PlantTileData> emptyPlants = new List<PlantTileData>();
    //    string plantsJson = JsonConvert.SerializeObject(emptyPlants);
    //    userRef.Child("Plants").SetRawJsonValueAsync(plantsJson);

    //    Debug.Log("User created with separate fields");
    //}
    public void SigninAccountWithFirebase()
    {
        string email = ipLoginEmail.text;
        string password = ipLoginPassword.text;

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Dang nhap bi huy");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.Log("Dang nhap that bai");
            }
            if (task.IsCompleted)
            {
                Debug.Log("Dang nhap thanh cong");
                FirebaseUser user = task.Result.User;
                LoadingManager.NEXT_SCENE = ("PlayScene");
                SceneManager.LoadScene("LoadingScene");
            }
        });
    }

    public void SwitchForm()
    { 
        loginForm.SetActive(!loginForm.activeSelf);
        registerForm.SetActive(!registerForm.activeSelf);
    }
}
