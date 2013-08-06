using System.Collections.Generic;
using System.Linq;
using SteamKit2;
using SteamTrade;
using System.Threading;
using SteamTrade.Exceptions;
using System;

namespace SteamBot
{
    /// <summary>
    /// The abstract base class for users of SteamBot that will allow a user
    /// to extend the functionality of the Bot.
    /// </summary>
    public abstract class UserHandler
    {
        protected enum Actions { DoNothing, NormalHarvest, CrateManager }

        protected Bot Bot;
        protected SteamID OtherSID, mySteamID;
        protected bool Success;

        // Used for Bot trade
        protected static List<SteamID> TradeReadyBots = new List<SteamID>();
        protected static Dictionary<SteamID, List<Inventory.Item>> BotItemMap = new Dictionary<SteamID, List<Inventory.Item>>();
        protected static List<SteamID> Admins = new List<SteamID>();

        // OnTradeAccept() isn't very reliable. May use this.
        // public static bool traded = false;

        // Used for Bot communication in trade
        // public static bool adderReadySet = false;
        // public static bool errorOcccured = false;
        protected static SteamID MainSID { get; set; }
        protected static SteamID ReceivingSID { get; set; }
        protected static SteamID CrateSID { get; set; }

        // Dragging Configuration all the way here just to make future options much easier to manage.
        protected static Configuration Settings = null;

        // Important Startup mode value, defaulting to "do nothing"
        protected static int BotMode = 0;

        // Recreating specific settings in the config here for readability in the UserHandlers
        #region Configuration Settings
        protected static int NumberOfBots = -1;

        protected static bool AutoCraftWeps, ManageCrates, CrateUHIsRunning, MainUHIsRunning;
        protected static int DeleteCrates, TransferCrates;

        protected static int[] ExcludedCrates;
        #endregion

        #region Crate Defindexes
        protected static readonly int[] StandardCrates = new int[3] { 5022, 5041, 5045 };

        // Salvaged and the new crates changed to rare drops (robo and summer 2013)
        protected static readonly int[] RareDropCrates = new int[3] { 5068, 5635, 5639 };
        #endregion

        public UserHandler(Bot bot, SteamID sid, Configuration config)
        {

            Bot = bot;
            OtherSID = sid;
            if (Settings == null)
            {
                BotMode = config.BotMode;
                NumberOfBots = config.TotalBots;

                AutoCraftWeps = config.Options.AutoCraftWeapons;
                ManageCrates = config.Options.ManageCrates;
                DeleteCrates = config.Options.DeleteCrates;
                TransferCrates = config.Options.TransferCrates;
                ExcludedCrates = config.Options.SavedCrates;

                CrateUHIsRunning = config.HasCrateUHLoaded;
                MainUHIsRunning = config.HasMainUHLoaded;

                Settings = config;
            }
        }

        /// <summary>
        /// Gets the Bot's current trade.
        /// </summary>
        /// <value>
        /// The current trade.
        /// </value>
        public Trade Trade
        {
            get { return Bot.CurrentTrade; }
        }

        /// <summary>
        /// Gets the log the bot uses for convenience.
        /// </summary>
        protected Log Log
        {
            get { return Bot.log; }
        }

        /// <summary>
        /// Gets a value indicating whether the other user is admin.
        /// </summary>
        /// <value>
        /// <c>true</c> if the other user is a configured admin; otherwise, <c>false</c>.
        /// </value>
        protected bool IsAdmin
        {
            get { return (Admins.Contains(OtherSID) || Bot.Admins.Contains(OtherSID)); }
        }

        /// <summary>
        /// Called when the user adds the bot as a friend.
        /// </summary>
        /// <returns>
        /// Whether to accept.
        /// </returns>
        public abstract bool OnFriendAdd();

        /// <summary>
        /// Called when the user removes the bot as a friend.
        /// </summary>
        public abstract void OnFriendRemove();

        /// <summary>
        /// Called whenever a message is sent to the bot.
        /// This is limited to regular and emote messages.
        /// </summary>
        public abstract void OnMessage(string message, EChatEntryType type);

        /// <summary>
        /// Called when the bot is fully logged in.
        /// </summary>
        public abstract void OnLoginCompleted();

        /// <summary>
        /// Called whenever a user requests a trade.
        /// </summary>
        /// <returns>
        /// Whether to accept the request.
        /// </returns>
        public abstract bool OnTradeRequest();

