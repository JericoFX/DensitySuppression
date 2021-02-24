using CitizenFX.Core;
using CitizenFX.Core.Native;
using Essentials.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously.

namespace DensitySuppression.Client
{
    public class Main : ClientScript
    {

        [Tick]
        internal async Task OnTick()
        {
            Vector3 pos = Game.PlayerPed.Position;
            float oldMultiplier = this._vehMultiplier;
            if (pos.Z < -30f)
            {
                this._vehMultiplier = 0f;
            }
            else
            {
                this._vehMultiplier = 1f;
            }
            int nearbyPlayers = 0;
            foreach (Player p in base.Players)
            {
                if (p != Game.Player && Game.PlayerPed.Position.DistanceToPlayer(p.Character.Position) < 200f)
                {
                    nearbyPlayers++;
                }
            }
            if (nearbyPlayers > 0)
            {
                this._vehMultiplier -= (float)(nearbyPlayers / this._maxPlayers);
            }
            this._vehMultiplier = (float)Math.Abs(Math.Round((double)this._vehMultiplier, 2));
            if (this._vehMultiplier > 1f)
            {
                this._vehMultiplier = 1f;
            }
            if (this._vehMultiplier != oldMultiplier)
            {
                Debug.WriteLine(string.Format("New multiplier value! Old: {0}, New: {1}", oldMultiplier, this._vehMultiplier));
            }
            API.SetVehicleDensityMultiplierThisFrame(this._vehMultiplier);
            API.SetRandomVehicleDensityMultiplierThisFrame(this._vehMultiplier);
            API.SetPedDensityMultiplierThisFrame(this._vehMultiplier);
            API.SetScenarioPedDensityMultiplierThisFrame(this._vehMultiplier, this._vehMultiplier);
            API.SetRandomTrains(false);
            API.SetRandomBoats(false);
        }


