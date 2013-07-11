using System;
using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using SteamTrade.Exceptions;
using System.Threading;

namespace SteamBot
{
    public class ReceivingUserHandler : UserHandler
    {
        int totalAdded = 0;

        public ReceivingUserHandler(Bot bot, SteamID sid, Configuration config) : base(bot, sid, config) 
        {
            Success = false;
            mySteamID = Bot.SteamUser.SteamID;
            PrimaryAltSID = mySteamID;
        }

        public override void OnLoginCompleted()
        {
            Log.Debug("attend: " + BotItemMap.Count);
            Log.Debug("num: " + NumberOfBots);

            if (!BotItemMap.ContainsKey(mySteamID))
            {
                // Adding for complete attendance of all active bots, no need for inventory.
                BotItemMap.Add(mySteamID, null);
                Admins.Add(mySteamID);
            }
            Log.Info("[Receiving] SteamID: " + mySteamID + " checking in.");

            // Loop until another bot is ready to trade, or all bots have fully loaded.
            while (NumberOfBots < 0)
            {
                Thread.Sleep(1000);
            }
            while (TradeReadyBots.Count == 0 && (BotItemMap.Count < NumberOfBots))
            {
                Log.Info("Waiting for bots...");
                Log.Debug("attend: " + BotItemMap.Count);
                Log.Debug("num: " + NumberOfBots);
                Thread.Sleep(1000);
            }

            if (TradeReadyBots.Count > 0)
            {
                BeginNextTrade(TradeReadyBots[0]);
            }
            else
            {
                Log.Error("No Bots available for trade.");
            }
        }

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {
            Log.Info(Bot.SteamFriends.GetFriendPersonaName(sender) + ": " + message);
            base.OnChatRoomMessage(chatID, sender, message);
        }

        public override bool OnFriendAdd()
        {
            if (IsAdmin)
            {
                return true;
            }
            return false;
        }

        public override void OnFriendRemove() { }
        
        public override void OnMessage(string message, EChatEntryType type)
        {
            if (IsAdmin)
            {
                if (message == "failed")
                {
                    CancelTrade();
                }
            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, type, "Invalid: User is not Admin");
            }
        }

        public override bool OnTradeRequest()
        {
            if (IsAdmin)
            {
                return true;
            }
            return false;
        }

        public override void OnTradeError(string error)
        {
            Log.Warn("OnTradeError: " + error);

            if (error.Equals("InitiatorAlreadyTrading", StringComparison.CurrentCultureIgnoreCase))
            {
                Log.Info("Steam says a trade or request is still active. Telling target to attempt to close the trade, then waiting 5s to try again.");

                Bot.SteamFriends.SendChatMessage(TradeReadyBots[0], EChatEntryType.ChatMsg, "failed");
                Thread.Sleep(5000);
            }

            if (OtherSID != MainSID)
            {
                if (Trade != null)
                {
                    CancelTrade();
                }
                else
                {
                    OnTradeClose();
                }
            }
            else
            {
                Log.Error("Trade with Main account failed. Not retrying.");
            }
        }

        public override void OnTradeTimeout()
        {
            //Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
            //                                  "Trade timeout.");
            Log.Warn("Trade timeout.");
            Log.Debug("Something's gone wrong.");
            Bot.GetOtherInventory(OtherSID);
            if (GetAllNonCrates(Bot.OtherInventory).Count > 0)
            {
                Log.Debug("Still has items to trade");
                //errorOcccured = true;
            }
            else
            {
                Log.Debug("No items in inventory, removing");
                if (TradeReadyBots.Contains(OtherSID))
                { 
                    TradeReadyBots.Remove(OtherSID); 
                }
            }
            if (OtherSID != MainSID)
            {
                CancelTrade();
                OnTradeClose();
            }
        }

        public override void OnTradeClose()
        {
            Log.Warn("[Receiving] TRADE CLOSED");
            Bot.CloseTrade();
            Thread.Sleep(150);

            if (OtherSID != MainSID)
            {
                if (TradeReadyBots.Count > 0)
                {
                    BeginNextTrade(TradeReadyBots[0]);
                }
                else if (BotItemMap.Count < NumberOfBots)
                {
                    // Wait for the rest of the bots
                    while ((TradeReadyBots.Count == 0) && (BotItemMap.Count < NumberOfBots))
                    {
                        Log.Info("Waiting for bots...");
                        Log.Debug("Bot count: " + BotItemMap.Count + " of " + NumberOfBots);
                        Thread.Sleep(1000);
                    }

                    if (TradeReadyBots.Count > 0)
                    {
                        BeginNextTrade(TradeReadyBots[0]);
                    }
                    else
                    {
                        Log.Info("Trade List is empty");
                        Log.Success("All Bots have traded. Items moved: " + totalAdded);
                        FinishTrades();
                    }
                }
                else
                {
                    Log.Info("Trade List is empty");
                    Log.Success("All Bots have traded. Items moved: " + totalAdded);
                    FinishTrades();
                }
            }
            else
            {
                Log.Success("Success: All actions completed.");
            }
        }

