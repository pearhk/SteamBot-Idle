## Basic Info

This branch **(Idle-Trader)** is designed to automatically collect items from alternate accounts and optionally craft all weapons for easy use. Setup is easy, just follow all instructions for Jessecar96's Steambot, then change your settings file accordingly. The settings-template+info.json file in Bin/Debug provides additional information about each setting.

Basic setup of your accounts require you to have an idle "hub" account which receives items from all other idling accounts. This "hub" will use **ReceivingUserHandler**, while the other idles will use **GivingUserHandler**. 

Optionally you can also add your "Main" account to log in using **MainUserHandler** and, after crafting, all items will be traded to your Main. This is completely optional, without the main, the items will just stay with the hub.

**Note:** I've moved sentry files and logs to their own folders to avoid the mess of 100+ files. If you already have these files you must move them into folders named "Sentry Files" and "Logs" placed in Bin\Debug

**Double Note:** This is not error-proof, testing for me is limited to about once a week, but the program *should* be able trade and craft for every idle you have. (for me, it handled 100 alts with 2 program restarts) If you see several warnings or errors in log during a trade, the program should continue unimpeded, the bot will simply retry the trade. If an error causes the bot to stop, restarting the program should still work. Be sure to make note of any errors though, and if you can track the source, feel free to offer up a fix.

***New Feature*** -
Bot now accepts commands via bot manager console (Only for threaded mode only. Processes needs extra work)
"exec (X) (Y)" where X = the username or index of the bot and Y = your custom command to execute. Your command can include spaces and be as long as you want. Handle the command by overriding UserHandler.OnBotCommand

Planning to add soon:
 - Backpack count management (basic implementation in use though untested, needs smarter tracking still)
 - Optional account configuration (basically whether or not to use a hub)
 - Crate deletion option
 - Add summary of bot's function/process
 - More details for setup and configuration

Planning to add eventually: 
 - Inventory Management (nothing fancy like steam item manager, but item manipulation/organization none-the-less)
 - More configuration options from settings.json

All credit where credit is due, with Jessecar and all contributors to SteamBot. Original SteamBot info below.
_______________________________________________________________________________________________
**SteamBot** is a bot written in C# for the purpose of interacting with Steam Chat and Steam Trade.  As of right now, about 8 contributors have all added to the bot.  The bot is publicly available under the MIT License. Check out [LICENSE] for more details.

**DO NOT DOWNLOAD THE ZIP FROM GITHUB!**

This bot requires you use git to download the bot. *Downloading the zip will not work!* It doesn't work because the bot uses git submodules, which are not included with the zip download.

There are several things you must do in order to get SteamBot working:

1. Download the source using Git.
2. Compile the source code.
3. Configure the bot (username, password, etc.).
4. *Optionally*, customize the bot by changing the source code.

## Getting the Source

**AGAIN: DO NOT DOWNLOAD THE ZIP FROM GITHUB!**

Retrieving the source code should be done by following the [installation guide] on the wiki. The install guide covers the instructions needed to obtain the source code as well as the instructions for compiling the code.

## Configuring the Bot

Next you need to actually edit the bot to make it do what you want. You can edit the files `SimpleUserHandler.cs` or `AdminUserHandler.cs` or you can create your very own `UserHandler`. See the [configuration guide] on the wiki. This guide covers configuring a basic bot as well as creating a custom user handler.

## Bot Administration

While running the bots you may find it necessary to do some basic operations like shutting down and restarting a bot. The console will take some commands to allow you to do some this. See the [usage guide] for more information.

## More help?
If it's a bug, open an Issue; if you have a fix, read [CONTRIBUTING.md] and open a Pull Request.  A list of contributors (add yourself if you want to):

- [Jessecar96](http://steamcommunity.com/id/jessecar) (project lead)
- [geel9](http://steamcommunity.com/id/geel9)
- [cwhelchel](http://steamcommunity.com/id/cmw69krinkle)

## Wanna Contribute?
Please read [CONTRIBUTING.md].


   [installation guide]: https://github.com/Jessecar96/SteamBot/wiki/Installation-Guide
   [CONTRIBUTING.md]: https://github.com/Jessecar96/SteamBot/blob/master/CONTRIBUTING.md
   [LICENSE]: https://github.com/Jessecar96/SteamBot/blob/master/LICENSE
   [configuration guide]: https://github.com/Jessecar96/SteamBot/wiki/Configuration-Guide
   [usage guide]: https://github.com/Jessecar96/SteamBot/wiki/Usage-Guide
