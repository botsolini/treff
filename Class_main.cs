using System;

using AltV.Net.Elements.Entities;
using AltV.Net;
using AltV.Net.Data;

namespace roleplay
{
    public class Server : Resource
    {

        #region globals

        public Class_tools tools;
        public Class_playermodel pm;
        public Class_mysql mysql;
        public Class_voice voice;
        public Class_player_handler playerhandler;
        public Class_timeweather_handler timeweatherhandler;
        public Class_timer tmr;
        public Class_faction_handler factionhandler;
        public Class_vehicle_handler vehiclehandler;
        public Class_checkpoint_handler checkpointhandler;

        #endregion

        #region overrides

        public override void OnStart()
        {
            try
            {
                //log
                Console.WriteLine("Roleplay loading...");

                //tools
                tools = new Class_tools();

                //playermodel
                pm = new Class_playermodel(this);

                //sql
                mysql = new Class_mysql(this);
                if (mysql.CheckCon()) { Console.WriteLine("Mysql connection was successful"); }
                else { Console.WriteLine("Mysql connection was not successful"); }

                //voice 
                voice = new Class_voice(this);

                //player
                playerhandler = new Class_player_handler(this);

                //time and weather
                timeweatherhandler = new Class_timeweather_handler(this);

                //faction
                factionhandler = new Class_faction_handler(this);

                //vehicles
                vehiclehandler = new Class_vehicle_handler(this);

                //marker
                checkpointhandler = new Class_checkpoint_handler(this);

                //server timer - soll als letztes kommen 
                tmr = new Class_timer(this);

                //server trigger
                Alt.OnPlayerConnect += playerhandler.Alt_OnPlayerConnect;
                Alt.OnPlayerDisconnect += playerhandler.Alt_OnPlayerDisconnect;
                Alt.OnPlayerDead += playerhandler.Alt_OnPlayerDead;
                Alt.OnPlayerEnterVehicle += vehiclehandler.Alt_OnPlayerEnterVehicle;
                Alt.OnPlayerLeaveVehicle += vehiclehandler.Alt_OnPlayerLeaveVehicle;
                Alt.OnColShape += checkpointhandler.Alt_OnColShape;
                
                //client trigger
                Alt.OnClient<IPlayer, string, string, string>("server:register", playerhandler.CreatePlayerInDB);
                Alt.OnClient<IPlayer, string>("server:login", playerhandler.LoadPlayerFromDB);
                Alt.OnClient<IPlayer>("server:changevoicelevel", voice.ChangeVoiceLevel);
                Alt.OnClient<IPlayer>("server:spawnlogin", playerhandler.SpawnLogin);
                Alt.OnClient<IPlayer>("server:trigger_e", playerhandler.Trigger_e);
                Alt.OnClient<IPlayer, string, bool>("server:skinfin", playerhandler.SkinFin);

                Alt.OnClient<IPlayer, int>("server:test", test);

                //log
                Console.WriteLine("Roleplay loaded");
            }
            catch (Exception exc) { tools.ErrorHandling(exc); }
        }



        public override void OnStop()
        {
            try
            {
                //log
                Console.WriteLine("Roleplay stopping...");

                //spieler speichern und kicken
                foreach (Class_player pl in playerhandler.playerlist)
                {
                    playerhandler.UpdatePlayerToDB(pl);
                    pl.player.Kick("Server fährt herunter");
                }

                //log
                Console.WriteLine("Roleplay stopped");
            }
            catch (Exception exc) { tools.ErrorHandling(exc); }
        }

        public override void OnTick()
        {
            try
            {
                /*
                if (playerhandler.playerlist.Count < 1) return;
                Class_player pl = playerhandler.playerlist[0];
                if(pl != null)
                {
                    Alt.Log("ss:" + tools.RadianToDegree(pl.player.Rotation.Yaw).ToString().Replace(",", ".")); 
                }
                */
            }
            catch (Exception exc) { tools.ErrorHandling(exc); }
        }

        #endregion



        #region test

        public void test(IPlayer player, int id)
        {
            try
            {
                switch (id)
                {
                    case 0:
                        {
                            Alt.CreateVehicle("maverick", player.Position, player.Rotation);
                            break;
                        }
                    case 1:
                        {
                            Alt.CreateVehicle("banshee", player.Position, player.Rotation);
                            break;
                        }
                    case 2:
                        {
                            Class_player pl = playerhandler.FindPlayer(player.SocialClubId);
                            if(pl!=null)
                            {
                                pl.adminlevel = 1;
                                playerhandler.SendAlert(player, "Du bist nun adminlevel: " + pl.adminlevel.ToString());
                            }
                            
                            break;
                        }
                    case 3:
                        {
                            player.Position = new Position((float)976.6364, (float)70.29476, (float)115.1641);
                            break;
                        }
                    case 4:
                        {
                            playerhandler.SendAlert(player, player.Position.X.ToString() + " " + player.Position.Y.ToString() + " " + player.Position.Z.ToString());
                            break;
                        }
                    case 5:
                        {
                            break;
                        }
                    case 6:
                        {
                            break;
                        }
                    case 7:
                        {
                            break;
                        }
                    case 8:
                        {
                            
                            break;
                        }
                    case 9:
                        {
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            catch (Exception exc) { tools.ErrorHandling(exc); }
        }

        #endregion
    }

}
