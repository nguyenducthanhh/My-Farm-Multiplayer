using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

public class Map
{
    public List<TilemapDetail> lstTilemapDetail {  get; set; }

    public Map()
    {

    }

    public Map(List<TilemapDetail> lstTilemapDetail)
    {
        this.lstTilemapDetail = lstTilemapDetail;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }

    public int GetLength()
    {
        if (lstTilemapDetail == null)
        {
            Debug.LogError("mapData chua duoc khoi tao!");
            return 0;
        }
        return lstTilemapDetail.Count;
    }
}
