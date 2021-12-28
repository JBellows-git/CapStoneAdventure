using CSAEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;




namespace CapStoneAdventure
{
    public partial class CapStoneAdventure : Form
    {
        private Player _player;
        private Monster _currentMonster;
        private const string PLAYER_DATA_FILE_NAME = "PlayerData.xml";
        public CapStoneAdventure()
        {   
            InitializeComponent();

            if (File.Exists(PLAYER_DATA_FILE_NAME))
            {
                _player = Player.CreatePlayerFromXmlString(File.ReadAllText(PLAYER_DATA_FILE_NAME));
            }
            else
            {
                _player = Player.CreateDefaultPlayer();
            }

            lblHitPoints.DataBindings.Add("Text", _player, "CurrentHitPoints");
            lblGold.DataBindings.Add("Text", _player, "Gold");
            lblExperience.DataBindings.Add("Text", _player, "ExperiencePoints");
            lblExperienceNeededToLevel.DataBindings.Add("Text", _player, "ExpNeededToLevel");
            lblLevel.DataBindings.Add("Text", _player, "Level");

            dgvInventory.RowHeadersVisible = false;
            dgvInventory.AutoGenerateColumns = false;

            dgvInventory.DataSource = _player.Inventory;
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                Width = 197,
                DataPropertyName = "Description"
            });

            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Quantity",
                DataPropertyName = "Quantity"
            });

            MoveTo(_player.CurrentLocation);
            
        }

        

        private void btnNorth_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToNorth);
        }

        private void btnEast_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToEast);
        }

        private void btnSouth_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToSouth);
        }

        private void btnWest_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToWest);
        }

        private void MoveTo(Location newLocation)
        {

            //if an item is required to enter new square
            if (!_player.HasRequiredItemtoEnterLocation(newLocation))
            {
                rtbMessages.Text += "You must have a " + newLocation.ItemRequiredToEnter.Name + " to enter this location." + Environment.NewLine;
                ScrollToBottom();
                return;
            }

            _player.CurrentLocation = newLocation;

            //toggle available movement buttons
            btnNorth.Visible = (newLocation.LocationToNorth != null);
            btnEast.Visible = (newLocation.LocationToEast != null);
            btnSouth.Visible = (newLocation.LocationToSouth != null);
            btnWest.Visible = (newLocation.LocationToWest != null);

            //display location name and description
            rtbLocation.Text = newLocation.Name;
            rtbMessages.Text += newLocation.Description + Environment.NewLine;
            ScrollToBottom();

            //heal player - to be removed later
            _player.CurrentHitPoints = _player.MaximumHitPoints;

            //check location for quest, if player has quest already, and if quest is already complete
            //also checking for quest items and completing if they have enough
            if(newLocation.QuestAvailableHere != null)
            {
                bool playerAlreadyHasQuest = _player.HasThisQuest(newLocation.QuestAvailableHere);
                bool playerAlreadyCompleted = _player.CompletedThisQuest(newLocation.QuestAvailableHere);                

                if (playerAlreadyHasQuest)
                {
                    if (!playerAlreadyCompleted)
                    {
                        bool playerHasQuestItems = _player.HasAllQuestItems(newLocation.QuestAvailableHere);

                        if (playerHasQuestItems)
                        {
                            rtbMessages.Text += Environment.NewLine;
                            rtbMessages.Text += "You've completed the '" + newLocation.QuestAvailableHere.Name + "' quest!" + Environment.NewLine;

                            _player.RemoveQuestCompletionItems(newLocation.QuestAvailableHere);

                            //give quest comletion rewards
                            rtbMessages.Text += "You receive: " + Environment.NewLine;
                            rtbMessages.Text += newLocation.QuestAvailableHere.RewardExperiencePoints.ToString() + " experience points" + Environment.NewLine;
                            rtbMessages.Text += newLocation.QuestAvailableHere.RewardGold.ToString() + " gold" + Environment.NewLine;
                            rtbMessages.Text += newLocation.QuestAvailableHere.RewardItem.ToString() + Environment.NewLine;
                            ScrollToBottom();

                            _player.ExperiencePoints += newLocation.QuestAvailableHere.RewardExperiencePoints;
                            if (_player.ExperiencePoints >= _player.ExpNeededToLevel)
                            {
                                _player.LevelUp(_player.ExpNeededToLevel);                                
                                rtbMessages.Text += "You leveled up! Your Max HitPoints are now " + _player.MaximumHitPoints.ToString() + ".";
                            }
                            _player.Gold += newLocation.QuestAvailableHere.RewardGold;

                            //add reward item to inventory
                            _player.AddItemToInventory(newLocation.QuestAvailableHere.RewardItem);

                            //mark quest as complete
                            //find the quest in the quest list
                            _player.MarkQuestCompleted(newLocation.QuestAvailableHere);
                        }
                    }
                }
                else
                {
                    // the player does not have the quest

                    //display quest message
                    rtbMessages.Text += "You received the '" + newLocation.QuestAvailableHere.Name + "' quest." + Environment.NewLine;
                    rtbMessages.Text += newLocation.QuestAvailableHere.Description + Environment.NewLine;
                    rtbMessages.Text += "To complete it, return with: " + Environment.NewLine;                    

                    foreach (QuestCompletionItem qci in newLocation.QuestAvailableHere.QuestCompletionItems)
                    {
                        if(qci.Quantity == 1)
                        {
                            rtbMessages.Text += qci.Quantity.ToString() + " " + qci.Details.Name + Environment.NewLine;
                        }
                        else
                        {
                            rtbMessages.Text += qci.Quantity.ToString() + " " + qci.Details.NamePlural + Environment.NewLine;
                        }
                    }
                    rtbMessages.Text += Environment.NewLine;
                    ScrollToBottom();

                    //add quest to questList
                    _player.Quests.Add(new PlayerQuest(newLocation.QuestAvailableHere));
                }
            }

            //Check for Monster
            if(newLocation.MonsterLivingHere != null)
            {
                rtbMessages.Text += "You see a " + newLocation.MonsterLivingHere.Name + Environment.NewLine;
                ScrollToBottom();

                //make a monster from world.monster list
                Monster standardMonster = World.MonsterByID(newLocation.MonsterLivingHere.ID);

                _currentMonster = new Monster(standardMonster.ID, standardMonster.Name, standardMonster.MaximumDamage, 
                    standardMonster.RewardExperiencePoints, standardMonster.RewardGold, standardMonster.CurrentHitPoints, 
                    standardMonster.MaximumHitPoints);

                foreach(LootItem lootItem in standardMonster.LootTable)
                {
                    _currentMonster.LootTable.Add(lootItem);
                }

                cboWeapons.Visible = true;
                cboPotions.Visible = true;
                btnUseWeapon.Visible = true;
                btnUsePotion.Visible = true;
            }
            else
            {
                _currentMonster = null;

                cboWeapons.Visible = false;
                cboPotions.Visible = false;
                btnUseWeapon.Visible = false;
                btnUsePotion.Visible = false;
            }           

            //refresh quest list
            UpdateQuestListUI();

            //refresh weapon combobox
            UpdateWeaponListUI();

            //refresh potion combobox
            UpdatePotionListUI();
        }
        

        private void UpdateQuestListUI()
        {
            dgvQuests.RowHeadersVisible = false;

            dgvQuests.ColumnCount = 2;
            dgvQuests.Columns[0].Name = "Name";
            dgvQuests.Columns[0].Width = 197;
            dgvQuests.Columns[1].Name = "Done?";

            dgvQuests.Rows.Clear();

            foreach (PlayerQuest playerQuest in _player.Quests)
            {
                dgvQuests.Rows.Add(new[] { playerQuest.Details.Name, playerQuest.IsCompleted.ToString() });
            }
        }

        private void UpdateWeaponListUI()
        {
            List<Weapon> weapons = new List<Weapon>();
            foreach (InventoryItem invItem in _player.Inventory)
            {
                if (invItem.Details is Weapon)
                {
                    if (invItem.Quantity > 0)
                    {
                        weapons.Add((Weapon)invItem.Details);
                    }
                }
            }

            if (weapons.Count == 0)
            {
                //if there are no weapons then hide weapon display
                cboWeapons.Visible = false;
                btnUseWeapon.Visible = false;
            }
            else
            {
                cboWeapons.SelectedIndexChanged -= cboWeapons_SelectedIndexChanged;
                cboWeapons.DataSource = weapons;
                cboWeapons.SelectedIndexChanged += cboWeapons_SelectedIndexChanged;
                cboWeapons.DisplayMember = "Name";
                cboWeapons.ValueMember = "ID";

                if (_player.CurrentWeapon != null)
                {
                    cboWeapons.SelectedItem = _player.CurrentWeapon;
                }
                else
                {
                    cboWeapons.SelectedIndex = 0;
                }
            }
        }

        private void UpdatePotionListUI()
        {
            List<HealingPotion> healingPotions = new List<HealingPotion>();

            foreach (InventoryItem invItem in _player.Inventory)
            {
                if (invItem.Details is HealingPotion)
                {
                    if (invItem.Quantity > 0)
                    {
                        healingPotions.Add((HealingPotion)invItem.Details);
                    }
                }
            }

            if (healingPotions.Count == 0)
            {
                cboPotions.Visible = false;
                btnUsePotion.Visible = false;
            }
            else
            {
                cboPotions.DataSource = healingPotions;
                cboPotions.DisplayMember = "Name";
                cboPotions.ValueMember = "ID";
                cboPotions.SelectedIndex = 0;
            }
        }

        private void btnUseWeapon_Click(object sender, EventArgs e)
        {
            //get selected weapon
            Weapon currentWeapon = (Weapon)cboWeapons.SelectedItem;

            //Determine Damage to deal to Monster
            int damageToMonster = RandomNumberGenerator.NumberBetween(currentWeapon.MinimumDamage, currentWeapon.MaximumDamage);

            //Apply damage to monster
            _currentMonster.CurrentHitPoints -= damageToMonster;

            //display message
            rtbMessages.Text += "You hit the " + _currentMonster.Name + " for " + damageToMonster.ToString() + " points." + Environment.NewLine;
            ScrollToBottom();

            //Check if the monster is dead
            if (_currentMonster.CurrentHitPoints <= 0)
            {
                //Monster is dead
                rtbMessages.Text += Environment.NewLine +"You defeated the " + _currentMonster.Name + Environment.NewLine;

                //rewards
                _player.ExperiencePoints += _currentMonster.RewardExperiencePoints;
                rtbMessages.Text += "You receive " + _currentMonster.RewardExperiencePoints.ToString() + " experience points," + Environment.NewLine;
                _player.Gold += _currentMonster.RewardGold;
                rtbMessages.Text += "and " + _currentMonster.RewardGold.ToString() + " gold." + Environment.NewLine;
                if(_player.ExperiencePoints >= _player.ExpNeededToLevel)
                {
                    _player.LevelUp(_player.ExpNeededToLevel);                
                    rtbMessages.Text += "You leveled up! Your Max HitPoints are now " + _player.MaximumHitPoints.ToString() + ".";
                }
                ScrollToBottom();

                List<InventoryItem> loot = new List<InventoryItem>();
                foreach(LootItem lootItem in _currentMonster.LootTable)
                {
                    if (RandomNumberGenerator.NumberBetween(1 ,100) <= lootItem.DropPercentage)
                    {
                        loot.Add(new InventoryItem(lootItem.Details, 1));
                    }
                }
                
                if(loot.Count == 0)
                {
                    foreach(LootItem lootItem in _currentMonster.LootTable)
                    {
                        if (lootItem.IsDefaultItem)
                        {
                            loot.Add(new InventoryItem(lootItem.Details, 1));
                        }
                    }
                }

                foreach(InventoryItem inventoryItem in loot)
                {
                    _player.AddItemToInventory(inventoryItem.Details);

                    if(inventoryItem.Quantity == 1)
                    {
                        rtbMessages.Text += "You loot " + inventoryItem.Quantity.ToString() + " " + inventoryItem.Details.Name + Environment.NewLine;
                    }
                    else
                    {
                        rtbMessages.Text += "You loot " + inventoryItem.Quantity.ToString() + " " + inventoryItem.Details.NamePlural + Environment.NewLine;
                    }
                }                
                
                UpdateWeaponListUI();
                UpdatePotionListUI();

                rtbMessages.Text += Environment.NewLine;
                ScrollToBottom();
                MoveTo(_player.CurrentLocation);
            }
            else
            {
                //Monster is still alive
                int damageToPlayer = RandomNumberGenerator.NumberBetween(0, _currentMonster.MaximumDamage);
                rtbMessages.Text += "The " + _currentMonster.Name + " did " + damageToPlayer.ToString() + " points of damage." + Environment.NewLine;
                _player.CurrentHitPoints -= damageToPlayer;                

                if(_player.CurrentHitPoints <= 0)
                {
                    rtbMessages.Text += "The " + _currentMonster.Name + " killed you." + Environment.NewLine;
                    MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
                }
                ScrollToBottom();
            }
        }

        private void btnUsePotion_Click(object sender, EventArgs e)
        {
            //get the selected potion from the combobox
            HealingPotion potion = (HealingPotion)cboPotions.SelectedItem;

            //add heling amount to player
            _player.CurrentHitPoints = (_player.CurrentHitPoints + potion.AmountToHeal);

            //CurrentHitPoints cannot exceed MaximumHitPoints
            if(_player.CurrentHitPoints > _player.MaximumHitPoints)
            {
                _player.CurrentHitPoints = _player.MaximumHitPoints;
            }

            //remove the used potion
            foreach(InventoryItem ii in _player.Inventory)
            {
                if(ii.Details.ID == potion.ID)
                {
                    ii.Quantity--;
                    break;
                }
            }

            rtbMessages.Text += "You drink a " + potion.Name + Environment.NewLine;

            //Monster now gets its turn
            int damageToPlayer = RandomNumberGenerator.NumberBetween(0, _currentMonster.MaximumDamage);
            rtbMessages.Text += "The " + _currentMonster.Name + " did " + damageToPlayer + " points of damage." + Environment.NewLine;
            _player.CurrentHitPoints -= damageToPlayer;
            if (_player.CurrentHitPoints <= 0)
            {
                rtbMessages.Text += "The " + _currentMonster.Name + " killed you." + Environment.NewLine;
                MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
            }
            ScrollToBottom();

            //refresh player data in UI                        
            UpdatePotionListUI();
        }

        private void ScrollToBottom()
        {
            rtbMessages.SelectionStart = rtbMessages.Text.Length;
            rtbMessages.ScrollToCaret();
        }       

        private void CapStoneAdventure_FormClosing(object sender, FormClosingEventArgs e)
        {
            File.WriteAllText(PLAYER_DATA_FILE_NAME, _player.ToXmlString());
        }

        private void cboWeapons_SelectedIndexChanged(object sender, EventArgs e)
        {
            _player.CurrentWeapon = (Weapon)cboWeapons.SelectedItem;
        }
    }
}
