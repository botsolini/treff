using AltV.Net;
using AltV.Net.Elements.Entities;
using AltV.Net.Data;

using System;
using System.Collections.Generic;

namespace roleplay
{

    public class Class_checkpoint_handler
    {

        #region globals

        private Server server;
        private List<Class_checkpoint> checkpointlist;

        #endregion

        #region constructor

        public Class_checkpoint_handler(Server server)
        {
            this.server = server;
            checkpointlist = new List<Class_checkpoint>();

            //binco vespucci canals
            AddCheckpoint("Binco Vespucci", 2, (float)-818.99 , (float)-1073.45 , (float)11.31, 0, 73, 48);
        }

        #endregion


        #region checkpoint

        public void AddCheckpoint(string name, int markertype, float pos_x, float pos_y, float pos_z, int dimension, int sprite, int color)
        {
            try
            {
                Class_checkpoint cp = new Class_checkpoint(name, markertype, pos_x, pos_y, pos_z, dimension, sprite, color);
                checkpointlist.Add(cp);
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void Alt_OnColShape(IColShape colShape, IEntity targetEntity, bool state)
        {
            try
            {
                if (targetEntity is IPlayer player)
                {
                    Class_player pl = server.playerhandler.FindPlayer(player.SocialClubId);
                    if(pl != null)
                    {
                        colShape.GetData("name", out string name);
                        if (state)
                        {
                            pl.currentcp = name;
                        }
                        else
                        {
                            pl.currentcp = "";
                        }
                    }
                }
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        public void CreateMarkerForPlayer(IPlayer player)
        {
            try
            {
                foreach (Class_checkpoint cp in checkpointlist)
                {
                    player.Emit("client:createmarker", cp.name, cp.markertype, cp.pos_x, cp.pos_y, cp.pos_z, cp.sprite, cp.color);
                }
                player.Emit("client:showmarker");
            }
            catch (Exception exc) { server.tools.ErrorHandling(exc); }
        }

        #endregion

    }

    public class Class_checkpoint
    {

        #region globals

        public string name;
        public int markertype;
        public float pos_x;
        public float pos_y;
        public float pos_z;
        public int dimension;
        public IColShape cs;
        public int sprite;
        public int color;

        #endregion

        #region constructor

        public Class_checkpoint(string name, int markertype, float pos_x, float pos_y, float pos_z, int dimension, int sprite, int color)
        {
            this.name = name;
            this.markertype = markertype;
            this.pos_x = pos_x;
            this.pos_y = pos_y;
            this.pos_z = pos_z;
            this.dimension = dimension;
            cs = Alt.CreateColShapeCylinder(new Position(pos_x, pos_y, pos_z), 1, 1);
            cs.SetData("name", name);
            cs.Dimension = dimension;
            this.sprite = sprite;
            this.color = color;
        }

        #endregion

    }

}