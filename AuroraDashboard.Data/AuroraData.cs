using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuroraDashboard
{

    public enum InstallationType
    {
        ConstructionFactory, FighterFactory, OrdnanceFactory, Mine, AutoMine, DSTS, Refinery, FinancialCenter, MaintenanceFacility,
        ResearchFacility, TerraforingInstallation, MassDriver, Infrastructure, InfrastructureLG, CivilianMine, Other
    }
    public enum MineralType { Duranium, Neutronium, Corbomite, Tritanium, Boronide, Mercassium, Vendarite, Sorium, Uridium, Corundium, Gallicite }

    public class AurHull
    {
        public int ID;
        public string abbrev;
        public string name;
    }

    public enum ComponentType { Engine, Armor, Weapon, ECM, ECCM, AcitveSensor, PassiveSensor, JumpDrive, BFC, Other }

    public class AurComponent
    {
        public int ID;
        public string name;
        public ComponentType componentType;
        public double cost;
        public double[] mineralCost = new double[Enum.GetValues(typeof(MineralType)).Length];
        public int crew;
        public int HTK;
        public double size;

        public static ComponentType GetComponentType(string typeName)
        {
            switch (typeName)
            {
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

        public static AurComponent CreateComponentOfType(ComponentType type)
        {
            switch (type)
            {
                case ComponentType.Weapon:
                    return new AurCompWeapon();
                case ComponentType.BFC:
                    return new AurCompBFC();
            }

            return new AurComponent();
        }

        public AurComponent()
        {
            componentType = ComponentType.Other;
        }
    }

    public class AurCompWeapon : AurComponent
    {
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

        public AurCompWeapon()
        {
            componentType = ComponentType.Weapon;
        }
    }

    public class AurCompBFC : AurComponent
    {
        public double trackingSpeed;
        public double rangeMax;

        public AurCompBFC()
        {
            componentType = ComponentType.BFC;
        }
    }

    public class AurClass
    {
        public class Comp
        {
            public AurComponent component;
            public int number;

            public Comp(AurComponent comp, int num)
            {
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
        public double cargoCapacity;
        public double colonistCapacity;
        public int armor;
        public int crew;
        public int miningModules;
        public int fuelHarvestingModules;
        public bool isMilitary;
        public bool isCivilianLine;
        public bool isObsolete;
        public bool isTanker;
        public bool isCollier;
        public bool isSupplyShip;
        public bool isFighter;
        public AurHull hull;
        public Dictionary<ComponentType, List<Comp>> components = new Dictionary<ComponentType, List<Comp>>();

        public AurClass()
        {
            foreach (ComponentType type in Enum.GetValues(typeof(ComponentType)))
            {
                components.Add(type, new List<Comp>());
            }
        }
    }

    public class AurShip
    {
        public int ID;
        public string name;
        public int crew;
        public double fuel;
        public double grade;
        public double msp;
        public double overhaulTime;
        public bool isCivilianLine;

        public AurRace race;
        public AurFleet fleet;
        public AurClass shipClass;
        public AurShip mothership;
    }

    public class AurFleet
    {
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

    public class AurPop
    {
        public int ID;
        public string name;

        public AurRace race;
        public AurBody body;
        public AurSpecies species;
        public double population;
        public double fuel;
        public bool fuelProdEnabled;
        public double msp;
        public bool mspProdEnabled;
        public double prodEfficiency;

        public double[] installations = new double[Enum.GetValues(typeof(InstallationType)).Length];
        public double[] minerals = new double[Enum.GetValues(typeof(MineralType)).Length];
        public List<AurFleet> oribitingFleets = new List<AurFleet>();
    }

    public class AurRSystem
    {
        public int ID;
        public string Name;
        public double x, y;

        public AurRace race;
        public AurSystem system;
    }

    public class AurJP
    {
        public int ID;
        public double x, y;
        public bool stable;

        public AurSystem system;
        public AurJP connection;
    }

    public class AurBody
    {
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

        public string GetName(AurRace viewRace)
        {
            if (FirstRaceName != null)
            {
                return FirstRaceName;
            }
            else if (Name != "")
            {
                return Name;
            }
            else if (populations.ContainsKey(viewRace))
            {
                return populations[viewRace].name;
            }
            else if (viewRace.knownSysIdx.ContainsKey(system))
            {
                return viewRace.knownSysIdx[system].Name;
            }
            else
            {
                return ID.ToString();
            }
        }
    }

    public class AurSystem
    {
        public int ID;
        public string FirstRaceName;

        public List<AurBody> Bodies = new List<AurBody>();
    }

    public class AurSpecies
    {
        public int ID;
        public string name;
    }

    public class AurRace
    {
        public class Comp
        {
            public AurComponent component;
            public bool isObsolete;

            public Comp(AurComponent comp, bool obs)
            {
                component = comp;
                isObsolete = obs;
            }
        }

        public int ID;
        public string name;
        public double crewmen;

        public double capMaintenance;
        public double prodFuel;
        public double prodMSP;
        public double prodConstruction;
        public double prodOrdnance;
        public double prodFighter;
        public double prodMine;


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

        public AurRace()
        {
            foreach (ComponentType type in Enum.GetValues(typeof(ComponentType)))
            {
                knownCompIdx.Add(type, new List<Comp>());
            }
        }
    }

    public class AurGame
    {
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

        public AurGame()
        {
            foreach (ComponentType type in Enum.GetValues(typeof(ComponentType)))
            {
                Components.Add(type, new List<AurComponent>());
            }
        }
    }

    public class AuroraData
    {
        public List<AurGame> Games = new List<AurGame>();

        public List<AurHull> Hulls = new List<AurHull>();
        public Dictionary<int, AurHull> hullIdx = new Dictionary<int, AurHull>();

        public void Save(string filePath)
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
                WriteIndented = true,
                IncludeFields = true
            };
            string jsonString = System.Text.Json.JsonSerializer.Serialize(this, options);
            System.IO.File.WriteAllText(filePath, jsonString);
        }

        public static AuroraData Load(string filePath)
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
                IncludeFields = true
            };
            string jsonString = System.IO.File.ReadAllText(filePath);
            return System.Text.Json.JsonSerializer.Deserialize<AuroraData>(jsonString, options);
        }
    }
}
