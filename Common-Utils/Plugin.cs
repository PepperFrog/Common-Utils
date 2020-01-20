﻿using EXILED;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MEC;

namespace Common_Utils
{
    public class Plugin : EXILED.Plugin
    {
        public override string getName => "Common-Utils";

        // Item upgrade class :D

        public partial class Scp914ItemUpgrade
        {
            public ItemType ToUpgrade { get; set; }
            public ItemType UpgradedTo { get; set; }

            public static Scp914ItemUpgrade ParseString(string config)
            {
                string[] splitted = config.Split('-');
                DebugBoi("Adding upgrade: " + splitted[0] + " --> " + splitted[1]);
                return new Scp914ItemUpgrade() { ToUpgrade = (ItemType)Enum.Parse(typeof(ItemType), splitted[0], true), UpgradedTo = (ItemType)Enum.Parse(typeof(ItemType), splitted[1], true) };
            }
        }

        // Iven tory. lol

        public partial class CustomInventory
        {
            public List<ItemType> NtfCadet = null;

            public List<ItemType> NtfLieutenant = null;

            public List<ItemType> NtfCommander = null;

            public List<ItemType> ClassD = null;

            public List<ItemType> Scientist = null;

            public List<ItemType> NtfScientist = null;

            public List<ItemType> Chaos = null;

            public List<ItemType> Guard = null;

            public static List<ItemType> ConvertToItemList(List<string> list)
            {
                if (list == null)
                {
                    DebugBoi("Ignoring a inventory.");
                    return null;
                }
                List<ItemType> listd = new List<ItemType>();
                foreach(string s in list)
                {
                    DebugBoi("Adding item " + s);
                    listd.Add((ItemType)Enum.Parse(typeof(ItemType), s, true));
                }
                return listd;
            }

        }




        public CoroutineHandle cor;

        // Config settings.

        public CustomInventory Inventories = new CustomInventory();

        public Dictionary<RoleType, int> roleHealth = new Dictionary<RoleType, int>();

        public Dictionary<RoleType, RoleType> scp914Roles = new Dictionary<RoleType, RoleType>();

        public Dictionary<Scp914ItemUpgrade, Scp914.Scp914Knob> scp914Items = new Dictionary<Scp914ItemUpgrade, Scp914.Scp914Knob>();

        public EventHandlers EventHandler;

        public override void OnDisable()
        {
            Events.Scp914UpgradeEvent -= EventHandler.SCP914Upgrade;
            Events.PlayerJoinEvent -= EventHandler.PlayerJoin;
            Events.SetClassEvent -= EventHandler.SetClass;

            Timing.KillCoroutines(cor);

            Inventories = null;
            roleHealth.Clear();
            scp914Items.Clear();
            scp914Roles.Clear();

            scp914Items = null;
            scp914Roles = null;
            EventHandler = null;
        }

        public static void DebugBoi(string line)
        {
            if (Plugin.Config.GetBool("util_debug", false))
                Plugin.Info("CU-DEBUG | " + line);
        }

        public override void OnEnable()
        {
            if (!Config.GetBool("util_enable", true))
                return;

            Dictionary<string, string> configHealth = KConf.ExiledConfiguration.GetDictonaryValue(Config.GetString("util_role_health", "NtfCommander:400,NtfScientist:350"));

            try
            {
                foreach (KeyValuePair<string, string> kvp in configHealth)
                {
                    roleHealth.Add((RoleType)Enum.Parse(typeof(RoleType), kvp.Key), int.Parse(kvp.Value));
                }
            }
            catch (Exception e)
            {
                Error("Failed to add custom health to roles. Check your 'util_role_health' config values for errors!\n" + e);
                return;
            }

            Dictionary<string, string> configRoles = KConf.ExiledConfiguration.GetDictonaryValue(Config.GetString("util_914_roles", "ClassD:Scientist,NtfCadet:NtfLieutenant,NtfLieutenant:NtfScientist,NtfScientist:NtfCommander"));

            try
            {
                foreach (KeyValuePair<string, string> kvp in configRoles)
                {
                    scp914Roles.Add((RoleType)Enum.Parse(typeof(RoleType), kvp.Key), (RoleType)Enum.Parse(typeof(RoleType), kvp.Value));
                }
            }
            catch (Exception e)
            {
                Error("Failed to add roles. Check your 'util_914_roles' config values for errors!\n" + e);
                return;
            }

            Dictionary<string, string> configItems = KConf.ExiledConfiguration.GetDictonaryValue(Config.GetString("util_914_items", "Painkillers-Medkit:Fine,Coin-Flashlight:OneToOne"));

            try
            {
                foreach (KeyValuePair<string, string> kvp in configItems)
                {
                    scp914Items.Add(Scp914ItemUpgrade.ParseString(kvp.Key), (Scp914.Scp914Knob) Enum.Parse(typeof(Scp914.Scp914Knob), kvp.Value));
                }
            }
            catch (Exception e)
            {
                Error("Failed to add items to 914. Check your 'util_914_items' config values for errors!\n" + e);
                return;
            }

            // Custom items
            try
            {
                Inventories = new CustomInventory();
                Inventories.ClassD = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_classd_inventory", null)));
                Inventories.Chaos = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_choas_inventory", null)));
                Inventories.NtfCadet = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_ntfcadet_inventory", null)));
                Inventories.NtfCommander = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_ntfcommander_inventory", null)));
                Inventories.NtfLieutenant = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_ntflieutenant_inventory", null)));
                Inventories.NtfScientist = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_ntfscientist_inventory", null)));
                Inventories.Scientist = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_scientist_inventory", null)));
                Inventories.Guard = CustomInventory.ConvertToItemList(KConf.ExiledConfiguration.GetListStringValue(Config.GetString("util_guard_inventory", null)));
            }
            catch (Exception e)
            {
                Error("Failed to add items to custom inventorys!. Check your inventory config values for errors!\n[EXCEPTION] For Developers:\n" + e);
                return;
            }

            string broadcastMessage = Config.GetString("util_broadcast_message", "<color=lime>This server is running <color=red>EXILED-CommonUtils</color>, enjoy playing!</color>");

            int boradcastSeconds = Config.GetInt("util_broadcast_seconds", 300); // 300 is 5 minutes. :D
            int boradcastTime = Config.GetInt("util_broadcast_time", 4);

            string joinMessage = Config.GetString("util_joinMessage", "<color=lime>Welcome %player%! Please read our rules!</color>");
            int joinMessageTime = Config.GetInt("util_joinMessage_time", 6);    

            EventHandler = new EventHandlers(scp914Roles,scp914Items, roleHealth, broadcastMessage, joinMessage, boradcastTime, boradcastSeconds, joinMessageTime, Inventories);
            Events.PlayerJoinEvent += EventHandler.PlayerJoin;
            Events.Scp914UpgradeEvent += EventHandler.SCP914Upgrade;
            Events.SetClassEvent += EventHandler.SetClass;

            cor = Timing.RunCoroutine(EventHandler.CustomBroadcast());

            Info("Common-Utils Loaded! Created by KadeDev.");
        }

        public override void OnReload()
        {
            
        }
    }
}
