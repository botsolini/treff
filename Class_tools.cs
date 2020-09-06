using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Data;
using System.Timers;
using AltV.Net;
using AltV.Net.Elements.Entities;

using MySql.Data.MySqlClient;

namespace roleplay
{

    public class Class_tools
    {
        #region globals
        #endregion

        #region constructor
        #endregion


        #region tools

        public string CreateMD5Hash(string input)
        {
            try
            {
                MD5 md5 = MD5.Create();
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
            catch (Exception exc) { ErrorHandling(exc); }
            return "";
        }

        public string Get(Uri uri)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception exc) { ErrorHandling(exc); }
            return "";
        }

        public void ErrorHandling(Exception exc)
        {
            try
            {
                var stackTrace = new StackTrace(exc, true);
                var frame = stackTrace.GetFrame(0);

                Console.WriteLine("Exception in file: {0}", frame.GetFileName());
                Console.WriteLine("Exception in method: {0}", frame.GetMethod());
                Console.WriteLine("Exception in line: {0}", frame.GetFileLineNumber());
                Console.WriteLine("Exception message: {0}", exc.Message.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }

        public double RadianToDegree(float rad)
        {
            try
            {
                return rad * (180 / Math.PI);
            }
            catch (Exception exc) { ErrorHandling(exc); }
            return 0;
        }

        public double DegreeToRadian(double deg)
        {
            try
            {
                return deg * (Math.PI / 180);
            }
            catch (Exception exc) { ErrorHandling(exc); }
            return 0;
        }

        #endregion

    }

    public class Class_mysql
    {

        #region globals

        private Server server;
        private string cs;
        private MySqlConnection con;

        #endregion

        #region constructor

        public Class_mysql(Server server)
        {
            this.server = server;

            cs = "server=localhost;port=3306;user=roleplayuser;password=*Cd1ow32;database=roleplay";
            con = new MySqlConnection(cs);
        }

        #endregion


        #region mysql public

        public bool OpenCon()
        {
            try
            {
                con.Open();
                if (con.State == ConnectionState.Open) return true;
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
            return false;
        }

        public bool CloseCon()
        {
            try
            {
                if (con.State != ConnectionState.Open) return false;
                con.Close();
                return true;
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
            return false;
        }

        public bool CheckCon()
        {
            try
            {
                OpenCon();
                if (con.State == ConnectionState.Open)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("Mysql was needed but not opened");
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
            finally { CloseCon(); }
            return false;
        }

        #endregion

        #region mysql private

        public int ExecuteNQ(string sql, List<KeyValuePair<string, string>> args)
        {
            try
            {
                OpenCon();
                MySqlCommand cmd = new MySqlCommand(sql, con);
                for (int i = 0; i < args.Count; i++)
                {
                    cmd.Parameters.AddWithValue(args[i].Key, args[i].Value);
                }
                int affected = cmd.ExecuteNonQuery();

                cmd.Dispose();
                cmd = null;
                return affected;
            }
            catch (Exception ex) { server.tools.ErrorHandling(ex); }
            finally { CloseCon(); }
            return -1;
        }

        public object ExecuteS(string sql, List<KeyValuePair<string, string>> args)
        {
            try
            {
                OpenCon();
                MySqlCommand cmd = new MySqlCommand(sql, con);
                for (int i = 0; i < args.Count; i++)
                {
                    cmd.Parameters.AddWithValue(args[i].Key, args[i].Value);
                }
                object result = cmd.ExecuteScalar();

                cmd.Dispose();
                cmd = null;
                return result;
            }
            catch (Exception ex) { server.tools.ErrorHandling(ex); }
            finally { CloseCon(); }
            return null;
        }

        public DataTable GetDataTable(string sqlstr)
        {
            try
            {
                OpenCon();
                MySqlDataAdapter da = new MySqlDataAdapter(sqlstr, con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                da.Dispose();
                return dt;

            }
            catch (Exception ex) { server.tools.ErrorHandling(ex); }
            finally { CloseCon(); }
            return null;
        }

        #endregion

    }

    public class Class_voice
    {

        #region globals

        private Server server;

        private IVoiceChannel channel_low;
        private IVoiceChannel channel_med;
        private IVoiceChannel channel_high;

        #endregion

        #region constructor

        public Class_voice(Server server)
        {
            this.server = server;

            channel_low = Alt.CreateVoiceChannel(true, 1);
            channel_med = Alt.CreateVoiceChannel(true, 8);
            channel_high = Alt.CreateVoiceChannel(true, 25);
        }

        #endregion


        #region voice

        public void AddPlayerToVoiceChat(Class_player pl)
        {
            try
            {
                RemovePlayerFromVoiceChat(pl);
                channel_low.AddPlayer(pl.player);
                channel_med.AddPlayer(pl.player);
                channel_high.AddPlayer(pl.player);
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void RemovePlayerFromVoiceChat(Class_player pl)
        {
            try
            {
                channel_low.RemovePlayer(pl.player);
                channel_med.RemovePlayer(pl.player);
                channel_high.RemovePlayer(pl.player);
                pl.player.Emit("client:destroyvoice");
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void ChangeVoiceLevel(IPlayer player)
        {
            try
            {
                if (player.Health > 0)
                {
                    if (!channel_low.IsPlayerMuted(player))
                    {
                        SetVoiceLevel(player, 2);
                    }
                    else if (!channel_med.IsPlayerMuted(player))
                    {
                        SetVoiceLevel(player, 3);
                    }
                    else if (!channel_high.IsPlayerMuted(player))
                    {
                        SetVoiceLevel(player, 1);
                    }
                    else
                    {
                        SetVoiceLevel(player, 2);//default 2
                        server.playerhandler.SendAlert(player, "Du kannst wieder reden");
                    }
                }
                else
                {
                    SetVoiceLevel(player, 4);
                    server.playerhandler.SendAlert(player, "Du kannst zur Zeit nicht reden");
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void SetVoiceLevel(IPlayer player, int voicelevel)
        {
            try
            {
                switch (voicelevel)
                {
                    case 1://low
                        {
                            channel_low.UnmutePlayer(player);
                            channel_med.MutePlayer(player);
                            channel_high.MutePlayer(player);
                            break;
                        }
                    case 2://medium
                    default:
                        {
                            channel_low.MutePlayer(player);
                            channel_med.UnmutePlayer(player);
                            channel_high.MutePlayer(player);
                            break;
                        }
                    case 3://high
                        {
                            channel_low.MutePlayer(player);
                            channel_med.MutePlayer(player);
                            channel_high.UnmutePlayer(player);
                            break;
                        }
                    case 4://death
                        {
                            channel_low.MutePlayer(player);
                            channel_med.MutePlayer(player);
                            channel_high.MutePlayer(player);
                            break;
                        }
                }
                player.Emit("client:showvoice", voicelevel);
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        #endregion

    }
    
    public class Class_timer
    {

        #region globals

        Server server;
        private Timer minutetimer;

        #endregion

        #region constructor

        public Class_timer(Server server)
        {
            this.server = server;

            minutetimer = new Timer();
            minutetimer.Interval = 60000;
            minutetimer.AutoReset = true;
            minutetimer.Enabled = true;
            minutetimer.Elapsed += Minutetimer_Elapsed;
        }

        #endregion


        #region timer

        void Minutetimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                //todhandling
                server.playerhandler.Minutetimertick_dead();

                //time and weather serverside
                server.timeweatherhandler.Minutetimertick_timeweather();

                //time and weather clientside
                server.timeweatherhandler.LoadTimeWeatherPlayerSettingsForAll();
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        #endregion

    }

}