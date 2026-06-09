using UnityEngine;
using UnityEngine.UI;

public class AudioToggleButton : MonoBehaviour
{
    [SerializeField] private Button muteButton;
    [SerializeField] private Image soundOnImage;
    [SerializeField] private Image soundOffImage;

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
        Debug.Log(" Audio toggled");
    }

    private void UpdateAudioIcon()
    {
        if (AudioManager.Instance.IsMuted())
        {
            if (soundOnImage != null)
                soundOnImage.gameObject.SetActive(false);
            if (soundOffImage != null)
                soundOffImage.gameObject.SetActive(true);

            Debug.Log(" Showing muted icon");
        }
        else
        {
            if (soundOnImage != null)
                soundOnImage.gameObject.SetActive(true);
            if (soundOffImage != null)
                soundOffImage.gameObject.SetActive(false);

            Debug.Log(" Showing unmuted icon");
        }
    }
}