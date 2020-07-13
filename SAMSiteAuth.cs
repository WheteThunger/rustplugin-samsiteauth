using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace Oxide.Plugins
{
    [Info("SAM Site Authorization", "haggbart", "2.3.4")]
    [Description("Makes SAM Sites act in a similar fashion to shotgun traps and flame turrets.")]
    internal class SAMSiteAuth : RustPlugin
    {
        private static readonly List<BasePlayer> players = new List<BasePlayer>();
        private static Dictionary<uint, int> vehicles;
        private static BuildingPrivlidge buildingPrivilege;
        private static BasePlayer driver;

        private const string ALLTARGET = "samsite.alltarget";
        private const string TARGET_HELI = "Target heli (requires alltarget)";

        private const uint MINICOPTER = 2278499844;
        private const uint CHINOOK_PLAYER = 1675349834;
        private const uint CHINOOK_NPC = 1514383717;
        private const uint SEDAN = 350141265;
        private const uint SCRAPHELI = 3484163637;
        private const uint BALLOON = 3111236903;
        private const uint HACKABLE_CRATE = 209286362;
        private const uint PATROL_HELICOPTER = 3029415845;


        protected override void LoadDefaultConfig()
        {
            Config[ALLTARGET] = false;
            Config[TARGET_HELI] = true;
        }

        private void Init()
        {
            vehicles = new Dictionary<uint, int>
            {
                {MINICOPTER, 1},    
                {CHINOOK_PLAYER, 1},     
                {SEDAN, 1},      
                {SCRAPHELI, 1},     
                {BALLOON, 2}      
            };
            if (!(bool)Config[ALLTARGET]) return;
            SamSite.alltarget = true;
            if (!(bool)Config[TARGET_HELI]) vehicles.Add(PATROL_HELICOPTER, 0); // attack heli
        }

        private void Unload() => SamSite.alltarget = false;

        private object OnSamSiteTarget(SamSite samSite, BaseCombatEntity target)
        {
            if (SamSite.alltarget)
            {
                Puts(target.prefabID.ToString());
                if (samSite.OwnerID == 0) // stop monument samsites from shooting attack heli or ch47
                {
                    if (target.prefabID == CHINOOK_NPC || target.prefabID == PATROL_HELICOPTER)
                    {
                        return false;
                    }
                }
                if (target.prefabID == HACKABLE_CRATE) // stop hackable crate being shot
                {
                    return false;
                }
            }
            int kind;
            if (!vehicles.TryGetValue(target.prefabID, out kind)) return null;
            if (!IsAuthed(samSite, target, kind)) return null;
            return false;
        }

        private static bool IsAuthed(SamSite samSite, BaseCombatEntity target, int kind)
        {
            switch (kind)
            {
                case 0: return true;
                case 1: return IsPilot(samSite, (BaseVehicle)target);
                case 2: return IsVicinity(samSite, target);
                default: return false;
            }
        }

        private static bool IsAuthed(BasePlayer player, BaseCombatEntity entity)
        {
            buildingPrivilege = entity.GetBuildingPrivilege();
            if (buildingPrivilege == null) return false;

            foreach (PlayerNameID nameId in buildingPrivilege.authorizedPlayers)
            {
                if (nameId.userid == player.userID) return true;
            }
            return false;
        }

        private static bool IsPilot(SamSite entity, BaseVehicle target)
        {
            driver = target.GetDriver();
            return driver == null || IsAuthed(driver, entity);
        }

        private static bool IsVicinity(SamSite entity, BaseCombatEntity target)
        {
            players.Clear();
            Vis.Entities(target.transform.position, 2, players, Rust.Layers.Mask.Player_Server);
            if (players.Count == 0) return true;

            foreach (BasePlayer player in players)
            {
                if (IsAuthed(player, entity)) return true;
            }

            return false;
        }
    }
}