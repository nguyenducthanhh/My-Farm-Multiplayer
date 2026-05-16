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
    //public TileBase tb_Forest;
    [SerializeField] FirebaseDatabaseManager firebaseDatabaseManager;
    private DatabaseReference reference;
    public PlayerFarmController playerFarmController;

    

    //private float checkInterval = 1f; // moi 1 giay
    private float timer;
    void Update()
    {
        //timer += Time.deltaTime;
        //if (timer >= checkInterval)
        //{
        //    timer = 0f;
        //   // UpdateGrowingPlants();
        //}
    }

    private void Start()
    {
        //firebaseDatabaseManager = GetComponent<FirebaseDatabaseManager>();
        //firebaseDatabaseManager = GameObject.Find("DatabaseManager").GetComponent<FirebaseDatabaseManager>();

        if (LoadDataManager.userInGame.MapInGame.lstTilemapDetail != null)
        {
            LoadMapForUser();
        }
        //else
        //{
        //   // WriteAllTileMapToFirebase();

        //}
        FirebaseApp app = FirebaseApp.DefaultInstance;
        reference = FirebaseDatabase.DefaultInstance.RootReference;

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
        for (int i = 0;i < LoadDataManager.userInGame.MapInGame.GetLength();i++)
        {
            if (LoadDataManager.userInGame.MapInGame.lstTilemapDetail[i].x == x && LoadDataManager.userInGame.MapInGame.lstTilemapDetail[i].y == y)
            {
                LoadDataManager.userInGame.MapInGame.lstTilemapDetail[i].tilemapState = state;
                //LoadDataManager.userInGame.MapInGame.lstTilemapDetail[i].growTime = DateTime.Now;

                //firebaseDatabaseManager.WriteDatabase("Users/" + LoadDataManager.firebaseUser.UserId, LoadDataManager.userInGame.ToString());
                //27/3
                string mapJson = JsonConvert.SerializeObject(LoadDataManager.userInGame.MapInGame);

                FirebaseDatabase.DefaultInstance
                    .GetReference("Users")
                    .Child(LoadDataManager.firebaseUser.UserId)
                    .Child("MapInGame")  // ← Chỉ lưu Map
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

    //void UpdatePlantVisual(Vector3Int pos, PlantTileData plant, PlantData data)
    //{
    //    TileBase tile = data.growthTiles[plant.currentStage];

    //    tm_Forest.SetTile(pos, tile);
    //}

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

               //UpdatePlantVisual(pos, plant, plantData);
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
