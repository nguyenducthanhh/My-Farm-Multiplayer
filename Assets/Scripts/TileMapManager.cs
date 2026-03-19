using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;
using static PlayerFarmController;

public class TileMapManager : MonoBehaviour
{
    public Tilemap tm_Ground;
    public Tilemap tm_Grass;
    public Tilemap tm_Forest;
    public Tilemap tm_GroundWet;
    public TileBase tb_Forest;
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
        else
        {
            WriteAllTileMapToFirebase();

        }
        FirebaseApp app = FirebaseApp.DefaultInstance;
        reference = FirebaseDatabase.DefaultInstance.RootReference;

        playerFarmController.LoadPlantsFromFirebase();

        StartCoroutine(PlantUpdateLoop());
    }

    public void WriteAllTileMapToFirebase()
    {
        //List<TilemapDetail> tilemaps = new List<TilemapDetail>();
        //for (int x = tm_GroundWet.cellBounds.min.x; x < tm_GroundWet.cellBounds.max.x; x++)
        //{
        //    for(int y = tm_GroundWet.cellBounds.min.y; y < tm_GroundWet .cellBounds.max.y; y++)
        //    {
        //        TilemapDetail tm_detail = new TilemapDetail(x, y, TileMapState.Grass, DateTime.Now);
        //        tilemaps.Add(tm_detail);
        //    }

        //    LoadDataManager.userInGame.MapInGame = new Map(tilemaps);

        //    firebaseDatabaseManager.WriteDatabase("Users/" + LoadDataManager.firebaseUser.UserId, LoadDataManager.userInGame.ToString());
        //}


        List<TilemapDetail> tilemaps = new List<TilemapDetail>();

        for (int x = tm_GroundWet.cellBounds.min.x; x < tm_GroundWet.cellBounds.max.x; x++)
        {
            for (int y = tm_GroundWet.cellBounds.min.y; y < tm_GroundWet.cellBounds.max.y; y++)
            {
                TilemapDetail tm_detail = new TilemapDetail(x, y, TileMapState.Grass, DateTime.Now);
                tilemaps.Add(tm_detail);
            }
        }

        LoadDataManager.userInGame.MapInGame = new Map(tilemaps);

        firebaseDatabaseManager.WriteDatabase("Users/" + LoadDataManager.firebaseUser.UserId, LoadDataManager.userInGame.ToString());

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
                        // Dang dung
        //else if (tilemapDetail.tilemapState == TileMapState.Pumpkin)
        //{
        //    //double elapsedTime = (DateTime.Now - tilemapDetail.growTime).TotalSeconds;

        //    double elapsedTime = DateTime.Now.Subtract(tilemapDetail.growTime).TotalSeconds;
        //    tm_Grass.SetTile(cellPos, null);

        //    if (elapsedTime > 15)
        //    {
        //        tm_Forest.SetTile(cellPos, lstTb_Pumpkin[3]);
        //    }

        //    else if (elapsedTime > 10)
        //    {
        //        playerFarmController.StartCoroutine(playerFarmController.GrowPlant(cellPos, tm_Forest, lstTb_Pumpkin.GetRange(2, 2)));
        //        tm_Forest.SetTile(cellPos, lstTb_Pumpkin[2]);
        //    }
        //    else if (elapsedTime > 5)
        //    {
        //        playerFarmController.StartCoroutine(playerFarmController.GrowPlant(cellPos, tm_Forest, lstTb_Pumpkin.GetRange(1, 3)));
        //        tm_Forest.SetTile(cellPos, lstTb_Pumpkin[1]);
        //    }
        //    else
        //    {
        //        playerFarmController.StartCoroutine(playerFarmController.GrowPlant(cellPos, tm_Forest, lstTb_Pumpkin.GetRange(0, 4)));
        //        tm_Forest.SetTile(cellPos, lstTb_Pumpkin[0]);
        //    }

        //}

                            // Comment
        //else if (tilemapDetail.tilemapState == TileMapState.Pumpkin)
        //{
        //    double elapsedTime = (DateTime.Now - tilemapDetail.growTime).TotalSeconds;
        //    int newStage = GetPumpkinStage(elapsedTime);

        //    if (newStage != tilemapDetail.growStage)
        //    {
        //        tilemapDetail.growStage = newStage;

        //        tm_Grass.SetTile(cellPos, null);
        //        tm_Forest.SetTile(cellPos, lstTb_Pumpkin[newStage]);
        //    }
        //}

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
                firebaseDatabaseManager.WriteDatabase("Users/" + LoadDataManager.firebaseUser.UserId, LoadDataManager.userInGame.ToString());
              

            }
        }
    }

    public void SaveToFirebase()
    {
        string json = JsonConvert.SerializeObject(allPlantedTiles);
        FirebaseDatabase.DefaultInstance
            .GetReference("users")
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
