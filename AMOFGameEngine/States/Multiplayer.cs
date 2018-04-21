﻿using System;
using System.Collections.Generic;
using Mogre;
using Mogre_Procedural.MogreBites;
using AMOFGameEngine.Mods;
using AMOFGameEngine.Network;
using AMOFGameEngine.Widgets;
using AMOFGameEngine.UI;

namespace AMOFGameEngine.States
{
    public class Multiplayer : AppState
    {
        private delegate bool ServerStartDelegate();
        private InputBox ibServerName;
        private InputBox ibServerPort;
        private CheckBox chkHasPasswd;
        private InputBox ibPasswd;
        private GameServer thisServer;
        private Dictionary<string, string> option;
        private StringVector serverState;
        private ParamsPanel serverpanel;
        private bool isEscapeMenuOpened;

        public Multiplayer()
        {
            option = new Dictionary<string, string>();
            serverState = new StringVector();
        }

        public override void enter(Mods.ModData e = null)
        {
            m_Data = e;
            m_SceneMgr = GameManager.Instance.mRoot.CreateSceneManager(Mogre.SceneType.ST_GENERIC, "MenuSceneMgr");
            ColourValue cvAmbineLight = new ColourValue(0.7f, 0.7f, 0.7f);
            m_SceneMgr.AmbientLight = cvAmbineLight;
            m_Camera = m_SceneMgr.CreateCamera("multiplayerCam");
            GameManager.Instance.mViewport.Camera = m_Camera;
            m_Camera.AspectRatio = GameManager.Instance.mViewport.ActualWidth / GameManager.Instance.mViewport.ActualHeight;
            GameManager.Instance.mViewport.OverlaysEnabled = true;

            GameManager.Instance.mKeyboard.KeyPressed += new MOIS.KeyListener.KeyPressedHandler(mKeyboard_KeyPressed);
            GameManager.Instance.mKeyboard.KeyReleased += new MOIS.KeyListener.KeyReleasedHandler(mKeyboard_KeyReleased);

            BuildGameListUI();
        }

        #region UI

        private void BuildGameListUI()
        {
            GameManager.Instance.mTrayMgr.destroyAllWidgets();
            List<string> columns = new List<string>();
            columns.Add("Server Name");
            columns.Add("Module");
            columns.Add("Game Type");
            columns.Add("Map");
            columns.Add("Players");
            columns.Add("HasPassword");
            GameListUI ui = GameUIManager.Instance.CreateGameListUI("gamelist", columns);
            for (int i = 0; i < 20; i++)
            {
                ui.AppendItem(new List<string>()
                {
                    "Server_1",
                    "Native",
                    "Battle",
                    "Temp_Map_1",
                    "0/20",
                    "No"
                });
            }
            GameManager.Instance.mTrayMgr.createButton(TrayLocation.TL_RIGHT, "btnJoin", "Join",50);
            GameManager.Instance.mTrayMgr.createButton(TrayLocation.TL_RIGHT, "btnHost", "Host", 50);
            GameManager.Instance.mTrayMgr.createButton(TrayLocation.TL_RIGHT, "btnExit", "Exit", 50);
        }
        void HostGameUI()
        {
            GameManager.Instance.mTrayMgr.destroyAllWidgets();
            GameManager.Instance.mTrayMgr.createLabel(TrayLocation.TL_CENTER, "lbHost", "Host Game", 300);
            ibServerName = GameManager.Instance.mTrayMgr.createInputBox(TrayLocation.TL_CENTER, "ibServerName", "Server Name:",300, 180, "New Server");
            ibServerPort = GameManager.Instance.mTrayMgr.createInputBox(TrayLocation.TL_CENTER, "ibServerPort", "Server Port:",300, 180, "7458",true);
            chkHasPasswd = GameManager.Instance.mTrayMgr.createCheckBox(TrayLocation.TL_CENTER, "chkHasPass", "Has Password", 300);
            GameManager.Instance.mTrayMgr.createLongSelectMenu(TrayLocation.TL_CENTER, "smServerMaps", "Server Map:", 190, 10);
            GameManager.Instance.mTrayMgr.createButton(TrayLocation.TL_RIGHT, "btnOK", "OK");
            GameManager.Instance.mTrayMgr.createButton(TrayLocation.TL_RIGHT, "btnCancel", "Cancel");
        }

