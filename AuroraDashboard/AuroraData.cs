using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace AuroraDashboard {

    public enum InstallationType { CFactory, FFactory, OFactory, Mine, AutoMine, DSTS, Refinery, FinCenter, Maint, Research, Terraformer, MDriver, Infr, InfrLG}
    public enum MineralType { Duranium, Neutronium, Corbomite, Tritanium, Boronide, Mercassium, Vendarite, Sorium, Uridium, Corundium, Gallicite }

    public class AurPop {
        public int ID;
        public string name;

        public AurRace race;
        public AurBody body;

        public double[] installations = new double[Enum.GetValues(typeof(InstallationType)).Length];
        public double[] minerals = new double[Enum.GetValues(typeof(MineralType)).Length];
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

        public List<AurPop> populations = new List<AurPop>();
        public Dictionary<AurRace, bool> isSurveyed = new Dictionary<AurRace, bool>();

        public double[] minerals = new double[Enum.GetValues(typeof(MineralType)).Length];
        public double[] mineralsAcc = new double[Enum.GetValues(typeof(MineralType)).Length];
        public int surveyPotential;
        public int ruins;
    }

    public class AurSystem {
        public int ID;

        public List<AurBody> Bodies = new List<AurBody>();
    }

    public class AurRace {
        public int ID;
        public string name;

        public double[] minerals = new double[Enum.GetValues(typeof(MineralType)).Length];

        public AurPop capital;
        public List<AurRSystem> knownSystems = new List<AurRSystem>();
        public List<AurPop> populations = new List<AurPop>();
    }

    public class AurGame {
        public int ID;
        public string name;

        public List<AurRace> Races = new List<AurRace>();
        public List<AurSystem> Systems = new List<AurSystem>();
    }

    public class AuroraData {
        public List<AurGame> Games = new List<AurGame>();
    }

    public class AuroraDBReader {
        SqliteConnection con;

        public int[] instToID = new int[Enum.GetValues(typeof(InstallationType)).Length];
        public int[] IDToInst = new int[200];

        public Dictionary<int, AurRace> raceIdx = new Dictionary<int, AurRace>();
        public Dictionary<int, AurSystem> sysIdx = new Dictionary<int, AurSystem>();
        public Dictionary<int, AurBody> bodyIdx = new Dictionary<int, AurBody>();
        public Dictionary<int, AurPop> popIdx = new Dictionary<int, AurPop>();

        int getCol(SqliteDataReader reader, string name) {
            for (int i = 0; i < reader.FieldCount; i++) {
                if (reader.GetName(i) == name) {
                    return i;
                }
            }

            return -1;
        }

        void LoadDIMData(SqliteConnection con) {
            var cmd = new SqliteCommand("SELECT * FROM DIM_PlanetaryInstallation;", con);
            SqliteDataReader instReader = cmd.ExecuteReader();

            while (instReader.Read()) {
                int ID = instReader.GetInt32(getCol(instReader, "PlanetaryInstallationID"));

                switch (instReader.GetString(getCol(instReader, "Name"))) {
                case "Construction Factory":
                    instToID[(int)InstallationType.CFactory] = ID;
                    IDToInst[ID] = (int)InstallationType.CFactory;
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
                    instToID[(int)InstallationType.OFactory] = ID;
                    IDToInst[ID] = (int)InstallationType.OFactory;
                    break;
                case "Fighter Factory":
                    instToID[(int)InstallationType.FFactory] = ID;
                    IDToInst[ID] = (int)InstallationType.FFactory;
                    break;
                case "Deep Space Tracking Station":
                    instToID[(int)InstallationType.DSTS] = ID;
                    IDToInst[ID] = (int)InstallationType.DSTS;
                    break;
                case "Mass Driver":
                    instToID[(int)InstallationType.MDriver] = ID;
                    IDToInst[ID] = (int)InstallationType.MDriver;
                    break;
                case "Research Facility":
                    instToID[(int)InstallationType.Research] = ID;
                    IDToInst[ID] = (int)InstallationType.Research;
                    break;
                case "Infrastructure":
                    instToID[(int)InstallationType.Infr] = ID;
                    IDToInst[ID] = (int)InstallationType.Infr;
                    break;
                case "Low Gravity Infrastructure":
                    instToID[(int)InstallationType.InfrLG] = ID;
                    IDToInst[ID] = (int)InstallationType.InfrLG;
                    break;
                case "Terraforming Installation":
                    instToID[(int)InstallationType.Terraformer] = ID;
                    IDToInst[ID] = (int)InstallationType.Terraformer;
                    break;
                case "Maintenance Facility":
                    instToID[(int)InstallationType.Maint] = ID;
                    IDToInst[ID] = (int)InstallationType.Maint;
                    break;
                case "Financial Centre":
                    instToID[(int)InstallationType.FinCenter] = ID;
                    IDToInst[ID] = (int)InstallationType.FinCenter;
                    break;
                }
            }
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

            con = new SqliteConnection(dbPath);
            con.Open();

            LoadDIMData(con);

            var cmd = new SqliteCommand("SELECT * FROM FCT_Game;", con);
            SqliteDataReader gameReader = cmd.ExecuteReader();

            // To be used for progress percentage
            int gameCnt = 0;
            while (gameReader.Read()) {
                gameCnt++;
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

                progress.Report(new PrgMsg("Loading game " + game.name + "...", prgBase + 0.0f * prgMult));

                string GameCl = " GameID = " + game.ID;

                cmd = new SqliteCommand("SELECT * FROM FCT_Race WHERE" + GameCl + ";", con);
                SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    var race = new AurRace();

                    race.ID = reader.GetInt32(getCol(reader, "RaceID"));
                    race.name = reader.GetString(getCol(reader, "RaceName"));

                    raceIdx.Add(race.ID, race);
                    game.Races.Add(race);
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
                        body.Name = bodyReader.IsDBNull(getCol(bodyReader, "Name")) ? "n/a" : bodyReader.GetString(getCol(bodyReader, "Name"));
                        body.system = sys;

                        // TODO - stars, star hierarchy
                        sys.Bodies.Add(body);

                        bool parentStar = bodyReader.GetInt32(getCol(bodyReader, "ParentBodyType")) == 0;
                        int parentID = bodyReader.GetInt32(getCol(bodyReader, "ParentBodyID"));
                        if(!parentStar) {
                            body.parent = bodyIdx[parentID];
                            body.parent.children.Add(body);
                        }

                        foreach (AurRace race in game.Races) {
                            body.isSurveyed.Add(race, false);
                        }

                        bodyIdx.Add(body.ID, body);
                    }

                    sysIdx.Add(sys.ID, sys);
                    game.Systems.Add(sys);
                }

                progress.Report(new PrgMsg("Loaded systems.", prgBase + 0.5f * prgMult));

                cmd = new SqliteCommand("SELECT * FROM FCT_MineralDeposit WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();
                while (reader.Read()) {
                    int bodIdx = reader.GetInt32(getCol(reader, "SystemBodyID"));

                    // Found out there might be invalid ID-s in the survey table
                    if (bodyIdx.ContainsKey(bodIdx)) {
                        int minID = reader.GetInt32(getCol(reader, "MaterialID")) - 1;
                        bodyIdx[bodIdx].minerals[minID] = reader.GetDouble(getCol(reader, "Amount"));
                        bodyIdx[bodIdx].mineralsAcc[minID] = reader.GetDouble(getCol(reader, "Accessibility"));
                    }
                }

                progress.Report(new PrgMsg("Loaded bodies.", prgBase + 0.6f * prgMult));

                cmd = new SqliteCommand("SELECT * FROM FCT_SystemBodySurveys WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();
                while (reader.Read()) {
                    int bodIdx = reader.GetInt32(getCol(reader, "SystemBodyID"));

                    if (bodyIdx.ContainsKey(bodIdx)) {
                        bodyIdx[bodIdx].isSurveyed[raceIdx[reader.GetInt32(getCol(reader, "RaceID"))]] = true;
                    }
                }

                cmd = new SqliteCommand("SELECT * FROM FCT_RaceSysSurvey WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    AurRSystem rSys = new AurRSystem();

                    rSys.ID = reader.GetInt32(getCol(reader, "SystemID"));
                    rSys.system = sysIdx[rSys.ID];
                    rSys.race = raceIdx[reader.GetInt32(getCol(reader, "RaceID"))];
                    rSys.Name = reader.GetString(getCol(reader, "Name"));

                    rSys.x = reader.GetDouble(getCol(reader, "Xcor"));
                    rSys.y = reader.GetDouble(getCol(reader, "Ycor"));

                    rSys.race.knownSystems.Add(rSys);
                }

                cmd = new SqliteCommand("SELECT * FROM FCT_SystemBodyName WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    int raceID = reader.GetInt32(getCol(reader, "RaceID"));

                    if(raceID == game.Races[0].ID) {
                        int bodIdx = reader.GetInt32(getCol(reader, "SystemBodyID"));

                        if (bodyIdx.ContainsKey(bodIdx)) {
                            bodyIdx[bodIdx].FirstRaceName = reader.GetString(getCol(reader, "Name"));
                        }
                    }
                }

                progress.Report(new PrgMsg("Loaded surveys.", prgBase + 0.7f * prgMult));

                cmd = new SqliteCommand("SELECT * FROM FCT_Population WHERE" + GameCl + ";", con);
                reader = cmd.ExecuteReader();

                while (reader.Read()) {
                    var pop = new AurPop();

                    pop.ID = reader.GetInt32(getCol(reader, "PopulationID"));
                    pop.name = reader.GetString(getCol(reader, "PopName"));
                    pop.race = raceIdx[reader.GetInt32(getCol(reader, "RaceID"))];
                    pop.race.populations.Add(pop);
                    pop.body = bodyIdx[reader.GetInt32(getCol(reader, "SystemBodyID"))];
                    pop.body.populations.Add(pop);

                    pop.minerals[(int)MineralType.Duranium] = reader.GetDouble(getCol(reader, "Duranium"));
                    pop.minerals[(int)MineralType.Neutronium] = reader.GetDouble(getCol(reader, "Neutronium"));
                    pop.minerals[(int)MineralType.Corbomite] = reader.GetDouble(getCol(reader, "Corbomite"));
                    pop.minerals[(int)MineralType.Tritanium] = reader.GetDouble(getCol(reader, "Tritanium"));
                    pop.minerals[(int)MineralType.Boronide] = reader.GetDouble(getCol(reader, "Boronide"));
                    pop.minerals[(int)MineralType.Mercassium] = reader.GetDouble(getCol(reader, "Mercassium"));
                    pop.minerals[(int)MineralType.Vendarite] = reader.GetDouble(getCol(reader, "Vendarite"));
                    pop.minerals[(int)MineralType.Sorium] = reader.GetDouble(getCol(reader, "Sorium"));
                    pop.minerals[(int)MineralType.Uridium] = reader.GetDouble(getCol(reader, "Uridium"));
                    pop.minerals[(int)MineralType.Corundium] = reader.GetDouble(getCol(reader, "Corundium"));
                    pop.minerals[(int)MineralType.Gallicite] = reader.GetDouble(getCol(reader, "Gallicite"));

                    for (int i = 0; i <= (int)MineralType.Gallicite; i++) {
                        pop.race.minerals[i] += pop.minerals[i];
                    }

                    cmd = new SqliteCommand("SELECT * FROM FCT_PopulationInstallations WHERE" + GameCl + " AND PopID = " + pop.ID + ";", con);
                    var instReader = cmd.ExecuteReader();

                    while (instReader.Read()) {
                        pop.installations[IDToInst[instReader.GetInt32(getCol(instReader, "PlanetaryInstallationID"))]] = instReader.GetFloat(getCol(instReader, "Amount"));
                    }

                    if (reader.GetInt32(getCol(reader, "Capital")) == 1) {
                        pop.race.capital = pop;
                    }

                    popIdx.Add(pop.ID, pop);
                }

                progress.Report(new PrgMsg("Loaded populations.", prgBase + 0.99f * prgMult));

                ret.Games.Add(game);
            }

            con.Close();

            progress.Report(new PrgMsg("DB Loaded.", 1.00f));

            return ret;
        }
    }
}
