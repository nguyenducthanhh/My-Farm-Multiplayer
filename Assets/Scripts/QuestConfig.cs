using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Quest Config", menuName = "Quest/Quest Config")]
public class QuestConfig : ScriptableObject
{
    [System.Serializable]
    public class QuestRequirement
    {
        public string itemName;      // "grilled_pork", "carrot", etc.
        public int quantity = 1;     // Số lượng cần
        public string itemDescription; // Mô tả item (hiển thị trong UI)
    }

    [System.Serializable]
    public class Quest
    {
        public int questId;
        public string questName;     // "Mang thịt lợn nướng cho tôi"
        public string questDescription;
        public List<QuestRequirement> requirements = new List<QuestRequirement>();

        public int rewardGold;
        public int rewardExperience;  // ✅ CHỈ cộng XP từ quest
        
        //public int minimumLevel = 1;
    }

    [SerializeField] public List<Quest> allQuests = new List<Quest>();
}