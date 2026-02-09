using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerFarmController : MonoBehaviour
{
    [SerializeField] Tilemap tM_Ground;
    [SerializeField] Tilemap tM_Grass;
    [SerializeField] Tilemap tM_Forest;
    [SerializeField] TileBase tB_Ground;
    [SerializeField] TileBase tB_Grass;
    [SerializeField] TileBase tB_Forest;
    [SerializeField] PlayerMovementWithMouse player;

    private RecyclableInventory recyclableInventory;
   
    public TileMapManager tileMapManager;
    public List<TileBase> lstTb_Pumpkin;
    Vector2 input;

    Vector3Int pendingHoeCell;
    public bool isHoeing = false;
    
    public enum FacingDirection
    {
        Top,
        Down,
        Left,
        Right
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

                //canHoe = true;
                //isHoeing = true;
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


    //public void HandleFarmAction()
    //{
    //    if (!canHoe) return;

    //    tM_Grass.SetTile(pendingHoeCell, null);
    //    tileMapManager.SetStateForTilemapDetail(pendingHoeCell.x, pendingHoeCell.y, TileMapState.Ground);

    //    canHoe = false;
    //    isHoeing = false;

    //}

    public void HandleFarmActionPlant()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        if (Input.GetMouseButtonDown(1))
        {
            Vector3Int cellPos = tM_Ground.WorldToCell(mouseWorldPos);
            TileBase currentTileBaseForest = tM_Forest.GetTile(cellPos);
            TileBase currentTileBaseGrass = tM_Grass.GetTile(cellPos);
            TileBase currentTileBaseGround = tM_Ground.GetTile(cellPos);
            if (currentTileBaseForest == null && currentTileBaseGrass == null && currentTileBaseGround  != null &&Vector3.Distance(player.transform.position, mouseWorldPos) <= 2f)
            {
                //tM_Forest.SetTile(cellPos, tB_Forest);
                StartCoroutine(GrowPlant(cellPos, tM_Forest, lstTb_Pumpkin));
                tileMapManager.SetStateForTilemapDetail(cellPos.x, cellPos.y, TileMapState.Pumpkin);
            }


        }
    }

    public void HandleFarmActionHarvest()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        if (Input.GetKeyDown(KeyCode.M))
        {
            Vector3Int cellPos = tM_Ground.WorldToCell(mouseWorldPos);
            TileBase currentTileBase = tM_Forest.GetTile(cellPos);

            if (currentTileBase == lstTb_Pumpkin[3])
            {
                tM_Grass.SetTile(cellPos, tB_Grass);
                tM_Forest.SetTile(cellPos, null);

                // Lay item va them vao tui do
                InventoryItems itemPumpkin = new InventoryItems();
                itemPumpkin.name = ("bi do");
                itemPumpkin.description = ("bi ngon");
                recyclableInventory.AddInventoryItem(itemPumpkin);
                tileMapManager.SetStateForTilemapDetail(cellPos.x, cellPos.y, TileMapState.Grass);
            }
        }
    }

    public IEnumerator GrowPlant(Vector3Int cellPos, Tilemap tilemap, List<TileBase> lstTileBase)
    {
        int currentStage = 0;
        while(currentStage < lstTileBase.Count)
        {
            tilemap.SetTile(cellPos, lstTileBase[currentStage]);
            yield return new WaitForSeconds(5);
            currentStage++;
        }
    }
}
