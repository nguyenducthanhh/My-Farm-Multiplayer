using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector2 mapMinBounds = Vector2.zero;
    [SerializeField] private Vector2 mapMaxBounds = new Vector2(100f, 100f);
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private bool debugBounds = true;

    [SerializeField]private Camera mainCamera;
    [SerializeField]private GameObject playerObj;
    private bool isInitialized = false;

    private void Start()
    {

        if (player == null)
        {
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        if (mainCamera == null)
        {
            Debug.LogError("❌ Camera component not found!");
            return;
        }

        if (player == null)
        {
            Debug.LogError("❌ Player not found! Make sure Player has 'Player' tag");
            return;
        }

        isInitialized = true;

        // Clamp ngay lần đầu
        Vector3 startPos = player.position;
        startPos.z = transform.position.z;
        transform.position = ClampCameraPosition(startPos);

        Debug.Log($"✅ Camera Controller initialized.");
        Debug.Log($"📐 Map Bounds - Min({mapMinBounds.x}, {mapMinBounds.y}) Max({mapMaxBounds.x}, {mapMaxBounds.y})");
        Debug.Log($"📷 Camera Size: {mainCamera.orthographicSize}, Aspect: {mainCamera.aspect:F2}");
    }

    private void LateUpdate()
    {
        if (!isInitialized || player == null || mainCamera == null)
            return;

        FollowPlayer();
    }

    private void FollowPlayer()
    {
        // Lấy vị trí nhân vật
        Vector3 targetPosition = player.position;
        targetPosition.z = transform.position.z;

        // Clamp target position TRƯỚC khi Lerp
        targetPosition = ClampCameraPosition(targetPosition);

        // Smooth follow
        if (smoothSpeed > 0)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    private Vector3 ClampCameraPosition(Vector3 position)
    {
        // Lấy kích thước viewport của camera
        float cameraHeight = mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        // Tính toán giới hạn camera dựa trên kích thước của nó
        float minX = mapMinBounds.x + cameraWidth;
        float maxX = mapMaxBounds.x - cameraWidth;
        float minY = mapMinBounds.y + cameraHeight;
        float maxY = mapMaxBounds.y - cameraHeight;

        // Xử lý trường hợp map quá nhỏ so với camera
        if (minX > maxX)
        {
            position.x = (minX + maxX) / 2f;
        }
        else
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
        }

        if (minY > maxY)
        {
            position.y = (minY + maxY) / 2f;
        }
        else
        {
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }

        return position;
    }

    // Hàm công khai để set bounds từ ngoài
    public void SetMapBounds(Vector2 minBounds, Vector2 maxBounds)
    {
        mapMinBounds = minBounds;
        mapMaxBounds = maxBounds;
        Debug.Log($"🎯 Map bounds updated: Min({mapMinBounds}) Max({mapMaxBounds})");
    }

    // Hàm công khai để set smooth speed
    public void SetSmoothSpeed(float speed)
    {
        smoothSpeed = Mathf.Clamp(speed, 0f, 10f);
    }

    // Debug: In thông tin camera
    public void DebugCameraInfo()
    {
        if (mainCamera != null)
        {
            float cameraHeight = mainCamera.orthographicSize;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            Debug.Log($"🔍 Camera Info: Width={cameraWidth:F2}, Height={cameraHeight:F2}");
            Debug.Log($"🔍 Camera Pos: {transform.position}");
            Debug.Log($"🔍 Player Pos: {player.position}");
        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (!debugBounds)
            return;

        // Vẽ bounds trong scene view
        Gizmos.color = Color.green;

        Vector3 minPos = new Vector3(mapMinBounds.x, mapMinBounds.y, 0);
        Vector3 maxPos = new Vector3(mapMaxBounds.x, mapMaxBounds.y, 0);

        // Vẽ hình chữ nhật bounds
        Gizmos.DrawLine(new Vector3(minPos.x, minPos.y, 0), new Vector3(maxPos.x, minPos.y, 0));
        Gizmos.DrawLine(new Vector3(maxPos.x, minPos.y, 0), new Vector3(maxPos.x, maxPos.y, 0));
        Gizmos.DrawLine(new Vector3(maxPos.x, maxPos.y, 0), new Vector3(minPos.x, maxPos.y, 0));
        Gizmos.DrawLine(new Vector3(minPos.x, maxPos.y, 0), new Vector3(minPos.x, minPos.y, 0));

        // Vẽ đường từ center (nếu có camera)
        if (Application.isPlaying && GetComponent<Camera>() != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                new Vector3((mapMinBounds.x + mapMaxBounds.x) / 2f, (mapMinBounds.y + mapMaxBounds.y) / 2f, 0),
                new Vector3(mapMaxBounds.x - mapMinBounds.x, mapMaxBounds.y - mapMinBounds.y, 1f)
            );
        }
    }
}