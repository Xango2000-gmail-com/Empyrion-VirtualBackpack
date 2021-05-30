using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace VirtualBackpack
{
    class SetupYaml
    {
        public static Root2 ConversionsYamlData;
        public class Root2
        {
            public List<Conversions> InBothLists { get; set; }
            public List<Conversions> InOldListOnly { get; set; }
            public List<Conversions> InNewListOnly { get; set; }
            public List<Conversions> OldAndNewMatch { get; set; }
        }
        public class Conversions
        {
            public int oldID { get; set; }
            public int newID { get; set; }
            public string Name { get; set; }
        }
        public static Dictionary<int, int> BlockIDconverter = new Dictionary<int, int> { };
        public static Dictionary<int, int> NewIDs = new Dictionary<int, int> { };

        public static void Setup()
        {
            //string ts = CommonFunctions.TimestampArray()[0] +""+ CommonFunctions.TimestampArray()[1];
            //if (ts == "202012" || ts == "202101")
            //CommonFunctions.Debug("Timestamp check: " + 1612310400 + " > " + Int32.Parse(CommonFunctions.UnixTimeStamp()));
            Restore DefaultRestoreOnWipe = new Restore
            {
                VirtualBackpack = "true",
                Backpack = "false",
                Toolbar = "false",
                Credits = "false",
                FactoryResources = "false"
            };
            VBSettings DefaultVBSettings = new VBSettings
            {
                DeleteOnDeath = "false",
                Commands = new List<string> { },
                MinimumLevelRequired = 0,
                UsageCost = 0,
                Stack = "false",
                MaxSuperStack = 10000,
                RestrictedPlayfields = new List<string> { },
                RequireNearby = new List<Restrictions> { }
            };
            GeneralSettings DefaultGeneralSettings = new GeneralSettings
            {
                DefaultPrefix = "/",
                ReinitializeCommand = "vb reinit",
                //SaveGameName = "DoesntMatter",
                BlockedChannels = new List<string> { }
            };
            Root DefaultRoot = new Root
            {
                General = DefaultGeneralSettings,
                VirtualBackpacks = DefaultVBSettings,
                RestoreOnWipe = DefaultRestoreOnWipe
            };
            //if (MyEmpyrionMod.debug) { CommonFunctions.Debug(1620277199 + ">" + int.Parse(CommonFunctions.UnixTimeStamp())); }
                MyEmpyrionMod.SetupYamlData = ReadYaml(MyEmpyrionMod.ModPath + "Setup.yaml");
                try { DefaultRoot.General.DefaultPrefix = MyEmpyrionMod.SetupYamlData.General.DefaultPrefix; } catch { }
                try { DefaultRoot.General.ReinitializeCommand = MyEmpyrionMod.SetupYamlData.General.ReinitializeCommand; } catch { }
                try { DefaultRoot.General.BlockedChannels = MyEmpyrionMod.SetupYamlData.General.BlockedChannels; } catch { }
                try { DefaultRoot.VirtualBackpacks.DeleteOnDeath = MyEmpyrionMod.SetupYamlData.VirtualBackpacks.DeleteOnDeath; } catch { }
                try { DefaultRoot.VirtualBackpacks.Commands = MyEmpyrionMod.SetupYamlData.VirtualBackpacks.Commands; } catch { }
                try { DefaultRoot.VirtualBackpacks.MinimumLevelRequired = MyEmpyrionMod.SetupYamlData.VirtualBackpacks.MinimumLevelRequired; } catch { }
                try { DefaultRoot.VirtualBackpacks.UsageCost = MyEmpyrionMod.SetupYamlData.VirtualBackpacks.UsageCost; } catch { }
                try { DefaultRoot.VirtualBackpacks.Stack = MyEmpyrionMod.SetupYamlData.VirtualBackpacks.Stack; } catch { }
                try { DefaultRoot.VirtualBackpacks.MaxSuperStack = MyEmpyrionMod.SetupYamlData.VirtualBackpacks.MaxSuperStack; } catch { }
                try { DefaultRoot.VirtualBackpacks.RestrictedPlayfields = MyEmpyrionMod.SetupYamlData.VirtualBackpacks.RestrictedPlayfields; } catch { }
                try { DefaultRoot.VirtualBackpacks.RequireNearby = MyEmpyrionMod.SetupYamlData.VirtualBackpacks.RequireNearby; } catch { }
                try { DefaultRoot.RestoreOnWipe.VirtualBackpack = MyEmpyrionMod.SetupYamlData.RestoreOnWipe.VirtualBackpack; } catch { }
                try { DefaultRoot.RestoreOnWipe.Backpack = MyEmpyrionMod.SetupYamlData.RestoreOnWipe.Backpack; } catch { }
                try { DefaultRoot.RestoreOnWipe.Toolbar = MyEmpyrionMod.SetupYamlData.RestoreOnWipe.Toolbar; } catch { }
                try { DefaultRoot.RestoreOnWipe.Credits = MyEmpyrionMod.SetupYamlData.RestoreOnWipe.Credits; } catch { }
                try { DefaultRoot.RestoreOnWipe.FactoryResources = MyEmpyrionMod.SetupYamlData.RestoreOnWipe.FactoryResources; } catch { }
                try { MyEmpyrionMod.SetupYamlData = DefaultRoot; } catch { }
                //if (MyEmpyrionMod.SetupYamlData.General.BlockedChannels.Exists())
                List<string> CommandsList = new List<string> { };
                foreach (string Command in MyEmpyrionMod.SetupYamlData.VirtualBackpacks.Commands)
                {
                    CommandsList.Add(Command.ToLower());
                }
                MyEmpyrionMod.SetupYamlData.VirtualBackpacks.Commands = CommandsList;
                CommonFunctions.Debug("Setup Complete");
           
            CommonFunctions.Debug("Setup Section Complete");
        }

        public static Root2 ReadConversionsYaml(String filePath)
        {
            var input = File.OpenText(filePath);
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
            var Output = deserializer.Deserialize<Root2>(input);
            return Output;
        }


        public static Root ReadYaml(String filePath)
        {
            var input = File.OpenText(filePath);
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
            var Output = deserializer.Deserialize<Root>(input);
            return Output;
        }

        public class Root
        {
            public GeneralSettings General { get; set; }
            public VBSettings VirtualBackpacks { get; set; }
            public Restore RestoreOnWipe { get; set; }
        }

        public class GeneralSettings
        {
            public string DefaultPrefix { get; set; }
            public string ReinitializeCommand { get; set; }
            //public string SaveGameName { get; set; }
            public List<string> BlockedChannels { get; set; }
        }

        public class VBSettings
        {
            public string DeleteOnDeath { get; set; }
            public List<string> Commands { get; set; }
            public int MinimumLevelRequired { get; set; }
            public int UsageCost { get; set; }
            public string Stack { get; set; }
            public int MaxSuperStack { get; set; }
            public List<string> RestrictedPlayfields { get; set; }
            public List<Restrictions> RequireNearby { get; set; }
        }

        public class Restrictions
        {
            public string Type { get; set; }
            public int MaxRange { get; set; }
            public string EntityId { get; set; }
        }

        public class Restore
        {
            public string VirtualBackpack { get; set; }
            public string Backpack { get; set; }
            public string Toolbar { get; set; }
            public string Credits { get; set; }
            public string FactoryResources { get; set; }
        }

        public static void WriteYaml(string Path, Root ConfigData)
        {
            File.WriteAllText(Path, "---\r\n");
            Serializer serializer = new SerializerBuilder()
                .Build();
            string WriteThis = serializer.Serialize(ConfigData);
            File.AppendAllText(Path, WriteThis);
        }
    }
}
