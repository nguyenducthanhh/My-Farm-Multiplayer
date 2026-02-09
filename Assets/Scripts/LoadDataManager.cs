using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
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
        GetUserInGame();

    }
    void Start()
    {
    }

    void Update()
    {
        
    }

    public void GetUserInGame()
    {
    
        reference.Child("Users").Child(firebaseUser.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                userInGame = JsonConvert.DeserializeObject<User>(snapshot.Value.ToString());
                Debug.Log("User in game" + userInGame.ToString());
            }
            else
            {
                Debug.Log("Doc du lieu that bai" + task.Exception);
            }
        });

        }

}
