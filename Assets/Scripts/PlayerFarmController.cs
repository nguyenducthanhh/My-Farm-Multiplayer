using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.PlayerSettings;

public class PlayerFarmController : MonoBehaviour
{

    [SerializeField] Tilemap tM_Ground;
    [SerializeField] Tilemap tM_Grass;
    [SerializeField] Tilemap tM_Forest;
    [SerializeField] Tilemap tM_GroundWet;
    [SerializeField] TileBase tB_Ground;
    [SerializeField] TileBase tB_Grass;
    [SerializeField] TileBase tB_GroundWet;
    [SerializeField] PlayerMovementWithMouse player;

    [SerializeField] RecyclableInventory recyclableInventory;
   
    public TileMapManager tileMapManager;
    Vector2 input;

    [System.Serializable]
    public class PlantData
    {
        public string plantType;
        public List<TileBase> growthTiles;
        public float[] growthTimes;
    }
    public List<PlantData> allPlantDatas;
    // Them Dictionary
    private Dictionary<string, PlantData> plantDataDict = new Dictionary<string, PlantData>();

    // Them 2
    public PlantData GetPlantData(string type)
    {
        if (plantDataDict.TryGetValue(type, out PlantData data))
        {
            return data;
        }

        return null;
        //return allPlantDatas.Find(p => p.plantType == type);
    }
    Vector3Int pendingHoeCell;
    public bool isHoeing = false;
    public bool isWatering = false;
   
    public enum FacingDirection
    {
        Top,
        Down,
        Left,
        Right
    }


    private PlantData selectedPlantData;
    // Chon hat giong
    // Them 3
    public void SelectPlant(string plantType)
    {
        //selectedPlantData = allPlantDatas.Find(p => p.plantType == plantType);
        selectedPlantData = GetPlantData(plantType);
        if (selectedPlantData != null)
        {
            Debug.Log("Selected plant: " + selectedPlantData.plantType);
        }
    }


    public int GetStage(PlantData plantData, double elapsed)
    {
        if (plantData == null)
        {
            Debug.LogError("PlantData is null in GetStage!");
            return 0;
        }

        if (plantData.growthTimes == null || plantData.growthTimes.Length == 0)
        {
            Debug.LogError($"PlantData {plantData.plantType} has invalid growthTimes!");
            return 0;
        }
        for (int i = 0; i < plantData.growthTimes.Length; i++)
        {
            if (elapsed < plantData.growthTimes[i])
            {
                return i;
            }
        }
        if (plantData.growthTiles == null || plantData.growthTiles.Count == 0)
        {
            Debug.LogError($"PlantData {plantData.plantType} has invalid growthTiles!");
            return 0;
        }
        return plantData.growthTiles.Count - 1;
    }

