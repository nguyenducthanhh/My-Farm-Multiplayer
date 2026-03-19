using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryItems
{
    public string name {  get; set; }
    public string description { get; set; }
    public int quantity { get; set; } 
    public string spritePath { get; set; }
    public InventoryItems()
    {
        quantity = 1;
    }

    public InventoryItems(string name, string description, int quantity = 1)
    {
        this.name = name;
        this.description = description;
        this.quantity = quantity;
        this.spritePath = $"ItemSprites/{name}";
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }


}
