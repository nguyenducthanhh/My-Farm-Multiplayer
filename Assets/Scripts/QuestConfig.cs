using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Quest Config", menuName = "Quest/Quest Config")]
public class QuestConfig : ScriptableObject
{
    [System.Serializable]
    public class QuestRequirement
    {
        public string itemName;
        public int quantity = 1;
        public string itemDescription;
    }

    [System.Serializable]
    public class Quest
    {
        public int questId;
        public string questName;
        public string questDescription;
        public List<QuestRequirement> requirements = new List<QuestRequirement>();

        public int rewardGold;
        public int rewardExperience;

    }

    [SerializeField] public List<Quest> allQuests = new List<Quest>();
}