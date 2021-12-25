using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CSAEngine
{
    public class Player : Creature
    {        
        public int Gold { get; set; }
        public int ExperiencePoints { get; set; }
        public int ExpNeededToLevel { get; set; }
        public int Level { get; set; }
        public Location CurrentLocation { get; set; }
        public List<InventoryItem> Inventory { get; set; }
        public List<PlayerQuest> Quests { get; set; }

        public Player(int currentHitPoints, int maximumHitPoints, int gold, int experiencePoints, int expNeededToLevel, int level) : base(currentHitPoints, maximumHitPoints)
        {            
            Gold = gold;
            ExperiencePoints = experiencePoints;
            ExpNeededToLevel = expNeededToLevel;
            Level = level;
            Inventory = new List<InventoryItem>();
            Quests = new List<PlayerQuest>();
        }

        public bool HasRequiredItemtoEnterLocation(Location location)
        {
            if(location.ItemRequiredToEnter == null)
            {
                return true;
            }
            
            return Inventory.Exists(ii => ii.Details.ID == location.ItemRequiredToEnter.ID);
        }

        public bool HasThisQuest(Quest quest)
        {
            return Quests.Exists(pq => pq.Details.ID == quest.ID);
        }

        public bool CompletedThisQuest(Quest quest)
        {
            foreach(PlayerQuest playerQuest in Quests)
            {
                if(playerQuest.Details.ID == quest.ID)
                {
                    return playerQuest.IsCompleted;
                }
            }

            return false;
        }

        public bool HasAllQuestItems(Quest quest)
        {

            foreach(QuestCompletionItem qci in quest.QuestCompletionItems)
            {
                if (!Inventory.Exists(ii => ii.Details.ID == qci.Details.ID && ii.Quantity == qci.Quantity))
                {
                    return false;
                }
            }
            return true;
        }

        public void RemoveQuestCompletionItems(Quest quest)
        {
            foreach(QuestCompletionItem qci in quest.QuestCompletionItems)
            {
                InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.ID == qci.Details.ID);

                if(item != null)
                {
                    item.Quantity -= qci.Quantity;
                }
            }
        }

        public void AddItemToInventory(Item itemToAdd)
        {
            InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.ID == itemToAdd.ID);

            if (item == null)
            {
                Inventory.Add(new InventoryItem(itemToAdd, 1));
            }else
            {
                item.Quantity++;
            }
        }

        public void MarkQuestCompleted(Quest quest)
        {

            PlayerQuest playerQuest = Quests.SingleOrDefault(pq => pq.Details.ID == quest.ID);
            if(playerQuest != null)
            {
                playerQuest.IsCompleted = true;
            }
        }

        public void LevelUp(int expNeededToLevel)
        {
            Level++;
            ExpNeededToLevel = expNeededToLevel + (Level * 100);            
            MaximumHitPoints += RandomNumberGenerator.NumberBetween((1 * Level), (5 * Level));            
        }

        
    }
}
