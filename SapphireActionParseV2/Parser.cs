using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace SapphireActionParseV2
{
    public static class Parser
    {
        public static void ParseAll()
        {
            Dictionary<uint, Pair<string, string>> actionNameTable = new Dictionary<uint, Pair<string, string>>();
            Dictionary<uint, Pair<string, string>> statusNameTable = new Dictionary<uint, Pair<string, string>>();

            string basePath = Path.GetDirectoryName(Application.ExecutablePath);
            string actionNameFileEN = Path.Combine(basePath, "SkillList_EN.txt");
            string actionNameFileJP = Path.Combine(basePath, "SkillList_JP.txt");
            if (!File.Exists(actionNameFileEN) || !File.Exists(actionNameFileJP))
            {
                Console.Write("ERROR: SkillList_EN.txt or SkillList_JP.txt not found.");
                return;
            }
            Dictionary<uint, string> actionNameTableEN = new Dictionary<uint, string>();
            using (StreamReader sr = new StreamReader(actionNameFileEN))
            {
                string nextLine = sr.ReadLine();
                while (nextLine != null)
                {
                    string[] values = nextLine.Split('|');
                    if (values.Length == 2)
                    {
                        actionNameTableEN.Add(uint.Parse(values[0], System.Globalization.NumberStyles.HexNumber), values[1]);
                    }
                    nextLine = sr.ReadLine();
                }
            }
            Dictionary<uint, string> actionNameTableJP = new Dictionary<uint, string>();
            using (StreamReader sr = new StreamReader(actionNameFileJP))
            {
                string nextLine = sr.ReadLine();
                while (nextLine != null)
                {
                    string[] values = nextLine.Split('|');
                    if (values.Length == 2)
                    {
                        actionNameTableJP.Add(uint.Parse(values[0], System.Globalization.NumberStyles.HexNumber), values[1]);
                    }
                    nextLine = sr.ReadLine();
                }
            }
            foreach (var entry in actionNameTableEN)
            {
                if (actionNameTableJP.ContainsKey(entry.Key))
                {
                    var namePair = new Pair<string, string>(entry.Value, actionNameTableJP[entry.Key]);
                    //Console.WriteLine(string.Format("Found action: {0}, {1}, {2}", entry.Key, namePair.First, namePair.Second));
                    actionNameTable.Add(entry.Key, namePair);
                }
            }

            string statusNameFileEN = Path.Combine(basePath, "BuffList_EN.txt");
            string statusNameFileJP = Path.Combine(basePath, "BuffList_JP.txt");
            if (!File.Exists(statusNameFileEN) || !File.Exists(statusNameFileJP))
            {
                Console.Write("ERROR: BuffList_EN.txt or BuffList_JP.txt not found.");
                return;
            }
            Dictionary<uint, string> statusNameTableEN = new Dictionary<uint, string>();
            using (StreamReader sr = new StreamReader(statusNameFileEN))
            {
                string nextLine = sr.ReadLine();
                while (nextLine != null)
                {
                    string[] values = nextLine.Split('|');
                    if (values.Length == 2)
                    {
                        statusNameTableEN.Add(uint.Parse(values[0], System.Globalization.NumberStyles.HexNumber), values[1]);
                    }
                    nextLine = sr.ReadLine();
                }
            }
            Dictionary<uint, string> statusNameTableJP = new Dictionary<uint, string>();
            using (StreamReader sr = new StreamReader(statusNameFileJP))
            {
                string nextLine = sr.ReadLine();
                while (nextLine != null)
                {
                    string[] values = nextLine.Split('|');
                    if (values.Length == 2)
                    {
                        statusNameTableJP.Add(uint.Parse(values[0], System.Globalization.NumberStyles.HexNumber), values[1]);
                    }
                    nextLine = sr.ReadLine();
                }
            }
            foreach (var entry in statusNameTableEN)
            {
                if (statusNameTableJP.ContainsKey(entry.Key))
                {
                    var namePair = new Pair<string, string>(entry.Value, statusNameTableJP[entry.Key]);
                    //Console.WriteLine(string.Format("Found status: {0}, {1}, {2}", entry.Key, namePair.First, namePair.Second));
                    statusNameTable.Add(entry.Key, namePair);
                }
            }

            Dictionary<uint, FFXIVAction> actionTable = new Dictionary<uint, FFXIVAction>();
            Dictionary<uint, List<FFXIVStatusEffect>> statusEffectTable = new Dictionary<uint, List<FFXIVStatusEffect>>();

            DirectoryInfo di = new DirectoryInfo(basePath);
            foreach (var f in di.GetFiles("abilities_*.xml"))
            {
                Console.WriteLine("Found ability xml file: " + f.Name);

                XDocument xmlDoc = XDocument.Load(f.FullName);
                XElement root = xmlDoc.Element("AbilityList");
                foreach (XElement job in root.Elements("job"))
                {
                    foreach (XElement ability in job.Elements("ability"))
                    {
                        XAttribute attrId = ability.Attribute("id");
                        if (attrId == null) { continue; }
                        FFXIVAction action = new FFXIVAction();
                        uint id = uint.Parse(attrId.Value, System.Globalization.NumberStyles.HexNumber);
                        if (actionNameTable.ContainsKey(id))
                        {
                            var namePair = actionNameTable[id];
                            action.Id = id;
                            action.NamePairENJP = namePair;
                            Console.WriteLine(string.Format("Parsing {0}, {1}, {2}", id, namePair.First, namePair.Second));
                        }
                        else
                        {
                            continue;
                        }

                        XElement eleDamage = ability.Element("damage");
                        if (eleDamage != null)
                        {
                            XAttribute attrPontency = eleDamage.Attribute("potency");
                            XAttribute attrComboPontency = eleDamage.Attribute("combopotency");
                            if (attrPontency != null)
                            {
                                action.DamagePotency = uint.Parse(attrPontency.Value);
                                if (attrComboPontency != null)
                                {
                                    action.DamageComboPotency = uint.Parse(attrComboPontency.Value);
                                }
                                Console.WriteLine(string.Format("Found damage: potency={0}, combopotency={1}", action.DamagePotency, action.DamageComboPotency));
                            }
                        }

                        XElement eleHeal = ability.Element("heal");
                        if (eleHeal != null)
                        {
                            XAttribute attrPontency = eleHeal.Attribute("potency");
                            if (attrPontency != null)
                            {
                                if (eleHeal.Attribute("target")?.Value == "self")
                                {
                                    action.SelfHealPotency = uint.Parse(attrPontency.Value);
                                    Console.WriteLine(string.Format("Found self heal: potency={0}", action.SelfHealPotency));
                                }
                                else
                                {
                                    action.HealPotency = uint.Parse(attrPontency.Value);
                                    Console.WriteLine(string.Format("Found heal: potency={0}", action.HealPotency));
                                }
                            }
                        }

                        XElement elePowerHeal = ability.Element("powerheal");
                        if (elePowerHeal != null)
                        {
                            action.PowerHealTag = "";
                            Console.WriteLine("Found powerheal.");
                        }

                        XElement eleEnmity = ability.Element("enmity");
                        if (eleEnmity != null)
                        {
                            action.EnmityTag = eleEnmity.Attribute("type")?.Value;
                            if (action.EnmityTag != null)
                            {
                                Console.WriteLine("Found enmity.");
                            }
                        }

                        foreach (XElement eleBuff in ability.Elements("buff"))
                        {
                            XAttribute attrBuffId = eleBuff.Attribute("id");
                            if (attrBuffId == null) { continue; }
                            uint buffId = uint.Parse(attrBuffId.Value, System.Globalization.NumberStyles.HexNumber);
                            if (!statusNameTable.ContainsKey(buffId)) { continue; }
                            var namePair = statusNameTable[buffId];
                            if (eleBuff.Attribute("target")?.Value == "self")
                            {
                                action.SelfStatus = buffId;
                                XAttribute attrDuration = eleBuff.Attribute("duration");
                                action.SelfStatusDuration = attrDuration == null ? 0 : uint.Parse(attrDuration.Value) * 1000;
                                Console.WriteLine(string.Format("Found self status: {0}, {1}, {2}, {3}", id, action.SelfStatusDuration, namePair.First, namePair.Second));
                            }
                            else
                            {
                                action.TargetStatus = buffId;
                                XAttribute attrDuration = eleBuff.Attribute("duration");
                                action.TargetStatusDuration = attrDuration == null ? 0 : uint.Parse(attrDuration.Value) * 1000;
                                Console.WriteLine(string.Format("Found target status: {0}, {1}, {2}, {3}", id, action.TargetStatusDuration, namePair.First, namePair.Second));
                            }
                            foreach(XElement eleEffect in eleBuff.Elements("effect"))
                            {
                                XAttribute attrType = eleEffect.Attribute("type");
                                if (attrType == null) { continue; }
                                switch (attrType.Value)
                                {
                                    case "damagemultiplier":
                                        {
                                            XAttribute attrAmount = eleEffect.Attribute("amount");
                                            if (attrAmount == null) { continue; }
                                            FFXIVStatusEffect se = new FFXIVStatusEffect
                                            {
                                                StatusId = buffId,
                                                EffectType = StatusEffectType.DamageMultiplier,
                                                EffectValue1 = 255 // defaults to all
                                            };
                                            XAttribute attrLimitToDmgType = eleEffect.Attribute("limitto_damagetype");
                                            if (attrLimitToDmgType != null)
                                            {
                                                switch (attrLimitToDmgType.Value)
                                                {
                                                    case "physical":
                                                        {
                                                            se.EffectValue1 = (int)ActionTypeFilter.Physical;
                                                        }
                                                        break;
                                                    case "magic":
                                                        {
                                                            se.EffectValue1 = (int)ActionTypeFilter.Magical;
                                                        }
                                                        break;
                                                }
                                            }
                                            se.EffectValue2 = int.Parse(attrAmount.Value);
                                            statusEffectTable.Add(se);
                                        }
                                        break;
                                    case "damagereceivemultiplier":
                                        {
                                            XAttribute attrAmount = eleEffect.Attribute("amount");
                                            if (attrAmount == null) { continue; }
                                            FFXIVStatusEffect se = new FFXIVStatusEffect
                                            {
                                                StatusId = buffId,
                                                EffectType = StatusEffectType.DamageReceiveMultiplier,
                                                EffectValue1 = 255 // defaults to all
                                            };
                                            XAttribute attrLimitToDmgType = eleEffect.Attribute("limitto_damagetype");
                                            if (attrLimitToDmgType != null)
                                            {
                                                switch (attrLimitToDmgType.Value)
                                                {
                                                    case "physical":
                                                        {
                                                            se.EffectValue1 = (int)ActionTypeFilter.Physical;
                                                        }
                                                        break;
                                                    case "magic":
                                                        {
                                                            se.EffectValue1 = (int)ActionTypeFilter.Magical;
                                                        }
                                                        break;
                                                }
                                            }
                                            se.EffectValue2 = int.Parse(attrAmount.Value);
                                            statusEffectTable.Add(se);
                                        }
                                        break;
                                    case "healreceivemultiplier":
                                        {
                                            XAttribute attrAmount = eleEffect.Attribute("amount");
                                            if (attrAmount == null) { continue; }
                                            FFXIVStatusEffect se = new FFXIVStatusEffect
                                            {
                                                StatusId = buffId,
                                                EffectType = StatusEffectType.HealReceiveMultiplier,
                                                EffectValue2 = int.Parse(attrAmount.Value)
                                            };
                                            statusEffectTable.Add(se);
                                        }
                                        break;
                                    case "healcastmultiplier":
                                        {
                                            XAttribute attrAmount = eleEffect.Attribute("amount");
                                            if (attrAmount == null) { continue; }
                                            FFXIVStatusEffect se = new FFXIVStatusEffect
                                            {
                                                StatusId = buffId,
                                                EffectType = StatusEffectType.HealCastMultiplier,
                                                EffectValue2 = int.Parse(attrAmount.Value)
                                            };
                                            statusEffectTable.Add(se);
                                        }
                                        break;
                                    case "critmultiplier":
                                        {
                                            XAttribute attrAmount = eleEffect.Attribute("amount");
                                            if (attrAmount == null) { continue; }
                                            FFXIVStatusEffect se = new FFXIVStatusEffect
                                            {
                                                StatusId = buffId,
                                                EffectType = StatusEffectType.CritDHRateBonus,
                                                EffectValue1 = (int)CritDHBonusFilter.Damage,
                                                EffectValue2 = int.Parse(attrAmount.Value)
                                            };
                                            statusEffectTable.Add(se);
                                        }
                                        break;
                                }
                            }
                            foreach (XElement eleProc in eleBuff.Elements("proc"))
                            {
                                XAttribute attrTrigger = eleProc.Attribute("trigger");
                                if (attrTrigger == null) { continue; }
                                switch (attrTrigger.Value)
                                {
                                    case "dot":
                                        {
                                            XAttribute attrPot = eleProc.Attribute("potency");
                                            if (attrPot == null) { continue; }
                                            FFXIVStatusEffect se = new FFXIVStatusEffect
                                            {
                                                StatusId = buffId,
                                                EffectType = StatusEffectType.Dot,
                                                EffectValue1 = 0 // defaults to unknown
                                            };
                                            XAttribute attrDmgType = eleProc.Attribute("damagetype");
                                            if (attrDmgType != null)
                                            {
                                                switch (attrDmgType.Value)
                                                {
                                                    case "physical":
                                                        {
                                                            se.EffectValue1 = (int)ActionTypeFilter.Physical;
                                                        }
                                                        break;
                                                    case "magic":
                                                        {
                                                            se.EffectValue1 = (int)ActionTypeFilter.Magical;
                                                        }
                                                        break;
                                                }
                                            }
                                            se.EffectValue2 = int.Parse(attrPot.Value);
                                            statusEffectTable.Add(se);
                                        }
                                        break;
                                    case "hot":
                                        {
                                            XAttribute attrPot = eleProc.Attribute("potency");
                                            if (attrPot == null) { continue; }
                                            FFXIVStatusEffect se = new FFXIVStatusEffect
                                            {
                                                StatusId = buffId,
                                                EffectType = StatusEffectType.Hot,
                                                EffectValue2 = int.Parse(attrPot.Value)
                                            };
                                            statusEffectTable.Add(se);
                                        }
                                        break;
                                    case "damagereceived":
                                        {
                                            switch (eleProc.Attribute("type")?.Value)
                                            {
                                                case "damage":
                                                    {
                                                        XAttribute attrPot = eleProc.Attribute("potency");
                                                        if (attrPot == null) { continue; }
                                                        FFXIVStatusEffect se = new FFXIVStatusEffect
                                                        {
                                                            StatusId = buffId,
                                                            EffectType = StatusEffectType.DamageReceiveTrigger,
                                                            EffectValue1 = (int)ActionTypeFilter.Physical, // defaults to physical as only vengeance seems to have this
                                                            EffectValue2 = int.Parse(attrPot.Value),
                                                            EffectValue3 = (int)StatusEffectTriggerResult.ReflectDamage,
                                                        };
                                                        statusEffectTable.Add(se);
                                                    }
                                                    break;
                                            }
                                        }
                                        break;
                                    case "damagedealt":
                                        {
                                            switch (eleProc.Attribute("type")?.Value)
                                            {
                                                case "absorb":
                                                    {
                                                        XAttribute attrPercent = eleProc.Attribute("percent");
                                                        if (attrPercent == null) { continue; }
                                                        FFXIVStatusEffect se = new FFXIVStatusEffect
                                                        {
                                                            StatusId = buffId,
                                                            EffectType = StatusEffectType.DamageDealtTrigger,
                                                            EffectValue2 = int.Parse(attrPercent.Value),
                                                            EffectValue3 = (int)StatusEffectTriggerResult.AbsorbHP,
                                                        };
                                                        statusEffectTable.Add(se);
                                                    }
                                                    break;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        actionTable.Add(action);
                    }
                }
            }

            //#####################
            actionTable.Add(new FFXIVAction { Id = 7, NamePairENJP = actionNameTable[7], DamagePotency = 110 });
            actionTable.Add(new FFXIVAction { Id = 8, NamePairENJP = actionNameTable[8], DamagePotency = 100 });

            //actionTable[0].Modify(a => { });
            actionTable[3].Modify(a => { a.SelfStatusParam = 30; });
            actionTable[5].Modify(a => { a.SelfStatus = 0; a.SelfStatusDuration = 0; });
            actionTable[15].Modify(a => { a.GainMPPercentage = 10; });
            actionTable[29].Modify(a => { a.DamagePotency = 370; a.GainMPPercentage = 5; a.Comment = "potency set to max for now"; });
            actionTable[16457].Modify(a => { a.GainMPPercentage = 5; });
            actionTable[16460].Modify(a => { a.GainMPPercentage = 4; });
            actionTable[3623].Modify(a => { a.GainMPPercentage = 6; });
            actionTable[16468].Modify(a => { a.GainMPPercentage = 6; });
            actionTable[16508].Modify(a => { a.GainMPPercentage = 5; });
            actionTable[3571].Modify(a => { a.GainMPPercentage = 5; });
            actionTable[3643].Modify(a => { a.GainMPPercentage = 6; });
            actionTable[166].Modify(a => { a.GainMPPercentage = 10; });
            actionTable[7383].Modify(a => { a.DamagePotency = 550; a.Comment = "potency set to max for now"; });
            actionTable[158].Modify(a => { a.GainMPPercentage = 30; });
            actionTable[167].Modify(a => { a.GainMPPercentage = 5; });
            actionTable[44].Modify(a => { a.TargetStatus = 89; a.TargetStatusDuration = 15000; a.Comment = "This is a cheat to make vengeance working. Does not match retail packet but end result is the same. Have to script it if that matters."; });
            actionTable[88].Modify(a => { a.DamagePotency = 100; a.DamageComboPotency = 290; a.DamageDirectionalPotency = 330; });
            actionTable[7535].Modify(a => { a.DamagePotency = 0; });

            statusEffectTable.Overwrite(new FFXIVStatusEffect { StatusId = 1191, EffectType = StatusEffectType.DamageReceiveMultiplier, EffectValue1 = (int)ActionTypeFilter.All, EffectValue2 = -20 });
            statusEffectTable.Overwrite(new FFXIVStatusEffect { StatusId = 86, EffectType = StatusEffectType.CritDHRateBonus, EffectValue1 = (int)CritDHBonusFilter.Damage, EffectValue2 = 100, EffectValue3 = 100 });
            statusEffectTable.Overwrite(new FFXIVStatusEffect { StatusId = 1177, EffectType = StatusEffectType.CritDHRateBonus, EffectValue1 = (int)CritDHBonusFilter.Damage, EffectValue2 = 100, EffectValue3 = 100 });
            statusEffectTable.Overwrite(new FFXIVStatusEffect { StatusId = 1825, EffectType = StatusEffectType.CritDHRateBonus, EffectValue1 = (int)CritDHBonusFilter.Damage, EffectValue2 = 20, EffectValue3 = 20 });
            statusEffectTable.Overwrite(new FFXIVStatusEffect { StatusId = 1857, EffectType = StatusEffectType.DamageDealtTrigger, EffectValue2 = 50, EffectValue3 = (int)StatusEffectTriggerResult.AbsorbHP });
            //#####################

            using (StreamWriter sw = new StreamWriter("ActionLutData.cpp"))
            {
                sw.WriteLine("#include \"ActionLut.h\"");
                sw.WriteLine("");
                sw.WriteLine("using namespace Sapphire::World::Action;");
                sw.WriteLine("");
                sw.WriteLine("ActionLut::Lut ActionLut::m_actionLut =");
                sw.WriteLine("{");
                foreach (var action in actionTable)
                {
                    sw.WriteLine(string.Format("  //{0}, {1}", action.Value.NamePairENJP.First, action.Value.NamePairENJP.Second));
                    if (action.Value.DamagePotency > 0)
                    {
                        sw.WriteLine(string.Format("  //has damage: potency {0}, combo potency {1}, directional potency {2}", action.Value.DamagePotency, action.Value.DamageComboPotency, action.Value.DamageDirectionalPotency));
                    }
                    if (action.Value.HealPotency > 0)
                    {
                        sw.WriteLine(string.Format("  //has heal: potency {0}", action.Value.HealPotency));
                    }
                    if (action.Value.SelfHealPotency > 0)
                    {
                        sw.WriteLine(string.Format("  //has self heal: potency {0}", action.Value.SelfHealPotency));
                    }
                    if (action.Value.SelfStatus != 0)
                    {
                        var statusNamePair = statusNameTable[action.Value.SelfStatus];
                        sw.WriteLine(string.Format("  //applies to self: {0}, {1}, duration {2}, param {3}", statusNamePair.First, statusNamePair.Second, action.Value.SelfStatusDuration, action.Value.SelfStatusParam));
                    }
                    if (action.Value.TargetStatus != 0)
                    {
                        var statusNamePair = statusNameTable[action.Value.TargetStatus];
                        sw.WriteLine(string.Format("  //applies to targets: {0}, {1}, duration {2}, param {3}", statusNamePair.First, statusNamePair.Second, action.Value.TargetStatusDuration, action.Value.TargetStatusParam));
                    }
                    if (action.Value.GainMPPercentage > 0)
                    {
                        sw.WriteLine(string.Format("  //restores mp: {0}%", action.Value.GainMPPercentage));
                    }
                    if (action.Value.PowerHealTag != null)
                    {
                        sw.WriteLine("  //has powerheal: " + action.Value.PowerHealTag);
                    }
                    if (action.Value.EnmityTag != null)
                    {
                        sw.WriteLine("  //has enmity: " + action.Value.EnmityTag);
                    }
                    if (action.Value.Comment != null)
                    {
                        sw.WriteLine("  //comment: " + action.Value.Comment);
                    }
                    sw.WriteLine(string.Format("  {{ {0}, {{ {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12} }} }},",
                        action.Key,
                        action.Value.DamagePotency,
                        action.Value.DamageComboPotency,
                        action.Value.DamageDirectionalPotency,
                        action.Value.HealPotency,
                        action.Value.SelfHealPotency,
                        action.Value.SelfStatus,
                        action.Value.SelfStatusDuration,
                        action.Value.SelfStatusParam,
                        action.Value.TargetStatus,
                        action.Value.TargetStatusDuration,
                        action.Value.TargetStatusParam,
                        action.Value.GainMPPercentage));
                    sw.WriteLine("");
                }
                sw.WriteLine("};");
                sw.WriteLine("");
                sw.WriteLine("ActionLut::StatusEffectTable ActionLut::m_statusEffectTable =");
                sw.WriteLine("{");
                foreach (var entry in statusEffectTable)
                {
                    if (entry.Value.Count > 0)
                    {
                        for (int i = 0; i < entry.Value.Count; i++)
                        {
                            if (i == 1)
                            {
                                sw.WriteLine("  //more than 1 effect is found");
                            }
                            var statusNamePair = statusNameTable[entry.Key];
                            switch (entry.Value[i].EffectType)
                            {
                                case StatusEffectType.DamageMultiplier:
                                    {
                                        sw.Write(string.Format("  //{0}, {1}: DamageMultiplier, ", statusNamePair.First, statusNamePair.Second));
                                        sw.Write(((ActionTypeFilter)(entry.Value[i].EffectValue1)).ToString() + ", ");
                                        sw.WriteLine(entry.Value[i].EffectValue2.ToString() + "%");
                                    }
                                    break;
                                case StatusEffectType.DamageReceiveMultiplier:
                                    {
                                        sw.Write(string.Format("  //{0}, {1}: DamageReceiveMultiplier, ", statusNamePair.First, statusNamePair.Second));
                                        sw.Write(((ActionTypeFilter)(entry.Value[i].EffectValue1)).ToString() + ", ");
                                        sw.WriteLine(entry.Value[i].EffectValue2.ToString() + "%");
                                    }
                                    break;
                                case StatusEffectType.Dot:
                                    {
                                        sw.Write(string.Format("  //{0}, {1}: Dot, ", statusNamePair.First, statusNamePair.Second));
                                        sw.Write(((ActionTypeFilter)(entry.Value[i].EffectValue1)).ToString() + ", ");
                                        sw.WriteLine("potency " + entry.Value[i].EffectValue2.ToString());
                                    }
                                    break;
                                case StatusEffectType.Hot:
                                    {
                                        sw.Write(string.Format("  //{0}, {1}: Hot, ", statusNamePair.First, statusNamePair.Second));
                                        sw.WriteLine("potency " + entry.Value[i].EffectValue2.ToString());
                                    }
                                    break;
                                case StatusEffectType.HealReceiveMultiplier:
                                    {
                                        sw.Write(string.Format("  //{0}, {1}: HealReceiveMultiplier, ", statusNamePair.First, statusNamePair.Second));
                                        sw.WriteLine(entry.Value[i].EffectValue2.ToString() + "%");
                                    }
                                    break;
                                case StatusEffectType.HealCastMultiplier:
                                    {
                                        sw.Write(string.Format("  //{0}, {1}: HealCastMultiplier, ", statusNamePair.First, statusNamePair.Second));
                                        sw.WriteLine(entry.Value[i].EffectValue2.ToString() + "%");
                                    }
                                    break;
                                case StatusEffectType.CritDHRateBonus:
                                    {
                                        sw.Write(string.Format("  //{0}, {1}: CritDHRateBonus, ", statusNamePair.First, statusNamePair.Second));
                                        sw.Write(((CritDHBonusFilter)(entry.Value[i].EffectValue1)).ToString() + ", ");
                                        sw.Write("crit " + entry.Value[i].EffectValue2.ToString() + "%, ");
                                        sw.WriteLine("dh " + entry.Value[i].EffectValue3.ToString() + "%");
                                    }
                                    break;
                                case StatusEffectType.DamageReceiveTrigger:
                                    {
                                        sw.Write(string.Format("  //{0}, {1}: DamageReceiveTrigger, ", statusNamePair.First, statusNamePair.Second));
                                        sw.Write(((StatusEffectTriggerResult)(entry.Value[i].EffectValue3)).ToString() + ", ");
                                        switch ((StatusEffectTriggerResult)entry.Value[i].EffectValue3)
                                        {
                                            case StatusEffectTriggerResult.ReflectDamage:
                                                {
                                                    sw.Write(((ActionTypeFilter)(entry.Value[i].EffectValue1)).ToString() + ", ");
                                                    sw.WriteLine("potency " + entry.Value[i].EffectValue2.ToString());
                                                }
                                                break;
                                        }
                                    }
                                    break;
                                case StatusEffectType.DamageDealtTrigger:
                                    {
                                        sw.Write(string.Format("  //{0}, {1}: DamageDealtTrigger, ", statusNamePair.First, statusNamePair.Second));
                                        sw.Write(((StatusEffectTriggerResult)(entry.Value[i].EffectValue3)).ToString() + ", ");
                                        switch ((StatusEffectTriggerResult)entry.Value[i].EffectValue3)
                                        {
                                            case StatusEffectTriggerResult.AbsorbHP:
                                                {
                                                    sw.WriteLine(entry.Value[i].EffectValue2.ToString() + "%");
                                                }
                                                break;
                                        }
                                    }
                                    break;
                            }
                            if (entry.Value[i].Comment != null)
                            {
                                sw.WriteLine("  //comment: " + entry.Value[i].Comment);
                            }
                            sw.Write("  ");
                            if (i > 0)
                            {
                                sw.Write("//");
                            }
                            sw.WriteLine(string.Format("{{ {0}, {{ {1}, {2}, {3}, {4}, {5} }} }},", 
                                entry.Value[i].StatusId,
                                (uint)entry.Value[i].EffectType,
                                entry.Value[i].EffectValue1,
                                entry.Value[i].EffectValue2,
                                entry.Value[i].EffectValue3,
                                entry.Value[i].EffectValue4));
                        }
                        sw.WriteLine("");
                    }
                }
                sw.WriteLine("};");
            }
            Console.WriteLine("##### DONE #####");
        }

        private static void Add(this Dictionary<uint, FFXIVAction> actionTable, FFXIVAction action)
        {
            actionTable[action.Id] = action;
        }

        private static void Modify(this FFXIVAction action, Action<FFXIVAction> callback)
        {
            callback(action);
        }

        private static void Add(this Dictionary<uint, List<FFXIVStatusEffect>> table, FFXIVStatusEffect value)
        {
            if (value.EffectType == StatusEffectType.Invalid) { return; }
            if (!table.ContainsKey(value.StatusId))
            {
                table[value.StatusId] = new List<FFXIVStatusEffect>();
            }
            table[value.StatusId].Insert(0, value);
        }

        private static void Overwrite(this Dictionary<uint, List<FFXIVStatusEffect>> table, FFXIVStatusEffect value)
        {
            if (value.EffectType == StatusEffectType.Invalid) { return; }
            if (!table.ContainsKey(value.StatusId))
            {
                table[value.StatusId] = new List<FFXIVStatusEffect>();
            }
            table[value.StatusId].Clear();
            table[value.StatusId].Add(value);
        }

        private class Pair<T1, T2>
        {
            public T1 First{ get; set; }
            public T2 Second { get; set; }

            public Pair(T1 first, T2 second)
            {
                this.First = first;
                this.Second = second;
            }
        }

        private class FFXIVAction
        {
            public Pair<string, string> NamePairENJP { get; set; }
            public uint Id { get; set; }

            public uint DamagePotency { get; set; }
            public uint DamageComboPotency { get; set; }
            public uint DamageDirectionalPotency { get; set; }

            public uint HealPotency { get; set; }
            public uint SelfHealPotency { get; set; }

            public uint SelfStatus { get; set; }
            public uint SelfStatusDuration { get; set; }
            public uint SelfStatusParam { get; set; }
            public uint TargetStatus { get; set; }
            public uint TargetStatusDuration { get; set; }
            public uint TargetStatusParam { get; set; }
            public uint GainMPPercentage { get; set; }

            public string EnmityTag { get; set; }
            public string PowerHealTag { get; set; }

            public string Comment { get; set; }
        }

        private class FFXIVStatusEffect
        {
            public uint StatusId { get; set; }
            public StatusEffectType EffectType { get; set; }
            public int EffectValue1 { get; set; }
            public int EffectValue2 { get; set; }
            public int EffectValue3 { get; set; }
            public int EffectValue4 { get; set; }

            public string Comment { get; set; }
        }

        private enum StatusEffectType : uint
        {
            Invalid = 0,
            DamageMultiplier = 1,
            DamageReceiveMultiplier = 2,
            Hot = 3,
            Dot = 4,
            HealReceiveMultiplier = 5,
            HealCastMultiplier = 6,
            CritDHRateBonus = 7,
            DamageReceiveTrigger = 8,
            DamageDealtTrigger = 9,
            Shield = 10,
        }

        [Flags]
        private enum ActionTypeFilter : int
        {
            Unknown = 0,
            Physical = 1,
            Magical = 2,
            All = 255,
        }

        [Flags]
        private enum CritDHBonusFilter : int
        {
            None = 0,
            Damage = 1,
            Heal = 2,
            All = 255,
        }

        private enum StatusEffectTriggerResult : int
        {
            ReflectDamage = 1,
            AbsorbHP = 2,
        }
    }
}
