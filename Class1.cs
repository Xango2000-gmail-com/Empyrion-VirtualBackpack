using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Eleon.Modding;
//using ProtoBuf;
using YamlDotNet.Serialization;


namespace VirtualBackpack
{
    public class MyEmpyrionMod : ModInterface, IMod
    {
        internal static string ModShortName = "VirtualBackpack";
        public static string ModVersion = ModShortName + " v2.2.24 made by Xango2000 (E3255)";
        public static string ModPath = "..\\Content\\Mods\\" + ModShortName + "\\";
        internal static IModApi modApi;
        internal static bool ModNetwork = false;

        internal static bool debug = false;
        internal static Dictionary<int, Storage.StorableData> SeqNrStorage = new Dictionary<int, Storage.StorableData> { };
        public int thisSeqNr = 8000;
        internal static SetupYaml.Root SetupYamlData = new SetupYaml.Root { };
        public int VoidOpen = 0;
        internal static string SaveGameName = "";
        internal static string SaveGamePath = "";

        //Dictionary<int, Storage.StorableData> VBopenDelay = new Dictionary<int, Storage.StorableData> { };
        Dictionary<string, Accessing> AccessingVB = new Dictionary<string, Accessing> { };
        //Dictionary<int, int> PlayersAccessingVB = new Dictionary<int, int> { };

        List<string> OnlinePlayers = new List<string> { };
        bool LiteVersion = false;
        bool Disable = false;
        internal static int Expiration = 1628312399;

        internal class Accessing
        {
            public int player;
            public string PlayerType;
            public int VBindex;
        }

        internal bool delayActive = false;
        internal int delayTimer = 0;
        internal Dictionary<int, delayClass> delayDictionary = new Dictionary<int, delayClass> { };
        internal class delayClass
        {
            public int player;
            public bool connecting;
            public int attempt;
        }

        //########################################################################################################################################################
        //################################################ This is where the actual Empyrion Modding API1 stuff Begins ############################################
        //########################################################################################################################################################
        public void Game_Start(ModGameAPI gameAPI)
        {
            Storage.DediAPI = gameAPI;
            if (File.Exists(ModPath + "ERROR.txt")) { File.Delete(ModPath + "ERROR.txt"); }
            if (File.Exists(ModPath + "debug.txt")) { File.Delete(ModPath + "debug.txt"); }
            //CommonFunctions.LogFile("debug.txt", CommonFunctions.UnixTimeStamp());
        }

