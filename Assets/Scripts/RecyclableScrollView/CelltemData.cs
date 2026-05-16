
using PolyAndCode.UI;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

public class CelltemData : MonoBehaviour, ICell
{
    //UI
    public Text nameLabel;
    public Text desLabel;
    //Model
    private InventoryItems _contactInfo;
    private int _cellIndex;
    //This is called from the SetCell method in DataSource

    public void ConfigureCell(InventoryItems invenItems, int cellIndex)
    {
        _cellIndex = cellIndex;
        _contactInfo = invenItems;
        nameLabel.text = invenItems.name;
        desLabel.text = invenItems.description;
    }
}
