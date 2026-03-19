using PolyAndCode.UI;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCell : MonoBehaviour, ICell
{
    [SerializeField] private Image itemImage;
    [SerializeField] private Text itemNameText;
    [SerializeField] private Text quantityText;
    
    public void ConfigureCell(string itemName, int quantity)
    {
        if (itemNameText != null)
            itemNameText.text = itemName;
            
        if (quantityText != null)
            quantityText.text = $"x{quantity}";
    }
    
    public void SetSprite(Sprite sprite)
    {
        if (itemImage != null)
            itemImage.sprite = sprite;
    }
}
