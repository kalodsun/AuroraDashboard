using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace AuroraDashboard {

    public enum InstallationType { ConstructionFactory, FighterFactory, OrdnanceFactory, Mine, AutoMine, DSTS, Refinery, FinancialCenter, MaintenanceFacility,
        ResearchFacility, TerraforingInstallation, MassDriver, Infrastructure, InfrastructureLG, CivilianMine, Other }
    public enum MineralType { Duranium, Neutronium, Corbomite, Tritanium, Boronide, Mercassium, Vendarite, Sorium, Uridium, Corundium, Gallicite }

    public class AurHull {
        public int ID;
        public string abbrev;
        public string name;
    }

    public enum ComponentType { Engine, Armor, Weapon, ECM, ECCM, AcitveSensor, PassiveSensor, JumpDrive, BFC, Other}

    public class AurComponent {
        public int ID;
        public string name;
        public ComponentType componentType;
        public double cost;
        public double[] mineralCost = new double[Enum.GetValues(typeof(MineralType)).Length];
        public int crew;
        public int HTK;
        public double size;

        public static ComponentType GetComponentType(string typeName) {
            switch (typeName) {
            case "Railgun":
            case "Gauss Cannon":
            case "Carronade":
            case "Meson Cannon":
            case "High Power Microwave":
            case "Laser":
            case "Particle Beam":
                return ComponentType.Weapon;
            case "Beam Fire Control":
                return ComponentType.BFC;
            default:
                return ComponentType.Other;
            }
        }

        public static AurComponent CreateComponentOfType(ComponentType type) {
            switch (type) {
            case ComponentType.Weapon:
                return new AurCompWeapon();
            case ComponentType.BFC:
                return new AurCompBFC();
            }

            return new AurComponent();
        }

        public AurComponent() {
            componentType = ComponentType.Other;
        }
    }

    public class AurCompWeapon : AurComponent {
        public double damage;
        public double rangeMod;
        public double rangeMax;
        public double toHitMod;
        public double trackingSpeed;
        public double recharge;
        public double powerRequired;
        public int shots;

        public bool isSpinal;
        public bool ignoreShields;
        public bool ignoreArmor;

        public AurCompWeapon() {
            componentType = ComponentType.Weapon;
        }
    }

    public class AurCompBFC : AurComponent {
        public double trackingSpeed;
        public double rangeMax;

        public AurCompBFC() {
            componentType = ComponentType.BFC;
        }
    }

    public class AurClass {
        public class Comp {
            public AurComponent component;
            public int number;

            public Comp(AurComponent comp, int num) {
                component = comp;
                number = num;
            }
        }

        public int ID;
        public string name;
        public double tonnage;
        public double cost;
        public double[] mineralCost = new double[Enum.GetValues(typeof(MineralType)).Length];
        public double fuel;
        public double msp;
        public double fuelEff;
        public double maxSpeed;
        public double ppv;
        public int armor;
        public int crew;
        public int miningModules;
        public bool isMilitary;
        public bool isObsolete;
        public AurHull hull;
        public Dictionary<ComponentType, List<Comp>> components = new Dictionary<ComponentType, List<Comp>>();

        public AurClass() {
            foreach (ComponentType type in Enum.GetValues(typeof(ComponentType))) {
                components.Add(type, new List<Comp>());
            }
        }
    }

    public class AurShip {
        public int ID;
        public string name;
        public int crew;
        public double fuel;
        public double grade;
        public double msp;
        public double overhaulTime;

        public AurRace race;
        public AurFleet fleet;
        public AurClass shipClass;
    }

    public class AurFleet {
        public int ID;
        public string name;

        public AurRace race;
        public List<AurShip> ships = new List<AurShip>();
        public AurSystem system;
        public AurBody orbitBody;
        public AurPop assignedPop;

        public double x, y;
        public double speed;
    }

    public class AurPop {
        public int ID;
        public string name;

        public AurRace race;
        public AurBody body;
        public AurSpecies species;
        public double population;
        public double fuel;
        public double fuelProd;
        public double msp;
        public double mspProd;

        public double[] installations = new double[Enum.GetValues(typeof(InstallationType)).Length];
        public double[] minerals = new double[Enum.GetValues(typeof(MineralType)).Length];
        public List<AurFleet> oribitingFleets = new List<AurFleet>();
    }

    public class AurRSystem {
        public int ID;
        public string Name;
        public double x, y;

        public AurRace race;
        public AurSystem system;
    }

    public class AurJP {
        public int ID;
        public double x, y;
        public bool stable;

        public AurSystem system;
        public AurJP connection;
    }

    public class AurBody {
        public int ID;
        public string Name;
        public string FirstRaceName;

        public AurSystem system;
        public AurBody parent = null;
        public List<AurBody> children = new List<AurBody>();

        public Dictionary<AurRace, AurPop> populations = new Dictionary<AurRace, AurPop>();
        public Dictionary<AurRace, bool> isSurveyed = new Dictionary<AurRace, bool>();

        public bool hasMinerals;
        public double[] minerals = new double[Enum.GetValues(typeof(MineralType)).Length];
        public double[] mineralsAcc = new double[Enum.GetValues(typeof(MineralType)).Length];
        public int surveyPotential;
        public int ruins;

        public string GetName(AurRace viewRace) {
            if (FirstRaceName != null) {
                return FirstRaceName;
            } else if (Name != "") {
                return Name;
            } else if (populations.ContainsKey(viewRace)) {
                return populations[viewRace].name;
            } else if (viewRace.knownSysIdx.ContainsKey(system)) {
                return viewRace.knownSysIdx[system].Name;
            } else {
                return ID.ToString();
            }
        }
    }

    public class AurSystem {
        public int ID;

        public List<AurBody> Bodies = new List<AurBody>();
    }

    public class AurSpecies {
        public int ID;
        public string name;
    }

    public class AurRace {
        public class Comp {
            public AurComponent component;
            public bool isObsolete;

            public Comp(AurComponent comp, bool obs) {
                component = comp;
                isObsolete = obs;
            }
        }

        public int ID;
        public string name;

        public double[] minerals = new double[Enum.GetValues(typeof(MineralType)).Length];
        public double[] installations = new double[Enum.GetValues(typeof(InstallationType)).Length];

        public double populationSum;

        public double popFuelSum;
        public double shipFuelSum;
        public double shipFuelCapSum;

        public double popMSPSum;
        public double shipMSPSum;
        public double shipMSPCapSum;
        public double shipMSPAnnualCost;

        public AurPop capital;
        public List<AurRSystem> knownSystems = new List<AurRSystem>();
        public List<AurPop> populations = new List<AurPop>();

        public List<AurClass> classes = new List<AurClass>();
        public List<AurShip> ships = new List<AurShip>();
        public List<AurFleet> fleets = new List<AurFleet>();

        public Dictionary<AurSystem, AurRSystem> knownSysIdx = new Dictionary<AurSystem, AurRSystem>();
        public Dictionary<ComponentType, List<Comp>> knownCompIdx = new Dictionary<ComponentType, List<Comp>>();

        public AurRace() {
            foreach (ComponentType type in Enum.GetValues(typeof(ComponentType))) {
                knownCompIdx.Add(type, new List<Comp>());
            }
        }
    }

    public class AurGame {
        public int ID;
        public string name;
        public double curTime;

        public List<AurRace> Races = new List<AurRace>();
        public List<AurSpecies> Species = new List<AurSpecies>();
        public List<AurSystem> Systems = new List<AurSystem>();
        public Dictionary<ComponentType, List<AurComponent>> Components = new Dictionary<ComponentType, List<AurComponent>>();

        public Dictionary<int, AurRace> raceIdx = new Dictionary<int, AurRace>();
        public Dictionary<int, AurSpecies> speciesIdx = new Dictionary<int, AurSpecies>();
        public Dictionary<int, AurSystem> sysIdx = new Dictionary<int, AurSystem>();
        public Dictionary<int, AurBody> bodyIdx = new Dictionary<int, AurBody>();
        public Dictionary<int, AurPop> popIdx = new Dictionary<int, AurPop>();
        public Dictionary<int, AurClass> classIdx = new Dictionary<int, AurClass>();
        public Dictionary<int, AurShip> shipIdx = new Dictionary<int, AurShip>();
        public Dictionary<int, AurFleet> fleetIdx = new Dictionary<int, AurFleet>();
        public Dictionary<int, AurComponent> componentIdx = new Dictionary<int, AurComponent>();

        public AurGame() {
            foreach(ComponentType type in Enum.GetValues(typeof(ComponentType))) {
                Components.Add(type, new List<AurComponent>());
            }
        }
    }

    public class AuroraData {
        public List<AurGame> Games = new List<AurGame>();

        public List<AurHull> Hulls = new List<AurHull>();
        public Dictionary<int, AurHull> hullIdx = new Dictionary<int, AurHull>();
    }

    public class AuroraDBReader {
        SqliteConnection con;

        public int[] instToID = new int[Enum.GetValues(typeof(InstallationType)).Length];
        public int[] IDToInst = new int[200];

        public Dictionary<int, string> componentTypesToString = new Dictionary<int, string>();

        public Dictionary<SqliteDataReader, Dictionary<string, int>> colCache = new Dictionary<SqliteDataReader, Dictionary<string, int>>();
        int getCol(SqliteDataReader reader, string name) {
            if (colCache.ContainsKey(reader)) {
                if (colCache[reader].ContainsKey(name)) {
                    return colCache[reader][name];
                }
            } else {
                colCache.Add(reader, new Dictionary<string, int>());
            }

            for (int i = 0; i < reader.FieldCount; i++) {
                if (reader.GetName(i) == name) {
                    colCache[reader].Add(name, i);
                    return i;
                }
            }

            return -1;
        }

        void LoadDIMData(SqliteConnection con) {
            var cmd = new SqliteCommand("SELECT * FROM DIM_PlanetaryInstallation;", con);
            SqliteDataReader instReader = cmd.ExecuteReader();

            for(int i = 0;i < IDToInst.Length; i++) {
                IDToInst[i] = (int)InstallationType.Other;
            }

            while (instReader.Read()) {
                int ID = instReader.GetInt32(getCol(instReader, "PlanetaryInstallationID"));

                switch (instReader.GetString(getCol(instReader, "Name"))) {
                case "Construction Factory":
                    instToID[(int)InstallationType.ConstructionFactory] = ID;
                    IDToInst[ID] = (int)InstallationType.ConstructionFactory;
                    break;
                case "Fuel Refinery":
                    instToID[(int)InstallationType.Refinery] = ID;
                    IDToInst[ID] = (int)InstallationType.Refinery;
                    break;
                case "Mine":
                    instToID[(int)InstallationType.Mine] = ID;
                    IDToInst[ID] = (int)InstallationType.Mine;
                    break;
                case "Automated Mine":
                    instToID[(int)InstallationType.AutoMine] = ID;
                    IDToInst[ID] = (int)InstallationType.AutoMine;
                    break;
                case "Ordnance Factory":
                    instToID[(int)InstallationType.OrdnanceFactory] = ID;
                    IDToInst[ID] = (int)InstallationType.OrdnanceFactory;
                    break;
                case "Fighter Factory":
                    instToID[(int)InstallationType.FighterFactory] = ID;
                    IDToInst[ID] = (int)InstallationType.FighterFactory;
                    break;
                case "Deep Space Tracking Station":
                    instToID[(int)InstallationType.DSTS] = ID;
                    IDToInst[ID] = (int)InstallationType.DSTS;
                    break;
                case "Mass Driver":
                    instToID[(int)InstallationType.MassDriver] = ID;
                    IDToInst[ID] = (int)InstallationType.MassDriver;
                    break;
                case "Research Facility":
                    instToID[(int)InstallationType.ResearchFacility] = ID;
                    IDToInst[ID] = (int)InstallationType.ResearchFacility;
                    break;
                case "Infrastructure":
                    instToID[(int)InstallationType.Infrastructure] = ID;
                    IDToInst[ID] = (int)InstallationType.Infrastructure;
                    break;
                case "Low Gravity Infrastructure":
                    instToID[(int)InstallationType.InfrastructureLG] = ID;
                    IDToInst[ID] = (int)InstallationType.InfrastructureLG;
                    break;
                case "Terraforming Installation":
                    instToID[(int)InstallationType.TerraforingInstallation] = ID;
                    IDToInst[ID] = (int)InstallationType.TerraforingInstallation;
                    break;
                case "Maintenance Facility":
                    instToID[(int)InstallationType.MaintenanceFacility] = ID;
                    IDToInst[ID] = (int)InstallationType.MaintenanceFacility;
                    break;
                case "Financial Centre":
                    instToID[(int)InstallationType.FinancialCenter] = ID;
                    IDToInst[ID] = (int)InstallationType.FinancialCenter;
                    break;
                case "Civilian Mining Complex":
                    instToID[(int)InstallationType.CivilianMine] = ID;
                    IDToInst[ID] = (int)InstallationType.CivilianMine;
                    break;
                }
            }

            cmd = new SqliteCommand("SELECT * FROM DIM_ComponentType;", con);
            SqliteDataReader compReader = cmd.ExecuteReader();

            while (compReader.Read()) {
                componentTypesToString.Add(compReader.GetInt32(getCol(compReader, "ComponentTypeID")), compReader.GetString(getCol(compReader, "TypeDescription")));
            }
        }

        void LoadMineralByNamedCols(double[] minerals, SqliteDataReader reader) {
            minerals[(int)MineralType.Duranium] = reader.GetDouble(getCol(reader, "Duranium"));
            minerals[(int)MineralType.Neutronium] = reader.GetDouble(getCol(reader, "Neutronium"));
            minerals[(int)MineralType.Corbomite] = reader.GetDouble(getCol(reader, "Corbomite"));
            minerals[(int)MineralType.Tritanium] = reader.GetDouble(getCol(reader, "Tritanium"));
            minerals[(int)MineralType.Boronide] = reader.GetDouble(getCol(reader, "Boronide"));
            minerals[(int)MineralType.Mercassium] = reader.GetDouble(getCol(reader, "Mercassium"));
            minerals[(int)MineralType.Vendarite] = reader.GetDouble(getCol(reader, "Vendarite"));
            minerals[(int)MineralType.Sorium] = reader.GetDouble(getCol(reader, "Sorium"));
            minerals[(int)MineralType.Uridium] = reader.GetDouble(getCol(reader, "Uridium"));
            minerals[(int)MineralType.Corundium] = reader.GetDouble(getCol(reader, "Corundium"));
            minerals[(int)MineralType.Gallicite] = reader.GetDouble(getCol(reader, "Gallicite"));
        }

        public struct PrgMsg {
            public string msg;
            public float progress;

            public PrgMsg(string m, float p) {
                msg = m;
                progress = p;
            }
        }

        public AuroraData ReadDB(string dbPath, IProgress<PrgMsg> progress) {
            AuroraData ret = new AuroraData();

            try {
                var conString = new SqliteConnectionStringBuilder {
                    DataSource = dbPath,
                    Mode = SqliteOpenMode.ReadOnly
                };

                con = new SqliteConnection(conString.ConnectionString);
                con.Open();
            } catch (SqliteException ex) {
                return null;
            }

            LoadDIMData(con);

            var cmd = new SqliteCommand("SELECT * FROM FCT_Game;", con);
            SqliteDataReader gameReader = cmd.ExecuteReader();

            // To be used for progress percentage
            int gameCnt = 0;
            while (gameReader.Read()) {
                gameCnt++;
            }

            cmd = new SqliteCommand("SELECT * FROM FCT_HullDescription", con);
            SqliteDataReader hullReader = cmd.ExecuteReader();

            while (hullReader.Read()) {
                AurHull hull = new AurHull();

                hull.ID = hullReader.GetInt32(getCol(hullReader, "HullDescriptionID"));
                hull.name = hullReader.GetString(getCol(hullReader, "Description"));
                hull.abbrev = hullReader.GetString(getCol(hullReader, "HullAbbr"));

                ret.hullIdx.Add(hull.ID, hull);
                ret.Hulls.Add(hull);
            }

            cmd = new SqliteCommand("SELECT * FROM FCT_Game;", con);
            gameReader = cmd.ExecuteReader();

            int curGame = 0;
            while (gameReader.Read()) {
                AurGame game = new AurGame();

                float prgBase = ((float)curGame / gameCnt);
                float prgMult = (1.0f / gameCnt);
                curGame++;

                game.ID = gameReader.GetInt32(getCol(gameReader, "GameID"));
                game.name = gameReader.GetString(getCol(gameReader, "GameName"));
                game.curTime = gameReader.GetDouble(getCol(gameReader, "GameTime"));

                progress.Report(new PrgMsg("Loading game " + game.name + "...", prgBase + 0.0f * prgMult));

                string GameCl = " GameID = " + game.ID;

                cmd = new SqliteCommand("SELECT * FROM FCT_Race WHERE" + GameCl + ";", con);
                SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    var race = new AurRace();

                    race.ID = reader.GetInt32(getCol(reader, "RaceID"));
                    race.name = reader.GetString(getCol(reader, "RaceName"));

                    game.raceIdx.Add(race.ID, race);
                    game.Races.Add(race);
                }

                cmd = new SqliteCommand("SELECT * FROM FCT_Species WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    var species = new AurSpecies();

                    species.ID = reader.GetInt32(getCol(reader, "SpeciesID"));
                    species.name = reader.GetString(getCol(reader, "SpeciesName"));

                    game.speciesIdx.Add(species.ID, species);
                    game.Species.Add(species);
                }

                progress.Report(new PrgMsg("Loaded races.", prgBase + 0.05f * prgMult));

                cmd = new SqliteCommand("SELECT * FROM FCT_System WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    var sys = new AurSystem();

                    sys.ID = reader.GetInt32(getCol(reader, "SystemID"));

                    cmd = new SqliteCommand("SELECT * FROM FCT_SystemBody WHERE" + GameCl + " AND SystemID = " + sys.ID + ";", con);
                    SqliteDataReader bodyReader = cmd.ExecuteReader();

                    while (bodyReader.Read()) {
                        var body = new AurBody();

                        body.ID = bodyReader.GetInt32(getCol(bodyReader, "SystemBodyID"));
                        body.Name = bodyReader.IsDBNull(getCol(bodyReader, "Name")) ? "" : bodyReader.GetString(getCol(bodyReader, "Name"));
                        body.system = sys;

                        // TODO - stars, star hierarchy
                        sys.Bodies.Add(body);

                        bool parentStar = bodyReader.GetInt32(getCol(bodyReader, "ParentBodyType")) == 0;
                        int parentID = bodyReader.GetInt32(getCol(bodyReader, "ParentBodyID"));
                        if(!parentStar) {
                            body.parent = game.bodyIdx[parentID];
                            body.parent.children.Add(body);
                        }

                        foreach (AurRace race in game.Races) {
                            body.isSurveyed.Add(race, false);
                        }

                        game.bodyIdx.Add(body.ID, body);
                    }

                    game.sysIdx.Add(sys.ID, sys);
                    game.Systems.Add(sys);
                }

                progress.Report(new PrgMsg("Loaded systems.", prgBase + 0.5f * prgMult));

                cmd = new SqliteCommand("SELECT * FROM FCT_MineralDeposit WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();
                while (reader.Read()) {
                    int bodIdx = reader.GetInt32(getCol(reader, "SystemBodyID"));

                    // Found out there might be invalid ID-s in the survey table
                    if (game.bodyIdx.ContainsKey(bodIdx)) {
                        int minID = reader.GetInt32(getCol(reader, "MaterialID")) - 1;
                        game.bodyIdx[bodIdx].minerals[minID] = reader.GetDouble(getCol(reader, "Amount"));
                        game.bodyIdx[bodIdx].mineralsAcc[minID] = reader.GetDouble(getCol(reader, "Accessibility"));

                        game.bodyIdx[bodIdx].hasMinerals = game.bodyIdx[bodIdx].minerals.Sum() > 0;
                    }
                }

                progress.Report(new PrgMsg("Loaded bodies.", prgBase + 0.55f * prgMult));

                cmd = new SqliteCommand("SELECT * FROM FCT_SystemBodySurveys WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();
                while (reader.Read()) {
                    int bodIdx = reader.GetInt32(getCol(reader, "SystemBodyID"));

                    if (game.bodyIdx.ContainsKey(bodIdx)) {
                        game.bodyIdx[bodIdx].isSurveyed[game.raceIdx[reader.GetInt32(getCol(reader, "RaceID"))]] = true;
                    }
                }

                cmd = new SqliteCommand("SELECT * FROM FCT_RaceSysSurvey WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    AurRSystem rSys = new AurRSystem();

                    rSys.ID = reader.GetInt32(getCol(reader, "SystemID"));
                    rSys.system = game.sysIdx[rSys.ID];
                    rSys.race = game.raceIdx[reader.GetInt32(getCol(reader, "RaceID"))];
                    rSys.Name = reader.GetString(getCol(reader, "Name"));

                    rSys.x = reader.GetDouble(getCol(reader, "Xcor"));
                    rSys.y = reader.GetDouble(getCol(reader, "Ycor"));

                    rSys.race.knownSystems.Add(rSys);
                    rSys.race.knownSysIdx.Add(rSys.system, rSys);
                }

                cmd = new SqliteCommand("SELECT * FROM FCT_SystemBodyName WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    int raceID = reader.GetInt32(getCol(reader, "RaceID"));

                    if(raceID == game.Races[0].ID) {
                        int bodIdx = reader.GetInt32(getCol(reader, "SystemBodyID"));

                        if (game.bodyIdx.ContainsKey(bodIdx)) {
                            game.bodyIdx[bodIdx].FirstRaceName = reader.GetString(getCol(reader, "Name"));
                        }
                    }
                }

                progress.Report(new PrgMsg("Loaded surveys.", prgBase + 0.6f * prgMult));

                cmd = new SqliteCommand("SELECT * FROM FCT_Population WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    var pop = new AurPop();

                    pop.ID = reader.GetInt32(getCol(reader, "PopulationID"));
                    pop.name = reader.GetString(getCol(reader, "PopName"));
                    pop.race = game.raceIdx[reader.GetInt32(getCol(reader, "RaceID"))];
                    pop.race.populations.Add(pop);
                    pop.species = game.speciesIdx[reader.GetInt32(getCol(reader, "SpeciesID"))];
                    pop.population = reader.GetDouble(getCol(reader, "Population"));
                    pop.race.populationSum += pop.population;
                    pop.body = game.bodyIdx[reader.GetInt32(getCol(reader, "SystemBodyID"))];
                    pop.body.populations.Add(pop.race, pop);

                    LoadMineralByNamedCols(pop.minerals, reader);

                    for (int i = 0; i < Enum.GetValues(typeof(MineralType)).Length; i++) {
                        pop.race.minerals[i] += pop.minerals[i];
                    }

                    pop.fuel = reader.GetDouble(getCol(reader, "FuelStockpile"));
                    pop.race.popFuelSum += pop.fuel;

                    pop.msp = reader.GetDouble(getCol(reader, "MaintenanceStockpile"));
                    pop.race.popMSPSum += pop.msp;

                    cmd = new SqliteCommand("SELECT * FROM FCT_PopulationInstallations WHERE" + GameCl + " AND PopID = " + pop.ID + ";", con);
                    var instReader = cmd.ExecuteReader();

                    while (instReader.Read()) {
                        pop.installations[IDToInst[instReader.GetInt32(getCol(instReader, "PlanetaryInstallationID"))]] = instReader.GetFloat(getCol(instReader, "Amount"));
                    }

                    for (int i = 0; i < pop.installations.Length; i++) {
                        pop.race.installations[i] += pop.installations[i];
                    }

                    //pop.fuelProd = reader.GetInt32(getCol(reader, "FuelProdStatus")) == 1 ? pop.installations[InstallationType.Refinery] : 0;

                    if (reader.GetInt32(getCol(reader, "Capital")) == 1) {
                        pop.race.capital = pop;
                    }

                    game.popIdx.Add(pop.ID, pop);
                }

                progress.Report(new PrgMsg("Loaded populations.", prgBase + 0.65f * prgMult));

                cmd = new SqliteCommand("SELECT * FROM FCT_ShipDesignComponents WHERE" + GameCl + " OR GameID = 0;", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    ComponentType type = AurComponent.GetComponentType(componentTypesToString[reader.GetInt32(getCol(reader, "ComponentTypeID"))]);

                    AurComponent comp = AurComponent.CreateComponentOfType(type);

                    comp.ID = reader.GetInt32(getCol(reader, "SDComponentID"));
                    comp.name = reader.GetString(getCol(reader, "Name"));
                    comp.size = reader.GetDouble(getCol(reader, "Size"));
                    comp.cost = reader.GetDouble(getCol(reader, "Cost"));
                    comp.crew = reader.GetInt32(getCol(reader, "Crew"));
                    comp.HTK = reader.GetInt32(getCol(reader, "HTK"));
                    LoadMineralByNamedCols(comp.mineralCost, reader);

                    switch (comp.componentType) {
                    case ComponentType.Weapon:
                        AurCompWeapon weap = (AurCompWeapon)comp;

                        weap.damage = reader.GetDouble(getCol(reader, "DamageOutput"));
                        weap.shots = reader.GetInt32(getCol(reader, "NumberOfShots"));
                        weap.rangeMod = reader.GetDouble(getCol(reader, "RangeModifier"));
                        weap.rangeMax = reader.GetDouble(getCol(reader, "MaxWeaponRange"));
                        weap.toHitMod = reader.GetDouble(getCol(reader, "WeaponToHitModifier"));
                        weap.trackingSpeed = reader.GetDouble(getCol(reader, "TrackingSpeed"));
                        weap.recharge = reader.GetDouble(getCol(reader, "RechargeRate"));
                        weap.powerRequired = reader.GetDouble(getCol(reader, "PowerRequirement"));

                        weap.isSpinal = reader.GetInt32(getCol(reader, "SpinalWeapon")) != 0;
                        weap.ignoreShields = reader.GetInt32(getCol(reader, "IgnoreShields")) != 0;
                        weap.ignoreArmor = reader.GetInt32(getCol(reader, "IgnoreArmour")) != 0;

                        if(weap.rangeMax == 0) {
                            weap.rangeMax = weap.damage * weap.rangeMod;
                        }
                        break;
                    case ComponentType.BFC:
                        AurCompBFC bfc = (AurCompBFC)comp;

                        bfc.rangeMax = reader.GetDouble(getCol(reader, "ComponentValue"));
                        bfc.trackingSpeed = reader.GetDouble(getCol(reader, "TrackingSpeed"));

                        break;
                    }

                    game.componentIdx.Add(comp.ID, comp);
                    game.Components[comp.componentType].Add(comp);
                }

                progress.Report(new PrgMsg("Loaded components.", prgBase + 0.7f * prgMult));

                cmd = new SqliteCommand("SELECT * FROM FCT_RaceTech WHERE" + GameCl + " OR GameID = 0;", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    int techID = reader.GetInt32(getCol(reader, "TechID"));
                    int raceID = reader.GetInt32(getCol(reader, "RaceID"));
                    bool obsolete = reader.GetInt32(getCol(reader, "Obsolete")) != 0;

                    if (game.componentIdx.ContainsKey(techID)) {
                        AurComponent comp = game.componentIdx[techID];
                        game.raceIdx[raceID].knownCompIdx[comp.componentType].Add(new AurRace.Comp(comp, obsolete));
                    }
                }

                progress.Report(new PrgMsg("Loaded tech.", prgBase + 0.8f * prgMult));

                cmd = new SqliteCommand("SELECT * FROM FCT_ShipClass WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    AurClass cls = new AurClass();

                    cls.ID = reader.GetInt32(getCol(reader, "ShipClassID"));
                    cls.name = reader.GetString(getCol(reader, "ClassName"));
                    cls.tonnage = reader.GetDouble(getCol(reader, "Size")) * 50;
                    cls.cost = reader.GetDouble(getCol(reader, "Cost"));
                    cls.armor = reader.GetInt32(getCol(reader, "ArmourThickness"));
                    cls.crew = reader.GetInt32(getCol(reader, "Crew"));
                    cls.msp = reader.GetDouble(getCol(reader, "MaintSupplies"));
                    cls.fuel = reader.GetDouble(getCol(reader, "FuelCapacity"));
                    cls.fuelEff = reader.GetDouble(getCol(reader, "FuelEfficiency"));
                    cls.maxSpeed = reader.GetDouble(getCol(reader, "MaxSpeed"));
                    cls.ppv = reader.GetDouble(getCol(reader, "ProtectionValue"));
                    cls.isMilitary = reader.GetInt32(getCol(reader, "Commercial")) != 0;
                    cls.isObsolete = reader.GetInt32(getCol(reader, "Obsolete")) != 0;
                    cls.hull = ret.hullIdx[reader.GetInt32(getCol(reader, "HullDescriptionID"))];
                    cls.miningModules = reader.GetInt32(getCol(reader, "MiningModules"));

                    game.classIdx.Add(cls.ID, cls);
                    game.raceIdx[reader.GetInt32(getCol(reader, "RaceID"))].classes.Add(cls);
                }

                cmd = new SqliteCommand("SELECT * FROM FCT_ClassMaterials WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();
                while (reader.Read()) {
                    int classIdx = reader.GetInt32(getCol(reader, "ClassID"));

                    int minID = reader.GetInt32(getCol(reader, "MaterialID")) - 1;
                    game.classIdx[classIdx].mineralCost[minID] = reader.GetDouble(getCol(reader, "Amount"));
                }

                cmd = new SqliteCommand("SELECT * FROM FCT_ClassComponent WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();
                while (reader.Read()) {
                    int classIdx = reader.GetInt32(getCol(reader, "ClassID"));
                    int compID = reader.GetInt32(getCol(reader, "ComponentID"));

                    AurComponent component = game.componentIdx[compID];

                    game.classIdx[classIdx].components[component.componentType].Add(new AurClass.Comp(component, reader.GetInt32(getCol(reader, "NumComponent"))));
                }

                progress.Report(new PrgMsg("Loaded classes.", prgBase + 0.85f * prgMult));

                cmd = new SqliteCommand("SELECT * FROM FCT_Fleet WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    AurFleet fleet = new AurFleet();

                    fleet.ID = reader.GetInt32(getCol(reader, "FleetID"));
                    fleet.name = reader.GetString(getCol(reader, "FleetName"));
                    fleet.race = game.raceIdx[reader.GetInt32(getCol(reader, "RaceID"))];

                    int orbitID = reader.GetInt32(getCol(reader, "OrbitBodyID"));
                    fleet.orbitBody = orbitID != 0 ? game.bodyIdx[orbitID] : null;
                    fleet.system = game.sysIdx[reader.GetInt32(getCol(reader, "SystemID"))];
                    fleet.x = reader.GetDouble(getCol(reader, "Xcor"));
                    fleet.y = reader.GetDouble(getCol(reader, "Ycor"));
                    fleet.speed = reader.GetDouble(getCol(reader, "Speed"));

                    int assignedPopID = reader.GetInt32(getCol(reader, "AssignedPopulationID"));
                    if(assignedPopID != 0) {
                        fleet.assignedPop = game.popIdx[assignedPopID];
                        fleet.assignedPop.oribitingFleets.Add(fleet);
                    }

                    fleet.race.fleets.Add(fleet);
                    game.fleetIdx.Add(fleet.ID, fleet);
                }

                progress.Report(new PrgMsg("Loaded fleets.", prgBase + 0.90f * prgMult));

                cmd = new SqliteCommand("SELECT * FROM FCT_Ship WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    AurShip ship = new AurShip();

                    ship.ID = reader.GetInt32(getCol(reader, "ShipID"));
                    ship.name = reader.GetString(getCol(reader, "ShipName"));
                    ship.race = game.raceIdx[reader.GetInt32(getCol(reader, "RaceID"))];
                    ship.shipClass = game.classIdx[reader.GetInt32(getCol(reader, "ShipClassID"))];
                    ship.fleet = game.fleetIdx[reader.GetInt32(getCol(reader, "FleetID"))];
                    ship.fleet.ships.Add(ship);
                    ship.crew = reader.GetInt32(getCol(reader, "CurrentCrew"));
                    ship.fuel = reader.GetDouble(getCol(reader, "Fuel"));
                    ship.grade = reader.GetDouble(getCol(reader, "GradePoints"));
                    ship.overhaulTime = reader.GetDouble(getCol(reader, "LastOverhaul"));
                    ship.msp = reader.GetDouble(getCol(reader, "CurrentMaintSupplies"));

                    ship.race.shipFuelSum += ship.fuel;
                    ship.race.shipFuelCapSum += ship.shipClass.fuel;

                    if (ship.shipClass.isMilitary) {
                        ship.race.shipMSPAnnualCost += ship.shipClass.cost / 4;
                    }

                    ship.race.shipMSPSum += ship.msp;
                    ship.race.shipMSPCapSum += ship.shipClass.msp;

                    ship.race.ships.Add(ship);
                    game.shipIdx.Add(ship.ID, ship);
                }

                ret.Games.Add(game);

                progress.Report(new PrgMsg("Loaded ships.", prgBase + 0.99f * prgMult));
            }

            con.Close();

            progress.Report(new PrgMsg("DB Loaded.", 1.00f));

            return ret;
        }
    }
}
