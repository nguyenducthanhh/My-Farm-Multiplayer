using UnityEngine;
using UnityEngine.UI;

public class AudioToggleButton : MonoBehaviour
{
    [SerializeField] private Button muteButton;
    [SerializeField] private Image soundOnImage;    // Hiển thị khi bật
    [SerializeField] private Image soundOffImage;   // Hiển thị khi tắt

    private void Start()
    {
        if (muteButton != null)
            muteButton.onClick.AddListener(OnMuteButtonClicked);

        UpdateAudioIcon();
    }

    private void OnMuteButtonClicked()
    {
        AudioManager.Instance.ToggleMute();
        UpdateAudioIcon();
        Debug.Log("🔊 Audio toggled");
    }

    // ✅ Cập nhật hiển thị image
    private void UpdateAudioIcon()
    {
        if (AudioManager.Instance.IsMuted())
        {
            // Tắt âm thanh → hiển thị icon tắt
            if (soundOnImage != null)
                soundOnImage.gameObject.SetActive(false);
            if (soundOffImage != null)
                soundOffImage.gameObject.SetActive(true);

            Debug.Log("🔇 Showing muted icon");
        }
        else
        {
            // Bật âm thanh → hiển thị icon bật
            if (soundOnImage != null)
                soundOnImage.gameObject.SetActive(true);
            if (soundOffImage != null)
                soundOffImage.gameObject.SetActive(false);

            Debug.Log("🔊 Showing unmuted icon");
        }
    }
}