        private static ConfigModel config = new ConfigModel();
        public Main()
        {
            var data = LoadResourceFile(GetCurrentResourceName(), "config.json");
            try
            {
                config = JsonConvert.DeserializeObject<ConfigModel>(data);
                if (config.ClearRandomCops)
                {

                    Tick += ClearRandomCops;
                }
                if (config.Traffic)
                {
                    Tick += ClearTraffic;
                }
                if (config.ClearAudio)
                {
                    Tick += ClearAudio;
                }
                if (config.ClearDispatch)
                {
                    Tick += ClearDispatch;
                }
                if (config.ClearModels)
                {
                    Tick += ClearModels;
                }
                if (config.ClearScenarioTypes)
                {
                    Tick += ClearScenarioTypes;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

        }
        [Tick]
        internal async Task ClearAudio()
        {
            API.StartAudioScene("CHARACTER_CHANGE_IN_SKY_SCENE");
        }


        [Tick]
        internal async Task ClearDispatch()
        {
            for (int i = 0; i < 16; i++)
            {
                API.EnableDispatchService(i, false);
            }
            await BaseScript.Delay(1500);
        }

        [Tick]
        internal async Task ClearCops()
        {
            Ped p = Game.PlayerPed;
            Vector3 pos = p.Position;
            if (Entity.Exists(p))
            {
                API.ClearAreaOfCops(pos.X, pos.Y, pos.Z, 800f, 0);
            }
        }

        [Tick]
        internal async Task ClearTraffic()
        {
            await Delay(0);
            API.SetGarbageTrucks(false);
            var pos = Game.PlayerPed.Position;
            API.ClearAreaOfVehicles(pos.X, pos.Y, pos.Z, 1000, false, false, false, false, false);
            API.RemoveVehiclesFromGeneratorsInArea(pos.X - 500.0F, pos.Y - 500.0F, pos.Z - 500.0F, pos.X + 500.0F, pos.Y + 500.0F, pos.Z + 500.0F, 0);
        }

        [Tick]
        internal async Task ClearRandomCops()
        {
            await Delay(100);
            API.SetCreateRandomCops(false);
            API.SetCreateRandomCopsNotOnScenarios(false);
            API.SetCreateRandomCopsOnScenarios(false);
        }

        [Tick]
        internal async Task PlayerSettings()
        {
            await Delay(0);
            API.SetMaxWantedLevel(0);
        }

        [Tick]
        internal async Task ClearLowLODVehicles()
        {
            await Delay(0);
            API.SetDistantCarsEnabled(false);
            API.SetFarDrawVehicles(false);
        }

        [Tick]
        internal async Task ClearModels()
        {
            foreach (string model in this._modelsToSuppress)
            {
                API.SetVehicleModelIsSuppressed((uint)API.GetHashKey(model), true);
            }
            await Task.FromResult<int>(0);
        }

        [Tick]
        internal async Task ClearScenarioTypes()
        {
            foreach (string scenarioType in this._scenarioTypes)
            {
                API.SetScenarioTypeEnabled(scenarioType, false);
            }
            await BaseScript.Delay(3000);
        }

        [Tick]
        internal async Task ClearScenarioGroups()
        {
            foreach (string scenarioGroup in this._scenarioGroups)
            {
                API.SetScenarioGroupEnabled(scenarioGroup, false);
            }
            await BaseScript.Delay(3000);
        }

        [Tick]
        internal async Task ClearBlacklistedVehicles()
        {
            World.GetAllVehicles().ToList<Vehicle>().ForEach(async delegate (Vehicle v)
            {
                if (this._modelsToSuppress.Any((string m) => API.GetHashKey(m) == v.Model.Hash) && Entity.Exists(v) && !v.PreviouslyOwnedByPlayer)
                {
                    API.NetworkRequestControlOfEntity(v.Handle);
                    int timeout = 5000;
                    while (timeout > 0 && !API.NetworkHasControlOfEntity(v.Handle))
                    {
                        await BaseScript.Delay(100);
                        timeout -= 100;
                    }
                    v.Delete();
                }
            });
            await BaseScript.Delay(100);
        }

        private float _vehMultiplier = 1f;

        private readonly int _maxPlayers = API.GetConvarInt("sv_maxclients", 32);

        private readonly List<string> _modelsToSuppress = new List<string>
        {
            "police",
            "police2",
            "police3",
            "police4",
            "policeb",
            "policeold1",
            "policeold2",
            "policet",
            "polmav",
            "pranger",
            "sheriff",
            "sheriff2",
            "stockade3",
            "buffalo3",
            "fbi",
            "fbi2",
            "firetruk",
            "jester2",
            "lguard",
            "ambulance",
            "riot",
            "SHAMAL",
            "LUXOR",
            "LUXOR2",
            "JET",
            "LAZER",
            "TITAN",
            "BARRACKS",
            "BARRACKS2",
            "CRUSADER",
            "RHINO",
            "AIRTUG",
            "RIPLEY",
            "docktrailer",
            "trflat",
            "trailersmall",
            "boattrailer",
            "cargobob",
            "cargobob2",
            "cargobob3",
            "cargobob4",
            "volatus",
            "buzzard",
            "buzzard2",
            "besra"
        };

        private readonly List<string> _scenarioTypes = new List<string>
        {
            "WORLD_VEHICLE_POLICE_BIKE",
            "WORLD_VEHICLE_POLICE_CAR",
            "WORLD_VEHICLE_POLICE_NEXT_TO_CAR",
            "WORLD_VEHICLE_MILITARY_PLANES_SMALL",
            "WORLD_VEHICLE_MILITARY_PLANES_BIG",
            "CODE_HUMAN_POLICE_CROWD_CONTROL",
            "CODE_HUMAN_POLICE_INVESTIGATE",
            "WORLD_HUMAN_COP_IDLES",
            "WORLD_HUMAN_GUARD_STAND_ARMY",
            "PROP_HUMAN_MUSCLE_CHIN_UPS_ARMY",
            "WORLD_FISH_FLEE",
            "WORLD_FISH_IDLE"
        };

        private readonly List<string> _scenarioGroups = new List<string>
        {
            "FIB_GROUP_1",
            "FIB_GROUP_2",
            "MP_POLICE",
            "ARMY_HELI",
            "POLICE_POUND1",
            "POLICE_POUND2",
            "POLICE_POUND3",
            "POLICE_POUND4",
            "POLICE_POUND5",
            "SANDY_PLANES",
            "LSA_Planes",
            "GRAPESEED_PLANES",
            "Grapeseed_Planes",
            "ALAMO_PLANES",
            "ng_planes"
        };
    }
}