        private void BuildEscapeMenu()
        {
            GameManager.Instance.mTrayMgr.destroyAllWidgets();
            GameManager.Instance.mTrayMgr.createButton(TrayLocation.TL_CENTER, "choose_side", "Choose Side", 200f);
            GameManager.Instance.mTrayMgr.createButton(TrayLocation.TL_CENTER, "choose_chara", "Choose Character", 200f);
            GameManager.Instance.mTrayMgr.createButton(TrayLocation.TL_CENTER, "exit_multiplayer", "Exit", 200f);
            this.isEscapeMenuOpened = true;
        }
        #endregion


        void Server_OnEscapePressed()
        {
            ShowEscapeMenu();
        }

        private void ShowEscapeMenu()
        {
        }

        bool mKeyboard_KeyReleased(MOIS.KeyEvent arg)
        {
            return GameManager.Instance.mTrayMgr.injectKeyReleased(arg);
        }

        bool mKeyboard_KeyPressed(MOIS.KeyEvent arg)
        {
            if (arg.key == MOIS.KeyCode.KC_ESCAPE)
            {
                if (!this.isEscapeMenuOpened)
                {
                    this.BuildEscapeMenu();
                }
                else
                {
                    GameManager.Instance.mTrayMgr.destroyAllWidgets();
                    this.serverpanel = GameManager.Instance.mTrayMgr.createParamsPanel(TrayLocation.TL_CENTER, "serverpanel", 400f, this.serverState);
                    this.isEscapeMenuOpened = false;
                }
            }
            return GameManager.Instance.mTrayMgr.injectKeyPressed(arg);
        }

        //build a dummy scene...
        private unsafe void BuildGameSccene()
        {
            m_SceneMgr.SetSkyBox(true, "Examples/SpaceSkyBox");
            Light terrainLight = base.m_SceneMgr.CreateLight();
            terrainLight.Type = Light.LightTypes.LT_DIRECTIONAL;
            terrainLight.Direction = new Mogre.Vector3(0.55f, -0.3f, 0.75f);
            terrainLight.DiffuseColour = ColourValue.White;
            terrainLight.SpecularColour = new ColourValue(0.4f, 0.4f, 0.4f);
            TerrainGlobalOptions terrainOptions = new TerrainGlobalOptions
            {
                MaxPixelError = 8f,
                CompositeMapDistance = 3000f,
                LightMapDirection = terrainLight.Direction,
                CompositeMapAmbient = base.m_SceneMgr.AmbientLight,
                CompositeMapDiffuse = terrainLight.DiffuseColour
            };
            TerrainGroup terrainGroup = new TerrainGroup(base.m_SceneMgr, Terrain.Alignment.ALIGN_X_Z, 0x201, 12000f);
            terrainGroup.SetFilenameConvention("terrain", "dat");
            terrainGroup.Origin = Mogre.Vector3.ZERO;
            Terrain.ImportData importdata = terrainGroup.DefaultImportSettings;
            importdata.terrainSize = 0x201;
            importdata.worldSize = 12000f;
            importdata.inputScale = 600f;
            importdata.minBatchSize = 0x21;
            importdata.maxBatchSize = 0x41;
            importdata.layerList.Resize(3, new Terrain.LayerInstance());
            importdata.layerList[0].worldSize = 100f;
            importdata.layerList[0].textureNames.Add("dirt_grayrocky_diffusespecular.dds");
            importdata.layerList[0].textureNames.Add("dirt_grayrocky_normalheight.dds");
            importdata.layerList[1].worldSize = 30f;
            importdata.layerList[1].textureNames.Add("grass_green-01_diffusespecular.dds");
            importdata.layerList[1].textureNames.Add("grass_green-01_normalheight.dds");
            importdata.layerList[2].worldSize = 200f;
            importdata.layerList[2].textureNames.Add("growth_weirdfungus-03_diffusespecular.dds");
            importdata.layerList[2].textureNames.Add("growth_weirdfungus-03_normalheight.dds");
            for (int x = 0; x <= 0; x++)
            {
                for (int y = 0; y <= 0; y++)
                {
                    string fileName = terrainGroup.GenerateFilename(x, y);
                    if (ResourceGroupManager.Singleton.ResourceExists(terrainGroup.ResourceGroup, fileName))
                    {
                        terrainGroup.DefineTerrain(x, y);
                    }
                    else
                    {
                        Image img = new Image();
                        img.Load("terrain.png", ResourceGroupManager.DEFAULT_RESOURCE_GROUP_NAME);
                        if ((x % 2) != 0)
                        {
                            img.FlipAroundY();
                        }
                        if ((y % 2) != 0)
                        {
                            img.FlipAroundX();
                        }
                        terrainGroup.DefineTerrain(x, y, img);
                    }
                }
            }
            terrainGroup.LoadAllTerrains(true);
            TerrainGroup.TerrainIterator ti = terrainGroup.GetTerrainIterator();
            while (ti.MoveNext())
            {
                Terrain t = ti.Current.instance;
                float minHeight0 = 70f;
                float fadeDist0 = 40f;
                float minHeight1 = 70f;
                float fadeDist1 = 40f;
                TerrainLayerBlendMap blendMap0 = t.GetLayerBlendMap(1);
                TerrainLayerBlendMap blendMap1 = t.GetLayerBlendMap(2);
                float* pBlend0 = blendMap0.BlendPointer;
                float* pBlend1 = blendMap0.BlendPointer;
                for (ushort y = 0; y <= t.LayerBlendMapSize; y = (ushort)(y + 1))
                {
                    for (ushort x = 0; x <= t.LayerBlendMapSize; x = (ushort)(x + 1))
                    {
                        float tx;
                        float ty;
                        blendMap0.ConvertImageToTerrainSpace(x, y, out tx, out ty);
                        float height = t.GetHeightAtTerrainPosition(tx, ty);
                        float val = (height - minHeight0) / fadeDist0;
                        val = AMOFGameEngine.Utilities.Helper.Clamp<float>(val, 0f, 1f);
                        *pBlend0++ = val;
                        val = (height - minHeight1) / fadeDist1;
                        val = AMOFGameEngine.Utilities.Helper.Clamp<float>(val, 0f, 1f);
                        *pBlend1++ = val;
                    }
                }
                blendMap0.Dirty();
                blendMap1.Dirty();
                blendMap0.Update();
                blendMap1.Update();
            }
        }

