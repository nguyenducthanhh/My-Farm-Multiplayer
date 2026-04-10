using System;

[Serializable]
public class SeedSlotData
{
    public string plantType;
    public int quantity;
    public string slotId;

    public SeedSlotData()
    {
        plantType = "";
        quantity = 0;
        slotId = "";
    }

    public SeedSlotData(string plantType, int quantity, string slotId)
    {
        this.plantType = plantType;
        this.quantity = quantity;
        this.slotId = slotId;
    }
}
