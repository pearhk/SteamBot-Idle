using System;
using System.IO;
using System.Runtime.InteropServices;
using NDesk.Options;

namespace SteamBot
{
    public class Program
    {
        private static OptionSet opts = new OptionSet()
                                     {
                                         { "help", "shows this help text", p => showHelp = (p != null) }
                                     };

        private static bool showHelp;

        private static BotManager manager;
        private static bool isclosing = false;

        [STAThread]
        public static void Main(string[] args)
        {
            opts.Parse(args);

            if (showHelp)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("If no options are given SteamBot defaults to Bot Manager mode.");
                opts.WriteOptionDescriptions(Console.Out);
                Console.Write("Press Enter to exit...");
                Console.ReadLine();
                return;
            }
                
            BotManagerMode();
        }

        #region SteamBot Operational Modes

        // This mode is to manage child bot processes and take use command line inputs
        private static void BotManagerMode()
        {
            Console.Title = "Bot Manager";

            manager = new BotManager();

            var loadedOk = manager.LoadConfiguration("settings.json");

            if (!loadedOk)
            {
                Console.WriteLine(
                    "Configuration file Does not exist or is corrupt. Please rename 'settings-template.json' to 'settings.json' and modify the settings to match your environment");
                Console.Write("Press Enter to exit...");
                Console.ReadLine();
            }
            else
            {
                var IdleManager = new System.Threading.Thread(() => manager.StartManaging());
                IdleManager.Start();

                Console.WriteLine("Type help for bot manager commands. ");
                Console.Write("botmgr > ");

                var bmi = new BotManagerInterpreter(manager);

                // command interpreter loop.
                do
                {
                    string inputText = Console.ReadLine();

                    if (String.IsNullOrEmpty(inputText))
                        continue;

                    bmi.CommandInterpreter(inputText);

                    Console.Write("botmgr > ");

                } while (!isclosing);
            }
        }

        #endregion Bot Modes

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            // Put your own handler here
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                case CtrlTypes.CTRL_BREAK_EVENT:
                case CtrlTypes.CTRL_CLOSE_EVENT:
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    if (manager != null)
                    {
                        manager.StopBots();
                    }
                    isclosing = true;
                    break;
            }
            
            return true;
        }

        #region Console Control Handler Imports

        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        #endregion
    }
}