        public override void OnTradeInit()
        {
            Log.Success("Trade Started");
            if (OtherSID != MainSID)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "initialized");
            }
            else
            {
                Thread.Sleep(500);
                Log.Info("Getting Inventory");
                Bot.GetInventory();

                Log.Debug("Adding all non crates");
                List<Inventory.Item> AllItems = GetAllNonCrates(Bot.MyInventory);
                uint added = AddItemsFromList(AllItems);

                if (added > 0)
                {
                    Log.Info("Added " + added + " items.");
                    System.Threading.Thread.Sleep(50);
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
                        CancelTrade();
                        OnTradeClose();
                    }
                    else
                    {
                        Log.Debug("No items in bot inventory. This shouldn't be possible.");
                        TradeReadyBots.Remove(mySteamID);
                        CancelTrade();

                        Log.Warn("[Receiving] TRADE CLOSED");
                        Bot.CloseTrade();
                        Bot.StopBot();
                    }
                }
            }
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem) 
        { 
            Log.Debug("Item has been added. ID: " + inventoryItem.Id);
            totalAdded++;
        }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeMessage(string message) 
        {
            if (OtherSID == MainSID)
            {
                System.Threading.Thread.Sleep(100);
                Log.Debug("Message Received: " + message);
                if (message == "ready")
                {
                    if (!SetReady(true))
                    {
                        CancelTrade();
                        OnTradeClose();
                    }
                }
            }
            else
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
        }

        public override void OnTradeReady(bool ready) 
        {
            Log.Debug("OnTradeReady");
            Thread.Sleep(100);

            if (OtherSID == MainSID)
            {
                TradeAccept();
            }
            else if (IsAdmin)
            {
                if (!SetReady(true))
                {
                    CancelTrade();
                    OnTradeClose();
                }
            }
        }

        public void TradeAccept()
        {
            if (OtherSID == MainSID)
            {
                Thread.Sleep(100);
                Success = AcceptTrade();
                if (Success)
                {
                    Log.Success("Trade was Successful!");
                    TradeReadyBots.Remove(mySteamID);
                    //Trade.Poll();
                    //Bot.StopBot();
                }
                else
                {
                    Log.Warn("Trade might have failed.");
                    Bot.GetInventory();
                    if (GetAllNonCrates(Bot.MyInventory).Count == 0)
                    {
                        Log.Warn("Bot has no items, trade may have succeeded. Removing bot.");
                        TradeReadyBots.Remove(mySteamID);
                        CancelTrade();

                        Log.Warn("[Receiving] TRADE CLOSED");
                        Bot.CloseTrade();
                        Bot.StopBot();

                    }
                }
            }
            else if (IsAdmin)
            {
                if (AcceptTrade())
                {
                    Log.Success("Trade was Successful!");
                }
                else
                {
                    Log.Warn("Trade might have failed.");
                    // Going to wait a little while to give the other bot time to finish prep if necessary.
                    Thread.Sleep(1000);
                }
                OnTradeClose();
            }
        }

        public override void OnTradeAccept()
        {
            Log.Debug("OnTradeAccept");
            if (OtherSID == MainSID)
            {
                Log.Warn("[Receiving] TRADE CLOSED");
                Bot.CloseTrade();
                //Bot.StopBot();
            }
            else if (IsAdmin)
            {
                if (AcceptTrade())
                {
                    Log.Success("Trade was Successful!");
                }
                else
                {
                    Log.Warn("Trade might have failed.");
                    // Going to wait a little while to give the other bot time to finish prep if necessary.
                    Thread.Sleep(1000);
                }
            }
            OnTradeClose();
        }

        public override void OnBotCommand(string command)
        {
            Log.Debug("Received command via console: " + command);
            if (command == "craft")
            {
                AutoCraftAll();
            }
            if (command == "stop")
            {
                Bot.StopBot();
            }
            if (command == "start")
            {
                Bot.RestartBot();
            }
        }
        /// <summary>
        /// Starts a new trade if a SteamID is available. If a trade is already open, it is 
        /// closed and another is started. If Bot inventory is approaching maximum, 
        /// AutoCraftAll() will be called.
        /// </summary>
        private void BeginNextTrade(SteamID tradeSID)
        {
            // Thread.Sleep(100);
            // May change to smart inventory tracking to avoid getting inventory before every trade.
            Bot.GetInventory();
            if (Bot.MyInventory.Items.Length > (Bot.MyInventory.NumSlots - 20))
            {
                if (AutoCraftWeps)
                {
                    AutoCraftAll();
                }
                else
                {
                    Log.Warn("Backpack approaching maximum capacity. May not be able to trade soon...");
                }
            }
            else if (Bot.MyInventory.Items.Length >= Bot.MyInventory.NumSlots)
            {
                Log.Error("Backpack is at or over maximum capacity. Trade is unlikely to succeed.");
            }

            Log.Info("Starting Trade with: " + tradeSID);

            if (!Bot.OpenTrade(tradeSID))
            {
                Log.Info("Bot already in trade, closing and starting another.");
            }
        }

        /// <summary>
        /// Finishes minor tasks at the end of the normal trading sequence
        /// </summary>
        private void FinishTrades()
        {
            // Remove the Display name prefix
            Bot.SteamFriends.SetPersonaName(Bot.DisplayName);

            if (AutoCraftWeps)
            {
                AutoCraftAll();
            }

            if (MainSID != null)
            {
                Log.Info("Now moving items to main.");
                BeginNextTrade(MainSID);
            }
            else
            {
                Log.Info("Main account not found. All normal tasks complete.");
            }
        }
    }
}

