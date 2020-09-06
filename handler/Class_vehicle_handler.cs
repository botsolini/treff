using AltV.Net.Elements.Entities;
using System;

namespace roleplay
{
    public class Class_vehicle_handler
    {

        #region globals

        Server server;

        #endregion

        #region constructor

        public Class_vehicle_handler(Server server)
        {
            this.server = server;
        }

        #endregion


        #region on vehicles

        public void Alt_OnPlayerEnterVehicle(IVehicle vehicle, IPlayer player, byte seat)
        {
            try
            {
                player.Emit("client:showmap");
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void Alt_OnPlayerLeaveVehicle(IVehicle vehicle, IPlayer player, byte seat)
        {
            try
            {
                player.Emit("client:hidemap");
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        #endregion

    }

    public class Class_vehicle
    {

        #region globals



        #endregion

        #region constructor



        #endregion


        #region vehicle



        #endregion

    }
}
