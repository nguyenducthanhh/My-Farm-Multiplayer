using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 3f;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D playerCollider;

    private Vector2 input;
    private float savePositionTimer = 0f;
    private const float SAVE_INTERVAL = 2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();  // ✅ THÊM: Get collider

        LoadPlayerPosition();
    }

    private void Start()
    {
    }

 void Update()
    {
        if (UsernameWizard.IsEnteringUsername)
        {
            input = Vector2.zero;
            UpdateAnimation(input);
            return;
        }

        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        UpdateAnimation(input);

        savePositionTimer += Time.deltaTime;
        if (savePositionTimer >= SAVE_INTERVAL)
        {
            SavePlayerPosition();
            savePositionTimer = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (UsernameWizard.IsEnteringUsername)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        Move(input);
    }

    private void Move(Vector2 direction)
    {
        rb.velocity = direction.normalized * speed;
    }

    private void UpdateAnimation(Vector2 direction)
    {
        animator.SetFloat("Horizontal", direction.x);
        animator.SetFloat("Vertical", direction.y);
        animator.SetFloat("Speed", direction.sqrMagnitude);
    }

    private void OnApplicationQuit()
    {
        SavePlayerPositionImmediately();
        Debug.Log("💾 Game closed - position saved");
    }

    private void SavePlayerPositionImmediately()
    {
        if (LoadDataManager.userInGame == null || LoadDataManager.firebaseUser == null)
            return;

        LoadDataManager.userInGame.LastPosition = new User.PlayerPosition
        {
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z
        };

        var userRef = FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("LastPosition");

        string positionJson = JsonConvert.SerializeObject(LoadDataManager.userInGame.LastPosition);
        userRef.SetRawJsonValueAsync(positionJson);

        Debug.Log($"💾 Position saved: ({LoadDataManager.userInGame.LastPosition.x}, {LoadDataManager.userInGame.LastPosition.y})");
    }

    public void SavePlayerPositionBeforeLogout()
    {
        SavePlayerPositionImmediately();
    }

    private void SavePlayerPosition()
    {
        if (LoadDataManager.userInGame == null || LoadDataManager.firebaseUser == null)
            return;

        LoadDataManager.userInGame.LastPosition = new User.PlayerPosition
        {
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z
        };

        var userRef = FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("LastPosition");

        string positionJson = JsonConvert.SerializeObject(LoadDataManager.userInGame.LastPosition);
        userRef.SetRawJsonValueAsync(positionJson).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log($"💾 Position saved: ({LoadDataManager.userInGame.LastPosition.x}, {LoadDataManager.userInGame.LastPosition.y})");
            }
        });
    }

    private void LoadPlayerPosition()
    {
        if (LoadDataManager.userInGame?.LastPosition == null)
        {
            Debug.Log("⚠️ No saved position, using default (-0.5, -0.5)");
            transform.position = new Vector3(-0.5f, -0.5f, 0f);
            return;
        }

        Vector3 savedPosition = new Vector3(
            LoadDataManager.userInGame.LastPosition.x,
            LoadDataManager.userInGame.LastPosition.y,
            LoadDataManager.userInGame.LastPosition.z
        );

        transform.position = savedPosition;
        Debug.Log($"✅ Position loaded: {savedPosition}");
    }
}
