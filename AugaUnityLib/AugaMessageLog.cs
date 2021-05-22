using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AugaUnity
{
    public enum LogType
    {
        Arrival,
        Departure,
        PlayerDeath,
        TombstoneRecovered,
        NewItem,
        NewBiome,
        NewRecipe,
        NewCraftingStation,
        NewBuildPiece,
        Teleport,
        Crafted,
        Upgraded,
        PickUp,
        SkillImproved,
        Sleep,
        DefeatedBoss
    }

    public interface ILogData
    {
        LogType Type { get; }
        DateTime TimeStamp { get; }
        string Message { get; }
        string Subtext { get; }
    }

    public class SimpleLogData : ILogData
    {
        public LogType Type { get; }
        public DateTime TimeStamp { get; }
        public string Message { get; }
        public string Subtext { get; }

        public SimpleLogData(LogType type, DateTime timeStamp, string message, string subtext = "")
        {
            Type = type;
            TimeStamp = timeStamp;
            Message = message;
            Subtext = subtext;
        }
    }

    public class GroupedLogData : ILogData
    {
        public static readonly TimeSpan UpdateTimeframe = TimeSpan.FromSeconds(60);

        public LogType Type { get; }
        public DateTime TimeStamp => LastUpdatedTimeStamp;
        public string Message { get; private set; }
        public string Subtext { get; private set; }

        public string SingularLocId;
        public string PluralLocid;
        public DateTime CreationTimeStamp;
        public DateTime LastUpdatedTimeStamp;
        public readonly Dictionary<string, int> Values = new Dictionary<string, int>();

        public GroupedLogData(LogType type, DateTime timeStamp, string singularLocId, string pluralLocid, IDictionary<string, int> values)
        {
            Type = type;
            SingularLocId = singularLocId;
            PluralLocid = pluralLocid;
            CreationTimeStamp = timeStamp;
            AddValues(values);
        }

        public void AddValues(IDictionary<string, int> values)
        {
            if (values != null && values.Count > 0)
            {
                LastUpdatedTimeStamp = DateTime.Now;

                foreach (var newValueEntry in values)
                {
                    if (Values.ContainsKey(newValueEntry.Key))
                    {
                        Values[newValueEntry.Key] += newValueEntry.Value;
                    }
                    else
                    {
                        Values.Add(newValueEntry.Key, newValueEntry.Value);
                    }
                }

                var itemCount = Values.Sum(x => x.Value);
                Message = Localization.instance.Localize(itemCount == 1 ? SingularLocId : PluralLocid, itemCount.ToString());
                var valueStrings = Values.Select(x => x.Value > 1 ? $"{x.Key} x{x.Value}" : x.Key);
                Subtext = Localization.instance.Localize(string.Join(", ", valueStrings));
            }
        }

        public bool ShouldUpdate()
        {
            var timeSinceCreation = DateTime.Now - CreationTimeStamp;
            return timeSinceCreation <= UpdateTimeframe;
        }
    }

    [RequireComponent(typeof(MessageHud))]
    public class AugaMessageLog : MonoBehaviour
    {
        // ReSharper disable once InconsistentNaming
        public static AugaMessageLog instance { get; private set; }

        protected readonly Dictionary<LogType, float> _logCounters = new Dictionary<LogType, float>();
        protected readonly List<ILogData> _allLogs = new List<ILogData>();
        protected readonly Dictionary<LogType, List<ILogData>> _logsByType = new Dictionary<LogType, List<ILogData>>();

        public event Action<ILogData> OnLogAdded;
        public event Action<GroupedLogData> OnLogUpdated;

        public void Awake()
        {
            instance = this;
        }

        protected virtual void AddLog(ILogData data)
        {
            Debug.LogWarning($"AddLog ({data.Type}): {data.Message}");
            _allLogs.Add(data);
            if (!_logsByType.TryGetValue(data.Type, out var logList))
            {
                logList = new List<ILogData>();
                _logsByType.Add(data.Type, logList);
            }
            logList.Add(data);

            OnLogAdded?.Invoke(data);
        }

        protected virtual void AddGroupedLog(GroupedLogData groupedLogData)
        {
            for (var index = 0; index < _allLogs.Count; index++)
            {
                var log = _allLogs[index];
                if (log.Type == groupedLogData.Type && log is GroupedLogData olderGroupedLog && olderGroupedLog.ShouldUpdate())
                {
                    olderGroupedLog.AddValues(groupedLogData.Values);
                    _allLogs.Remove(olderGroupedLog);
                    _allLogs.Add(olderGroupedLog);
                    OnLogUpdated?.Invoke(olderGroupedLog);
                    return;
                }
            }

            AddLog(groupedLogData);
        }

        protected virtual string Loc(string s)
        {
            return Localization.instance.Localize(s);
        }

        protected virtual string Loc(string s, string arg1)
        {
            return Localization.instance.Localize(s, Loc(arg1));
        }

        protected virtual string Loc(string s, string arg1, string arg2)
        {
            return Localization.instance.Localize(s, Loc(arg1), Loc(arg2));
        }

        public virtual void AddArrivalLog(Player player)
        {
            var playerName = player == Player.m_localPlayer ? "$log_you" : player.GetPlayerName();
            AddLog(new SimpleLogData(LogType.Arrival, DateTime.Now, Loc("$log_arrival", playerName)));
        }

        public virtual void AddDepartureLog(Player player)
        {
            AddLog(new SimpleLogData(LogType.Arrival, DateTime.Now, Loc("$log_departure", player.GetPlayerName())));
        }

        public void AddDeathLog(Player player, HitData lastHit)
        {
            var isLocalPlayer = player == Player.m_localPlayer;
            var noCause = lastHit == null || (!lastHit.HaveAttacker() && lastHit.m_point == Vector3.zero && string.IsNullOrEmpty(lastHit.m_statusEffect));
            string message;
            if (noCause)
            {
                message = isLocalPlayer ? Loc("$log_death_local_nocause") : Loc("$log_death_nocause", player.GetPlayerName());
            }
            else
            {
                var cause = GetDeathCause(lastHit);
                message = isLocalPlayer ? Loc("$log_death_local", cause) : Loc("$log_death", player.GetPlayerName(), cause);
            }

            var percentSkillLoss = (player.m_skills.m_DeathLowerFactor * 100).ToString("0.#");
            var subtext = isLocalPlayer ? Loc("$death_skill_loss", percentSkillLoss) : "";

            AddLog(new SimpleLogData(LogType.PlayerDeath, DateTime.Now, message, subtext));
        }

        private static string GetDeathCause(HitData lastHit)
        {
            if (lastHit.HaveAttacker())
            {
                if (lastHit.GetAttacker().IsPlayer() && lastHit.GetAttacker() is Player player)
                {
                    return $"<color=#CD2121>{player.GetPlayerName()}</color>";
                }
                else
                {
                    return $"<color=#CD2121>{lastHit.GetAttacker().m_name}</color>";
                }
            }
            else if (lastHit.m_damage.m_fire > 0)
            {
                return "<color=#F9C84A>$inventory_fire $inventory_damage</color>";
            }
            else if (lastHit.m_damage.m_poison > 0)
            {
                return "<color=#9DFF5A>$inventory_poison $inventory_damage</color>";
            }
            else if (Mathf.Approximately(lastHit.m_damage.GetTotalDamage(), lastHit.m_damage.m_damage) && lastHit.m_point != Vector3.zero)
            {
                return "$log_death_natural";
            }
            else
            {
                return "<unknown>";
            }
        }

        public List<ILogData> GetAllLogs()
        {
            return _allLogs;
        }

        public void AddTombstoneLog(Player player)
        {
            AddLog(new SimpleLogData(LogType.TombstoneRecovered, DateTime.Now, Loc("$tombstone_recovered")));
        }

        public void AddNewMaterialLog(ItemDrop.ItemData item)
        {
            AddLog(new SimpleLogData(LogType.NewItem, DateTime.Now, Loc("$log_newmaterial", Loc(item.m_shared.m_name))));
        }

        public void AddNewTrophyLog(ItemDrop.ItemData item)
        {
            AddLog(new SimpleLogData(LogType.NewItem, DateTime.Now, Loc("$log_newtrophy", Loc(item.m_shared.m_name))));
        }

        public void AddNewItemLog(ItemDrop.ItemData item)
        {
            AddLog(new SimpleLogData(LogType.NewItem, DateTime.Now, Loc("$log_newitem", Loc(item.m_shared.m_name))));
        }

        public void AddNewRecipesLog(IReadOnlyList<Recipe> newRecipes)
        {
            if (newRecipes.Count == 0)
            {
                return;
            }

            if (newRecipes.Count == 1)
            {
                AddLog(new SimpleLogData(LogType.NewRecipe, DateTime.Now, Loc("$log_newrecipe", Loc(newRecipes[0].m_item.m_itemData.m_shared.m_name))));
            }
            else
            {
                var recipeList = newRecipes.Select(x => Loc(x.m_item.m_itemData.m_shared.m_name));
                AddLog(new SimpleLogData(LogType.NewRecipe, DateTime.Now, Loc("$log_newrecipes", newRecipes.Count.ToString()), string.Join(", ", recipeList)));
            }
        }

        public void AddNewStationLog(CraftingStation station)
        {
            AddLog(new SimpleLogData(LogType.NewCraftingStation, DateTime.Now, Loc("$log_newstation", Loc(station.m_name))));
        }

        private static string GetBiomeColor(Heightmap.Biome biome)
        {
            var biomeColor = "white";
            switch (biome)
            {
                case Heightmap.Biome.Meadows: biomeColor = "#75d966"; break;
                case Heightmap.Biome.BlackForest: biomeColor = "#72a178"; break;
                case Heightmap.Biome.Swamp: biomeColor = "#a88a6f"; break;
                case Heightmap.Biome.Mountain: biomeColor = "#a3bcd6"; break;
                case Heightmap.Biome.Plains: biomeColor = "#d6cea3"; break;
            }

            return biomeColor;
        }

        public void AddNewBiomeLog(Heightmap.Biome biome)
        {
            if (biome == Heightmap.Biome.None)
            {
                return;
            }

            var biomeColor = GetBiomeColor(biome);
            AddLog(new SimpleLogData(LogType.NewBiome, DateTime.Now, Loc("$log_newbiome", Loc($"$biome_{biome.ToString().ToLowerInvariant()}"), biomeColor)));
        }

        public void AddNewPieceLog(List<Piece> newPieces)
        {
            if (newPieces.Count == 0)
            {
                return;
            }

            if (newPieces.Count == 1)
            {
                AddLog(new SimpleLogData(LogType.NewBuildPiece, DateTime.Now, Loc("$log_newpiece", Loc(newPieces[0].m_name))));
            }
            else
            {
                var list = newPieces.Select(x => Loc(x.m_name));
                AddLog(new SimpleLogData(LogType.NewBuildPiece, DateTime.Now, Loc("$log_newpieces", newPieces.Count.ToString()), string.Join(", ", list)));
            }
        }

        public void AddTeleportLog(string portalTag, Player player)
        {
            portalTag = string.IsNullOrEmpty(portalTag) ? "$log_teleportnotag" : portalTag;
            AddLog(new SimpleLogData(LogType.Teleport, DateTime.Now, Loc("$log_teleport", Loc(portalTag))));
        }

        public void AddUpgradeItemLog(ItemDrop.ItemData item, int quality)
        {
            AddLog(new SimpleLogData(LogType.Upgraded, DateTime.Now, Loc("$log_upgrade", Loc(item.m_shared.m_name), quality.ToString())));
        }

        public void AddCraftItemLog(Recipe recipe)
        {
            AddLog(new SimpleLogData(LogType.Crafted, DateTime.Now, Loc("$log_craft", Loc(recipe.m_item.m_itemData.m_shared.m_name))));
        }

        public void AddSkillUpLog(Skills.SkillType skill, int level)
        {
            AddLog(new SimpleLogData(LogType.SkillImproved, DateTime.Now, Loc("$log_skillup", Loc("$skill_" + skill.ToString().ToLowerInvariant()), level.ToString())));
        }

        public void AddItemPickupLog(ItemDrop.ItemData item, int amount)
        {
            AddGroupedLog(new GroupedLogData(LogType.PickUp, DateTime.Now, "$log_pickup_one", "$log_pickup", new Dictionary<string, int>() { { item.m_shared.m_name, amount } }));
        }
    }
}
