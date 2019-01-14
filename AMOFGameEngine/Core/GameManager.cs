﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mogre;
using MOIS;
using Mogre_Procedural;
using Mogre_Procedural.MogreBites;
using NVorbis;
using AMOFGameEngine.Core;
using AMOFGameEngine.Localization;
using AMOFGameEngine.LogMessage;
using AMOFGameEngine.Mods;
using AMOFGameEngine.Network;
using AMOFGameEngine.Output;
using AMOFGameEngine.Game;
using AMOFGameEngine.Screen;
using AMOFGameEngine.Sound;
using AMOFGameEngine.States;
using AMOFGameEngine.Widgets;
using AMOFGameEngine.Utilities;
using ConfigFile = AMOFGameEngine.Utilities.ConfigFile;

namespace AMOFGameEngine
{
    public class GameManager : IDisposable
    {
        private string defaultRenderSystemName;
        private bool isEditMode;
        private bool isCheatMode;
        private AppStateManager appStateMgr;
        private LocateSystem locateMgr;
        private ModManager modMgr;
        private NetworkManager networkMgr;
        private OutputManager outputMgr;
        private SoundManager soundMgr;
        private ScreenManager uiMgr;
        private Dictionary<string, string> gameOptions;
        public Root root;
        public RenderWindow renderWindow;
        public Viewport viewport;
        public EngineLog log;
        public Log rendererLog;
        public Timer timer;
        public InputManager inputMgr;
        public Keyboard keyboard;
        public Mouse mouse;
        public SdkTrayManager trayMgr;
        public static string LastStateName;
        public event Action<float> Update;
        public Dictionary<int, GameObject> AllGameObjects;
        public Dictionary<string, uint> GameHashMap;
        public LoadingData loadingData;
        public bool EDIT_MODE
        {
            get
            {
                return isEditMode;
            }
        }
        public bool CHEAT_MODE
        {
            get
            {
                return isCheatMode;
            }
        }

        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameManager();
                }
                return instance;
            }
        }

        public NameValuePairList videoMode;
        

        public GameManager()
        {
            root = null;
            renderWindow = null;
            viewport = null;
            log = null;
            timer = null;

            inputMgr = null;
            keyboard = null;
            mouse = null;
            trayMgr = null;
            appStateMgr = null;
            soundMgr = null;
            AllGameObjects = new Dictionary<int,GameObject>();
            GameHashMap = new Dictionary<string, uint>();
            videoMode = new NameValuePairList();
            isEditMode = false;
            isCheatMode = false;
            loadingData = new LoadingData(LoadingType.NONE, null, null);
         }

        public bool Init(string windowTitle, Dictionary<string, string> gameOptions)
        {
            if (!InitRender(windowTitle, ref gameOptions))
                return false;

            if (!InitSubSystem(gameOptions))
                return false;

            if (!InitGame(gameOptions))
                return false;

            return true;
        }

        private bool InitRender(string wndTitle, ref Dictionary<string, string> gameOptions)
        {
            root = Root.Singleton == null ? new Root() : Root.Singleton;
            root.FrameStarted += new FrameListener.FrameStartedHandler(mRoot_FrameStarted);

            log = EngineLogManager.Instance.CreateLog("./Log/Engine.log");
            rendererLog = LogManager.Singleton.CreateLog("./Log/Mogre.log", true, true, false);
            rendererLog.SetDebugOutputEnabled(true);

            RenderSystem rs = null;
            ConfigFileParser parser = new ConfigFileParser();
            if (gameOptions == null)
            {
                gameOptions = new Dictionary<string, string>();

                ConfigFile cf = parser.Load("Game.cfg");
                var sections = cf.Sections;
                foreach (var section in sections)
                {
                    foreach(var kpl in section.KeyValuePairs)
                    {
                        gameOptions.Add(kpl.Key, kpl.Value);
                    }
                }

                cf = parser.Load("ogre.cfg");
                sections = cf.Sections;
                string renderSystem = null;
                foreach (var section in sections)
                {
                    if (section.Name == "")
                    {
                        foreach (var kpl in section.KeyValuePairs)
                        {
                            renderSystem = kpl.Value;
                            gameOptions.Add(kpl.Key, kpl.Value);
                        }
                    }
                    else if(section.Name == renderSystem)
                    {
                        foreach (var kpl in section.KeyValuePairs)
                        {
                            gameOptions.Add("Render Params_" + kpl.Key, kpl.Value);
                        }
                    }
                }
            }

            defaultRenderSystemName = gameOptions.Where(o => o.Key == "Render System").First().Value;
            var renderParams = gameOptions.Where(o => o.Key.StartsWith("Render Params"));
            if (!string.IsNullOrEmpty(defaultRenderSystemName))
            {
                var videModeRenderParam = renderParams.Where(o => o.Key == "Render Params_Video Mode").First();
                rs = root.GetRenderSystemByName(defaultRenderSystemName);
                string strVideoMode =  Regex.Match(
                    videModeRenderParam.Value, 
                    "[0-9]{3,4} x [0-9]{3,4}").Value;
                videoMode["Width"] = strVideoMode.Split('x')[0].Trim();
                videoMode["Height"] = strVideoMode.Split('x')[1].Trim();
            }

            var ogreConfigMap = rs.GetConfigOptions();

            if (rs != null && renderParams != null)
            {
                foreach (var kpl in renderParams)
                {
                    string renderParamKey = kpl.Key.Split('_')[1];
                    string renderParamValue = kpl.Value;
                    //Validate the render parameter
                    if (!ogreConfigMap[renderParamKey].possibleValues.Contains(renderParamValue))
                    {
                        renderParamValue = ogreConfigMap[renderParamKey].possibleValues[0];
                    }
                    rs.SetConfigOption(renderParamKey, renderParamValue);
                }
                root.RenderSystem = rs;
            }
            renderWindow = root.Initialise(true, wndTitle);
 
            viewport = renderWindow.AddViewport(null);
            ColourValue cv = new ColourValue(0.5f, 0.5f, 0.5f);
            viewport.BackgroundColour = cv;

            viewport.Camera = null;
 
            int hWnd = 0;
            
            renderWindow.GetCustomAttribute("WINDOW", out hWnd);
 
            inputMgr = InputManager.CreateInputSystem((uint)hWnd);
            keyboard = (MOIS.Keyboard)inputMgr.CreateInputObject(MOIS.Type.OISKeyboard, true);
            mouse =  (MOIS.Mouse)inputMgr.CreateInputObject(MOIS.Type.OISMouse, true);

            mouse.MouseMoved+=new MouseListener.MouseMovedHandler(mouseMoved);
            mouse.MousePressed += new MouseListener.MousePressedHandler(mousePressed);
            mouse.MouseReleased += new MouseListener.MouseReleasedHandler(mouseReleased);

            keyboard.KeyPressed += new KeyListener.KeyPressedHandler(keyPressed);
            keyboard.KeyReleased += new KeyListener.KeyReleasedHandler(keyReleased);

            MOIS.MouseState_NativePtr mouseState = mouse.MouseState;
                mouseState.width = viewport.ActualWidth;
                mouseState.height = viewport.ActualHeight;
 
            String secName, typeName, archName;
            AMOFGameEngine.Utilities.ConfigFile conf = new AMOFGameEngine.Utilities.ConfigFile();
            
            conf = parser.Load("resources.cfg");
            for (int i = 0; i < conf.Sections.Count; i++)
            {
                secName = conf.Sections[i].Name;
                for (int j = 0; j < conf.Sections[i].KeyValuePairs.Count; j++)
                {
                    typeName = conf.Sections[i].KeyValuePairs[j].Key;
                    archName = conf.Sections[i].KeyValuePairs[j].Value;
                    ResourceGroupManager.Singleton.AddResourceLocation(archName, typeName, secName);
                }
            }

            if (!LocateSystem.Singleton.IsInit)
            {
                LocateSystem.Singleton.InitLocateSystem(LocateSystem.Singleton.ConvertLocateShortStringToLocateInfo(gameOptions.Where(o => o.Key == "CurrentLocate").First().Value));
            }

            ResourceGroupManager.Singleton.AddResourceLocation(
                string.Format("./Media/Engine/Fonts/{0}/", LocateSystem.Singleton.Locate.ToString()), "FileSystem",
                "General");

            TextureManager.Singleton.DefaultNumMipmaps = 5;
            
            ResourceGroupManager.Singleton.InitialiseAllResourceGroups();

            trayMgr = new SdkTrayManager("AMOFTrayMgr", renderWindow, mouse, new SdkTrayListener() );

            timer = new Timer();
            timer.Reset();
 
            renderWindow.IsActive=true;

            this.gameOptions = gameOptions;

            log.LogMessage("Game Started!");

            return true;
        }

        private bool InitSubSystem(Dictionary<string, string> gameOptions)
        {
            appStateMgr = new AppStateManager();
            locateMgr = LocateSystem.Singleton;
            modMgr = new ModManager();
            networkMgr = new NetworkManager();
            outputMgr = new OutputManager();
            soundMgr = new SoundManager();
            uiMgr = new ScreenManager();

            SoundManager.Instance.InitSystem(gameOptions["EnableMusic"] == "True" ? true : false, gameOptions["EnableSound"] == "True" ? true : false);

            if (!locateMgr.IsInit)
            {
                locateMgr.InitLocateSystem(locateMgr.ConvertLocateShortStringToLocateInfo(gameOptions["CurrentLocae"]));
            }
            
            Update += modMgr.Update;
            Update += outputMgr.Update;
            Update += soundMgr.Update;
            Update += uiMgr.Update;
            
            return true;
        }

        private bool InitGame(Dictionary<string, string> gameOptions)
        {
            try
            {
                isEditMode = gameOptions["EditMode"] == "1" ? true : false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        bool mRoot_FrameStarted(FrameEvent evt)
        {
            if (Update != null)
            {
                Update(evt.timeSinceLastFrame);
            }
            UpdateGame(evt.timeSinceLastFrame);
            UpdateRender(evt.timeSinceLastFrame);
            return true;
        }

        public void Exit()
        {
            LocateSystem.Singleton.SaveLocateFile();
            log.LogMessage("Game Quit!");
            log.Dispose();
        }

        public void UpdateRender(double timeSinceLastFrame)
        {
        }

        public void UpdateGame(double timeSinceLastFrame)
        {
            foreach (var eachGameObj in AllGameObjects)
            {
                eachGameObj.Value.Update((float)timeSinceLastFrame);
            }
        }

        public bool keyPressed(KeyEvent keyEventRef)
        {
            if(keyboard.IsKeyDown(KeyCode.KC_V))
            {
                renderWindow.WriteContentsToTimestampedFile("AMGE_ScreenShot_", ".jpg");
                outputMgr.DisplayMessage(string.Format(locateMgr.GetLocalizedString(LocateFileType.GameString,"str_screenshots_saved_to_{0}"), Environment.CurrentDirectory));
                return true;
            }
            else if(keyboard.IsKeyDown(KeyCode.KC_O))
            {
                if(trayMgr.isLogoVisible())
                {
                    trayMgr.hideFrameStats();
                    trayMgr.hideLogo();
                }
                else
                {
                    trayMgr.showFrameStats(TrayLocation.TL_BOTTOMLEFT);
                    trayMgr.showLogo(TrayLocation.TL_BOTTOMRIGHT);
                }
            }
            else if (keyboard.IsKeyDown(KeyCode.KC_LSHIFT) && 
                     keyboard.IsKeyDown(KeyCode.KC_SPACE))//Left Shift + Space
            {
                renderWindow.SetFullscreen(
                    !renderWindow.IsFullScreen, 
                    Convert.ToUInt32(videoMode["Width"]), 
                    Convert.ToUInt32(videoMode["Height"])
                );
            }
            else if(keyboard.IsKeyDown(KeyCode.KC_LSHIFT) &&
                    keyboard.IsKeyDown(KeyCode.KC_I))//Left Shift + I
            {
                if(!uiMgr.CheckScreenIsVisual("Console"))
                {
                    uiMgr.ChangeScreen("Console");
                }
                else
                {
                    uiMgr.ExitCurrentScreen();
                }
            }
 
            return true;
        }
        public bool keyReleased(KeyEvent keyEventRef)
        {
            return true;
        }

        public bool mouseMoved(MouseEvent evt)
        {
            return true;
        }
        public bool mousePressed(MouseEvent evt, MouseButtonID id)
        {
            return true;
        }
        public bool mouseReleased(MouseEvent evt, MouseButtonID id)
        {
            return true;
        }
        public float Clamp(float val, float minval, float maxval)
        {
            return System.Math.Max(System.Math.Min(val, maxval), minval);
        }

        public void Dispose()
        {
            root.Dispose();
            trayMgr.Dispose();
            timer.Dispose();
        }
    }
}