        public void Game_Event(CmdId cmdId, ushort seqNr, object data)
        {
            if (debug) { CommonFunctions.Debug("cmdId=" + cmdId + "   seqNr=" + seqNr ); }
            try
            {
                switch (cmdId)
                {
                    case CmdId.Event_ChatMessage:
                        //Triggered when player says something in-game
                        ChatInfo Received_ChatInfo = (ChatInfo)data;
                        if (debug) { CommonFunctions.Debug("Chat Message Received"); }
                            string msg = Received_ChatInfo.msg.ToLower();
                            string msg1 = msg.Substring(1);
                            if (msg.Contains(' '))
                            {
                                try
                                {
                                    string[] msg2 = msg1.Split(' ');
                                    msg1 = msg2[0];
                                }
                                catch { }
                            }

                            if (msg == "/mods" || msg == "!mods")
                            {
                            string message = ModVersion;
                            if (Disable)
                            {
                                message = message + " *Disabled";
                            }
                            API.ServerTell(Received_ChatInfo.playerId, ModShortName, message, true);
                        }
                        else if (msg == "/debug vb")
                            {
                                if (debug)
                                {
                                    debug = false;
                                    API.ServerTell(Received_ChatInfo.playerId, ModShortName, "VB: Debug is now False", true);
                                }
                                else
                                {
                                    debug = true;
                                    API.ServerTell(Received_ChatInfo.playerId, ModShortName, "VB: Debug is now True", true);
                                }
                            }
                            else if (msg == SetupYamlData.General.DefaultPrefix + SetupYamlData.General.ReinitializeCommand.ToLower())
                            {
                                SetupYaml.Setup();
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "VirtualBackpack Reinitialized", true);
                            }
                            /* Section was moved to API2
                            else if (SetupYamlData.VirtualBackpacks.Commands.Contains(msg1) && !Received_ChatInfo.msg.Contains(' '))
                            {
                                //if (Received_ChatInfo.type 
                                try
                                {
                                    Storage.StorableData function = new Storage.StorableData
                                    {
                                        function = "VB",
                                        Match = Convert.ToString(Received_ChatInfo.playerId),
                                        Requested = "PlayerInfo",
                                        ChatInfo = Received_ChatInfo
                                    };
                                    API.PlayerInfo(Received_ChatInfo.playerId, function);
                                }
                                catch
                                {
                                    CommonFunctions.Debug("VirtualBackpack Fail: at ChatInfo");
                                }
                            }
                            */
                            else if (SetupYamlData.VirtualBackpacks.Commands.Contains(msg1) && msg.Contains(' '))
                            {
                                if (!Disable)
                                {
                                    try
                                    {
                                        Storage.StorableData function = new Storage.StorableData
                                        {
                                            function = "AdminVB",
                                            Match = Convert.ToString(Received_ChatInfo.playerId),
                                            Requested = "PlayerInfo",
                                            ChatInfo = Received_ChatInfo
                                        };
                                        API.PlayerInfo(Received_ChatInfo.playerId, function);
                                    }
                                    catch
                                    {
                                        CommonFunctions.Debug("VirtualBackpack Fail: at ChatInfo");
                                    }
                                }
                            }
                        
                        break;


                    case CmdId.Event_Player_Connected:
                        //Triggered when a player logs on
                        Id Received_PlayerConnected = (Id)data;

                        delayClass newDelayConnecting = new delayClass
                        {
                            player = Received_PlayerConnected.id,
                            connecting = true,
                            attempt = 1
                        };
                        delayTimer = delayTimer + 10;
                        delayDictionary.Add(delayTimer, newDelayConnecting);
                        delayActive = true;
                        /*
                        CommonFunctions.Debug("PlayerID Connecting = " + Received_PlayerConnected.id);
                        string ConnectingPlayerSteamID = modApi.Application.GetPlayerDataFor(Received_PlayerConnected.id).Value.SteamId;
                        if (!OnlinePlayers.Contains(ConnectingPlayerSteamID))
                        {
                            OnlinePlayers.Add(ConnectingPlayerSteamID);
                        }
                        if (OnlinePlayers.Count > 10 && LiteVersion)
                        {
                            Disable = true;
                        }
                        if (File.Exists(ModPath + "PlayersData\\" + ConnectingPlayerSteamID + "\\StopExploit.txt"))
                        {
                            ItemStack[] Backpack = new ItemStack[] { };
                            ItemStack[] Toolbar = new ItemStack[] { };
                            if(File.Exists(ModPath + "PlayersData\\" + ConnectingPlayerSteamID + "\\PlayerBackpack.txt"))
                            {
                                try
                                {
                                    string FolderPath = "PlayersData\\" + ConnectingPlayerSteamID + "\\";
                                    Backpack = CommonFunctions.ReadItemStacks(FolderPath, "PlayerBackpack.txt");
                                } catch { }
                            }
                            if(File.Exists("PlayersData\\" + ConnectingPlayerSteamID + "\\PlayerToolbar.txt"))
                            {
                                try
                                {
                                    string FolderPath = "PlayersData\\" + ConnectingPlayerSteamID + "\\";
                                    Toolbar = CommonFunctions.ReadItemStacks(FolderPath, "PlayerToolbar.txt");
                                } catch { }
                            }
                            API.PlayerInventorySet(Received_PlayerConnected.id, Backpack, Toolbar);
                            try { File.Delete(ModPath + "PlayersData\\" + ConnectingPlayerSteamID + "\\StopExploit.txt"); } catch { }
                        }
                        foreach (string AccessingPlayer in AccessingVB.Keys)
                        {
                            if (AccessingVB[AccessingPlayer].player == Received_PlayerConnected.id)
                            {
                                try { AccessingVB.Remove(AccessingPlayer); } catch { }
                            }
                        }
                        */
                        break;


                    case CmdId.Event_Player_Disconnected:
                        //Triggered when a player logs off
                        Id Received_PlayerDisconnected = (Id)data;
                        //if player disconnects while they are still showing as having VB open... do something
                        delayClass newDelayDisconnecting = new delayClass
                        {
                            player = Received_PlayerDisconnected.id,
                            connecting = true,
                            attempt = 1
                        };
                        delayTimer = delayTimer + 10;
                        delayDictionary.Add(delayTimer, newDelayDisconnecting);
                        delayActive = true;
                        /*
                        foreach (string AccessingPlayer in AccessingVB.Keys)
                        {
                            if (AccessingVB[AccessingPlayer].player == Received_PlayerDisconnected.id)
                            {
                                try { AccessingVB.Remove(AccessingPlayer); } catch { }
                            }
                            string DisconnectingPlayerSteamID = modApi.Application.GetPlayerDataFor(Received_PlayerDisconnected.id).Value.SteamId;
                            try { File.Create(ModPath + "PlayersData\\" + DisconnectingPlayerSteamID + "\\StopExploit.txt"); }
                            catch
                            {
                                string DisconnectingPlayerName = modApi.Application.GetPlayerDataFor(Received_PlayerDisconnected.id).Value.PlayerName;
                                CommonFunctions.LogFile("PossibleExploiter.txt", DisconnectingPlayerName + "   Disconnecting while VB open");
                            }
                        }
                        */
                        break;


                    case CmdId.Event_Player_ChangedPlayfield:
                        //Triggered when a player changes playfield
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ChangePlayfield, (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [PlayerID], [Playfield Name], [PVector3 position], [PVector3 Rotation] ));
                        IdPlayfield Received_PlayerChangedPlayfield = (IdPlayfield)data;
                        break;


                    case CmdId.Event_Playfield_Loaded:
                        //Triggered when a player goes to a playfield that isnt currently loaded in memory
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Load_Playfield, (ushort)CurrentSeqNr, new PlayfieldLoad( [float nSecs], [string nPlayfield], [int nProcessId] ));
                        PlayfieldLoad Received_PlayfieldLoaded = (PlayfieldLoad)data;
                        break;


                    case CmdId.Event_Playfield_Unloaded:
                        //Triggered when there are no players left in a playfield
                        PlayfieldLoad Received_PlayfieldUnLoaded = (PlayfieldLoad)data;
                        break;


                    case CmdId.Event_Faction_Changed:
                        //Triggered when an Entity (player too?) changes faction
                        FactionChangeInfo Received_FactionChange = (FactionChangeInfo)data;
                        break;


                    case CmdId.Event_Statistics:
                        //Triggered on various game events like: Player Death, Entity Power on/off, Remove/Add Core
                        StatisticsParam Received_EventStatistics = (StatisticsParam)data;
                        if (debug)
                        {
                            CommonFunctions.Debug("");
                            CommonFunctions.Debug("Type= " + Received_EventStatistics.type);
                            CommonFunctions.Debug("int1= " + Received_EventStatistics.int1);
                            CommonFunctions.Debug("int2= " + Received_EventStatistics.int2);
                            CommonFunctions.Debug("int3= " + Received_EventStatistics.int3);
                            CommonFunctions.Debug("int4= " + Received_EventStatistics.int4);
                            CommonFunctions.Debug("");
                        }

                        if (SetupYamlData.VirtualBackpacks.DeleteOnDeath.ToLower() == "true" && Received_EventStatistics.type == StatisticsType.PlayerDied)
                        {
                            //DB.EntityData PlayerData = DB.LookupPlayer(Received_EventStatistics.int1);
                            string SteamID = modApi.Application.GetPlayerDataFor(Received_EventStatistics.int1).Value.SteamId;
                            //string SteamID = DB.SteamID(Received_EventStatistics.int1);
                            string[] FileNames =  Directory.GetFiles(ModPath + "PlayersData\\" + SteamID);
                            foreach ( string FileName in FileNames)
                            {
                                try { File.Delete(FileName); } catch { }
                            }
                        }
                        break;


