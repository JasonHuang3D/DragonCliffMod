using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        public sealed class Param
        {
            public class Mod
            {
                public string Id;
                public bool Enabled = true;
            }

            public int ShortcutKeyId = 0;
            public int CheckUpdates = 1;
            public int ShowOnStart = 1;
            public float WindowWidth;
            public float WindowHeight;

            public List<Mod> ModParams = new List<Mod>();

            static readonly string filepath = Path.Combine(Path.GetDirectoryName(typeof(Param).Assembly.Location), "Params.xml");

            public static Param Load()
            {      
                return new Param();
            }
        }

        public class GameInfo
        {
            public string Name = "Dragon Cliff";
            public string Folder = "Dragon Cliff";
            public string ModsDirectory = "CSharpMods";
            public string ModInfo= "Info.json";
            public string EntryPoint = "[Assembly-CSharp.dll]LocalizationSession.Awake:After";
            public string StartingPoint;
            public string UIStartingPoint;
            public string GameExe= "game.exe";


            public static GameInfo Load()
            {
                return new GameInfo();
            }
        }
    }
}
