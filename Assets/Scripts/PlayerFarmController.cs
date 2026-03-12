using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerFarmController : MonoBehaviour
{
    [SerializeField] Tilemap tM_Ground;
    [SerializeField] Tilemap tM_Grass;
    [SerializeField] Tilemap tM_Forest;
    [SerializeField] Tilemap tM_GroundWet;
    [SerializeField] TileBase tB_Ground;
    [SerializeField] TileBase tB_Grass;
    [SerializeField] TileBase tB_Forest;
    [SerializeField] PlayerMovementWithMouse player;

    private RecyclableInventory recyclableInventory;
   
    public TileMapManager tileMapManager;
    public List<TileBase> lstTb_Pumpkin;
    Vector2 input;

    [System.Serializable]
    public class PlantData
    {
        public string plantType;
        public List<TileBase> growthTiles;
        public float[] growthTimes;
    }
    public List<PlantData> allPlantDatas;
    public PlantData GetPlantData(string type)
    {
        return allPlantDatas.Find(p => p.plantType == type);
    }
    Vector3Int pendingHoeCell;
    public bool isHoeing = false;
    public bool isWatering = false;
    public bool isatered = false;
    public enum FacingDirection
    {
        Top,
        Down,
        Left,
        Right
    }


    private PlantData selectedPlantData;
    // Chon hat giong
    public void SelectPlant(string plantType)
    {
        selectedPlantData = allPlantDatas.Find(p => p.plantType == plantType);

        if (selectedPlantData != null)
        {
            Debug.Log("Selected plant: " + selectedPlantData.plantType);
        }
    }


    public int GetStage(PlantData plantData, double elapsed)
    {
        for (int i = 0; i < plantData.growthTimes.Length; i++)
        {
            if (elapsed < plantData.growthTimes[i])
            {
                return i;
            }
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

    void Start()
    {
        recyclableInventory = GameObject.Find("InventoryManager").GetComponent<RecyclableInventory>();
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
        if (Input.GetKeyDown(KeyCode.W))
        {
            Vector3Int cellPos = tM_Ground.WorldToCell(mouseWorldPos);

            Debug.Log(cellPos);
            TileBase currentTileBaseGrass = tM_Grass.GetTile(cellPos);
            TileBase currentTileBaseGround = tM_Ground.GetTile(cellPos);
            TileBase currentTileBaseForest = tM_Forest.GetTile(cellPos);
            if (currentTileBaseForest != null && Vector3.Distance(player.transform.position, mouseWorldPos) <= 1f)
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
        //tileMapManager.SetStateForTilemapDetail(
        //    pendingHoeCell.x,
        //    pendingHoeCell.y,
        //    TileMapState.GroundWet
        //);

        isWatering = false;
    }

    //public void HandleFarmAction()
    //{
    //    if (!canHoe) return;

    //    tM_Grass.SetTile(pendingHoeCell, null);
    //    tileMapManager.SetStateForTilemapDetail(pendingHoeCell.x, pendingHoeCell.y, TileMapState.Ground);

    //    canHoe = false;
    //    isHoeing = false;

    //}

                    // Dang dung
    //public void HandleFarmActionPlant()
    //{
    //    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //    mouseWorldPos.z = 0f;

    //    if (Input.GetMouseButtonDown(1))
    //    {
    //        Vector3Int cellPos = tM_Ground.WorldToCell(mouseWorldPos);
    //        TileBase currentTileBaseForest = tM_Forest.GetTile(cellPos);
    //        TileBase currentTileBaseGrass = tM_Grass.GetTile(cellPos);
    //        TileBase currentTileBaseGround = tM_Ground.GetTile(cellPos);
    //        if (currentTileBaseForest == null && currentTileBaseGround  != null &&Vector3.Distance(player.transform.position, mouseWorldPos) <= 2f)
    //        {
    //            tileMapManager.SetStateForTilemapDetail(cellPos.x, cellPos.y, TileMapState.Pumpkin);
    //            StartCoroutine(GrowPlant(cellPos, tM_Forest, lstTb_Pumpkin));


    //        }


    //    }
    //}

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

            if (currentTileBaseForest == null
                && currentTileBaseGround != null
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
    //Dang dung
    //public void HandleFarmActionHarvest()
    //{
    //    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //    mouseWorldPos.z = 0f;

    //    if (Input.GetKeyDown(KeyCode.M))
    //    {
    //        Vector3Int cellPos = tM_Ground.WorldToCell(mouseWorldPos);
    //        TileBase currentTileBase = tM_Forest.GetTile(cellPos);

    //        if (currentTileBase == lstTb_Pumpkin[3])
    //        {
    //            tM_Grass.SetTile(cellPos, tB_Grass);
    //            tM_Forest.SetTile(cellPos, null);

    //            // Lay item va them vao tui do
    //            InventoryItems itemPumpkin = new InventoryItems();
    //            itemPumpkin.name = ("bi do");
    //            itemPumpkin.description = ("bi ngon");
    //            recyclableInventory.AddInventoryItem(itemPumpkin);
    //            tileMapManager.SetStateForTilemapDetail(cellPos.x, cellPos.y, TileMapState.Grass);
    //        }
    //    }
    //}

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

            if (plant == null) return;

            PlantData data = allPlantDatas.Find(p => p.plantType == plant.plantType);

            if (plant.currentStage == data.growthTiles.Count - 1)
            {
                tM_Forest.SetTile(cellPos, null);

                tileMapManager.allPlantedTiles.Remove(plant);
                tileMapManager.SaveToFirebase();

                InventoryItems item = new InventoryItems();
                item.name = plant.plantType;
                recyclableInventory.AddInventoryItem(item);
            }
        }
    }

    //public IEnumerator GrowPlant(Vector3Int cellPos, Tilemap tilemap, List<TileBase> lstTileBase)
    //{
    //    int currentStage = 0;
    //    while (currentStage < lstTileBase.Count)
    //    {
    //        tilemap.SetTile(cellPos, lstTileBase[currentStage]);

    //        yield return new WaitForSeconds(5);


    //    }
    //}
                    // Dang dung
    //public void UpdatePlantVisual(Vector3Int cellPos, PlantTileData tile)
    //{
    //    PlantData data = allPlants.Find(p => p.plantType == tile.plantType);
    //    if (data == null) return;

    //    DateTime plantedTime = new DateTime(tile.plantTimeTicks, DateTimeKind.Utc);
    //    double elapsedTime = (DateTime.UtcNow - plantedTime).TotalSeconds;

    //    int stage = 0;

    //    for (int i = 0; i < data.growthTimes.Length; i++)
    //    {
    //        if (elapsedTime >= data.growthTimes[i])
    //            stage = i;
    //    }

    //    stage = Mathf.Clamp(stage, 0, data.growthTiles.Count - 1);

    //    tM_Forest.SetTile(cellPos, data.growthTiles[stage]);
    //}

    public void UpdatePlantVisual(Vector3Int cellPos, PlantTileData tile)
    {
        PlantData data = allPlantDatas.Find(p => p.plantType == tile.plantType);
        if (data == null) return;

        DateTime plantedTime = new DateTime(tile.plantTimeTicks, DateTimeKind.Utc);
        double elapsedTime = (DateTime.UtcNow - plantedTime).TotalSeconds;

        int stage = 0;

        for (int i = 0; i < data.growthTimes.Length; i++)
        {
            if (elapsedTime >= data.growthTimes[i])
                stage = i;
        }

        stage = Mathf.Clamp(stage, 0, data.growthTiles.Count - 1);

        tile.currentStage = stage;

        tM_Forest.SetTile(cellPos, data.growthTiles[stage]);
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
