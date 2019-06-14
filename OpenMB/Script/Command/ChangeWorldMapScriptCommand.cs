﻿using OpenMB.Game;
using OpenMB.Mods;
using Mogre;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMB.Script.Command
{
    public class ChangeWorldMapScriptCommand : ScriptCommand
    {
        private string[] commandArgs;
        public ChangeWorldMapScriptCommand()
        {
            commandArgs = new string[] {
                "mapName"
            };
        }
        public override string[] CommandArgs
        {
            get
            {
                return commandArgs;
            }
        }

        public override string CommandName
        {
            get
            {
                return "change_world_map";
            }
        }

        public override ScriptCommandType CommandType
        {
            get
            {
                return ScriptCommandType.Line;
            }
        }

        public override void Execute(params object[] executeArgs)
        {
            string worldMapID = getParamterValue(CommandArgs[0]);

            GameWorld world = executeArgs[0] as GameWorld;
            world.ChangeWorldMap(worldMapID);
        }
    }
}