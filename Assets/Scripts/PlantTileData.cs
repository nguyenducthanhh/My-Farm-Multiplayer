using System;
using UnityEngine;

[Serializable]
public class PlantTileData
{
    public string plantType;

    public int x;
    public int y;

    public long plantTimeTicks;

    public int currentStage;

    public bool isWatered;

    // ===== Constructor =====
    public PlantTileData(string type, int posX, int posY)
    {
        plantType = type;
        x = posX;
        y = posY;
        plantTimeTicks = DateTime.UtcNow.Ticks;
        currentStage = 0;
        isWatered = false;
    }

    public PlantTileData() { }
    public double GetElapsedSeconds()
    {
        DateTime plantedTime = new DateTime(plantTimeTicks, DateTimeKind.Utc);
        return (DateTime.UtcNow - plantedTime).TotalSeconds;
    }
}