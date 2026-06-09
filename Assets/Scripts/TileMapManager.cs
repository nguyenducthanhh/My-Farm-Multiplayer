using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using static PlayerFarmController;

public class TileMapManager : MonoBehaviour
{
    public Tilemap tm_Ground;
    public Tilemap tm_Grass;
    public Tilemap tm_Forest;
    public Tilemap tm_GroundWet;
    public PlayerFarmController playerFarmController;

    void Update()
    {
    }

    private IEnumerator Start()
    {
        while (LoadDataManager.userInGame == null || !LoadDataManager.IsUserDataLoaded)
        {
            if (LoadDataManager.UserDataLoadFailed)
            {
                Debug.LogError("TileMapManager skipped because user data failed to load.");
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        if (!LoadDataManager.HasUserRecord)
        {
            Debug.LogError("TileMapManager skipped because user record is missing.");
            yield break;
        }

        if (LoadDataManager.userInGame.MapInGame?.lstTilemapDetail != null)
        {
            LoadMapForUser();
        }

        playerFarmController.LoadPlantsFromFirebase();

        StartCoroutine(PlantUpdateLoop());
    }

    public void LoadMapForUser()
    {
        MapToUI(LoadDataManager.userInGame.MapInGame);

    }

    public void TilemapDetailToTileBase(TilemapDetail tilemapDetail)
    {
        Vector3Int cellPos = new Vector3Int(tilemapDetail.x, tilemapDetail.y, 0);
        if (tilemapDetail.tilemapState == TileMapState.Ground)
        {

            tm_Grass.SetTile(cellPos, null);
            tm_Forest.SetTile(cellPos, null);

        }
        else if (tilemapDetail.tilemapState == TileMapState.GroundWet)
        {

            tm_Ground.SetTile(cellPos, null);
            tm_Grass.SetTile(cellPos, null);
            
        }
        else if (tilemapDetail.tilemapState == TileMapState.Grass)
        {
            tm_Forest.SetTile(cellPos, null);
        }
        else if (tilemapDetail.tilemapState == TileMapState.Forest)
        {
            tm_Grass.SetTile(cellPos, null);
            
        }

    }
    public void MapToUI(Map map)
    {
        Debug.Log("Load map to UI");
        for(int i = 0; i < map.GetLength(); i++)
        {
            TilemapDetailToTileBase(map.lstTilemapDetail[i]);
        }
    }

    public void SetStateForTilemapDetail(int x, int y, TileMapState state)
    {
        if (!LoadDataManager.IsUserDataLoaded || !LoadDataManager.HasUserRecord)
            return;

        for (int i = 0;i < LoadDataManager.userInGame.MapInGame.GetLength();i++)
        {
            if (LoadDataManager.userInGame.MapInGame.lstTilemapDetail[i].x == x && LoadDataManager.userInGame.MapInGame.lstTilemapDetail[i].y == y)
            {
                LoadDataManager.userInGame.MapInGame.lstTilemapDetail[i].tilemapState = state;

                string mapJson = JsonConvert.SerializeObject(LoadDataManager.userInGame.MapInGame);

                FirebaseDatabase.DefaultInstance
                    .GetReference("Users")
                    .Child(LoadDataManager.firebaseUser.UserId)
                    .Child("MapInGame") 
                    .SetRawJsonValueAsync(mapJson)
                    .ContinueWithOnMainThread(task =>
                    {
                        if (task.IsCompleted)
                        {
                            Debug.Log("Map saved successfully!");
                        }
                        else
                        {
                            Debug.LogError("Failed to save map: " + task.Exception);
                        }
                    });
                break;

            }
        }
    }

    public void SaveToFirebase()
    {
        if (!LoadDataManager.IsUserDataLoaded || !LoadDataManager.HasUserRecord)
            return;

        string json = JsonConvert.SerializeObject(allPlantedTiles);
        FirebaseDatabase.DefaultInstance
            .GetReference("Users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Plants")
            .SetRawJsonValueAsync(json);
    }

    public List<PlantTileData> allPlantedTiles = new List<PlantTileData>();

    public void AddPlant(PlantTileData plant)
    {
        allPlantedTiles.Add(plant);

        SaveToFirebase();
    }

    void UpdatePlants()
    {
        foreach (var plant in allPlantedTiles)
        {
            double elapsed = plant.GetElapsedSeconds();

            PlantData plantData = playerFarmController.GetPlantData(plant.plantType);

            int newStage = playerFarmController.GetStage(plantData, elapsed);

            if (newStage != plant.currentStage)
            {
                plant.currentStage = newStage;

                Vector3Int pos = new Vector3Int(plant.x, plant.y, 0);

                playerFarmController.UpdatePlantVisual(pos, plant);
            }
        }
    }


    IEnumerator PlantUpdateLoop()
    {
        while (true)
        {
            UpdatePlants();
            yield return new WaitForSeconds(1f);
        }
    }
}