                    case CmdId.Event_Player_DisconnectedWaiting:
                        //Triggered When a player is having trouble logging into the server
                        Id Received_PlayerDisconnectedWaiting = (Id)data;
                        break;


                    case CmdId.Event_TraderNPCItemSold:
                        //Triggered when a player buys an item from a trader
                        TraderNPCItemSoldInfo Received_TraderNPCItemSold = (TraderNPCItemSoldInfo)data;
                        break;


                    case CmdId.Event_Player_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_List, (ushort)CurrentSeqNr, null));
                        IdList Received_PlayerList = (IdList)data;
                        break;


                    case CmdId.Event_Player_Info:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Info, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        PlayerInfo Received_PlayerInfo = (PlayerInfo)data;
                        /*
                        if(!OnlinePlayers.Contains(Received_PlayerInfo.steamId))
                            {
                            OnlinePlayers.Add(Received_PlayerInfo.steamId);
                            }
                            */
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RetrievedData = SeqNrStorage[seqNr];
                            /*
                            if (RetrievedData.Requested == "PlayerInfo" && RetrievedData.function == "PIUpdate" && Convert.ToString(Received_PlayerInfo.entityId) == RetrievedData.Match)
                            {
                                if (!Directory.Exists(ModPath + "PlayersData\\" + Received_PlayerInfo.steamId))
                                {
                                    Directory.CreateDirectory(ModPath + "PlayersData\\" + Received_PlayerInfo.steamId);
                                }
                                SeqNrStorage.Remove(seqNr);
                            }
                            else */
                            if (RetrievedData.Requested == "PlayerInfo" && RetrievedData.function == "VB" && Convert.ToString(Received_PlayerInfo.entityId) == RetrievedData.Match)
                            {
                                try { SeqNrStorage.Remove(seqNr); } catch { }
                                int PlayerLevel = 0;
                                /* Disabled to try to diagnose drmccollum's issue
                                try
                                {
                                    PlayerLevel = DB.LookupPlayerLevel(Received_PlayerInfo.entityId);
                                }
                                catch
                                {
                                    API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "Unable to retrieve Player Level data", true);
                                }
                                */
                                bool Continue = true;
                                try
                                {
                                    if (SetupYamlData.VirtualBackpacks.RestrictedPlayfields.Contains(Received_PlayerInfo.playfield))
                                    {
                                        //Restricted Playfield
                                        Continue = false;
                                        API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "VB Cannot be used at " + Received_PlayerInfo.playfield, true);
                                        CommonFunctions.Debug("VB Cannot be used at " + Received_PlayerInfo.playfield);
                                    }
                                }
                                catch
                                {
                                    CommonFunctions.ERROR("ERROR: in 'Restricted Playfield' section");
                                }
                                try
                                {
                                    if (Received_PlayerInfo.credits < SetupYamlData.VirtualBackpacks.UsageCost)
                                    {
                                        //Deduct Credit Cost
                                        Continue = false;
                                        API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "You dont have enough Credits to use VB at this time, cost is " + SetupYamlData.VirtualBackpacks.UsageCost + " Per use.", true);
                                        CommonFunctions.Debug("You dont have enough Credits to use VB at this time, cost is " + SetupYamlData.VirtualBackpacks.UsageCost + " Per use.");
                                    }
                                    else
                                    {
                                        //Deduct Credits
                                        API.Credits(Received_PlayerInfo.entityId, -SetupYamlData.VirtualBackpacks.UsageCost);
                                        if (SetupYamlData.VirtualBackpacks.UsageCost > 0)
                                        {
                                            API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "Deducted " + SetupYamlData.VirtualBackpacks.UsageCost + " Credits (usage cost)", true);
                                            CommonFunctions.Debug("Deducted " + SetupYamlData.VirtualBackpacks.UsageCost + " Credits (usage cost)");
                                        }
                                    }
                                }
                                catch
                                {
                                    CommonFunctions.ERROR("ERROR: in 'Credit Cost' section");
                                }
                                try
                                {
                                    if (PlayerLevel < SetupYamlData.VirtualBackpacks.MinimumLevelRequired)
                                    {
                                        //Level Requirement
                                        Continue = false;
                                        API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "You must be at least level " + SetupYamlData.VirtualBackpacks.MinimumLevelRequired + " to use VB", true);
                                        CommonFunctions.Debug("You must be at least level " + SetupYamlData.VirtualBackpacks.MinimumLevelRequired + " to use VB");
                                    }
                                }
                                catch
                                {
                                    CommonFunctions.ERROR("ERROR: in 'Player Level' section");
                                }
                                if (Continue)
                                {
                                    try
                                    {
                                        int index = SetupYamlData.VirtualBackpacks.Commands.FindIndex(x => x.ToLower().StartsWith(RetrievedData.ChatInfo.msg.ToLower().Substring(1)));
                                        Accessing NewAccessing = new Accessing
                                        {
                                            VBindex = index,
                                            player = Received_PlayerInfo.entityId,
                                            PlayerType = "player"
                                        };
                                        if (AccessingVB.Keys.Contains(Received_PlayerInfo.entityId + "-" + index))
                                        {
                                            if (AccessingVB[Received_PlayerInfo.entityId + "-" + index] == NewAccessing)
                                            {
                                                //Admin editing player's VB
                                                Continue = false;
                                                API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "Admin is currently accessing " + RetrievedData.ChatInfo.msg, true);
                                                CommonFunctions.Debug("Admin is currently accessing " + RetrievedData.ChatInfo.msg);
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        CommonFunctions.ERROR("ERROR: in 'Admin currently accessing Player Backpack' section.");
                                    }
                                }
                                if (Continue)
                                {
                                    try
                                    {
                                        int index = SetupYamlData.VirtualBackpacks.Commands.FindIndex(x => x.ToLower().StartsWith(RetrievedData.ChatInfo.msg.ToLower().Substring(1)));
                                        if (AccessingVB.Keys.Contains(Received_PlayerInfo.entityId + "-" + index))
                                        {
                                            //Player editing their own VB
                                            //Continue = false;
                                            API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "You are currently accessing " + RetrievedData.ChatInfo.msg + "  ...Wait, What? Fixed.", true);
                                            try { AccessingVB.Remove(Received_PlayerInfo.entityId + "-" + index); } catch { }
                                            CommonFunctions.ERROR("Error: Player trying to open a VB they already have Open???   " + Received_PlayerInfo.entityId + "   " + RetrievedData.ChatInfo.msg);
                                        }
                                    }
                                    catch
                                    {
                                        CommonFunctions.ERROR("ERROR: in 'Player trying to access VB they currently have open' section");
                                    }
                                }

                                if (Continue)
                                {
                                    try { CommonFunctions.WriteItemStacksSimple("PlayersData\\" + Received_PlayerInfo.steamId + "\\", "PlayerBackpack.txt", Received_PlayerInfo.bag); } catch { }
                                    try { CommonFunctions.WriteItemStacksSimple("PlayersData\\" + Received_PlayerInfo.steamId + "\\", "PlayerToolbar.txt", Received_PlayerInfo.toolbar); } catch { }
                                    int index = 0;
                                    try
                                    {
                                        index = SetupYamlData.VirtualBackpacks.Commands.FindIndex(x => x.ToLower().StartsWith(RetrievedData.ChatInfo.msg.ToLower().Substring(1)));
                                    }
                                    catch
                                    {
                                        CommonFunctions.ERROR("ERROR: getting index failed");
                                    }
                                    //PlayersAccessingVB[Received_PlayerInfo.entityId] = index;

                                    string FolderPath = "PlayersData\\" + Received_PlayerInfo.steamId + "\\";
                                    string FilePath = "vb" + index + ".txt";
                                    string OldVB = "VirtualBackpack.csv";
                                    RetrievedData.function = "VB";
                                    RetrievedData.Match = Convert.ToString(Received_PlayerInfo.entityId);
                                    RetrievedData.Requested = "ItemExchange";
                                    RetrievedData.TriggerPlayer = Received_PlayerInfo;

                                    // Lock Admin out of Player's VB
                                    CommonFunctions.Debug("Read File= " + ModPath + FolderPath + FilePath);
                                    if (File.Exists(ModPath + FolderPath + FilePath))
                                    {
                                        //CommonFunctions.Debug("03032021 Regular file exists");
                                        try
                                        {
                                            //CommonFunctions.Debug("03032021 try1");
                                            ItemStack[] InventoryData = CommonFunctions.ReadItemStacks(FolderPath, FilePath);
                                            try
                                            {
                                                //CommonFunctions.Debug("03032021 try2");
                                                API.OpenItemExchange(Received_PlayerInfo.entityId, "Virtual Backpack " + (index + 1), "Wipe-Safe storage", "Close", InventoryData, RetrievedData);
                                            }
                                            catch
                                            {
                                                CommonFunctions.ERROR("ERROR: API.OpenItemExchange() failed for existing VB");
                                            }
                                        }
                                        catch
                                        {
                                            CommonFunctions.ERROR("ERROR: CommonFunctions.ReadItemStacks() failed for existing VB");

                                        }
                                    }
                                    else if (index == 0 && File.Exists(ModPath + "OldVirtualBackpacks\\" + Received_PlayerInfo.steamId + "\\" + OldVB))
                                    {
                                        //CommonFunctions.Debug("03032021 OldVB Exists");
                                        try
                                        {
                                            //CommonFunctions.Debug("03032021 try1");
                                            ItemStack[] InventoryData = CommonFunctions.ReadItemStacks("OldVirtualBackpacks\\" + Received_PlayerInfo.steamId + "\\", OldVB);
                                            try
                                            {
                                                //CommonFunctions.Debug("03032021 try2");
                                                API.OpenItemExchange(Received_PlayerInfo.entityId, "Virtual Backpack " + (index + 1), "Wipe-Safe storage", "Close", InventoryData, RetrievedData);
                                            }
                                            catch
                                            {
                                                CommonFunctions.ERROR("ERROR: API.OpenItemExchange() failed for OldVB");
                                            }
                                        }
                                        catch
                                        {
                                            CommonFunctions.ERROR("ERROR: CommonFunctions.ReadItemStacks() failed for OldVB");
                                        }
                                    }
                                    else
                                    {
                                        //CommonFunctions.Debug("03032021 else");
                                        ItemStack[] InventoryData = new ItemStack[] { };
                                        API.OpenItemExchange(Received_PlayerInfo.entityId, "Virtual Backpack " + (index + 1), "Wipe-Safe storage", "Close", InventoryData, RetrievedData);
                                    }
                                    //CommonFunctions.Debug("03032021 ItemExchange Window should be open");
                                }
                            }
                            else if (RetrievedData.Requested == "PlayerInfo" && RetrievedData.function == "AdminVB" && Convert.ToString(Received_PlayerInfo.entityId) == RetrievedData.Match)
                            {
                                if ( Received_PlayerInfo.permission > 2)
                                {
                                    try
                                    {
                                        string[] Splitmessage = RetrievedData.ChatInfo.msg.Split(' ');
                                        int index = SetupYamlData.VirtualBackpacks.Commands.FindIndex(x => x.ToLower().StartsWith(Splitmessage[0].ToLower().Substring(1)));
                                        try
                                        {
                                            int TargetPlayerID = Int32.Parse(Splitmessage[1]);
                                            //DB.EntityData TargetPlayerInfo = DB.LookupPlayer(Int32.Parse(Splitmessage[1]));
                                            string TargetPlayerName = modApi.Application.GetPlayerDataFor(TargetPlayerID).Value.PlayerName;
                                            string TargetSteamID = modApi.Application.GetPlayerDataFor(TargetPlayerID).Value.SteamId;
                                            //string SteamID = DB.SteamID(TargetPlayerInfo.EntityID);
                                            if (AccessingVB.ContainsKey(TargetPlayerID + "-" + index))
                                            {
                                                Accessing VB = AccessingVB[TargetPlayerID + "-" + index];
                                                string playerName = modApi.Application.GetPlayerDataFor(VB.player).Value.PlayerName;
                                                try
                                                {
                                                    API.ServerTell(Received_PlayerInfo.entityId, ModShortName, playerName + " is currently accessing that VB", true);
                                                }
                                                catch
                                                {
                                                    API.ServerTell(Received_PlayerInfo.entityId, ModShortName, VB.player + " is currently accessing that VB", true);
                                                }
                                                if (VB.player == Received_PlayerInfo.entityId)
                                                {
                                                    API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "...Wait a minute, that's you. Fixing...", false);
                                                    try { AccessingVB.Remove(TargetPlayerID + "-" + index); } catch { }
                                                }
                                            }

                                            if (TargetSteamID == "0" || TargetSteamID == "Blank")
                                            {
                                                //API.Chat("Player", Received_PlayerInfo.entityId, "Invalid PlayerID " + Int32.Parse(Splitmessage[1]));
                                                API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "Invalid PlayerID " + Int32.Parse(Splitmessage[1]), true);
                                            }
                                            else if (!AccessingVB.Keys.Contains(TargetPlayerID + "-" + index))
                                            {
                                                Accessing NewAccessing = new Accessing
                                                {
                                                    VBindex = index,
                                                    player = Received_PlayerInfo.entityId,
                                                    PlayerType = "admin"
                                                };
                                                AccessingVB[TargetPlayerID +"-" + index] = NewAccessing; // Lock player out of the VB being edited
                                                string targetPlayerSteamID = modApi.Application.GetPlayerDataFor(TargetPlayerID).Value.SteamId;
                                                string FolderPath = "PlayersData\\" + targetPlayerSteamID + "\\";
                                                string FilePath = "vb" + index + ".txt";
                                                string OldVB = "VirtualBackpack.csv";

                                                RetrievedData.function = "AdminVB";
                                                RetrievedData.Match = Convert.ToString(Received_PlayerInfo.entityId);
                                                RetrievedData.Requested = "ItemExchange";
                                                RetrievedData.TriggerPlayer = Received_PlayerInfo;

                                                if (File.Exists(ModPath + FolderPath + FilePath))
                                                {
                                                    ItemStack[] InventoryData = CommonFunctions.ReadItemStacks(FolderPath, FilePath);
                                                    API.OpenItemExchange(Received_PlayerInfo.entityId, "Admin Virtual Backpack " + (index + 1), TargetPlayerName, "Close", InventoryData, RetrievedData);
                                                }
                                                else if (index == 0 && File.Exists(ModPath + OldVB))
                                                {
                                                    ItemStack[] InventoryData = CommonFunctions.ReadItemStacks("OldVirtualBackpacks\\" + Received_PlayerInfo.steamId + "\\", OldVB);
                                                    API.OpenItemExchange(Received_PlayerInfo.entityId, "Admin Virtual Backpack " + (index + 1), TargetPlayerName, "Close", InventoryData, RetrievedData);
                                                }
                                                else
                                                {
                                                    ItemStack[] InventoryData = new ItemStack[] { };
                                                    API.OpenItemExchange(Received_PlayerInfo.entityId, "Admin Virtual Backpack " + (index + 1), TargetPlayerName, "Close", InventoryData, RetrievedData);
                                                }
                                            }
                                            else
                                            {
                                                API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "An Unknown Error Occurred, Please tell Xango2000 what you just did.", true);
                                                //API.Chat("Player", Received_PlayerInfo.entityId, "VB: Somone else is currently accessing that VB");
                                            }
                                        }
                                        catch
                                        {
                                            CommonFunctions.Debug("ERROR: Invalid PlayerID" + Splitmessage[1]);
                                            CommonFunctions.ERROR("ERROR: Invalid PlayerID" + Splitmessage[1]);
                                        }
                                    }
                                    catch
                                    {
                                        //API.Chat("Player", Received_PlayerInfo.entityId, "AdminVB Failed (unknown)");
                                        API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "AdminVB Failed (unknown)", true);
                                    }
                                }
                            }
                        }
                        break;


                    case CmdId.Event_Player_Inventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Player_ItemExchange:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ItemExchange, (ushort)CurrentSeqNr, new ItemExchangeInfo( [id], [title], [description], [buttontext], [ItemStack[]] ));
                        ItemExchangeInfo Received_ItemExchangeInfo = (ItemExchangeInfo)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RetrievedData = SeqNrStorage[seqNr];
                            //Storage.StorableData RequestTracker = SeqNrStorage[seqNr];
                            if (RetrievedData.Requested == "ItemExchange" && RetrievedData.function == "VB" && Convert.ToString(Received_ItemExchangeInfo.id) == RetrievedData.Match)
                            {
                                try { SeqNrStorage.Remove(seqNr); } catch { }
                                //chatinfo and Index
                                int index = SetupYamlData.VirtualBackpacks.Commands.FindIndex(x => x.ToLower().StartsWith(RetrievedData.ChatInfo.msg.ToLower().Substring(1)));
                                //AccessingVB.Remove(RetrievedData.TriggerPlayer.entityId + "-" + index); // UnLock player VB
                                string SteamID = modApi.Application.GetPlayerDataFor(Received_ItemExchangeInfo.id).Value.SteamId;
                                string FolderName = "PlayersData\\" + SteamID + "\\";
                                string FileName = "vb" + index + ".txt";
                                CommonFunctions.WriteItemStacks(FolderName, FileName, Received_ItemExchangeInfo.items, SetupYamlData.VirtualBackpacks.Stack, SetupYamlData.VirtualBackpacks.MaxSuperStack, true);
                                try { AccessingVB.Remove(Received_ItemExchangeInfo.id + "-" + index); } catch { }
                            }
                            else if (RetrievedData.Requested == "ItemExchange" && RetrievedData.function == "AdminVB" && Convert.ToString(Received_ItemExchangeInfo.id) == RetrievedData.Match)
                            {
                                try { SeqNrStorage.Remove(seqNr); } catch { }
                                string[] Splitmessage = RetrievedData.ChatInfo.msg.Split(' ');
                                int index = SetupYamlData.VirtualBackpacks.Commands.FindIndex(x => x.ToLower().StartsWith(Splitmessage[0].ToLower().Substring(1)));
                                //DB.EntityData TargetPlayerInfo = DB.LookupPlayer(Int32.Parse(Splitmessage[1]));
                                string SteamID = modApi.Application.GetPlayerDataFor(Int32.Parse(Splitmessage[1])).Value.SteamId;
                                try { AccessingVB.Remove(Int32.Parse(Splitmessage[1]) + "-" + index); } catch { } // UnLock player VB
                                string FolderName = "PlayersData\\" + SteamID + "\\"; 
                                string FileName = "vb" + index + ".txt";
                                CommonFunctions.WriteItemStacks(FolderName, FileName, Received_ItemExchangeInfo.items, SetupYamlData.VirtualBackpacks.Stack, SetupYamlData.VirtualBackpacks.MaxSuperStack, true);
                            }
                        }
                        break;


                    case CmdId.Event_DialogButtonIndex:
                        //All of This is a Guess
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ShowDialog_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        IdAndIntValue Received_DialogButtonIndex = (IdAndIntValue)data;
                        //Save/Pos = 0, Close/Cancel/Neg = 1
                        break;


                    case CmdId.Event_Player_Credits:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Credits, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        IdCredits Received_PlayerCredits = (IdCredits)data;
                        break;


                    case CmdId.Event_Player_GetAndRemoveInventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetAndRemoveInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerGetRemoveInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Playfield_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_List, (ushort)CurrentSeqNr, null));
                        PlayfieldList Received_PlayfieldList = (PlayfieldList)data;
                        break;


                    case CmdId.Event_Playfield_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Stats, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldStats Received_PlayfieldStats = (PlayfieldStats)data;
                        break;


                    case CmdId.Event_Playfield_Entity_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Entity_List, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldEntityList Received_PlayfieldEntityList = (PlayfieldEntityList)data;
                        break;


                    case CmdId.Event_Dedi_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Dedi_Stats, (ushort)CurrentSeqNr, null));
                        DediStats Received_DediStats = (DediStats)data;
                        break;


                    case CmdId.Event_GlobalStructure_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_List, (ushort)CurrentSeqNr, null));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_Update, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        GlobalStructureList Received_GlobalStructureList = (GlobalStructureList)data;
                        //foreach (GlobalStructureInfo item in Structs.globalStructures[storedInfo[seqNr].PlayerInfo.playfield])
                        break;


                    case CmdId.Event_Entity_PosAndRot:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_PosAndRot, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdPositionRotation Received_EntityPosRot = (IdPositionRotation)data;
                        break;


                    case CmdId.Event_Get_Factions:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Get_Factions, (ushort)CurrentSeqNr, new Id( [int] )); //Requests all factions from a certain Id onwards. If you want all factions use Id 1.
                        FactionInfoList Received_FactionInfoList = (FactionInfoList)data;
                        break;


                    case CmdId.Event_NewEntityId:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_NewEntityId, (ushort)CurrentSeqNr, null));
                        Id Request_NewEntityId = (Id)data;
                        break;


                    case CmdId.Event_Structure_BlockStatistics:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_BlockStatistics, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdStructureBlockInfo Received_StructureBlockStatistics = (IdStructureBlockInfo)data;
                        break;


                    case CmdId.Event_AlliancesAll:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesAll, (ushort)CurrentSeqNr, null));
                        AlliancesTable Received_AlliancesAll = (AlliancesTable)data;
                        break;


                    case CmdId.Event_AlliancesFaction:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesFaction, (ushort)CurrentSeqNr, new AlliancesFaction( [int nFaction1Id], [int nFaction2Id], [bool nIsAllied] ));
                        AlliancesFaction Received_AlliancesFaction = (AlliancesFaction)data;
                        break;


                    case CmdId.Event_BannedPlayers:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GetBannedPlayers, (ushort)CurrentSeqNr, null ));
                        BannedPlayerData Received_BannedPlayers = (BannedPlayerData)data;
                        break;


                    case CmdId.Event_GameEvent:
                        //Triggered by PDA Events
                        GameEventData Received_GameEvent = (GameEventData)data;
                        break;


                    case CmdId.Event_Ok:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetInventory, (ushort)CurrentSeqNr, new Inventory(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddItem, (ushort)CurrentSeqNr, new IdItemStack(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [+/- Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Finish, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Resources, (ushort)CurrentSeqNr, new BlueprintResources( [PlayerID], [List<ItemStack>], [bool ReplaceExisting?] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Teleport, (ushort)CurrentSeqNr, new IdPositionRotation( [EntityId OR PlayerID], [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_ChangePlayfield , (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [EntityId OR PlayerID], [Playfield],  [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy2, (ushort)CurrentSeqNr, new IdPlayfield( [EntityID], [Playfield] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_SetName, (ushort)CurrentSeqNr, new Id( [EntityID] )); Wait, what? This one doesn't make sense. This is what the Wiki says though.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Spawn, (ushort)CurrentSeqNr, new EntitySpawnInfo()); Doesn't make sense to me.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_Touch, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_Faction, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_AllPlayers, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)CurrentSeqNr, new PString( [Telnet Command] ));

                        //uh? Not Listed in Wiki... Received_ = ()data;
                        break;


                    case CmdId.Event_Error:
                        //Triggered when there is an error coming from the API
                        ErrorInfo Received_ErrorInfo = (ErrorInfo)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            try { SeqNrStorage.Remove(seqNr); } catch { }
                            CommonFunctions.ERROR("API Error Type: " + Received_ErrorInfo.errorType + "\r\n");
                        }
                        break;


                    case CmdId.Event_PdaStateChange:
                        //Triggered by PDA: chapter activated/deactivated/completed
                        PdaStateInfo Received_PdaStateChange = (PdaStateInfo)data;
                        break;


                    case CmdId.Event_ConsoleCommand:
                        //Triggered when a player uses a Console Command in-game
                        ConsoleCommandInfo Received_ConsoleCommandInfo = (ConsoleCommandInfo)data;
                        break;


                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.ERROR("\r\nException:");
                CommonFunctions.ERROR("Message: " + ex.Message);
                CommonFunctions.ERROR("Data: " + ex.Data);
                CommonFunctions.ERROR("HelpLink: " + ex.HelpLink);
                CommonFunctions.ERROR("InnerException: " + ex.InnerException);
                CommonFunctions.ERROR("Source: " + ex.Source);
                CommonFunctions.ERROR("StackTrace: " + ex.StackTrace);
                CommonFunctions.ERROR("TargetSite: " + ex.TargetSite + "\r\n");
            }
        }
        public void Game_Update()
        {
            //Triggered whenever Empyrion experiences "Downtime", roughly 75-100 times per second
            if (delayActive)
            {
                delayActive = false;
                Dictionary<int, delayClass> delayedDictionary = delayDictionary;
                delayDictionary = new Dictionary<int, delayClass> { };
                delayTimer = 0;
                foreach (int delayEvent in delayDictionary.Keys)
                {
                    try
                    {
                        if (delayDictionary[delayEvent].connecting == true)
                        {
                            CommonFunctions.Debug("PlayerID Connecting = " + delayDictionary[delayEvent].player);
                            string ConnectingPlayerSteamID = modApi.Application.GetPlayerDataFor(delayDictionary[delayEvent].player).Value.SteamId;
                            if (!OnlinePlayers.Contains(ConnectingPlayerSteamID))
                            {
                                OnlinePlayers.Add(ConnectingPlayerSteamID);
                            }
                            if (OnlinePlayers.Count > 10 && LiteVersion)
                            {
                                Disable = true;
                            }
                            if (File.Exists(ModPath + "PlayersData\\" + ConnectingPlayerSteamID + "\\StopExploit.txt"))
                            {
                                ItemStack[] Backpack = new ItemStack[] { };
                                ItemStack[] Toolbar = new ItemStack[] { };
                                if (File.Exists(ModPath + "PlayersData\\" + ConnectingPlayerSteamID + "\\PlayerBackpack.txt"))
                                {
                                    try
                                    {
                                        string FolderPath = "PlayersData\\" + ConnectingPlayerSteamID + "\\";
                                        Backpack = CommonFunctions.ReadItemStacks(FolderPath, "PlayerBackpack.txt");
                                    }
                                    catch { }
                                }
                                if (File.Exists("PlayersData\\" + ConnectingPlayerSteamID + "\\PlayerToolbar.txt"))
                                {
                                    try
                                    {
                                        string FolderPath = "PlayersData\\" + ConnectingPlayerSteamID + "\\";
                                        Toolbar = CommonFunctions.ReadItemStacks(FolderPath, "PlayerToolbar.txt");
                                    }
                                    catch { }
                                }
                                API.PlayerInventorySet(delayDictionary[delayEvent].player, Backpack, Toolbar);
                                try { File.Delete(ModPath + "PlayersData\\" + ConnectingPlayerSteamID + "\\StopExploit.txt"); } catch { }
                            }
                            foreach (string AccessingPlayer in AccessingVB.Keys)
                            {
                                if (AccessingVB[AccessingPlayer].player == delayDictionary[delayEvent].player)
                                {
                                    try { AccessingVB.Remove(AccessingPlayer); } catch { }
                                }
                            }
                        }
                        else
                        {
                            foreach (string AccessingPlayer in AccessingVB.Keys)
                            {
                                if (AccessingVB[AccessingPlayer].player == delayDictionary[delayEvent].player)
                                {
                                    try { AccessingVB.Remove(AccessingPlayer); } catch { }
                                }
                                string DisconnectingPlayerSteamID = modApi.Application.GetPlayerDataFor(delayDictionary[delayEvent].player).Value.SteamId;
                                try { File.Create(ModPath + "PlayersData\\" + DisconnectingPlayerSteamID + "\\StopExploit.txt"); }
                                catch
                                {
                                    string DisconnectingPlayerName = modApi.Application.GetPlayerDataFor(delayDictionary[delayEvent].player).Value.PlayerName;
                                    CommonFunctions.LogFile("PossibleExploiter.txt", DisconnectingPlayerName + "   Disconnecting while VB open");
                                }
                            }
                        }
                    }
                    catch
                    {
                        if (delayDictionary[delayEvent].attempt < 10)
                        {
                            delayClass recycledDelayentry = delayDictionary[delayEvent];
                            recycledDelayentry.attempt = delayDictionary[delayEvent].attempt + 1;
                            delayTimer = delayTimer + 10;
                            delayDictionary.Add(delayTimer, recycledDelayentry);
                            delayActive = true;
                        }
                    }
                }
            }
        }
        public void Game_Exit()
        {
            //Triggered when the server is Shutting down. Does NOT pause the shutdown.
        }

        public void Init(IModApi modAPI)
        {
            modApi = modAPI;
            if (Expiration < int.Parse(CommonFunctions.UnixTimeStamp()))
            {
                Disable = true;
            }

            if (debug) { CommonFunctions.Debug("Init Started"); }
            if (!Directory.GetCurrentDirectory().EndsWith("DedicatedServer"))
            {
                ModPath = "Content\\Mods\\" + ModShortName + "\\";
            }
            else
            {
                ModPath = modApi.Application.GetPathFor(AppFolder.Mod) + "\\" + ModShortName + "\\";
            }
            string SaveGamePath = modApi.Application.GetPathFor(AppFolder.SaveGame);
            string[] SaveGameArray = SaveGamePath.Split('/');
            SaveGameName = SaveGameArray.Last();
            try
            {
                if (debug) { CommonFunctions.Debug("Setup Started"); }
                SetupYaml.Setup();
                if (debug) { CommonFunctions.Debug("Setup Complete"); }
            }
            catch
            {
                CommonFunctions.ERROR("ERROR: running SetupYaml.Setup() while Initializing failed");
                if (debug) { CommonFunctions.Debug("Setup Error"); }
            }

            //if (!Directory.GetCurrentDirectory().EndsWith("DedicatedServer")) { }
            CommonFunctions.Debug("ModPath = " + ModPath);
            CommonFunctions.Debug("SaveGamePath = " + SaveGamePath);
            CommonFunctions.Debug("SaveGameName = " + SaveGameName);

            if (modApi.Application.Mode == ApplicationMode.DedicatedServer)
            {
                List<byte> ByteList = new List<byte> { };
                byte[] ByteArray = ByteList.ToArray();
                //modApi.Network.SendToDedicatedServer("ModsNetwork", ByteArray, ModShortName);
                modApi.Application.ChatMessageSent += Application_ChatMessageSent;
                modApi.Network.RegisterReceiverForPlayfieldPackets(PlayfieldDataReceiver);
            }
        }

        private void PlayfieldDataReceiver(string sender, string playfieldName, byte[] data)
        {
            if (sender == "SubscriptionVerifier")
            {
                string IncommingData = CommonFunctions.ConvertByteArrayToString(data);
                if (IncommingData.StartsWith("Expiration "))
                {
                    int NewExpiration = int.Parse(IncommingData.Split(' ')[1]);
                    Expiration = NewExpiration;
                    if ( Expiration > int.Parse(CommonFunctions.UnixTimeStamp()))
                    {
                        Disable = false;
                    }
                    else
                    {
                        Disable = true;
                    }
                    CommonFunctions.LogFile("SV.txt", "Expiration = " + Expiration);
                    CommonFunctions.LogFile("SV.txt", "Disable = " + Disable);
                }
            }
        }

        private void Application_ChatMessageSent(Eleon.MessageData chatMsgData)
        {
            
            try { CommonFunctions.Debug("Text = " + chatMsgData.Text); } catch { }
            try { CommonFunctions.Debug("Channel = " + chatMsgData.Channel); } catch { }
            try { CommonFunctions.Debug("Mode = " + modApi.Application.Mode);}catch { }
            try { CommonFunctions.Debug(""); }catch { }
            

            if (!Disable)
            {
                string API2msg = chatMsgData.Text.ToLower();
                string API2msg1 = API2msg.Substring(1);
                CommonFunctions.Debug("msg1 = " + API2msg1);
                CommonFunctions.Debug("ContainsValidCommand = " + SetupYamlData.VirtualBackpacks.Commands.Contains(API2msg1));
                CommonFunctions.Debug("ContainsSpace = " + chatMsgData.Text.Contains(' '));

                if (SetupYamlData.VirtualBackpacks.Commands.Contains(API2msg1) && !chatMsgData.Text.Contains(' '))
                {
                    CommonFunctions.Debug("Valid Command and does not contain a space");
                    bool open = true;
                    try
                    {
                        CommonFunctions.Debug("try start. Channel validation");
                        if (chatMsgData.Channel == Eleon.MsgChannel.Global && SetupYamlData.General.BlockedChannels.Contains("Global"))
                        {
                            open = false;
                            API.ServerTell(chatMsgData.SenderEntityId, ModShortName, "This mod cannot be used in the Global chat channel", true);
                        }
                        else if (chatMsgData.Channel == Eleon.MsgChannel.Faction && SetupYamlData.General.BlockedChannels.Contains("Faction"))
                        {
                            open = false;
                            API.ServerTell(chatMsgData.SenderEntityId, ModShortName, "This mod cannot be used in the Faction chat channel", true);
                        }
                        else if (chatMsgData.Channel == Eleon.MsgChannel.Alliance && SetupYamlData.General.BlockedChannels.Contains("Alliance"))
                        {
                            open = false;
                            API.ServerTell(chatMsgData.SenderEntityId, ModShortName, "This mod cannot be used in the Alliance chat channel", true);
                        }
                        else if (chatMsgData.Channel == Eleon.MsgChannel.SinglePlayer && SetupYamlData.General.BlockedChannels.Contains("Private"))
                        {
                            open = false;
                            API.ServerTell(chatMsgData.SenderEntityId, ModShortName, "This mod cannot be used in the Private chat channel", true);
                        }
                        else if (chatMsgData.Channel == Eleon.MsgChannel.Server && SetupYamlData.General.BlockedChannels.Contains("Server"))
                        {
                            open = false;
                            API.ServerTell(chatMsgData.SenderEntityId, ModShortName, "This mod cannot be used in the Server chat channel", true);
                        }
                        CommonFunctions.Debug("try end. Channel Validation");
                    }
                    catch
                    {
                        CommonFunctions.Debug("Catch. Channel Validation");
                    }
                    CommonFunctions.Debug("Open = " + open);
                    if (open)
                    {
                        CommonFunctions.Debug("Chat message not sent on blocked channel");
                        try
                        {
                            ChatInfo newChatInfo = new ChatInfo
                            {
                                msg = chatMsgData.Text,
                                playerId = chatMsgData.SenderEntityId

                            };
                            Storage.StorableData function = new Storage.StorableData
                            {
                                function = "VB",
                                Match = Convert.ToString(chatMsgData.SenderEntityId),
                                Requested = "PlayerInfo",
                                ChatInfo = newChatInfo
                            };
                            API.PlayerInfo(chatMsgData.SenderEntityId, function);
                        }
                        catch
                        {
                            CommonFunctions.Debug("VirtualBackpack Fail: at ChatInfo");
                        }
                    }
                }
                else
                {
                    CommonFunctions.Debug("Invalid Command");
                }
            }
            CommonFunctions.Debug("Chat Message Section Complete");
        }

        public void Shutdown()
        {
        }
    }
}