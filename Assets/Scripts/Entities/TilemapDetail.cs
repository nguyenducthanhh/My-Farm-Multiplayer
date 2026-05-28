using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileMapState
{
    Ground,
    GroundWet,
    Grass,
    Forest,
 
}
public class TilemapDetail
{
    public int x { get; set; }
    public int y { get; set; }
    public TileMapState tilemapState { get; set; }
    public DateTime growTime { get; set; }
   
   
    
    public TilemapDetail()
    {

    }

    public TilemapDetail(int x, int y, TileMapState tilemapState, DateTime growTime)
    {
        this.x = x;
        this.y = y;
        this.tilemapState = tilemapState;
        this.growTime = growTime;
        

    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
