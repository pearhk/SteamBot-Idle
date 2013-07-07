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
        protected Bot Bot;
        protected SteamID OtherSID;
        protected SteamID mySteamID;
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
        protected static SteamID PrimaryAltSID { get; set; }

        // Dragging Configuration all the way here just to make future options much easier to manage.
        protected static Configuration Settings = null;
        
        // Specific settings in the config
        protected static int NumberOfBots = -1;
        protected static bool AutoCraftWeps;
        protected static bool DeleteCrates;
        protected static bool ExcludedCrates;
        public UserHandler(Bot bot, SteamID sid, Configuration config)
        {
            
            Bot = bot;
            OtherSID = sid;
            if (Settings == null)
            {
                NumberOfBots = config.TotalBots;
                AutoCraftWeps = config.AutoCraftWeapons;
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
                Thread.Sleep(500);
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
                }
                catch (Exception e)
                {
                    Log.Warn("Set Ready failed");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, e);
                    Log.Debug(s);
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
                }
                catch (Exception e)
                {
                    Log.Warn("Send Message failed");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, e);
                    Log.Debug(s);
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
                }
                catch (Exception e)
                {
                    Log.Warn("Accept Trade failed");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, e);
                    Log.Debug(s);
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
                }
                catch (Exception e)
                {
                    Log.Warn("Add Item failed");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, e);
                    Log.Debug(s);
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
                }
                catch (Exception e)
                {
                    Log.Warn("Remove Item failed");
                    var s = string.Format("Loop #{0}{1}Exception:{2}", x, Environment.NewLine, e);
                    Log.Debug(s);
                }
            }
            if (!Success)
            {
                Log.Error("Could not remove item" + item.Id);
            }
            return Success;
        }

        ///// <summary>
        ///// Attempts the specified trade action, retrying 5 times if neccessary.
        ///// Attempts to cancel the trade if unsuccesful.
        ///// </summary>
        ///// <param name="action">The desired in-trade action.</param>
        ///// <param name="ready">Required only for SetReady</param>
        ///// <param name="message">Required only for SendMessage</param>
        ///// <param name="item">Required only for Add/Remove Item</param>
        ///// <returns>True if successful.</returns>
        //protected bool TryAction(TradeAction action, Inventory.Item item = null, bool ready = false, string message = null)
        //{
        //    Log.Debug("Action: " + action);
        //    int x = 0;
        //    Success = false;
        //    while (Success == false && x < 5)
        //    {
        //        x++;
        //        try
        //        {
        //            switch (action)
        //            {
        //                case TradeAction.CancelTrade:
        //                    Success = Trade.CancelTrade(); break;
        //                case TradeAction.AddItem:
        //                    Success = Trade.AddItem(item.Id); break;
        //                case TradeAction.RemoveItem:
        //                    Success = Trade.RemoveItem(item.Id); break;
        //                case TradeAction.SetReady:
        //                    Success = Trade.SetReady(ready); break;
        //                case TradeAction.AcceptTrade:
        //                    Success = Trade.AcceptTrade(); break;
        //                case TradeAction.SendMessage:
        //                    Success = Trade.SendMessage(message); break;
        //                default: Log.Error("Invalid trade action: " + action); break;
        //            }
        //        }
        //        catch (TradeException te)
        //        {
        //            Log.Warn(action + " failed.");
        //            Log.Debug(string.Format("Loop #{0}\nException:{1}", x, te));
        //        }
        //        catch (Exception e)
        //        {
        //            Log.Warn(action + " failed.");
        //            Log.Debug(string.Format("Loop #{0}\nException:{1}", x, e));
        //        }
        //    }
        //    if (!Success)
        //    {
        //        Log.Error("Could not " + action);
        //        if (action != TradeAction.CancelTrade)
        //        {
        //            TryAction(TradeAction.CancelTrade);
        //        }
        //        else
        //        {
        //            Thread.Sleep(500);
        //        }
        //    }
        //    return Success;
        //}
        #endregion

        #region Trading

        /// <summary>
        /// Gets all tradeable items other than normal crates.
        /// </summary>
        /// <returns>List of items to add.</returns>
        protected List<Inventory.Item> GetAllNonCrates(Inventory inv)
        {
            var items = new List<Inventory.Item>();
            foreach (Inventory.Item item in inv.Items)
            {
                if (item.Defindex != 5022 && item.Defindex != 5041 && item.Defindex != 5045 && item != null && !item.IsNotTradeable)
                {
                    items.Add(item);
                }
            }
            return items;
        }

        /// <summary>
        /// Adds all items from the given list.
        /// </summary>
        /// <returns>Number of items added.</returns>
        protected uint AddItemsFromList(List<Inventory.Item> items)
        {
            Log.Debug("Method called");
            Log.Debug("" + items.Count);
            uint added = 0;

            foreach (Inventory.Item item in items)
            {
                if (item != null && !item.IsNotTradeable)
                {
                    if (AddItem(item))
                    {
                        Log.Debug("Item successfully added");
                        added++;
                    }
                    else
                    {
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
        /// Crafts Items
        /// </summary>
        protected void Craft(ulong[] craftItems)
        {
            Log.Info("Crafting " + craftItems.Length + " items.");
            Log.Debug("Item IDs: " + craftItems[0] + " and " + craftItems[1]);
            TF2GC.Crafting.CraftItems(Bot, craftItems);

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
            List<Inventory.Item> myCleanWeapons = new List<Inventory.Item>();

            Log.Debug("Getting Inventory");
            Bot.GetInventory();

            myCleanWeapons = GetCleanItemsOfMaterial("weapon");
            Log.Info("Number of weapons to craft: " + myCleanWeapons.Count);
            if (myCleanWeapons.Count < 2)
            {
                Log.Warn("There are not enough weapons to craft");
                return false;
            }

            Log.Info("Setting Game State to Playing TF2.");
            Bot.SetGamePlaying(440);

            int ScrapMade = ScrapWeapons(myCleanWeapons);
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

            ulong[] craftIds;
            int scrapMade = 0;
            Log.Info("Beginning smelt sequence.");

            // Crafting off pairs of weapons in class lists
            foreach (List<Inventory.Item> list in allWeapons)
            {
                while (list.Count > 1)
                {
                    craftIds = new ulong[2];
                    craftIds[0] = list[0].Id;
                    list.RemoveAt(0);
                    craftIds[1] = list[0].Id;
                    list.RemoveAt(0);
                    Craft(craftIds);
                    scrapMade++;
                }
            }

            //(Still needs to be optimised) Crafting the remaining multi-class weapons
            Log.Info("Scrapping multi-class weps");
            foreach (List<Inventory.Item> list in allWeapons)
            {
                craftIds = new ulong[2];
                if (list.Count > 0)
                {
                    foreach (Inventory.Item item in multiWeps)
                    {
                        List<string> classes = new List<string>(Trade.CurrentSchema.GetItem(item.Defindex).UsableByClasses);
                        string[] itemClass = Trade.CurrentSchema.GetItem(list[0].Defindex).UsableByClasses;
                        if (classes.Contains(itemClass[0]))
                        {
                            craftIds[0] = item.Id;
                            // I can remove item from this foreach because I'm going to break anyway
                            multiWeps.Remove(item);
                            craftIds[1] = list[0].Id;
                            list.RemoveAt(0);
                            Craft(craftIds);
                            scrapMade++;
                            break;
                        }
                    }
                }
            }
            // Clean up any leftover
            while (multiWeps.Count > 1)
            {
                craftIds = new ulong[2];
                craftIds[0] = multiWeps[0].Id;
                multiWeps.RemoveAt(0);
                craftIds[1] = multiWeps[0].Id;
                multiWeps.RemoveAt(0);
                Craft(craftIds);
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
            // Lame loop to save a few lines of code
            for (int defindex = 5000; defindex < 5002; defindex++)
            {
                List<Inventory.Item> metalToCraft = new List<Inventory.Item>();
                Log.Debug("Getting Inventory");
                Thread.Sleep(300); // Just another pause to be sure inventory has updated.
                Bot.GetInventory();

                foreach (Inventory.Item invItem in Bot.MyInventory.Items)
                {
                    if (invItem.Defindex == defindex)
                    {
                        metalToCraft.Add(invItem);
                    }
                }
                Log.Debug("Combining Metal. Defindex: " + defindex);

                while (metalToCraft.Count > 2)
                {
                    ulong[] craftIds = new ulong[3];
                    craftIds[0] = metalToCraft[0].Id;
                    craftIds[1] = metalToCraft[1].Id;
                    craftIds[2] = metalToCraft[2].Id;
                    Craft(craftIds);
                    for (int x = 0; x < 3; x++)
                    {
                        metalToCraft.RemoveAt(0);
                    }
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
            List<Inventory.Item> myItems = new List<Inventory.Item>();

            foreach (Inventory.Item invItem in Bot.MyInventory.Items)
            {
                if (!invItem.IsNotTradeable || !invItem.IsNotCraftable || invItem.Quality == "unique")
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
    }
}