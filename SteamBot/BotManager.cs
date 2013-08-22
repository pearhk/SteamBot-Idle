using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamBot
{
    /// <summary>
    /// A class that manages SteamBot processes.
    /// </summary>
    public class BotManager
    {
        protected enum BotMode { DoNothing, NormalHarvest, CrateManager }

        private Bot MainBot;
        // Eventually multiple receiving?
        private Bot ReceivingBot;
        private Bot CrateBot;
        private List<Bot> GivingBots;

        #region Crate Defindexes
        private static readonly int[] StandardCrates = new int[3] { 5022, 5041, 5045 };
        // Salvaged and the new crates changed to rare drops (robo and summer 2013)
        private static readonly int[] RareDropCrates = new int[3] { 5068, 5635, 5639 };
        #endregion

        private readonly List<RunningBot> botProcs;
        private Log mainLog;

        public BotManager()
        {
            botProcs = new List<RunningBot>();
            GivingBots = new List<Bot>();
        }

        public Configuration ConfigObject { get; private set; }

        /// <summary>
        /// Loads a configuration file to use when creating bots.
        /// </summary>
        /// <param name="configFile"><c>false</c> if there was problems loading the config file.</param>
        public bool LoadConfiguration(string configFile)
        {
            if (!File.Exists(configFile))
                return false;

            try
            {
                ConfigObject = Configuration.LoadConfiguration(configFile);
            }
            catch (JsonReaderException)
            {
                // handle basic json formatting screwups
                ConfigObject = null;
            }

            if (ConfigObject == null)
                return false;

            mainLog = new Log(ConfigObject.MainLog, null, Log.LogLevel.Debug);

            for (int i = 0; i < ConfigObject.Bots.Length; i++)
            {
                Configuration.BotInfo info = ConfigObject.Bots[i];
                if (ConfigObject.AutoStartAllBots || info.AutoStart)
                {
                    mainLog.Info("Configured Bot: " + info.DisplayName + ".");
                }

                var v = new RunningBot(i, ConfigObject);
                botProcs.Add(v);
            }

            return true;
        }

        #region IdleManager
        internal bool Setup()
        {
            var startedOk = StartBots();
            if (!startedOk)
            {
                mainLog.Error("Error starting the bots because either the configuration was bad or because the log file was not opened.");
                return false;
            }



            return true;
        }

        internal void StartManaging()
        {

        }
        #endregion

        #region Starting and Stopping Bots
        /// <summary>
        /// Starts the bots that have been configured, starting Receiving, Crate, and Main UserHandlers first.
        /// </summary>
        /// <returns><c>false</c> if there was something wrong with the configuration or logging.</returns>
        public bool StartBots()
        {
            if (ConfigObject == null || mainLog == null)
                return false;

            // Start special UserHandlers if they exist.
            // Unsure why only AutoStartAllBots has the Sleep delay.
            if (ConfigObject.ReceivingIndex > -1)
            {
                ReceivingBot = botProcs[ConfigObject.ReceivingIndex].TheBot;

                if (ConfigObject.AutoStartAllBots || botProcs[ConfigObject.ReceivingIndex].BotConfig.AutoStart)
                {
                    mainLog.Info("ReceivingUserHandler Found. Starting " + botProcs[ConfigObject.ReceivingIndex].BotConfig.DisplayName + "...");
                    botProcs[ConfigObject.ReceivingIndex].Start();
                    Thread.Sleep(2000);
                }
            }

            if (ConfigObject.CrateIndex > -1)
            {
                CrateBot = botProcs[ConfigObject.CrateIndex].TheBot;

                if (ConfigObject.AutoStartAllBots || botProcs[ConfigObject.CrateIndex].BotConfig.AutoStart)
                {
                    mainLog.Info("CrateUserHandler Found. Starting " + botProcs[ConfigObject.CrateIndex].BotConfig.DisplayName + "...");
                    botProcs[ConfigObject.CrateIndex].Start();
                    Thread.Sleep(2000);
                }
            }

            if (ConfigObject.MainIndex > -1)
            {
                MainBot = botProcs[ConfigObject.MainIndex].TheBot;

                if (ConfigObject.AutoStartAllBots || botProcs[ConfigObject.MainIndex].BotConfig.AutoStart)
                {
                    mainLog.Info("MainUserHandler Found. Starting " + botProcs[ConfigObject.MainIndex].BotConfig.DisplayName + "...");
                    botProcs[ConfigObject.MainIndex].Start();
                    Thread.Sleep(2000);
                }
            }

            // Starting the rest.
            foreach (var runningBot in botProcs)
            {
                if (runningBot.BotConfig.BotControlClass == "SteamBot.GivingUserHandler")
                {
                    GivingBots.Add(runningBot.TheBot);

                    if (ConfigObject.AutoStartAllBots || runningBot.BotConfig.AutoStart)
                    {
                        runningBot.Start();
                        // Will probably make this sleep timer a variable in config.
                        Thread.Sleep(2000);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Kills all running bot processes.
        /// </summary>
        public void StopBots()
        {
            mainLog.Debug("Shutting down all bots.");
            foreach (var botProc in botProcs)
            {
                botProc.Stop();
            }
        }

        /// <summary>
        /// Kills a single bot process given that bots index in the configuration.
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        public void StopBot(int index)
        {
            mainLog.Debug(String.Format("Killing bot at index {0}.", index));
            if (index < botProcs.Count)
            {
                botProcs[index].Stop();
            }
        }

        /// <summary>
        /// Stops a bot given that bots configured username.
        /// </summary>
        /// <param name="botUserName">The bot's username.</param>
        public void StopBot(string botUserName)
        {
            mainLog.Debug(String.Format("Killing bot with username {0}.", botUserName));

            var res = from b in botProcs
                      where b.BotConfig.Username.Equals(botUserName, StringComparison.CurrentCultureIgnoreCase)
                      select b;

            foreach (var bot in res)
            {
                bot.Stop();
            }
        }

        /// <summary>
        /// Starts a bot in a new process given that bot's index in the configuration.
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        public void StartBot(int index)
        {
            mainLog.Debug(String.Format("Starting bot at index {0}.", index));

            if (index < ConfigObject.Bots.Length)
            {
                botProcs[index].Start();
            }
        }

        /// <summary>
        /// Starts a bot given that bots configured username.
        /// </summary>
        /// <param name="botUserName">The bot's username.</param>
        public void StartBot(string botUserName)
        {
            mainLog.Debug(String.Format("Starting bot with username {0}.", botUserName));

            var res = from b in botProcs
                      where b.BotConfig.Username.Equals(botUserName, StringComparison.CurrentCultureIgnoreCase)
                      select b;

            foreach (var bot in res)
            {
                bot.Start();
            }
            
        }
        #endregion

        #region Bot Messages
        /// <summary>
        /// Sets the SteamGuard auth code on the given bot
        /// </summary>
        /// <param name="index">The bot's index</param>
        /// <param name="AuthCode">The auth code</param>
        public void AuthBot(int index, string AuthCode)
        {
            if (index < botProcs.Count)
            {
                botProcs[index].TheBot.AuthCode = AuthCode;
            }
        }

        /// <summary>
        /// Sends the BotManager command to the target Bot
        /// </summary>
        /// <param name="index">The target bot's index</param>
        /// <param name="command">The command to be executed</param>
        public void SendCommand(int index, string command)
        {
            mainLog.Debug(String.Format("Sending command \"{0}\" to Bot at index {1}", command, index));
            if (index < botProcs.Count)
            {
                if (botProcs[index].IsRunning)
                {
                    botProcs[index].TheBot.HandleBotCommand(command);
                }
                else
                {
                    mainLog.Warn(String.Format("Bot at index {0} is not running. Use the 'Start' command first", index));
                }
            }
            else
            {
                mainLog.Warn(String.Format("Invalid Bot index: {0}", index));
            }
        }
        #endregion

        /// <summary>
        /// A method to return an instance of the <c>bot.BotControlClass</c>.
        /// </summary>
        /// <param name="bot">The bot.</param>
        /// <param name="sid">The steamId.</param>
        /// <returns>A <see cref="UserHandler"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown if the control class type does not exist.</exception>
        public static UserHandler UserHandlerCreator(Bot bot, SteamID sid)
        {
            Type controlClass = Type.GetType(bot.BotControlClass);

            if (controlClass == null)
                throw new ArgumentException("Configured control class type was null. You probably named it wrong in your configuration file.", "bot");

            return (UserHandler)Activator.CreateInstance(
                    controlClass, new object[] { bot, sid });
        }

        #region Nested RunningBot class

        /// <summary>
        /// Nested class that holds the information about a spawned bot process.
        /// </summary>
        private class RunningBot
        {
            public int BotConfigIndex { get; private set; }

            public Configuration.BotInfo BotConfig { get; private set; }

            public Bot TheBot { get; private set; }

            public bool IsRunning { get; private set; }

            /// <summary>
            /// Creates a new instance of <see cref="RunningBot"/> class as well as the corresponding Bot object.
            /// </summary>
            /// <param name="index">The index of the bot in the configuration.</param>
            /// <param name="config">The bots configuration object.</param>
            public RunningBot(int index, Configuration config)
            {
                BotConfigIndex = index;
                BotConfig = config.Bots[BotConfigIndex];

                Bot b = new Bot(BotConfig,
                                config.ApiKey,
                                UserHandlerCreator);

                TheBot = b;
            }

            public void Stop()
            {
                if (TheBot.IsRunning)
                {
                    TheBot.StopBot();
                    IsRunning = false;
                }
            }

            public void Start()
            {
                if (!TheBot.IsRunning)
                {
                    TheBot.StartBot();
                    IsRunning = true;
                }
            }
        }

        #endregion Nested RunningBot class
    }
}