    FacingDirection GetFacingDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            return dir.x > 0 ? FacingDirection.Right : FacingDirection.Left;
        }
        else
        {
            return dir.y > 0 ? FacingDirection.Top : FacingDirection.Down;
        }
    }
    // Them 1
    private void Awake()
    {
        plantDataDict.Clear();

        foreach (var plantData in allPlantDatas)
        {
            if (plantData == null || string.IsNullOrEmpty(plantData.plantType))
                continue;

            plantDataDict[plantData.plantType] = plantData;
        }
    }
    void Start()
    {
        //recyclableInventory = GameObject.Find("InventoryManager").GetComponent<RecyclableInventory>();
    }

    
    void Update()
    {
        HandleFarmActionWater();
        HandleFarmActionHoe();
        HandleFarmActionPlant();
        HandleFarmActionHarvest();
    }

    public float hoeDuration = 0.5f;

    public void HandleFarmActionHoe()
    {
        if (isHoeing) return;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
       
        if (Input.GetKeyDown(KeyCode.C))
        {
            Vector3Int cellPos = tM_Ground.WorldToCell(mouseWorldPos);
            TileBase tileBaseGround = tM_Ground.GetTile(cellPos);
            Debug.Log(cellPos);
            TileBase currentTileBase = tM_Grass.GetTile(cellPos);
            if (currentTileBase == tB_Grass && Vector3.Distance(player.transform.position, mouseWorldPos) <= 1f)
            {
                Vector2 dirToTile = (mouseWorldPos - player.transform.position).normalized;
                FacingDirection facing = GetFacingDirection(dirToTile);

                pendingHoeCell = cellPos;

                StartCoroutine(HoeRoutine(facing));
            }
        }        
    }

    IEnumerator HoeRoutine(FacingDirection facing)
    {
        isHoeing = true;
        player.PlayHoeAnimation(facing);

        yield return new WaitForSeconds(hoeDuration);

        tM_Grass.SetTile(pendingHoeCell, null);
        tileMapManager.SetStateForTilemapDetail(
            pendingHoeCell.x,
            pendingHoeCell.y,
            TileMapState.Ground
        );

        isHoeing = false;
    }

    // Watering
    public float waterDuration = 0.5f;
    public void HandleFarmActionWater()
    {
       
        if (isWatering) return;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        if (Input.GetKeyDown(KeyCode.E))
        {
            Vector3Int cellPos = tM_Ground.WorldToCell(mouseWorldPos);

            TileBase currentTileBaseForest = tM_Forest.GetTile(cellPos);
            TileBase currentTileBaseGround = tM_Ground.GetTile(cellPos);
            PlantTileData plant = tileMapManager.allPlantedTiles
                .Find(p => p.x == cellPos.x && p.y == cellPos.y);

            if (currentTileBaseGround != null && currentTileBaseForest != null
            && plant != null
            && !plant.isWatered 
            &&Vector3.Distance(player.transform.position, mouseWorldPos) <= 1f)

            {
                Vector2 dirToTile = (mouseWorldPos - player.transform.position).normalized;
                FacingDirection facing = GetFacingDirection(dirToTile);

                pendingHoeCell = cellPos;

                StartCoroutine(WaterRoutine(facing));
            }
        }
    }

    IEnumerator WaterRoutine(FacingDirection facing)
    {

        isWatering = true;
        player.PlayWaterAnimation(facing);

        yield return new WaitForSeconds(waterDuration);

        tM_Ground.SetTile(pendingHoeCell, null);

       tileMapManager.SetStateForTilemapDetail(
            pendingHoeCell.x,
            pendingHoeCell.y,
            TileMapState.GroundWet
        );

        PlantTileData plant = tileMapManager.allPlantedTiles
            .Find(p => p.x == pendingHoeCell.x && p.y == pendingHoeCell.y);


        if (plant != null)
        {
            plant.isWatered = true;
            plant.plantTimeTicks = DateTime.UtcNow.Ticks;     
            tileMapManager.SaveToFirebase(); 
        }

        isWatering = false;

    }

    
    public void HandleFarmActionPlant()
    {
        if (selectedPlantData == null) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        if (Input.GetMouseButtonDown(1))
        {
            Vector3Int cellPos = tM_Ground.WorldToCell(mouseWorldPos);

            TileBase currentTileBaseForest = tM_Forest.GetTile(cellPos);
            TileBase currentTileBaseGround = tM_Ground.GetTile(cellPos);
            TileBase currentTileBaseGrass = tM_Grass.GetTile(cellPos);

            if (currentTileBaseForest == null
                && currentTileBaseGround != null  
                && currentTileBaseGrass == null
                && Vector3.Distance(player.transform.position, mouseWorldPos) <= 2f)
            {

                PlantTileData newPlant = new PlantTileData();
                newPlant.plantType = selectedPlantData.plantType;
                newPlant.x = cellPos.x;
                newPlant.y = cellPos.y;
                newPlant.plantTimeTicks = DateTime.UtcNow.Ticks;
                newPlant.currentStage = 0;
                newPlant.isWatered = false;

                tileMapManager.AddPlant(newPlant);
                UpdatePlantVisual(cellPos, newPlant);


            }
        }
    }

    // Them 4
    public void HandleFarmActionHarvest()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        if (Input.GetKeyDown(KeyCode.M))
        {
            Vector3Int cellPos = tM_Ground.WorldToCell(mouseWorldPos);

            PlantTileData plant = tileMapManager
                .allPlantedTiles
                .Find(p => p.x == cellPos.x && p.y == cellPos.y);

            if (plant == null)
            {
                Debug.Log("No plant found at position");
                return;
            }


            //PlantData data = allPlantDatas.Find(p => p.plantType == plant.plantType);
            PlantData data = GetPlantData(plant.plantType);
            if (data == null)
            {
                Debug.LogError($"No PlantData found for {plant.plantType}");
                return;
            }
            if (plant.currentStage == data.growthTiles.Count - 1)
            {
                Debug.Log($"Harvesting {plant.plantType} at {cellPos}");

                tM_Forest.SetTile(cellPos, null);

                tM_Ground.SetTile(cellPos, tB_Ground);
                tM_Grass.SetTile(cellPos, tB_Grass);
                

                tileMapManager.SetStateForTilemapDetail(cellPos.x, cellPos.y, TileMapState.Grass);

                tileMapManager.allPlantedTiles.Remove(plant);
                tileMapManager.SaveToFirebase();
                if (recyclableInventory == null)
                {
                    Debug.LogError("RecyclableInventory is null! Make sure it's assigned in Inspector.");
                    return;
                }
                //InventoryItems item = new InventoryItems();
                //
                InventoryItems item = new InventoryItems(plant.plantType, $"Fresh {plant.plantType}", 1);
                Debug.Log($"Adding item to inventory: {item.name}");

                //item.name = plant.plantType;
                recyclableInventory.AddInventoryItem(item);
            }
        }
    }
    // Them 5
    public void UpdatePlantVisual(Vector3Int cellPos, PlantTileData tile)
    {
        PlantData data = GetPlantData(tile.plantType);
        //PlantData data = allPlantDatas.Find(p => p.plantType == tile.plantType);
        
       
        if (data == null)
        {
            return;
        }
        if (data.growthTimes == null || data.growthTimes.Length == 0 ||
            data.growthTiles == null || data.growthTiles.Count == 0)
        {
            return;
        }

        if (!tile.isWatered)
        {
            tile.currentStage = 0;
            tM_Forest.SetTile(cellPos, data.growthTiles[0]);
            return;
        }



        DateTime plantedTime = new DateTime(tile.plantTimeTicks, DateTimeKind.Utc);
        double elapsedTime = (DateTime.UtcNow - plantedTime).TotalSeconds;

      
        int stage = GetStage(data, elapsedTime);

        stage = Mathf.Clamp(stage, 0, data.growthTiles.Count - 1);

        tile.currentStage = stage;

        if (stage < data.growthTiles.Count && data.growthTiles[stage] != null)
        {
            tM_Forest.SetTile(cellPos, data.growthTiles[stage]);
        }

     
    }

   

    public void LoadPlantsFromFirebase()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("users")
            .Child(LoadDataManager.firebaseUser.UserId)
            .Child("Plants")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    string json = task.Result.GetRawJsonValue();

                    if (!string.IsNullOrEmpty(json))
                    {
                        tileMapManager.allPlantedTiles =
                            JsonConvert.DeserializeObject<List<PlantTileData>>(json);

                        foreach (var plant in tileMapManager.allPlantedTiles)
                        {
                            Vector3Int cellPos = new Vector3Int(plant.x, plant.y, 0);

                            UpdatePlantVisual(cellPos, plant);
                        }
                    }
                }
            });
    }

}
