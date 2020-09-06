using AltV.Net.Elements.Entities;
using System;

namespace roleplay
{
    public class Class_timeweather_handler
    {
        #region globals

        private Server server;
        private DateTime dati;
        private uint weatherid;
        private int weatherreqtime;

        #endregion

        #region constructor

        public Class_timeweather_handler(Server server)
        {
            this.server = server;
            this.dati = DateTime.Now;
            this.weatherid = 1;
            this.weatherreqtime = -1;
        }

        #endregion

        #region weather

        public uint GetWeather(bool real)
        {
            try
            {
                Random rand = new Random();
                uint widfinal = 0;

                if (real)
                {
                    string json = server.tools.Get(new Uri("http://api.openweathermap.org/data/2.5/weather?q=Berlin&appid=c270e2d18f51ef60a9cb5ba66e7f80f2"));
                    if (json.Contains("id"))
                    {
                        Console.WriteLine("Weather Update");

                        int start = json.IndexOf("id") + 4;
                        string wids = json.Substring(start, 3);
                        int wid = 0;
                        int.TryParse(wids, out wid);

                        switch (wid)
                        {
                            case 800://Clear        clear sky
                                {
                                    int next = rand.Next(0, 2);
                                    switch (next)
                                    {
                                        case 0://Extra Sunny 0
                                            {
                                                widfinal = 0;
                                                break;
                                            }
                                        case 1://Clear 1   
                                            {
                                                widfinal = 1;
                                                break;
                                            }
                                    }
                                    break;
                                }

                            case 801://Clouds           few clouds: 11-25%
                            case 802://Clouds           scattered clouds: 25-50%
                            case 803://Clouds           broken clouds: 51-84%
                            case 804://Clouds           overcast clouds: 85-100%
                                {
                                    int next = rand.Next(0, 2);
                                    switch (next)
                                    {
                                        case 0://Clouds 2  
                                            {
                                                widfinal = 2;
                                                break;
                                            }
                                        case 1://Overcast 5 
                                            {
                                                widfinal = 5;
                                                break;
                                            }
                                    }
                                    break;
                                }

                            case 701://Mist             mist  
                            case 711://Smoke            Smoke     
                            case 721://Haze             Haze     
                            case 731://Dust             sand/ dust whirls
                            case 741://Fog              fog
                            case 751://Sand             sand
                            case 761://Dust             dust
                            case 762://Ash              volcanic ash
                                {
                                    int next = rand.Next(0, 2);
                                    switch (next)
                                    {
                                        case 0://Smog 3
                                            {
                                                widfinal = 3;
                                                break;
                                            }
                                        case 1://Foggy 4   
                                            {
                                                widfinal = 4;
                                                break;
                                            }
                                    }
                                    break;
                                }

                            case 500://Rain             light rain
                            case 501://Rain             moderate rain
                            case 502://Rain             heavy intensity rain
                            case 503://Rain             very heavy rain
                            case 504://Rain             extreme rain
                            case 511://Rain             freezing rain
                            case 520://Rain             light intensity shower rain
                            case 521://Rain             shower rain
                            case 522://Rain             heavy intensity shower rain
                            case 531://Rain             ragged shower rain
                                {
                                    widfinal = 6;//Rain 6 
                                    break;
                                }

                            case 200://Thunderstorm     thunderstorm with light rain
                            case 201://Thunderstorm     thunderstorm with rain
                            case 202://Thunderstorm     thunderstorm with heavy rain
                            case 210://Thunderstorm     light thunderstorm
                            case 211://Thunderstorm     thunderstorm
                            case 212://Thunderstorm     heavy thunderstorm
                            case 221://Thunderstorm     ragged thunderstorm
                            case 230://Thunderstorm     thunderstorm with light drizzle
                            case 231://Thunderstorm     thunderstorm with drizzle
                            case 232://Thunderstorm     thunderstorm with heavy drizzle
                            case 771://Squall           squalls
                            case 781://Tornado          tornado
                                {
                                    widfinal = 7;//Thunder 7
                                    break;
                                }

                            case 300://Drizzle          light intensity drizzle
                            case 301://Drizzle          drizzle
                            case 302://Drizzle          heavy intensity drizzle
                            case 310://Drizzle          light intensity drizzle rain
                            case 311://Drizzle          drizzle rain
                            case 312://Drizzle          heavy intensity drizzle rain
                            case 313://Drizzle          shower rain and drizzle
                            case 314://Drizzle          heavy shower rain and drizzle
                            case 321://Drizzle          shower drizzle 
                                {
                                    widfinal = 8;
                                    break;
                                }

                            case 600://Snow             light snow
                            case 611://Snow             Sleet
                            case 612://Snow             Light shower sleet
                            case 613://Snow             Shower sleet
                            case 616://Snow             Rain and snow
                                {
                                    widfinal = 10;//Very light snow 10
                                    break;
                                }

                            case 615://Snow             Light rain and snow
                            case 620://Snow             Light shower snow
                                {
                                    widfinal = 11;//Windy light snow 11
                                    break;
                                }

                            case 601://Snow             Snow
                            case 602://Snow             Heavy snow
                            case 621://Snow             Shower snow
                            case 622://Snow             Heavy shower snow
                                {
                                    int next = rand.Next(0, 2);
                                    switch (next)
                                    {
                                        case 0://Light snow 12
                                            {
                                                widfinal = 12;
                                                break;
                                            }
                                        case 1://Christmas 13
                                            {
                                                widfinal = 13;
                                                break;
                                            }
                                    }
                                    break;
                                }

                            case 0:
                            default://default
                                {
                                    Console.WriteLine("Could not convert weather... openmapid: " + wid.ToString() + " ->default ingameid: " + widfinal.ToString());
                                    break;
                                }
                        }
                        Console.WriteLine("Weather Updated... openmapid: " + wid.ToString() + " ->ingameid: " + widfinal.ToString());
                    }
                    else
                    {
                        Console.WriteLine("Could not load weather ->default ingameid: " + widfinal.ToString());
                    }
                }
                else
                {
                    int randweather = rand.Next(0, 14);
                    uint.TryParse(randweather.ToString(), out widfinal);
                    Console.WriteLine("Weather Update: Random Weatherid: " + widfinal.ToString());
                }

                return widfinal;
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
            return 0;
        }

        public void Minutetimertick_timeweather()
        {
            try
            {
                dati = DateTime.Now;

                //weather
                if (weatherreqtime == -1 || weatherreqtime == 15)
                {
                    if (weatherreqtime != -1)
                    {
                        weatherreqtime = 0;
                    }
                    weatherid = GetWeather(false);
                }
                weatherreqtime += 1;
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void LoadTimeWeatherPlayerSettingsForAll()
        {
            try
            {
                foreach (Class_player pl in server.playerhandler.playerlist)
                {
                    LoadTimeWeatherPlayerSettings(pl.player);
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void LoadTimeWeatherPlayerSettings(IPlayer player)
        {
            try
            {
                player.SetWeather(weatherid);
                player.SetDateTime(dati);
                player.Emit("client:settimepm");
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        #endregion
    }

}
