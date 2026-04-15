using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level Config", menuName = "Level/Level Config")]
public class LevelConfig : ScriptableObject
{
    [System.Serializable]
    public class LevelThreshold
    {
        public int level;
        public int experienceRequired;  // XP cần để lên cấp này
    }

    [System.Serializable]
    public class LevelUnlock
    {
        public int requiredLevel;
        public string unlockedItemName;        // "grape_seed"
        public string unlockedItemDescription; // "Hạt giống nho"
        public UnlockType unlockType;          // Item hoặc Building
    }

    public enum UnlockType
    {
        Seed,        // Hạt giống
        AnimalPen,   // Chuồng vật nuôi
        Tool,        // Công cụ
        Feature      // Tính năng khác
    }

    [SerializeField] public List<LevelThreshold> levelThresholds = new List<LevelThreshold>();
    [SerializeField] public List<LevelUnlock> levelUnlocks = new List<LevelUnlock>();
}