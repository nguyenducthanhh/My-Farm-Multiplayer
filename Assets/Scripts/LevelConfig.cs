using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level Config", menuName = "Level/Level Config")]
public class LevelConfig : ScriptableObject
{
    [System.Serializable]
    public class LevelThreshold
    {
        public int level;
        public int experienceRequired;
    }

    [System.Serializable]
    public class LevelUnlock
    {
        public int requiredLevel;
        public string unlockedItemName; 
        public string unlockedItemDescription;
        public UnlockType unlockType;
    }

    public enum UnlockType
    {
        Seed,     
        AnimalPen,   
        Tool,       
        Feature     
    }

    [SerializeField] public List<LevelThreshold> levelThresholds = new List<LevelThreshold>();
    [SerializeField] public List<LevelUnlock> levelUnlocks = new List<LevelUnlock>();
}