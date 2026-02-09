using PolyAndCode.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RecyclableInventory : MonoBehaviour, IRecyclableScrollRectDataSource
{
    [SerializeField]public RecyclableScrollRect _recyclableScrollRect;
    [SerializeField]
    private int _dataLength;

    [SerializeField] private GameObject inventoryGameObject;

    private List<InventoryItems> _invenItems = new List<InventoryItems>();
    private void Awake()
    {
        _recyclableScrollRect.DataSource = this;
    }

    public int GetItemCount()
    {
        return _invenItems.Count;
    }

    public void SetCell(ICell cell, int index)
    {
        var item = cell as CelltemData;
        item.ConfigureCell(_invenItems[index], index);
    }

    private void Start()
    {
        List<InventoryItems> lstItem = new List<InventoryItems>();
        //for(int i = 0; i < 50; i++)
        //{
        //    InventoryItems invenItem = new InventoryItems();
        //    invenItem.name = "Name_" + i.ToString();
        //    invenItem.description = "Des_" +i.ToString();
        //    lstItem.Add(invenItem);
        //}
        //SetLstItem(lstItem);
        //_recyclableScrollRect.ReloadData();
    }

    public void SetLstItem(List<InventoryItems> lst)
    {
        _invenItems = lst;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            inventoryGameObject.SetActive(!inventoryGameObject.activeSelf);

            if (inventoryGameObject.activeSelf && needReload)
            {
                _recyclableScrollRect.ReloadData();
                needReload = false;
            }
        }

    
    }
    private bool needReload = false;

    public void AddInventoryItem(InventoryItems item)
    {
        _invenItems.Add(item);
        needReload = true;
    }
}
