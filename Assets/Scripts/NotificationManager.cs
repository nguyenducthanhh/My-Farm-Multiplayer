using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [SerializeField] private Text rewardText;

    // ✅ Queue để lưu thông báo
    private Queue<(string message, float duration)> notificationQueue = new Queue<(string, float)>();
    private Coroutine currentNotificationCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ✅ Hàm gọi từ bất kỳ đâu - Default 3 giây
    public static void ShowReward(string message)
    {
        ShowReward(message, 3f);
    }

    // ✅ Hàm gọi với custom duration
    public static void ShowReward(string message, float duration)
    {
        if (Instance != null)
        {
            Instance.EnqueueNotification(message, duration);
        }
    }

    // ✅ Thêm thông báo vào queue
    private void EnqueueNotification(string message, float duration)
    {
        notificationQueue.Enqueue((message, duration));

        // Nếu chưa có thông báo đang hiển thị → start ngay
        if (currentNotificationCoroutine == null)
        {
            currentNotificationCoroutine = StartCoroutine(ProcessNotificationQueue());
        }
    }

    // ✅ Xử lý queue
    private IEnumerator ProcessNotificationQueue()
    {
        while (notificationQueue.Count > 0)
        {
            var (message, duration) = notificationQueue.Dequeue();

            // Hiển thị thông báo
            if (rewardText != null)
            {
                rewardText.text = message;
                rewardText.gameObject.SetActive(true);
                Debug.Log($" Notification: {message}");
            }

            // Hiển thị duration
            yield return new WaitForSeconds(duration);

            // Ẩn thông báo
            if (rewardText != null)
            {
                rewardText.gameObject.SetActive(false);
                rewardText.text = "";
            }
        }

        currentNotificationCoroutine = null;
    }
}