        /// <summary>
        /// Called when a chat message is sent in a chatroom
        /// </summary>
        /// <param name="chatID">The SteamID of the group chat</param>
        /// <param name="sender">The SteamID of the sender</param>
        /// <param name="message">The message sent</param>
        public virtual void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {

        }

        /// <summary>
        /// Called when an 'exec' command is given via botmanager.
        /// </summary>
        /// <param name="command">The command message.</param>
        public virtual void OnBotCommand(string command)
        {
            //Handle Command
        }
        #region Trade events
        // see the various events in SteamTrade.Trade for descriptions of these handlers.

        public abstract void OnTradeError(string error);

        public abstract void OnTradeTimeout();

        public virtual void OnTradeClose()
        {
            Bot.log.Warn("[USERHANDLER] TRADE CLOSED");
            Bot.CloseTrade();
        }

        public abstract void OnTradeInit();

        public abstract void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem);

        public abstract void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem);

        public abstract void OnTradeMessage(string message);

        public abstract void OnTradeReady(bool ready);

        public abstract void OnTradeAccept();

        #endregion Trade events

        #region Basic Trade Functions
        /// <summary>
        /// Cancels Trade, retrying 5 times if neccessary
        /// </summary>
        /// <returns>True if successful.</returns>
        protected bool CancelTrade()
        {
            Log.Debug("Cancelling trade");

            if (Trade == null)
            {
                Log.Error("There is no trade to Cancel");
                return false;
            }

            int x = 0;
            Success = false;
            while (Success == false && x < 5)
            {
                x++;
                Log.Debug("Loop #" + x);
                try
                {
                    Success = Trade.CancelTrade();
                }
                catch (TradeException te)
                {
                    Log.Warn("Cancel Trade failed.");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, te);
                    Log.Debug(s);
                }
                catch (Exception e)
                {
                    Log.Warn("Cancel Trade failed");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, e);
                    Log.Debug(s);
                }
            }
            if (!Success)
            {
                Log.Error("Could not cancel trade");
            }
            return Success;
        }
        /// <summary>
        /// Sets Ready, retrying 5 times if neccessary
        /// </summary>
        /// <returns>True if successful.</returns>
        protected bool SetReady(bool ready)
        {
            Log.Debug("Setting ready");
            int x = 0;
            Success = false;
            while (Success == false && x < 5)
            {
                x++;
                Log.Debug("Loop #" + x);
                try
                {
                    Success = Trade.SetReady(ready);
                }
                catch (TradeException te)
                {
                    Log.Warn("Set Ready failed.");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, te);
                    Log.Debug(s);
                    Thread.Sleep(250);
                }
                catch (Exception e)
                {
                    Log.Warn("Set Ready failed");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, e);
                    Log.Debug(s);
                    Thread.Sleep(250);
                }
            }
            if (!Success)
            {
                Log.Error("Could not set ready");
            }
            return Success;
        }
        /// <summary>
        /// Sends a trade message, retrying 5 times if neccessary
        /// </summary>
        /// <returns>True if successful.</returns>
        protected bool SendMessage(string message)
        {
            Log.Debug("Sending message: " + message);
            int x = 0;
            Success = false;
            while (Success == false && x < 5)
            {
                x++;
                Log.Debug("Loop #" + x);
                try
                {
                    Success = Trade.SendMessage(message);
                }
                catch (TradeException te)
                {
                    Log.Warn("Send Message failed.");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, te);
                    Log.Debug(s);
                    Thread.Sleep(250);
                }
                catch (Exception e)
                {
                    Log.Warn("Send Message failed");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, e);
                    Log.Debug(s);
                    Thread.Sleep(250);
                }
            }
            if (!Success)
            {
                Log.Error("Could not send message: " + message);
            }
            return Success;
        }
        /// <summary>
        /// Accepts a trade, retrying 5 times if neccessary
        /// </summary>
        /// <returns>True if successful.</returns>
        protected bool AcceptTrade()
        {
            Log.Debug("Accepting Trade");
            int x = 0;
            Success = false;
            while (Success == false && x < 5)
            {
                x++;
                Log.Debug("Loop #" + x);
                try
                {
                    Success = Trade.AcceptTrade();
                }
                catch (TradeException te)
                {
                    Log.Warn("Accept Trade failed.");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, te);
                    Log.Debug(s);
                    Thread.Sleep(250);
                }
                catch (Exception e)
                {
                    Log.Warn("Accept Trade failed");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, e);
                    Log.Debug(s);
                    Thread.Sleep(250);
                }
            }
            if (!Success)
            {
                Log.Error("Could not accept trade");
            }
            return Success;
        }
        /// <summary>
        /// Adds an item, retrying 5 times if neccessary
        /// </summary>
        /// <returns>True if successful.</returns>
        protected bool AddItem(Inventory.Item item)
        {
            int x = 0;
            Success = false;
            Log.Debug("Adding item: " + item.Defindex);
            while (Success == false && x < 5)
            {
                x++;
                Log.Debug("Loop #" + x);
                try
                {
                    Success = Trade.AddItem(item.Id);
                }
                catch (TradeException te)
                {
                    Log.Warn("Add Item failed.");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, te);
                    Log.Debug(s);
                    Thread.Sleep(250);
                }
                catch (Exception e)
                {
                    Log.Warn("Add Item failed");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, e);
                    Log.Debug(s);
                    Thread.Sleep(250);
                }
            }
            if (!Success)
            {
                Log.Error("Could not add item " + item.Id);
            }
            return Success;
        }
        /// <summary>
        /// Removes an item, retrying 10 times if neccessary
        /// </summary>
        /// <returns>True if successful.</returns>
        protected bool RemoveItem(Inventory.Item item)
        {
            int x = 0;
            Success = false;
            Log.Debug("Removing item: " + item.Defindex);
            while (Success == false && x < 5)
            {
                x++;
                Log.Debug("Loop #" + x);
                try
                {
                    Success = Trade.RemoveItem(item.Id);
                }
                catch (TradeException te)
                {
                    Log.Warn("Remove Item failed.");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, te);
                    Log.Debug(s);
                    Thread.Sleep(250);
                }
                catch (Exception e)
                {
                    Log.Warn("Remove Item failed");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, e);
                    Log.Debug(s);
                    Thread.Sleep(250);
                }
            }
            if (!Success)
            {
                Log.Error("Could not remove item" + item.Id);
            }
            return Success;
        }
        #endregion

        #region Trading

        /// <summary>
        /// Gets a list of items to trade. In addition to the specific handling of crates, all normal items and any saved crates will also be traded.
        /// </summary>
        /// <param name="option">
        /// How to handle crates
        /// 0 indicates no crates will be traded.
        /// 1 indicates only standard mann co. crates will be traded.
        /// 2 indicates only event crates will be traded. (e.g. eerie/summer/winter crates)
        /// 3 indicates all crates will be traded.
        /// </param>
        /// <returns>List of items to add.</returns>
        protected List<Inventory.Item> GetTradeItems(Inventory inv, int option)
        {
            var ToTrade = new List<Inventory.Item>();

            switch (option)
            {
                case 0:
                    foreach (Inventory.Item item in inv.Items)
                    {
                        // if it's not a crate or if it's an excluded or rare-drop crate
                        if (item != null && (!item.IsCrate || ExcludedCrates.Contains<int>(item.Defindex) || RareDropCrates.Contains<int>(item.Defindex)) && !item.IsNotTradeable)
                        {
                            ToTrade.Add(item);
                        }
                    }
                    break;

                case 1:
                    foreach (Inventory.Item item in inv.Items)
                    {
                        // if it's not a crate or if it's a standard, excluded, or rare-drop crate
                        if (item != null && (!item.IsCrate || StandardCrates.Contains<int>(item.Defindex) || ExcludedCrates.Contains<int>(item.Defindex) || RareDropCrates.Contains<int>(item.Defindex)) && !item.IsNotTradeable)
                        {
                            ToTrade.Add(item);
                        }
                    }
                    break;

                case 2:
                    foreach (Inventory.Item item in inv.Items)
                    {
                        // if it's not a standard crate
                        if (item != null && !StandardCrates.Contains<int>(item.Defindex) && !item.IsNotTradeable)
                        {
                            ToTrade.Add(item);
                        }
                    }
                    break;

                case 3:
                    foreach (Inventory.Item item in inv.Items)
                    {
                        if (item != null && !item.IsNotTradeable)
                        {
                            ToTrade.Add(item);
                        }
                    }
                    break;
            }

            return ToTrade;
        }

        /// <summary>
        /// Gets a list of crates to trade based on the TransferCrate option.
        /// </summary>
        /// <param name="option">
        /// How to handle crates
        /// 0 indicates no crates will be traded.
        /// 1 indicates only standard mann co. crates will be traded.
        /// 2 indicates only event crates will be traded. (e.g. eerie/summer/winter crates)
        /// 3 indicates all crates will be traded.
        /// </param>
        /// <returns>List of items to add.</returns>
        protected List<Inventory.Item> GetCrates(Inventory inv, int option)
        {
            var ToTrade = new List<Inventory.Item>();

            switch (option)
            {
                case 0:
                    return ToTrade;
                    
                case 1:
                    foreach (Inventory.Item item in inv.Items)
                    {
                        // if it's a standard crate
                        if (item != null && StandardCrates.Contains<int>(item.Defindex) && !item.IsNotTradeable)
                        {
                            ToTrade.Add(item);
                        }
                    }
                    break;

                case 2:
                    foreach (Inventory.Item item in inv.Items)
                    {
                        // if it's an event crate
                        if (item != null && (item.IsCrate && !StandardCrates.Contains<int>(item.Defindex) && !RareDropCrates.Contains<int>(item.Defindex) && !item.IsNotTradeable))
                        {
                            ToTrade.Add(item);
                        }
                    }
                    break;

                case 3:
                    foreach (Inventory.Item item in inv.Items)
                    {
                        if (item != null && (item.IsCrate && !RareDropCrates.Contains<int>(item.Defindex) && !item.IsNotTradeable))
                        {
                            ToTrade.Add(item);
                        }
                    }
                    break;
            }

            return ToTrade;
        }

        /// <summary>
        /// Adds all items from the given list.
        /// </summary>
        /// <returns>Number of items added.</returns>
        protected uint AddItemsFromList(List<Inventory.Item> items)
        {
            Log.Debug("Adding " + items.Count + " items.");
            uint added = 0;

            foreach (Inventory.Item item in items)
            {
                if (item != null && !item.IsNotTradeable && Trade.MyInventory.ContainsItem(item))
                {
                    if (AddItem(item))
                    {
                        Log.Debug("Item successfully added");
                        added++;
                    }
                    else
                    {
                        // Todo: instead of aborting on an item-add fail, just move on and cancel trade when 0 items are added.
                        Log.Debug("ADDING FAILED, returning to cancel");
                        return 0;
                    }
                }
            }
            return added;
        }

        #endregion

        #region Crafting
        // A little rough around the edges.

        /// <summary>
        /// Crafts an array of items.
        /// </summary>
        protected void Craft(Inventory.Item[] CraftItems)
        {
            ulong[] CraftIds = new ulong[CraftItems.Length];
            Log.Info("Crafting " + CraftItems.Length + " items.");

            int index = 0;
            foreach (Inventory.Item item in CraftItems)
            {
                CraftIds[index] = item.Id;
                Log.Debug("Crafting Item ID: " + item.Id);
                index++;
            }

            TF2GC.Crafting.CraftItems(Bot, CraftIds);

            // Give time for callbacks to update, otherwise backpack may not be up-to-date
            // after a large amount of crafting. Sleep may be moved elsewhere to allow for
            // fast crafting when inventory refresh isn't required.
            Thread.Sleep(100);
        }

        /// <summary>
        /// Crafts all clean, unique weapons to ref (doesn't check for item duplicates or anything too fancy yet)
        /// Also calls GetInventory() so it's not necessary prior
        /// </summary>
        /// <returns>false if there were no weapons to craft otherwise, true</returns>
        protected bool AutoCraftAll()
        {
            Log.Info("AutoCrafting Weapons");

            // Will hold all craftable/tradable weapons
            List<Inventory.Item> MyCleanWeapons = new List<Inventory.Item>();

            Log.Debug("Getting Inventory");
            Bot.GetInventory();

            MyCleanWeapons = GetCleanItemsOfMaterial("weapon");
            Log.Info("Number of weapons to craft: " + MyCleanWeapons.Count);
            if (MyCleanWeapons.Count < 2)
            {
                Log.Info("There are not enough weapons to craft");
                return false;
            }

            Log.Info("Setting Game State to Playing TF2.");
            Bot.SetGamePlaying(440);

            int ScrapMade = ScrapWeapons(MyCleanWeapons);
            Log.Info("Scrap made: " + ScrapMade);

            Log.Info("Combining Metal");
            CombineAllMetal();

            Log.Info("Resetting Game State");
            Bot.SetGamePlaying(0);
            Log.Success("Crafting Complete");
            return true;
        }

        /// <summary>
        /// Scraps a List of Weapons into scrap.
        /// </summary>
        /// <param name="CleanWeapons">List of all weapons to scrap.</param>
        /// <returns>Number of scrap made.</returns>
        protected int ScrapWeapons(List<Inventory.Item> CleanWeapons)
        {
            if (Bot.CurrentGame != 440)
            {
                Bot.SetGamePlaying(440);
            }

            Log.Info("Sorting items by class.");
            List<List<Inventory.Item>> allWeapons = SortItemsByClass(CleanWeapons);

            List<Inventory.Item> multiWeps = allWeapons[9];

            // Seperate the multi-class weapons again because it's so special
            allWeapons.RemoveAt(9);

            Inventory.Item[] CraftItems;
            int scrapMade = 0;
            Log.Info("Beginning smelt sequence.");

            // Crafting off pairs of weapons in class lists
            foreach (List<Inventory.Item> list in allWeapons)
            {
                while (list.Count > 1)
                {
                    CraftItems = new Inventory.Item[2];
                    CraftItems[0] = list[0];
                    list.RemoveAt(0);
                    CraftItems[1] = list[0];
                    list.RemoveAt(0);
                    Craft(CraftItems);
                    scrapMade++;
                }
            }

            //(Still needs to be optimised) Crafting the remaining multi-class weapons
            Log.Info("Scrapping multi-class weps");
            foreach (List<Inventory.Item> list in allWeapons)
            {
                CraftItems = new Inventory.Item[2];
                if (list.Count > 0)
                {
                    foreach (Inventory.Item item in multiWeps)
                    {
                        List<string> classes = new List<string>(Trade.CurrentSchema.GetItem(item.Defindex).UsableByClasses);
                        string[] itemClass = Trade.CurrentSchema.GetItem(list[0].Defindex).UsableByClasses;
                        if (classes.Contains(itemClass[0]))
                        {
                            CraftItems[0] = item;
                            // I can remove items from this foreach because I'm going to break anyway
                            multiWeps.Remove(item);
                            CraftItems[1] = list[0];
                            list.RemoveAt(0);
                            Craft(CraftItems);
                            scrapMade++;
                            break;
                        }
                    }
                }
            }
            // Clean up any leftover
            while (multiWeps.Count > 1)
            {
                CraftItems = new Inventory.Item[2];
                CraftItems[0] = multiWeps[0];
                multiWeps.RemoveAt(0);
                CraftItems[1] = multiWeps[0];
                multiWeps.RemoveAt(0);
                Craft(CraftItems);
                scrapMade++;
            }
            return scrapMade;
        }

        /// <summary>
        /// Crafts all scrap into reclaimed, then all reclaimed into refined.
        /// </summary>
        protected void CombineAllMetal()
        {
            if (Bot.CurrentGame != 440)
            {
                Bot.SetGamePlaying(440);
            }
            // May use for inventory management
            //List<Inventory.Item> myScrap = new List<Inventory.Item>();
            //List<Inventory.Item> myReclaimed = new List<Inventory.Item>();
            //List<Inventory.Item> myRefined = new List<Inventory.Item>();

            Log.Info("Combining all metal");

            // Scrap, Reclaimed, and Refined are defindex 5000, 5001, 5002 respectively

            List<Inventory.Item> ScrapToCraft = new List<Inventory.Item>();

            Log.Debug("Getting Inventory");
            Thread.Sleep(300); // Just another pause to be sure inventory has updated.
            Bot.GetInventory();

            ScrapToCraft = Bot.MyInventory.GetItemsByDefindex(5000);

            Log.Debug("Total Scrap: " + ScrapToCraft.Count);
            Log.Debug("Crafting " + (ScrapToCraft.Count / 3) + " Reclaimed.");

            while (ScrapToCraft.Count > 2)
            {
                Inventory.Item[] CraftItems = new Inventory.Item[3];
                CraftItems[0] = ScrapToCraft[0];
                CraftItems[1] = ScrapToCraft[1];
                CraftItems[2] = ScrapToCraft[2];
                Craft(CraftItems);
                for (int x = 0; x < 3; x++)
                {
                    ScrapToCraft.RemoveAt(0);
                }
            }

            List<Inventory.Item> ReclaimedToCraft = new List<Inventory.Item>();

            Log.Debug("Getting Inventory");
            Thread.Sleep(300); // Just another pause to be sure inventory has updated.
            Bot.GetInventory();

            ReclaimedToCraft = Bot.MyInventory.GetItemsByDefindex(5001);

            Log.Debug("Total Reclaimed: " + ReclaimedToCraft.Count);
            Log.Debug("Crafting " + (ReclaimedToCraft.Count / 3) + " Refined.");

            while (ReclaimedToCraft.Count > 2)
            {
                Inventory.Item[] craftIds = new Inventory.Item[3];
                craftIds[0] = ReclaimedToCraft[0];
                craftIds[1] = ReclaimedToCraft[1];
                craftIds[2] = ReclaimedToCraft[2];
                Craft(craftIds);
                for (int x = 0; x < 3; x++)
                {
                    ReclaimedToCraft.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Gets all unique, craftable, and tradable items of the specified crafting material
        /// </summary>
        /// <param name="CraftingMaterial">CraftingMaterial from schema</param>
        /// <returns>List of items in inventory that are craftable, tradable, and match
        /// the crafting material.</returns>
        protected List<Inventory.Item> GetCleanItemsOfMaterial(string CraftingMaterial)
        {
            Log.Debug("Getting list of craftable weapons from schema");
            var CraftableItemList = Trade.CurrentSchema.GetItemsByCraftingMaterial(CraftingMaterial);
            var myItems = new List<Inventory.Item>();

            foreach (Inventory.Item invItem in Bot.MyInventory.Items)
            {
                if (!invItem.IsNotTradeable && !invItem.IsNotCraftable && invItem.Quality == "unique")
                {
                    foreach (Schema.Item scItem in CraftableItemList)
                    {
                        if (invItem.Defindex == scItem.Defindex)
                        {
                            myItems.Add(invItem);
                            break;
                        }
                    }
                }
            }
            return myItems;
        }

        /// <summary>
        /// Sorts items based on which of the 9 classes can use them
        /// </summary>
        /// <param name="items">List of items to sort</param>
        /// <returns>Lists of Lists of all the weapons in appropriate class lists, the 10th
        /// list is for multi-class weapons.</returns>
        protected List<List<Inventory.Item>> SortItemsByClass(List<Inventory.Item> items)
        {
            // List for each class' weps
            List<Inventory.Item> scoutWeps = new List<Inventory.Item>();
            List<Inventory.Item> soldierWeps = new List<Inventory.Item>();
            List<Inventory.Item> pyroWeps = new List<Inventory.Item>();
            List<Inventory.Item> demoWeps = new List<Inventory.Item>();
            List<Inventory.Item> heavyWeps = new List<Inventory.Item>();
            List<Inventory.Item> engyWeps = new List<Inventory.Item>();
            List<Inventory.Item> medicWeps = new List<Inventory.Item>();
            List<Inventory.Item> sniperWeps = new List<Inventory.Item>();
            List<Inventory.Item> spyWeps = new List<Inventory.Item>();
            List<Inventory.Item> multiWeps = new List<Inventory.Item>();

            // List of the above lists
            List<List<Inventory.Item>> allWeps = new List<List<Inventory.Item>>(10);

            allWeps.Add(scoutWeps);
            allWeps.Add(soldierWeps);
            allWeps.Add(pyroWeps);
            allWeps.Add(demoWeps);
            allWeps.Add(heavyWeps);
            allWeps.Add(engyWeps);
            allWeps.Add(medicWeps);
            allWeps.Add(sniperWeps);
            allWeps.Add(spyWeps);

            foreach (Inventory.Item item in items)
            {
                var classes = Trade.CurrentSchema.GetItem(item.Defindex).UsableByClasses;
                if (classes.Length > 1)
                {
                    multiWeps.Add(item);
                }
                else
                    switch (classes[0])
                    {
                        case ("Scout"): scoutWeps.Add(item); break;
                        case ("Soldier"): soldierWeps.Add(item); break;
                        case ("Pyro"): pyroWeps.Add(item); break;
                        case ("Demoman"): demoWeps.Add(item); break;
                        case ("Heavy"): heavyWeps.Add(item); break;
                        case ("Engineer"): engyWeps.Add(item); break;
                        case ("Medic"): medicWeps.Add(item); break;
                        case ("Sniper"): sniperWeps.Add(item); break;
                        case ("Spy"): spyWeps.Add(item); break;
                        default: Log.Debug("what happened? 10th class? idk"); break;
                    }
            }
            Log.Debug("Number of...");
            Log.Debug("Scout items: " + scoutWeps.Count);
            Log.Debug("Soldier items: " + soldierWeps.Count);
            Log.Debug("Pyro items: " + pyroWeps.Count);
            Log.Debug("Demoman items: " + demoWeps.Count);
            Log.Debug("Heavy items: " + heavyWeps.Count);
            Log.Debug("Engineer items: " + engyWeps.Count);
            Log.Debug("Medic items: " + medicWeps.Count);
            Log.Debug("Sniper items: " + sniperWeps.Count);
            Log.Debug("Spy items: " + spyWeps.Count);
            Log.Debug("Multi-class items (pain train + half-zatoichi): " + multiWeps.Count);

            // Add multiWeps back to return allWeps
            allWeps.Add(multiWeps);

            return allWeps;
        }

        #endregion

        #region Item Management

        /// <summary>
        /// Deletes an item
        /// </summary>
        protected void DeleteItem(Inventory.Item item)
        {
            Log.Info("Deleting item: " + item.Id);
            TF2GC.Items.DeleteItem(Bot, item.Id);

            // Again, some delay seems required for repetive commands - unsure how much.
            Thread.Sleep(100);
        }

        /// <summary>
        /// Deletes certain Crates based on the int parameter.
        /// </summary>
        /// <param name="option">
        /// Rare-dropping crates and crates in SavedCrates are excluded.
        /// 0 indicates no crates will be deleted.
        /// 1 indicates only standard mann co. crates will be deleted.
        /// 2 indicates only event crates will be deleted. (e.g. eerie/summer/winter crates)
        /// 3 indicates all crates will be deleted.
        /// </param>
        /// <returns>the number of crates deleted.</returns>
        protected int DeleteSelectedCrates(int option)
        {
            Log.Info("Setting Game State to Playing TF2.");
            Bot.SetGamePlaying(440);

            var ToDelete = new List<Inventory.Item>();

            switch (option)
            {
                case 0:
                    Log.Info("No Crates are selected to be deleted");
                    return 0;

                case 1:
                    foreach (Inventory.Item item in Bot.MyInventory.Items)
                    {
                        // if it is a standard crate and not excluded
                        if ((item != null) && StandardCrates.Contains<int>(item.Defindex) && !ExcludedCrates.Contains<int>(item.CrateSeriesNumber))
                        {
                            ToDelete.Add(item);
                        }
                    }
                    break;

                case 2:
                    foreach (Inventory.Item item in Bot.MyInventory.Items)
                    {
                        // if it is a crate, not a standard drop, not a rare drop, and not excluded
                        if ((item != null) && (item.IsCrate) && !StandardCrates.Contains<int>(item.Defindex) && !RareDropCrates.Contains<int>(item.Defindex) && !ExcludedCrates.Contains<int>(item.CrateSeriesNumber))
                        {
                            ToDelete.Add(item);
                        }
                    }
                    break;

                case 3:
                    foreach (Inventory.Item item in Bot.MyInventory.Items)
                    {
                        // if it is a crate, is not a rare drop, and is not excluded
                        if ((item != null) && (item.IsCrate) && !RareDropCrates.Contains<int>(item.Defindex) && !ExcludedCrates.Contains<int>(item.CrateSeriesNumber))
                        {
                            ToDelete.Add(item);
                        }
                    }
                    break;
            }

            // More items to delete can be added here (e.g. mann co. cap, seal mask, etc.)
            Log.Info("Deleting " + ToDelete.Count + " crates.");

            foreach (Inventory.Item item in ToDelete)
            {
                DeleteItem(item);
            }

            // May add some verification that all items were deleted.

            Log.Info("Resetting Game State");
            Bot.SetGamePlaying(0);

            return ToDelete.Count;
        }
        #endregion
    }
}