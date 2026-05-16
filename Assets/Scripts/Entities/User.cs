using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class User 
{
    public string Name { get; set; }
    public int Gold { get; set; }
    public Map MapInGame { get; set; }
    public List<InventoryItems> Inventory { get; set; }

    [System.Serializable]
    public class PlayerPosition
    {
        public float x;
        public float y;
        public float z;
    }
    public PlayerPosition LastPosition { get; set; }

    public User()
    {
        LastPosition = new PlayerPosition { x = 0, y = 0, z = 0 };
    }

    //public User(string name, int gold)
    //{
    //    Name = name;
    //    Gold = gold;
    //    Inventory = new List<InventoryItems>();
    //}
    //public User(string name, int gold, Map mapInGame)
    //{
    //    Name = name;
    //    Gold = gold;
    //    MapInGame = mapInGame;
    //    Inventory = new List<InventoryItems>();
    //}

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
