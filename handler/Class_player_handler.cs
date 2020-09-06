using AltV.Net.Data;
using AltV.Net.Elements.Entities;

using System;
using System.Collections.Generic;
using System.Data;
using System.Timers;

namespace roleplay
{
    public class Class_player_handler
    {

        #region globals

        private Server server;
        public List<Class_player> playerlist;

        #endregion

        #region constructor

        public Class_player_handler(Server server)
        {
            this.server = server;
            this.playerlist = new List<Class_player>();
        }

        #endregion


        #region onplayer

        public void Alt_OnPlayerConnect(IPlayer player, string reason)
        {
            try
            {
                if (!IsPlayerInDB(player))
                {
                    player.Emit("client:showregister");
                }
                else
                {
                    player.Emit("client:showlogin");
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void Alt_OnPlayerDisconnect(IPlayer player, string reason)
        {
            try
            {
                Class_player pl = FindPlayer(player.SocialClubId);
                if (pl != null)
                {
                    server.voice.RemovePlayerFromVoiceChat(pl);
                    UpdatePlayerToDB(pl);
                    DeletePlayer(player.SocialClubId);
                } 
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void Alt_OnPlayerDead(IPlayer player, IEntity killer, uint weapon)
        {
            try
            {
                server.voice.ChangeVoiceLevel(player);//dead, cant talk

                Class_player pl = FindPlayer(player.SocialClubId);
                if (pl != null)
                {
                    pl.deathtimer = 1;
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        #endregion

        #region player object

        public bool CreatePlayer(IPlayer player, DataTable dt)
        {
            try
            {
                //löschen falls schon vorhanden
                DeletePlayer(player.SocialClubId, true);

                Class_player pl = new Class_player(
                    player,
                    dt.Rows[0].Field<int>("id"),
                    dt.Rows[0].Field<int>("socialid"),
                    dt.Rows[0].Field<string>("password"),
                    dt.Rows[0].Field<string>("email")
                );

                pl.pos_x = dt.Rows[0].Field<double>("pos_x");
                pl.pos_y = dt.Rows[0].Field<double>("pos_y");
                pl.pos_z = dt.Rows[0].Field<double>("pos_z");
                pl.rot_z = dt.Rows[0].Field<double>("rot_z");
                pl.dimension = dt.Rows[0].Field<int>("dimension");
                pl.health = dt.Rows[0].Field<int>("health");
                pl.armor = dt.Rows[0].Field<int>("armor");
                pl.model = dt.Rows[0].Field<uint>("model");
                pl.gender = dt.Rows[0].Field<string>("gender");
                pl.deathtimer = dt.Rows[0].Field<int>("deathtimer");
                pl.adminlevel = dt.Rows[0].Field<int>("adminlevel");

                //player zur playerliste hinzufügen
                playerlist.Add(pl);

                //voice
                server.voice.AddPlayerToVoiceChat(pl);

                //checkpoints
                server.checkpointhandler.CreateMarkerForPlayer(player);
                return true;
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
            finally { dt.Dispose(); }
            Console.WriteLine("createPlayer unsuccessful for socialid: " + player.SocialClubId.ToString());
            return false;
        }

        public Class_player FindPlayer(ulong socialclubid, bool justcheck = false)
        {
            try
            {
                foreach (Class_player pl in playerlist)
                {
                    if ((ulong)pl.socialid != socialclubid) continue;
                    return pl;
                }
                if (!justcheck)
                {
                    Console.WriteLine("findplayer unsuccessful for socialid: " + socialclubid); 
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
            return null;
        }
        
        public void DeletePlayer(ulong socialclubid, bool justcheck = false)
        {
            try
            {
                Class_player pl = FindPlayer(socialclubid, justcheck);
                if (pl != null)
                {
                    playerlist.Remove(pl);
                }
                else
                {
                    if (!justcheck)
                    {
                        Console.WriteLine("deletePlayer unsuccessful for socialid: " + socialclubid);
                    }
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }
        
        #endregion

        #region mysql player

        public bool IsPlayerInDB(IPlayer player)
        {
            try
            {
                List<KeyValuePair<string, string>> kvp = new List<KeyValuePair<string, string>>();
                kvp.Add(new KeyValuePair<string, string>("@socialid", player.SocialClubId.ToString()));
                if (server.mysql.ExecuteS("SELECT id FROM user WHERE socialid = @socialid", kvp) != null) return true;
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
            return false;
        }

        public void CreatePlayerInDB(IPlayer player, string email, string pw, string gender)
        {
            try
            {
                List<KeyValuePair<string, string>> kvp = new List<KeyValuePair<string, string>>();
                kvp.Add(new KeyValuePair<string, string>("@socialid", player.SocialClubId.ToString()));
                kvp.Add(new KeyValuePair<string, string>("@email", email));
                kvp.Add(new KeyValuePair<string, string>("@password", server.tools.CreateMD5Hash(pw)));
                kvp.Add(new KeyValuePair<string, string>("@pos_x", "-496.43"));
                kvp.Add(new KeyValuePair<string, string>("@pos_y", "-335.79"));
                kvp.Add(new KeyValuePair<string, string>("@pos_z", "35.12"));
                kvp.Add(new KeyValuePair<string, string>("@rot_z", "100"));
                kvp.Add(new KeyValuePair<string, string>("@dimension", "0"));
                kvp.Add(new KeyValuePair<string, string>("@health", "200"));
                kvp.Add(new KeyValuePair<string, string>("@model", server.pm.GetRandomChar(gender).ToString()));
                kvp.Add(new KeyValuePair<string, string>("@voicelevel", "2"));
                kvp.Add(new KeyValuePair<string, string>("@gender", gender));

                if (!IsPlayerInDB(player) && server.mysql.ExecuteNQ("INSERT INTO user ( socialid, email, password, pos_x, pos_y, pos_z, rot_z, dimension, health, model, gender) VALUES ( @socialid, @email, @password, @pos_x, @pos_y, @pos_z, @rot_z, @dimension, @health, @model, @gender)", kvp) > 0)
                {
                    player.Emit("client:regok");
                    LoadPlayerFromDB(player, pw);
                }
                else
                {
                    player.Emit("client:regnotok");
                    Console.WriteLine("createPlayerInDB unsuccessful for socialid: " + player.SocialClubId.ToString());
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void LoadPlayerFromDB(IPlayer player, string pw)
        {
            try
            {
                DataTable dt = server.mysql.GetDataTable("SELECT * FROM user WHERE socialid = " + player.SocialClubId);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0 && dt.Rows[0].Field<string>("password") == server.tools.CreateMD5Hash(pw))
                    {
                        if (CreatePlayer(player, dt))
                        {
                            player.Emit("client:loginok");
                            return;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("loadPlayerFromDB unsuccessful for socialid: " + player.SocialClubId.ToString());
                }
                player.Emit("client:loginnotok");
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void UpdatePlayerToDB(Class_player pl)
        {
            try
            {
                if (pl != null)
                {
                    string sql = "UPDATE user SET pos_x=@pos_x, pos_y=@pos_y, pos_z=@pos_z, rot_z=@rot_z, dimension=@dimension, health=@health, armor=@armor, model=@model, gender=@gender, deathtimer=@deathtimer, adminlevel=@adminlevel WHERE socialid=@socialid";
                    List<KeyValuePair<string, string>> kvp = new List<KeyValuePair<string, string>>();
                    kvp.Add(new KeyValuePair<string, string>("@pos_x", pl.player.Position.X.ToString().Replace(",", ".")));
                    kvp.Add(new KeyValuePair<string, string>("@pos_y", pl.player.Position.Y.ToString().Replace(",", ".")));
                    kvp.Add(new KeyValuePair<string, string>("@pos_z", pl.player.Position.Z.ToString().Replace(",", ".")));
                    kvp.Add(new KeyValuePair<string, string>("@rot_z", server.tools.RadianToDegree(pl.player.Rotation.Yaw).ToString().Replace(",", ".")));
                    kvp.Add(new KeyValuePair<string, string>("@dimension", pl.dimension.ToString()));
                    kvp.Add(new KeyValuePair<string, string>("@health", pl.player.Health.ToString()));
                    kvp.Add(new KeyValuePair<string, string>("@armor", pl.player.Armor.ToString()));
                    kvp.Add(new KeyValuePair<string, string>("@model", pl.model.ToString()));
                    kvp.Add(new KeyValuePair<string, string>("@gender", pl.gender.ToString()));
                    kvp.Add(new KeyValuePair<string, string>("@deathtimer", pl.deathtimer.ToString()));
                    kvp.Add(new KeyValuePair<string, string>("@adminlevel", pl.adminlevel.ToString()));

                    kvp.Add(new KeyValuePair<string, string>("@socialid", pl.player.SocialClubId.ToString()));

                    if (server.mysql.ExecuteNQ(sql, kvp) < 1)
                    {
                        Console.WriteLine("updatePlayerToDB unsuccessful for socialid: " + pl.player.SocialClubId.ToString());
                    }
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        #endregion

        #region playerstuff

        public void SpawnPlayer(Class_player pl, string whathappened)
        {
            try
            {
                server.timeweatherhandler.LoadTimeWeatherPlayerSettings(pl.player);
                pl.player.Emit("client:hidemap");

                if (whathappened == "load")//login & reg
                {
                    pl.player.Emit("client:createmessage");
                    pl.player.Emit("client:createvoice");
                    pl.player.Emit("client:createskin");
                }

                if (whathappened == "died")
                {
                    pl.pos_x = -496.43;
                    pl.pos_y = -335.79;
                    pl.pos_z = 35.12;
                    pl.rot_z = 100;
                    pl.health = 200;
                }

                pl.player.Model = pl.model;
                pl.player.Position = new Position(float.Parse(pl.pos_x.ToString()), float.Parse(pl.pos_y.ToString()), float.Parse((pl.pos_z + 1).ToString()));
                //pl.player.Rotation = new Rotation(0, 0, float.Parse(server.tools.DegreeToRadian(pl.rot_z).ToString()));
                pl.player.Dimension = pl.dimension;
                pl.player.Health = (ushort)pl.health;
                pl.player.Armor = (ushort)pl.armor;

                pl.player.Emit("client:spawn");
                Timer spwntimer = new Timer();
                spwntimer.Interval = 5000;
                spwntimer.AutoReset = false;
                spwntimer.Enabled = true;
                spwntimer.Elapsed += delegate {
                    if(pl != null)
                    {
                        pl.dimension = 0;
                        pl.player.Dimension = pl.dimension;
                        pl.player.Emit("client:spawn2", pl.rot_z);
                        server.voice.ChangeVoiceLevel(pl.player);
                    }
                };
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }
        public void SpawnPlayer(IPlayer player, string whathappened)
        {
            try
            {
                Class_player pl = FindPlayer(player.SocialClubId);
                if (pl != null)
                {
                    SpawnPlayer(pl, whathappened);
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void SpawnLogin(IPlayer player)
        {
            try
            {
                SpawnPlayer(player, "load");
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void Minutetimertick_dead()
        {
            try
            {
                for (int i = 0; i < server.playerhandler.playerlist.Count; i++)
                {
                    Class_player pl = server.playerhandler.playerlist[i];
                    //death timer
                    if (pl.deathtimer != 0)
                    {
                        pl.deathtimer += 1;
                        if (pl.deathtimer == 3)
                        {
                            pl.deathtimer = 0;
                            server.playerhandler.SpawnPlayer(pl, "died");
                        }
                    }
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void SendAlert(IPlayer player, string message)
        {
            try
            {
                int timeperletter = 200;
                int time = timeperletter * message.Length;

                if (time <= 5000) time = 5000;
                player.Emit("client:showmessage", message, time);
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        #endregion



        #region trigger

        public void Trigger_e(IPlayer player)
        {
            try
            {
                Class_player pl = FindPlayer(player.SocialClubId);
                if(pl != null)
                {
                    switch(pl.currentcp)
                    {
                        case "Binco Vespucci":
                            {
                                ChangeSkin(pl, pl.currentcp);
                                break;
                            }
                        case "":
                            {
                                break;
                            }
                    }
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        #endregion

        #region skin

        public void ChangeSkin(IPlayer player, string ccp)
        {
            try
            {
                Class_player pl = FindPlayer(player.SocialClubId);
                if (pl != null)
                {
                    ChangeSkin(pl, ccp);
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void ChangeSkin(Class_player pl, string ccp)
        {
            try
            {
                if (pl.model == 1885233650 || pl.model == 2627665880)//changeable skin
                {
                }
                else//random skin
                {
                    if (pl.gender == "male")
                    {
                        pl.player.Model = 1885233650;
                    }
                    else
                    {
                        pl.player.Model = 2627665880;
                    }
                }
                pl.player.Dimension =pl.player.Id + 10000;

                Position pos = new Position();
                switch(ccp)
                {
                    case "Binco Vespucci":
                        {
                            pos = new Position((float)-819.85, (float)-1067.23, (float)11.35);
                            break;
                        }
                    default:
                        {
                            pos = new Position((float)0, (float)0, (float)80);
                            break;
                        }
                }
                pl.player.Position = pos;
                pl.player.Emit("client:showskin", ccp);
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void SkinFin(IPlayer player, string ccp, bool cancel)
        {
            try
            {
                Class_player pl = FindPlayer(player.SocialClubId);
                if (pl != null)
                {
                    if (cancel)
                    {
                        pl.player.Model = pl.model;
                    }
                    else
                    {
                        pl.model = pl.player.Model;
                    }

                    Position pos = new Position();
                    switch (ccp)
                    {
                        case "Binco Vespucci":
                            {
                                pos = new Position((float)-819.85, (float)-1067.23, (float)11.35);
                                break;
                            }
                        default:
                            {
                                pos = new Position((float)0, (float)0, (float)1000);
                                break;
                            }
                    }
                    pl.player.Position = pos;
                    pl.player.Dimension = 0;
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        #endregion

    }

    public class Class_player
    {

        #region globals

        public IPlayer player { get; }
        public int id { get; }
        public int socialid { get; }
        public string password { get; }
        public string email { get; }

        public double pos_x { get; set; }
        public double pos_y { get; set; }
        public double pos_z { get; set; }
        public double rot_z { get; set; }
        public int dimension { get; set; }
        public int health { get; set; }
        public int armor { get; set; }
        public uint model { get; set; }
        public string gender { get; set; }
        public int deathtimer { get; set; }
        public int adminlevel { get; set; }
        public string currentcp { get; set; }

        #endregion

        #region constructor

        public Class_player(IPlayer player, int id, int socialid, string password, string email)
        {
            this.player = player;
            this.id = id;
            this.socialid = socialid;
            this.password = password;
            this.email = email;

            pos_x = 0;
            pos_y = 0;
            pos_z = 0;
            rot_z = 0;
            dimension = 0;
            health = 200;
            armor = 0;
            model = 0;
            gender = "male";
            deathtimer = 0;
            adminlevel = 0;
            currentcp = "";
        }

        #endregion

    }

    public class Class_playermodel
    {

        #region globals

        private Server server;

        private uint[] mn = {
            1456705429,	//CocaineMale01
            2555758964,	//CounterfeitMale01
            1631482011	//ForgeryMale01
        };
        private uint[] womn = {
            1264941816,	//CocaineFemale01
            3079205365,	//CounterfeitFemale01
            2014985464	//ForgeryFemale01
        };

        #endregion

        #region constructor

        public Class_playermodel(Server server)
        {
            this.server = server;
        }

        #endregion


        #region models

        public uint GetRandomChar(string gender)
        {
            try
            {
                Random rand = new Random();
                if (gender == "male")
                {
                    int next = rand.Next(0, mn.Length);
                    return mn[next];
                }
                else
                {
                    int next = rand.Next(0, womn.Length);
                    return womn[next];
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
            return 0;
        }

        #endregion

        #region skin list
        /*
animals
111281960,	//Pigeon
113504370,	//TigerShark
351016938,	//Chop
307287994,	//MountainLion
402729631,	//Crow
802685111,	//Fish
1015224100,	//HammerShark
882848737,	//Retriever
1125994524,	//Poodle
1126154828,	//Shepherd
1193010354,	//Humpback
1318032802,	//Husky
1794449327,	//Hen
1457690978,	//Cormorant
1462895032,	//Cat
2344268885,	//Dolphin
2374682809,	//KillerWhale
2705875277,	//Stingray
2825402133,	//Chimp
2864127842,	//ChickenHawk
2971380566,	//Pig
3283429734,	//Rat
3462393972,	//Boar
3549666813,	//Seagull
3630914197,	//Deer
3753204865,	//Rabbit
1832265812,	//Pug
4244282910,	//Cow
2506301981,	//Rottweiler
1682622302,	//Coyote

special
3065114024,	//Fireman01SMY
3008586398,	//Paramedic01SMM
4017642090,	//UndercoverCopCutscene
451459928,	//Snowcop01SMM
1939545845,	//Hwaycop01SMY
2595446627,	//CopCutscene
1581098148,	//Cop01SMY
368603149,	//Cop01SFY
2974087609,	//Sheriff01SMY
1096929346,	//Sheriff01SFY
2374966032,	//Swat01SMY
653289389,	//FibOffice02SMM
874722259,	//FibArchitect
1558115333,	//FibSec01
2072724299,	//FibSec01SMM
3988550982,	//FibOffice01SMM
2243544680,	//FibMugger01
1482427218,	//FbiSuit01Cutscene
988062523,	//FbiSuit01
1348537411,	//FemaleAgent
1092080539,	//Scientist01SMM
71929310,	//Clown01SMY
2608926626,	//Trevor
225514697,	//Michael
2602752943,	//Franklin
2890614022,	//Zombie01
2934601397,	//Stripper01Cutscene
2168724337,	//Stripper02Cutscene
1846523796,	//Stripper02SFY
1544875514,	//StripperLiteSFY
695248020,	//StripperLite
1381498905,	//Stripper01SFY
1681385341,	//Priest
1299047806,	//PriestCutscene
3623056905,	//Drowned
1943971979,	//DeadHooker
2981862233,	//Prisoner01SMY
2073775040,	//Prisoner01
1596003233,	//PrisMuscl01SMY
1456041926,	//Prisguard01SMM
3660355662,	//SecuroGuardMale01
4131252449,	//Pilot02SMM
3881519900,	//Pilot01SMM
2872052743,	//Pilot01SMY
4000686095,	//GarbageSMY
3640249671,	//Busboy01SMY
1684083350,	//MovAlien01

A
1074457665,	//Abigail
2306246977,	//AbigailCutscene
4037813798,	//Abner
1413662315,	//Acult01AMM
1430544400,	//Acult01AMO
3043264555,	//Acult01AMY
1268862154,	//Acult02AMO
2162532142,	//Acult02AMY
3513928062,	//AfriAmer01AMM
756308504,	//AgathaCSB
1855569864,	//AgathaIG
610988552,	//Agent
3614493108,	//AgentCutscene
4227433577,	//Agent14
1841036427,	//Agent14Cutscene
1567728751,	//Airhostess01SFY
1644266841,	//AirworkerSMY
4042020578,	//AlDiNapoli
1830688247,	//AmandaTownley
2515474659,	//AmandaTownleyCutscene
2651349821,	//Ammucity01SMY
233415434,	//AmmuCountrySMM
1206185632,	//Andreas
3881194279,	//AndreasCutscene
117698822,	//AnitaCutscene
2781317046,	//AntonCutscene
3479321132,	//Antonb
4058522530,	//ArmBoss01GMM
4255728232,	//ArmGoon01GMM
3310258058,	//ArmGoon02GMY
3882958867,	//ArmLieut01GMM
3455013896,	//Armoured01
2512875213,	//Armoured01SMM
1669696074,	//Armoured02SMM
1657546978,	//Armymech01SMY
2129936603,	//Ashley
650367097,	//AshleyCutscene
2988916046,	//Autopsy01SMY
68070371,	//Autoshop01SMM
4033578141,	//Autoshop02SMM
1427949869,	//AveryCSB
3088269167,	//AveryIG
939183526,	//AviSchwartzman
2560490906,	//AviSchwartzmanCutscene
1752208920,	//Azteca01GMY

B
3658575486,	//Babyd
4096714883,	//BallaEast01GMY
588969535,	//BallaOrig01GMY
361513884,	//Ballas01GFY
2802535058,	//Ballasog
2884567044,	//BallasogCutscene
599294057,	//BallaSout01GMY
2426248831,	//Bankman
3272005365,	//Bankman01
2539657518,	//BankmanCutscene
3852538118,	//Barman01SMY
797459875,	//Barry
1767447799,	//BarryCutscene
2014052797,	//Bartender01SFY
1380197501,	//Baygor
1250841910,	//Baywatch01SFY
189425762,	//Baywatch01SMY
808859815,	//Beach01AFM
3349113128,	//Beach01AFY
1077785853,	//Beach01AMM
2217202584,	//Beach01AMO
3523131524,	//Beach01AMY
2021631368,	//Beach02AMM
600300561,	//Beach02AMY
3886638041,	//Beach03AMY
2114544056,	//Beachvesp01AMY
3394697810,	//Beachvesp02AMY
3300333010,	//Benny
1464257942,	//Bestmen
2503965067,	//BethUFY
3181518428,	//Beverly
3027157846,	//BeverlyCutscene
3188223741,	//Bevhills01AFM
1146800212,	//Bevhills01AFY
1423699487,	//Bevhills01AMM
1982350912,	//Bevhills01AMY
2688103263,	//Bevhills02AFM
1546450936,	//Bevhills02AFY
1068876755,	//Bevhills02AMM
1720428295,	//Bevhills02AMY
549978415,	//Bevhills03AFY
920595805,	//Bevhills04AFY
1984382277,	//BikeHire01
4198014287,	//BikerChic
3019107892,	//Blackops01SMY
2047212121,	//Blackops02SMY
1349953339,	//Blackops03SMY
2543361176,	//BlaneUMM
848542158,	//BoatStaff01F
3361671816,	//BoatStaff01M
1004114196,	//Bodybuild01AFM
2681481517,	//Bouncer01SMM
3183167778,	//Brad
1915268960,	//BradCadaverCutscene
4024807398,	//BradCutscene
933205398,	//Breakdance01AMY
1633872967,	//Bride
2193587873,	//BrideCutscene
3361779221,	//Brucie2CSB
3893268832,	//Brucie2IG
2340239206,	//BurgerDrug
2363277399,	//BurgerDrugCutscene
2597531625,	//Busicas01AMY
664399832,	//Business01AFY
2120901815,	//Business01AMM
3382649284,	//Business01AMY
532905404,	//Business02AFM
826475330,	//Business02AFY
3014915558,	//Business02AMY
2928082356,	//Business03AFY
2705543429,	//Business03AMY
3083210802,	//Business04AFY
2912874939,	//Busker01SMO

C
4150317356,	//CalebUMY
2230970679,	//Car3Guy1
71501447,	//Car3Guy1Cutscene
1975732938,	//Car3Guy2
327394568,	//Car3Guy2Cutscene
2362341647,	//CarBuyerCutscene
606876839,	//CarDesignFemale01
1415150394,	//CarolUFO
3774489940,	//Casey
3935738944,	//CaseyCutscene
3163733717,	//Casino01SFY
337826907,	//Casino01SMY
338154536,	//CasinoShop01UFM
3138220789,	//CasinoCash01UFO
1020431539,	//CasRN01GMM
3387290987,	//CCrew01SMM
1240128502,	//Chef
2739391114,	//ChefCutscene
261586155,	//Chef01SMY
2240322243,	//Chef2
2925257274,	//Chef2Cutscene
788443093,	//ChemSec01SMM
4128603535,	//ChemWork01GMM
3118269184,	//ChiBoss01GMM
275618457,	//ChiCold01GMM
2119136831,	//ChiGoon01GMM
4285659174,	//ChiGoon02GMM
2831296918,	//ChinGoonCutscene
610290475,	//Chip
1650288984,	//CiaSec01SMM
3237179831,	//Claude01
1825562762,	//Clay
3687553076,	//ClayCutscene
2634057640,	//Claypain
3865252245,	//Cletus
3404326357,	//CletusCutscene
3287737221,	//ClubhouseBar01
436345731,	//Cntrybar01SMM
1264941816,	//CocaineFemale01
1456705429,	//CocaineMale01
3064628686,	//ComJane
3621428889,	//Construct01SMY
3321821918,	//Construct02SMY
2624589981,	//Corpse01UFY
773063444,	//Corpse01UMY
228356856,	//Corpse02UFY
3079205365,	//CounterfeitFemale01
2555758964,	//CounterfeitMale01
678319271,	//CrisFormage
3253960934,	//CrisFormageCutscene
2145640135,	//CroupThief01UMY
4161104501,	//CurtisUMM
2756669323,	//CustomerCutscene
755956971,	//Cyclist01
4257633223,	//Cyclist01AMY

D
1182012905,	//Dale
216536661,	//DaleCutscene
365775923,	//DaveNorton
2240226444,	//DaveNortonCutscene
3835149295,	//Dealer01SMY
4188740747,	//DeanUMO
223828550,	//Debbie01UFM
3973074921,	//DebraCutscene
2181772221,	//Denise
1870669624,	//DeniseCutscene
3045926185,	//DeniseFriendCutscene
1952555184,	//Devin
788622594,	//DevinCutscene
2606068340,	//Devinsec01SMY
4282288299,	//Dhill01AMY
1646160893,	//DoaMan
349680864,	//Dockwork01SMM
2255894993,	//Dockwork01SMY
3564307372,	//Doctor01SMM
2620240008,	//Dom
1198698306,	//DomCutscene
579932932,	//Doorman01SMY
1699403886,	//Downtown01AFM
766375082,	//Downtown01AMY
3666413874,	//Dreyfuss
1012965715,	//DreyfussCutscene
3422293493,	//DrFriedlander
2745392175,	//DrFriedlanderCutscene
1976765073,	//DwService01SMY
4119890438,	//DwService02SMY

E
2638072698,	//Eastsa01AFM
4121954205,	//Eastsa01AFY
4188468543,	//Eastsa01AMM
2756120947,	//Eastsa01AMY
1674107025,	//Eastsa02AFM
70821038,	//Eastsa02AFY
131961260,	//Eastsa02AMM
377976310,	//Eastsa02AMY
1371553700,	//Eastsa03AFY
712602007,	//EdToh
2630685688,	//EileenUFO
1755064960,	//Epsilon01AFY
2010389054,	//Epsilon01AMY
2860711835,	//Epsilon02AMY
1161072059,	//ExArmy01
1126998116,	//ExecutivePAFemale01
1500695792,	//ExecutivePAFemale02
1048844220,	//ExecutivePAMale01

F
3499148112,	//Fabien
1191403201,	//FabienCutscene
1777626099,	//Factory01SFY
1097048408,	//Factory01SMY
3896218551,	//Famca01GMY
866411749,	//Famdd01
3681718840,	//Famdnf01GMY
2217749257,	//Famfor01GMY
1309468115,	//Families01GFY
2488675799,	//Farmer01AMM
4206136267,	//FatBla01AFM
3050275044,	//FatCult01AFM
1641152947,	//Fatlatin01AMM
951767867,	//FatWhite01AFM
373000027,	//FemBarberSFM
728636342,	//FilmDirector
732742363,	//FilmNoir
1189322339,	//Finguru01
1165780219,	//Fitness01AFY
331645324,	//Fitness02AFY
2981205682,	//Floyd
103106535,	//FloydCutscene
2014985464,	//ForgeryFemale01
1631482011,	//ForgeryMale01
466359675,	//FosRepCutscene
2627665880,	//FreemodeFemale01
1885233650,	//FreemodeMale01

G
2216405299,	//G
1278330017,	//GabrielUMY
2841034142,	//Gaffer01SMM
1240094341,	//Gardener01SMM
3519864886,	//Gay01AMY
2775713665,	//Gay02AMY
2727244247,	//GCutscene
2434503858,	//GenCasPat01AFY
2600762591,	//GenCasPat01AMY
115168927,	//Genfat01AMM
330231874,	//Genfat02AMM
793439294,	//Genhot01AFY
1640504453,	//Genstreet01AFO
2908022696,	//Genstreet01AMO
2557996913,	//Genstreet01AMY
891398354,	//Genstreet02AMY
411102470,	//GentransportSMM
1169888870,	//Glenstank01
2111372120,	//Golfer01AFY
2850754114,	//Golfer01AMM
3609190705,	//Golfer01AMY
3293887675,	//Griff01
815693290,	//Grip01SMY
2058033618,	//GroomCutscene
4274948997,	//Groom
3898166818,	//GroveStrDlrCutscene
261428209,	//GuadalopeCutscene
3333724719,	//Guido01
3005388626,	//GunVend01
3272931111,	//GurkCutscene

H
2579169528,	//Hacker
1099825042,	//Hairdress01SMM
1704428387,	//Hao
3969814300,	//HaoCutscene
1809430156,	//Hasjew01AMM
3782053633,	//Hasjew01AMY
1173958009,	//HeadTargets
431423238,	//HeliStaff01
4049719826,	//Highsec01SMM
691061163,	//Highsec02SMM
813893651,	//Hiker01AFY
1358380044,	//Hiker01AMY
1822107721,	//Hillbilly01AMM
2064532783,	//Hillbilly02AMM
4030826507,	//Hippie01
343259175,	//Hippie01AFY
2097407511,	//Hippy01AMY
2185745201,	//Hipster01AFY
587703123,	//Hipster01AMY
2549481101,	//Hipster02AFY
349505262,	//Hipster02AMY
2780469782,	//Hipster03AFY
1312913862,	//Hipster03AMY
429425116,	//Hipster04AFY
348382215,	//Hooker02SFY
2526768638,	//Hotposh01
1863555924,	//HughCutscene
3457361118,	//Hunter
1531218220,	//HunterCutscene

I
880829941,	//Imporage
2225189146,	//ImportExportFemale01
3164785898,	//ImportExportMale01
3812756443,	//ImranCutscene
3134700416,	//Indian01AFO
153984193,	//Indian01AFY
3721046572,	//Indian01AMM
706935758,	//Indian01AMY

J
1153203121,	//JackHowitzerCutscene
225287241,	//Janet
808778210,	//JanetCutscene
3254803008,	//JanitorCutscene
2842417644,	//JanitorSMM
2050158196,	//JayNorris
3459037009,	//Jesus01
767028979,	//Jetski01AMY
257763003,	//Jewelass
4040474158,	//Jewelass01
1145088004,	//JewelassCutscene
2899099062,	//JewelSec01
3872144604,	//JewelThief
3986688045,	//JimmyBoston
60192701,	//JimmyBostonCutscene
1459905209,	//JimmyDisanto
3100414644,	//JimmyDisantoCutscene
3189787803,	//JoeMinuteman
4036845097,	//JoeMinutemanCutscene
2278195374,	//JohnnyKlebitz
4203395201,	//JohnnyKlebitzCutscene
3776618420,	//Josef
1167549130,	//JosefCutscene
2040438510,	//Josh
1158606749,	//JoshCutscene
3675473203,	//Juggalo01AFY
2445950508,	//Juggalo01AMY
2109968527,	//Justin

K
3948009817,	//KarenDaniels
1269774364,	//KarenDanielsCutscene
1530648845,	//KerryMcintosh
891945583,	//KorBoss01GMM
611648169,	//Korean01GMY
2414729609,	//Korean02GMY
2093736314,	//KorLieut01GMY
1388848350,	//Ktown01AFM
1204772502,	//Ktown01AFO
3512565361,	//Ktown01AMM
355916122,	//Ktown01AMO
452351020,	//Ktown01AMY
1090617681,	//Ktown02AFM
696250687,	//Ktown02AMY

L
1706635382,	//LamarDavis
1162230285,	//LamarDavisCutscene
2659242702,	//Lathandy01SMM
321657486,	//Latino01AMY
967594628,	//LaurenUFY
3756278757,	//Lazlow
949295643,	//LazlowCutscene
1302784073,	//LesterCrest
3046438339,	//LesterCrestCutscene
1401530684,	//Lifeinvad01
1918178165,	//Lifeinvad01Cutscene
3724572669,	//Lifeinvad01SMM
666718676,	//Lifeinvad02
3684436375,	//LinecookSMM
4250220510,	//Lost01GFY
1330042375,	//Lost01GMY
1032073858,	//Lost02GMY
850468060,	//Lost03GMY
1985653476,	//Lsmetro01SMM

M
4242313482,	//Magenta
1477887514,	//MagentaCutscene
3767780806,	//Maid01SFM
4055673113,	//Malc
803106487,	//Malibu01AMM
3367706194,	//Mani
4248931856,	//Manuel
4222842058,	//ManuelCutscene
2124742566,	//Mariachi01SMM
4074414829,	//Marine01SMM
1702441027,	//Marine01SMY
4028996995,	//Marine02SMM
1490458366,	//Marine02SMY
1925237458,	//Marine03SMY
479578891,	//Markfost
411185872,	//Marnie
1464721716,	//MarnieCutscene
943915367,	//Marston01
1129928304,	//MartinMadrazoCutscene
2741999622,	//MaryAnn
161007533,	//MaryannCutscene
1005070462,	//Maude
3166991819,	//MaudeCutscene
1631478380,	//MerryWeatherCutscene
3534913217,	//MethFemale01
1768677545,	//Methhead01AMY
3988008767,	//MethMale01
1466037421,	//MexBoss01GMM
1226102803,	//MexBoss02GMM
3716251309,	//MexCntry01AMM
3185399110,	//MexGang01GMY
653210662,	//MexGoon01GMY
832784782,	//MexGoon02GMY
2521633500,	//MexGoon03GMY
2992445106,	//MexLabor01AMM
810804565,	//MexThug01AMY
3214308084,	//Michelle
1890499016,	//MichelleCutscene
3579522037,	//Migrant01SFY
3977045190,	//Migrant01SMM
1191548746,	//MilitaryBum
3408943538,	//Milton
3077190415,	//MiltonCutscene
1021093698,	//MimeSMY
1095737979,	//Miranda
1573528872,	//Mistress
3509125021,	//Misty01
1561088805,	//MLCrisis01AMM
2936266209,	//Molly
1167167044,	//MollyCutscene
1694362237,	//Motox01AMY
2007797722,	//Motox02AMY
894928436,	//MovieStar
1270514905,	//MoviePremFemaleCutscene
2372398717,	//MoviePremMaleCutscene
587253782,	//MovPrem01SFY
3630066984,	//Movprem01SMM
3887273010,	//Movspace01SMM
1822283721,	//MPros01
3990661997,	//MrK
3284966005,	//MrKCutscene
946007720,	//MrsPhillips
3422397391,	//MrsPhillipsCutscene
503621995,	//MrsThornhill
1334976110,	//MrsThornhillCutscene
1264920838,	//Musclbeac01AMY
3374523516,	//Musclbeac02AMY

N
3726105915,	//Natalia
1325314544,	//NataliaCutscene
3170921201,	//NervousRon
2023152276,	//NervousRonCutscene
3367442045,	//Nigel
3779566603,	//NigelCutscene
4007317449,	//Niko01

O
1746653202,	//OgBoss01AMM
1906124788,	//OldMan1a
518814684,	//OldMan1aCutscene
4011150407,	//OldMan2
2566514544,	//OldMan2Cutscene
1625728984,	//Omega
2339419141,	//OmegaCutscene
768005095,	//ONeil
1641334641,	//Orleans
2905870170,	//OrleansCutscene
648372919,	//Ortega
3235579087,	//OrtegaCutscene
4095687067,	//OscarCutscene

P
357551935,	//Paige
1528799427,	//PaigeCutscene
1346941736,	//Paparazzi
3972697109,	//Paparazzi01AMM
2577072326,	//Paper
1798879480,	//PaperCutscene
2180468199,	//PartyTarget
921110016,	//Party01
3312325004,	//Patricia
3750433537,	//PatriciaCutscene
1209091352,	//PestCont01SMY
994527967,	//PestContDriver
193469166,	//PestContGunman
3696858125,	//Pogo01
1329576454,	//PoloGoon01GMY
2733138262,	//PoloGoon02GMY
2849617566,	//Polynesian01AMM
2206530719,	//Polynesian01AMY
645279998,	//Popov
1635617250,	//PopovCutscene
602513566,	//Poppymich
793443893,	//PornDudesCutscene
1650036788,	//Postal01SMM
1936142927,	//Postal02SMM
3538133636,	//Princess
2237544099,	//PrologueDriver
4027271643,	//PrologueDriverCutscene
3306347811,	//PrologueHostage01
379310561,	//PrologueHostage01AFM
2534589327,	//PrologueHostage01AMM
2718472679,	//PrologueMournFemale01
3465937675,	//PrologueMournMale01
1888624839,	//PrologueSec01
2141384740,	//PrologueSec01Cutscene
666086773,	//PrologueSec02
512955554,	//PrologueSec02Cutscene

Q
R
3845001836,	//RampGang
3263172030,	//RampGangCutscene
1165307954,	//RampHic
2240582840,	//RampHicCutscene
3740245870,	//RampHipster
569740212,	//RampHipsterCutscene
1634506681,	//RampMarineCutscene
3870061732,	//RampMex
4132362192,	//RampMexCutscene
2680682039,	//Ranger01SFY
4017173934,	//Ranger01SMY
940330470,	//Rashkovsky
411081129,	//RashkovskyCutscene
776079908,	//ReporterCutscene
3268439891,	//Rhesus
1624626906,	//RivalPaparazzi
4116817094,	//Roadcyc01AMY
3227390873,	//Robber01SMY
3585757951,	//RoccoPelosi
2858686092,	//RoccoPelosiCutscene
1011059922,	//RsRanger01AMO
1064866854,	//Rurmeth01AFY
1001210244,	//Rurmeth01AMM
1024089777,	//RussianDrunk
1179785778,	//RussianDrunkCutscene
3343476521,	//Runner01AFY
623927022,	//Runner01AMY
2218630415,	//Runner02AMY

S
3725461865,	//Salton01AFM
3439295882,	//Salton01AFO
1328415626,	//Salton01AMM
539004493,	//Salton01AMO
3613420592,	//Salton01AMY
1626646295,	//Salton02AMM
2995538501,	//Salton03AMM
2521108919,	//Salton04AMM
2422005962,	//SalvaBoss01GMY
663522487,	//SalvaGoon01GMY
846439045,	//SalvaGoon02GMY
62440720,	//SalvaGoon03GMY
1794381917,	//SbikeAMO
3680420864,	//Scdressy01AFY
4293277303,	//ScreenWriter
2346790124,	//ScreenWriterCutscene
2874755766,	//Scrubs01SFY
3613962792,	//Security01SMM
2923947184,	//ShopHighSFM
416176080,	//ShopKeep01
2842568196,	//ShopLowSFY
1055701597,	//ShopMidSFY
1846684678,	//ShopMaskSMY
1283141381,	//SiemonYetarian
3230888450,	//SiemonYetarianCutscene
1767892582,	//Skater01AFY
3654768780,	//Skater01AMM
3250873975,	//Skater01AMY
2952446692,	//Skater02AMY
2962707003,	//Skidrow01AFM
1057201338,	//SlodHuman
2238511874,	//SlodLargeQuadped
762327283,	//SlodSmallQuadped
279228114,	//SmartCasPat01AFY
553826858,	//SmartCasPat01AMY
3446096293,	//SmugMech01
193817059,	//Socenlat01AMM
2260598310,	//Solomon
4140949582,	//SolomonCutscene
1951946145,	//Soucent01AFM
1039800368,	//Soucent01AFO
744758650,	//Soucent01AFY
1750583735,	//Soucent01AMM
718836251,	//Soucent01AMO
3877027275,	//Soucent01AMY
4079145784,	//Soucent02AFM
2775443222,	//Soucent02AFO
1519319503,	//Soucent02AFY
2674735073,	//Soucent02AMM
1082572151,	//Soucent02AMO
2896414922,	//Soucent02AMY
2276611093,	//Soucent03AFY
2346291386,	//Soucent03AMM
238213328,	//Soucent03AMO
3287349092,	//Soucent03AMY
3271294718,	//Soucent04AMM
2318861297,	//Soucent04AMY
3454621138,	//Soucentmc01AFM
2886641112,	//SpyActor
1535236204,	//SpyActress
2442448387,	//Staggrm01AMO
3482496489,	//Stbla01AMY
2563194959,	//Stbla02AMY
941695432,	//SteveHains
2766184958,	//SteveHainsCutscene
2255803900,	//Stlat01AMY
3265820418,	//Stlat02AMM
1813637474,	//StreetArt01
915948376,	//Stretch
2302502917,	//StretchCutscene
2035992488,	//Strperf01SMM
469792763,	//Strpreach01SMM
4246489531,	//StrPunk01GMY
228715206,	//StrPunk02GMY
3465614249,	//Strvend01SMM
2457805603,	//Strvend01SMY
605602864,	//Stwhi01AMY
919005580,	//Stwhi02AMY
3072929548,	//Sunbathe01AMY
3938633710,	//Surfer01AMY
824925120,	//Sweatshop01SFM
2231547570,	//Sweatshop01SFY

T
3885222120,	//Talina
226559113,	//Tanisha
1123963760,	//TanishaCutscene
3697041061,	//TaoCheng
2288257085,	//TaoChengCutscene
650034742,	//TaoCheng2CS
1506159504,	//TaoCheng2IG
2089096292,	//TaosTranslator
1397974313,	//TaosTranslatorCutscene
3017289007,	//TaosTranslator2CS
3828553631,	//TaosTranslator2IG
2585681490,	//Taphillbilly
2494442380,	//Tattoo01AMO
450271392,	//TaylorUFY
1416254276,	//Tennis01AMM
1426880966,	//Tennis01AFY
2721800023,	//TennisCoach
1545995274,	//TennisCoachCutscene
1728056212,	//Terry
978452933,	//TerryCutscene
4086880849,	//ThorntonCSB
2482949079,	//ThorntonIG
3488666811,	//TomCasinoCSB
1776856003,	//TomCutscene
3447159466,	//TomEpsilon
2349847778,	//TomEpsilonCutscene
3402126148,	//Tonya
1665391897,	//TonyaCutscene
2633130371,	//Topless01AFY
1347814329,	//Tourist01AFM
1446741360,	//Tourist01AFY
3365863812,	//Tourist01AMM
2435054400,	//Tourist02AFY
3728026165,	//TracyDisanto
101298480,	//TracyDisantoCutscene
1461287021,	//TrafficWarden
3727243251,	//TrafficWardenCutscene
1787764635,	//Tramp01
1224306523,	//Tramp01AFM
516505552,	//Tramp01AMM
390939205,	//Tramp01AMO
2359345766,	//TrampBeac01AFM
1404403376,	//TrampBeac01AMM
3773208948,	//Tranvest01AMM
4144940484,	//Tranvest02AMM
1498487404,	//Trucker01SMM
1382414087,	//TylerDixon

U
2680389410,	//Ups01SMM
3502104854,	//Ups02SMM
3389018345,	//Uscg01SMY
4218162071,	//UshiUMY

V
1520708641,	//Vagos01GFY
3299219389,	//VagosFun01
4194109068,	//VagosSpeak
1224690857,	//VagosSpeakCutscene
999748158,	//Valet01SMY
520636071,	//VincentCSB
736659122,	//VincentIG
2526968950,	//VinceUMM
3247667175,	//Vindouche01AMY
435429221,	//Vinewood01AFY
1264851357,	//Vinewood01AMY
3669401835,	//Vinewood02AFY
1561705728,	//Vinewood02AMY
933092024,	//Vinewood03AFY
534725268,	//Vinewood03AMY
4209271110,	//Vinewood04AFY
835315305,	//Vinewood04AMY

W
2459507570,	//Wade
3529955798,	//WadeCutscene
2907468364,	//Waiter01SMY
4154933561,	//WareMechMale01
921328393,	//WeaponExpertMale01
1099321454,	//WeaponWorkerMale01
2992993187,	//WeedFemale01
2441008217,	//WeedMale01
2867128955,	//WeiCheng
819699067,	//WeiChengCutscene
2719478597,	//WestSec01SMY
2910340283,	//Westy
2423691919,	//WillyFist
1426951581,	//WinClean01SMY

X
1142162924,	//Xmech01SMY
3189832196,	//Xmech02SMY
1762949645,	//Xmech02SMYMP

Y
3290105390,	//Yoga01AFY
2869588309,	//Yoga01AMY

Z
188012277,	//Zimbor
3937184496,	//ZimborCutscene
*/
        #endregion
    }

}