        public override void buttonHit(Button button)
        {
            if (button.getName() == "btnJoin")
            {
                GameUIManager.Instance.CloseUI("gamelist");
            }
            else if (button.getName() == "btnHost")
            {
                GameUIManager.Instance.CloseUI("gamelist");
            }
            else if (button.getName() == "btnCancel")
            {
                exit();
                enter();
            }
            else if (button.getName() == "btnOK")
            {

                thisServer = new GameServer();
                thisServer.OnEscapePressed += new Action(Server_OnEscapePressed);
                GameManager.Instance.mTrayMgr.destroyAllWidgets();
                serverpanel=GameManager.Instance.mTrayMgr.createParamsPanel(TrayLocation.TL_CENTER, "serverpanel", 400, serverState);
                BuildGameSccene();
                ServerStartDelegate server = new ServerStartDelegate(ServerStart);
                server.Invoke();
            }
            else if (button.getName() == "btnExit")
            {
                GameUIManager.Instance.CloseUI("gamelist");
                changeAppState(findByName("MainMenu"), m_Data);
            }
        }

        public bool ServerStart()
        {
            thisServer.Init();
            return thisServer.Go();
        }

        public override bool pause()
        {
            return base.pause();
        }

        public override void update(double timeSinceLastFrame)
        {
            if (thisServer!=null&&thisServer.Started)
            {
                thisServer.Update();
                thisServer.GetServerState(ref serverState);
                if(!isEscapeMenuOpened)
                    serverpanel.setAllParamValues(serverState);
            }
        }

        public override void exit()
        {
            if (m_SceneMgr != null)
            {
                m_SceneMgr.DestroyCamera(m_Camera);
                GameManager.Instance.mRoot.DestroySceneManager(m_SceneMgr);
            }
            if (thisServer != null)
            {
                thisServer.Exit();
            }
        }

        public override void checkBoxToggled(CheckBox box)
        {
           if (box == chkHasPasswd)
           {
               if (box.isChecked())
               {
                   ibPasswd = GameManager.Instance.mTrayMgr.createInputBox(TrayLocation.TL_CENTER,"ibPasswd","Password:",300,180);
               }
               else
               {
                   GameManager.Instance.mTrayMgr.destroyWidget("ibPasswd");
               }
           }
        }
    }
}
