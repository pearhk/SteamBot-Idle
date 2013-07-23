using System;
using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using SteamTrade.Exceptions;
using System.Threading;

namespace SteamBot
{
    public class CrateUserHandler : UserHandler
    {
        bool OtherInit = false;
        bool MeInit = false;
        bool MyItemsAdded = false;

        public CrateUserHandler(Bot bot, SteamID sid, Configuration config) : base(bot, sid, config) 
        {
            mySteamID = Bot.SteamUser.SteamID;
            CrateSID = mySteamID;
        }

        public override bool OnFriendAdd()
        {
            return false;
        }

        public override void OnLoginCompleted()
        {
            List<Inventory.Item> itemsToTrade = new List<Inventory.Item>();

            // Optional Crafting
            if (AutoCraftWeps)
            {
                AutoCraftAll();
                // Inventory must be up-to-date before trade
                Thread.Sleep(300);
            }

            // Must get inventory here
            Log.Info("Getting Inventory");
            Bot.GetInventory();

            itemsToTrade = GetAllNonCrates(Bot.MyInventory);
            if (!BotItemMap.ContainsKey(mySteamID))
            {
                BotItemMap.Add(mySteamID, itemsToTrade);
                Admins.Add(mySteamID);
            }

            Log.Info("[Giving] " + Bot.DisplayName + " checking in. " + BotItemMap.Count + " of " + NumberOfBots + " Bots.");
            CrateUHIsRunning = true;

            if (BotItemMap[mySteamID].Count > 0)
            {
                Log.Info(Bot.DisplayName + " has items. Added to list." + TradeReadyBots.Count + " Bots waiting to trade.");
            }
            else
            {
                Log.Warn(Bot.DisplayName + " did not have an item to trade.");
            }

            Log.Info("Waiting for Receiving to finish.");
        }

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message) {  }

        public override void OnFriendRemove() { }

        public override void OnMessage(string message, EChatEntryType type)
        {
            System.Threading.Thread.Sleep(100);
            Log.Debug("Message Received: " + message);

            switch (message)
            {
                case "initialized":
                    OtherInit = true;
                    if (MeInit)
                    {
                        AddItems();
                    }
                    break;

                case "failed":
                    CancelTrade();
                    break;
            }
        }

        public override bool OnTradeRequest()
        {
            MeInit = false;
            OtherInit = false;
            MyItemsAdded = false;

            if (ReceivingSID == OtherSID)
            {
                return true;
            }
            return false;
        }

        public override void OnTradeError(string error)
        {
            Log.Warn(error);
        }

        public override void OnTradeTimeout()
        {
            Log.Info("User was kicked because he was AFK.");
        }

        public override void OnTradeInit()
        {
            Log.Success("Trade Started");
            
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "initialized");
            
            MeInit = true;

            if (OtherInit)
            {
                AddItems();
            }
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeMessage(string message) 
        {
            System.Threading.Thread.Sleep(100);
            Log.Debug("Message Received: " + message);

            if (message == "ready")
            {
                if (!SendMessage("ready"))
                {
                    CancelTrade();
                    OnTradeClose();
                }
            }
        }

        public override void OnTradeReady(bool ready)
        {
            if (OtherSID == ReceivingSID)
            {
                SetReady(true);
            }
        }

        public override void OnTradeAccept()
        {
            bool success = AcceptTrade();

            if (success)
            {
                Log.Success("Trade was Successful!");
                OnTradeClose();
                Bot.StopBot();
            }
            else
            {
                Log.Warn("Trade might have failed.");
                OnTradeClose();
            }
        }

        public void AddItems()
        {
            if (BotItemMap[mySteamID].Count < 1)
                return;

            Thread.Sleep(500);

            Log.Debug("Adding all items.");

            uint added = AddItemsFromList(BotItemMap[mySteamID]);

            if (added > 0)
            {
                Log.Success("Added " + added + " items.");
                System.Threading.Thread.Sleep(50);
                MyItemsAdded = true;
                if (!SendMessage("ready"))
                {
                    CancelTrade();
                    OnTradeClose();
                }
            }
            else
            {
                Log.Debug("Something's gone wrong.");
                Bot.GetInventory();
                if (GetAllNonCrates(Bot.MyInventory).Count > 0)
                {
                    Log.Debug("Still have items to trade, aborting trade.");
                    //errorOcccured = true;
                    Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "failed");
                    OnTradeClose();
                }
                else
                {
                    Log.Debug("No items in bot inventory. This shouldn't be possible.");
                    TradeReadyBots.Remove(mySteamID);

                    CancelTrade();
                    OnTradeClose();
                    Bot.StopBot();
                }
            }
        }
    }

}
