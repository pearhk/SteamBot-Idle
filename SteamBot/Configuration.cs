using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SteamBot
{
    public class Configuration
    {
        public static Configuration LoadConfiguration(string filename)
        {
            TextReader reader = new StreamReader(filename);
            string json = reader.ReadToEnd();
            reader.Close();

            Configuration config = JsonConvert.DeserializeObject<Configuration>(json);

            config.Admins = config.Admins ?? new ulong[0];
            int bots = 0;

            // Gets number of Bots AutoStarting
            // Also checks and assigns indexes of special UserHandlers
            for (int index = 0; index < config.Bots.Length; index++)
            {
                switch (config.Bots[index].BotControlClass)
                    {
                        case "SteamBot.GivingUserHandler":
                            if (config.Bots[index].AutoStart)
                            {
                                bots++;
                            }
                            break;

                        case "SteamBot.ReceivingUserHandler":
                            config.ReceivingIndex = index;
                            if (config.Bots[index].AutoStart)
                            {
                                bots++;
                            }
                            break;

                        case "SteamBot.MainUserHandler":
                            config.MainIndex = index;
                            if (config.Bots[index].AutoStart)
                            {
                                config.HasMainUHLoaded = true;
                                bots++;
                            }
                            break;

                        case "SteamBot.CrateUserHandler":
                            config.CrateIndex = index;
                            if (config.Bots[index].AutoStart)
                            {
                                config.HasCrateUHLoaded = true;
                                bots++;
                            }
                            break;

                        default:
                            Console.WriteLine("Bot with unknown UserHandler found in config: " + config.Bots[index].BotControlClass);
                            break;
                    }
            }

            if (config.AutoStartAllBots)
            {
                config.HasCrateUHLoaded = (config.CrateIndex > -1);

                config.HasMainUHLoaded = (config.MainIndex > -1);
                
                bots = config.Bots.Length;
            }

            if (config.UseSeparateProcesses)
            {
                // Seperate Processes not currently supported. (Not likely in the future either)
                Console.WriteLine("Seperate Processes not supported.");
                config.TotalBots = 0;
            }
            else
            {
                config.TotalBots = bots;
            }

            // None of this should be neccessary.
            //foreach (BotInfo bot in config.Bots)
            //{
            //    // merge bot-specific admins with global admins
            //    foreach (ulong admin in config.Admins)
            //    {
            //        if (!bot.Admins.Contains(admin))
            //        {
            //            bot.Admins.Add(admin);
            //        }
            //    }
            //}

            return config;
        }

        #region Top-level config properties

        /// <summary>
        /// Gets or sets the way the program starts by default. *Important*
        /// </summary>
        /// <value>
        /// 0 = do nothing
        /// 1 = "normal mode", standard idle harvesting
        /// 2 = crate handling only
        /// </value>
        public int BotMode { get; set; }

        /// <summary>
        /// Gets or sets the admins.
        /// </summary>
        /// <value>
        /// An array of Steam Profile IDs (64 bit IDs) of the users that are an 
        /// Admin of your bot(s). Each Profile ID should be a string in quotes 
        /// and separated by a comma. These admins are global to all bots 
        /// listed in the Bots array.
        /// </value>
        public ulong[] Admins { get; set; }

        /// <summary>
        /// Gets or sets the bots array.
        /// </summary>
        /// <value>
        /// The Bots object is an array of BotInfo objects containing
        ///  information about each individual bot you will be running. 
        /// </value>
        public BotInfo[] Bots { get; set; }

        /// <summary>
        /// Gets or sets YOUR API key.
        /// </summary>
        /// <value>
        /// The API key you have been assigned by Valve. If you do not have 
        /// one, it can be requested from Value at their Web API Key page. This
        /// is required and the bot(s) will not work without an API Key. 
        /// </value>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the main log file name.
        /// </summary>
        public string MainLog { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use separate processes.
        /// </summary>
        /// <value>
        /// <c>true</c> if bot manager is to open each bot in it's own process;
        /// otherwise, <c>false</c> to open each bot in a separate thread.
        /// Default is <c>false</c>.
        /// </value>
        public bool UseSeparateProcesses { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to auto start all bots.
        /// </summary>
        /// <value>
        /// <c>true</c> to make the bots start on program load; otherwise,
        /// <c>false</c> to not start them.
        /// </value>
        public bool AutoStartAllBots { get; set; }

        /// <summary>
        /// Holds the index of the ReceivingUserHandler bot. Currently supports only one Receiver.
        /// </summary>
        public int ReceivingIndex = -1;

        /// <summary>
        /// True if a MainUserHandler is specified in the settings and is set to AutoStart
        /// </summary>
        public bool HasMainUHLoaded = false;

        /// <summary>
        /// Holds the index of the MainUserHandler bot if it exists, otherwise -1).
        /// </summary>
        public int MainIndex = -1;

        /// <summary>
        /// True if a CrateUserHandler is specified in the settings and is set to AutoStart
        /// </summary>
        public bool HasCrateUHLoaded = false;

        /// <summary>
        /// Holds the index of the CrateUserHandler bot if it exists, otherwise -1). Currently supports only one Crate handler.
        /// </summary>
        public int CrateIndex = -1;

        /// <summary>
        /// Gets or sets a value indicating the total number of bots being loaded.
        /// </summary>
        public int TotalBots { get; set; }

        /// <summary>
        /// Gets or sets the user's custom options.
        /// </summary>
        /// <value>
        /// A Dictionary of custom options where the option name is the key.
        /// </value>
        public Optional Options { get; set; }

        #endregion Top-level config properties


        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            var fields = this.GetType().GetProperties();

            foreach (var propInfo in fields)
            {
                sb.AppendFormat("{0} = {1}" + Environment.NewLine,
                    propInfo.Name,
                    propInfo.GetValue(this, null));
            }

            return sb.ToString();
        }

        public class Optional
        {
            /// <summary>
            /// Gets or sets a value indicating whether to auto craft all weapons in
            /// the bots' inventories. Defaults to false.
            /// </summary>
            /// <value>
            /// <c>true</c> to make the bots craft any weapons before trading.
            /// <c>false</c> to not craft any weapons at all.
            /// </value>
            [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
            [DefaultValue(false)]
            public bool AutoCraftWeapons { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to manage any Crates found in inventories. (salvaged will be handled like items)
            /// Defaults to false. CrateUserHandler required for use.
            /// </summary>
            /// <value>
            /// <c>true</c> to handle Crates.
            /// <c>false</c> to ignore Crates.
            /// </value>
            [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
            [DefaultValue(false)]
            public bool ManageCrates { get; set; }

            /// <summary>
            /// Gets or sets a value indicating how to handle Crate deletion. (Crates in SavedCrates will always
            /// be excluded.)
            /// 0 indicates no crates will be deleted.
            /// 1 indicates only standard mann co. crates will be deleted
            /// 2 indicates only event crates will be deleted (e.g. eerie/summer/winter crates)
            /// 3 indicates all crates will be deleted.
            /// Defaults to 0.
            /// </summary>
            /// <value>
            /// <c>true</c> to delete normal Crates, CrateUserHandler not required.
            /// <c>false</c> to save Crates.
            /// </value>
            [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
            [DefaultValue(false)]
            public int DeleteCrates { get; set; }

            /// <summary>
            /// Gets or sets a value indicating how to transfer Crates.
            /// 0 indicates no crates will be moved from idles.
            /// 1 indicates only standard mann co. crates will be moved from idles.
            /// 2 indicates only event crates will be moved from idles. (e.g. eerie/summer/winter crates)
            /// 3 indicates all crates will be moved from idles.
            /// Defaults to 0.
            /// </summary>
            /// <value>
            /// <c>true</c> to delete normal Crates, CrateUserHandler not required.
            /// <c>false</c> to save Crates.
            /// </value>
            [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
            [DefaultValue(false)]
            public int TransferCrates { get; set; }

            /// <summary>
            /// Gets or sets an array specifying series numbers of crates to save.
            /// </summary>
            public int[] SavedCrates { get; set; }
        }

        public class BotInfo
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string DisplayName { get; set; }
            public string ChatResponse { get; set; }
            public string LogFile { get; set; }
            public string BotControlClass { get; set; }
            public int MaximumTradeTime { get; set; }
            public int MaximumActionGap { get; set; }
            public string DisplayNamePrefix { get; set; }
            public int TradePollingInterval { get; set; }
            public string LogLevel { get; set; }
            //public List<ulong> Admins { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to auto start this bot.
            /// </summary>
            /// <value>
            /// <c>true</c> to make the bot start on program load.
            /// </value>
            /// <remarks>
            /// If <see cref="SteamBot.Configuration.AutoStartAllBots "/> is true,
            /// then this property has no effect and is ignored.
            /// </remarks>
            [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
            [DefaultValue(true)]
            public bool AutoStart { get; set; }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                var fields = this.GetType().GetProperties();

                foreach (var propInfo in fields)
                {
                    sb.AppendFormat("{0} = {1}" + Environment.NewLine,
                        propInfo.Name,
                        propInfo.GetValue(this, null));
                }

                return sb.ToString();
            }
        }
    }
}
