using System;
using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using SteamTrade.Exceptions;
using System.Threading;

namespace SteamBot
{
    public class MainUserHandler : UserHandler
    {
        public MainUserHandler(Bot bot, SteamID sid, Configuration config) : base(bot, sid, config) 
        {
            mySteamID = Bot.SteamUser.SteamID;
        }

        public override bool OnFriendAdd()
        {
            return false;
        }

        public override void OnLoginCompleted()
        {
            // Adding for complete attendance of all active bots, no need for inventory.
            BotItemMap.Add(mySteamID, null);
            Log.Info("[Main] SteamID: " + mySteamID + " checking in.");
            Admins.Add(mySteamID);
            MainSID = Bot.SteamUser.SteamID;
        }

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message) {  }

        public override void OnFriendRemove() { }

        public override void OnMessage(string message, EChatEntryType type) {  }

        public override bool OnTradeRequest()
        {
            if (PrimaryAltSID == OtherSID)
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

        public override void OnTradeInit() {  }

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
            if (OtherSID == PrimaryAltSID)
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
    }

}
