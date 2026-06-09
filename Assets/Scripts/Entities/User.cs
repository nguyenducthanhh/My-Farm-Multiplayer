using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class User
{
    public string Name { get; set; }
    public int Gold { get; set; }

    public Map MapInGame { get; set; }

    public List<InventoryItems> Inventory { get; set; }

    [System.Serializable]
    public class PlayerPosition
    {
        public float x;
        public float y;
        public float z;
    }

    public PlayerPosition LastPosition { get; set; }

    [System.Serializable]
    public class DailyRewardData
    {

        public string lastDailyClaimDate;           // Ngày claim quà hàng ngày cuối cùng
        public int lastDailyClaimGold;              // Gold nhận được từ quà hàng ngày
        public bool hasClaimedDailyToday;           // Đã claim quà hàng ngày hôm nay?

        public string lastRankingRewardDate;        // Ngày nhận quà ranking cuối cùng
        public int lastRankingRewardGold;           // Gold nhận được từ quà ranking
        public bool hasClaimedRankingToday;         // Đã claim quà ranking hôm nay?

        public string lastSavedSnapshotDate;        // Ngày snapshot được lưu

        public string accountCreatedDate;           // Ngày tạo account (để kiểm tra account mới)

        public DailyRewardData()
        {
            lastDailyClaimDate = "";
            lastDailyClaimGold = 0;
            hasClaimedDailyToday = false;

            lastRankingRewardDate = "";
            lastRankingRewardGold = 0;
            hasClaimedRankingToday = false;

            lastSavedSnapshotDate = "";

            //Khởi tạo accountCreatedDate = hôm nay
            accountCreatedDate = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
        }
    }

    public DailyRewardData DailyReward { get; set; }

    public User()
    {
        LastPosition = new PlayerPosition
        {
            x = 0,
            y = 0,
            z = 0
        };

        DailyReward = new DailyRewardData();
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}