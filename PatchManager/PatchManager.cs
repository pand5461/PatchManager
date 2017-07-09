﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using KSP.UI.Screens;

namespace PatchManager
{
    public class PatchInfo
    {

        public bool enabled;
        public bool toggle = false;
        public string fname;

        // Required settings.  
        // srcPath should use forward slashes, and include the full file name.  srcPath should be in a directory 
        // called ".../PatchManager/PluginData"
        public string modName;
        public string patchName;
        public string srcPath;
        public string shortDescr;

        // Optional, but recommended
        public string longDescr;

        //// Optional entries here

        // dependencies, only show this patch if these specified mods are available
        // List either the directory of the mod (as show by ModuleManager), or the 
        // mod DLL (as show by ModuleManager)
        public string dependencies;

        // Path to icon, if desirec
        public string icon;

        // Author's name, if desired
        public string author;
        public string destPath;
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PatchManagerClass : MonoBehaviour
    {

        const string CONFIG_NODENAME = "PatchManager";

        string KSP_DIR = KSPUtil.ApplicationRootPath;
        string DEFAULT_PATCH_DIRECTORY; 
        

        private ApplicationLauncherButton Button;
        bool visible = false;
        bool restartMsg = false;
        Rect windowPosition;
        const int WIDTH = 800;
        const int HEIGHT = 300;
        Vector2 fileSelectionScrollPosition = new Vector2();        

        static List<PatchInfo> availablePatches = new List<PatchInfo>();

        PatchInfo pi;

        public void Start()
        {
            DEFAULT_PATCH_DIRECTORY = KSP_DIR + "GameData/PatchManager/ActiveMMPatches";
            buildModList();
            LoadAllPatches();
            if (!HighLogic.CurrentGame.Parameters.CustomParams<PM>().alwaysShow  && (availablePatches == null || availablePatches.Count() == 0))
                return;


            windowPosition = new Rect((Screen.width - WIDTH)/2, (Screen.height - HEIGHT) / 2, WIDTH, HEIGHT);
            Texture2D Image = GameDatabase.Instance.GetTexture("PatchManager/Resources/PatchManager", false);

            Button = ApplicationLauncher.Instance.AddModApplication(onTrue, onFalse, null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER, Image);
            
           
        }

        static GUIStyle bodyButtonStyle = new GUIStyle(HighLogic.Skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        static GUIStyle bodyButtonStyleGreen = new GUIStyle(HighLogic.Skin.button)
        {
            normal =
            {
                textColor = Color.green
            },
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        static GUIStyle bodyButtonStyleRed = new GUIStyle(HighLogic.Skin.button)
        {
            normal =
            {
                textColor = Color.red
            },
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        void ToggleActivation(PatchInfo pi)
        {
            pi.toggle = !pi.toggle;
        }

        void HideWindow()
        {
            visible = false;
            Button.SetFalse();
        }

        void CenterLine(string msg)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(msg);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

        }
        void drawRestartWindow(int windowid)
        {
            CenterLine(" ");
            CenterLine("The changes you just made by installing/uninstalling one or");
            CenterLine("more patches will not take effect until the game is restarted");
            CenterLine(" ");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(" Acknowledged ", GUILayout.Width(150), GUILayout.Height(40)))
                restartMsg = false;
            GUILayout.FlexibleSpace();
        }

        void drawWindow(int windowid)
        {
            GUILayout.BeginHorizontal();
            fileSelectionScrollPosition = GUILayout.BeginScrollView(fileSelectionScrollPosition);
            GUILayout.BeginVertical();
            for (int i = 0; i < availablePatches.Count(); i++)
            {
                pi = availablePatches[i];
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUIStyle gs = bodyButtonStyle;

                if (pi.enabled)
                {
                    if (!pi.toggle)
                        gs = bodyButtonStyleGreen;
                    else
                        gs = bodyButtonStyleRed;
                } else
                {
                    if (!pi.toggle)
                        gs = bodyButtonStyleRed;
                    else
                        gs = bodyButtonStyleGreen;

                }

                Texture2D Image = GameDatabase.Instance.GetTexture(pi.icon, false);
                if (Image == null)
                {
                    Log.Info("No image loaded for button");
                    Image = new Texture2D(2, 2);
                }


                if (GUILayout.Button(Image, GUILayout.Width(38), GUILayout.Height(38)))
                {
                    ToggleActivation(pi);
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical();

                if (GUILayout.Button(pi.modName + "\n" + pi.shortDescr, gs, GUILayout.Width(175)))
                {
                    ToggleActivation(pi);
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                GUILayout.Label(pi.longDescr + "\n" + pi.author + "\n", GUILayout.Width(WIDTH - 175 - 38 - 2));
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.EndScrollView();

                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply All", GUILayout.Width(90)))
            {
                ApplyAllChanges();
                HideWindow();

            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(90)))
            {
                HideWindow();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        void ApplyAllChanges()
        {
            for (int i = 0; i < availablePatches.Count(); i++)
            {
                pi = availablePatches[i];
                string s = pi.destPath + pi.fname;
                if (pi.toggle)
                {
                    if (pi.enabled)
                    {
                        // delete the dest file
                        Log.Info("Deleting patch at: " + s);
                        File.Delete(s);
                    }
                    else
                    {
                        // Copy the file to the dest
                        Log.Info("Copying patch from: " + KSP_DIR + "/GameData/" + pi.srcPath + "   to: " + s);
                        File.Copy(KSP_DIR + "/GameData/" + pi.srcPath, s);
                    }
                }
            }
            restartMsg = true;
        }

        public void Destroy()
        {
            ApplicationLauncher.Instance.RemoveModApplication(Button);
        }

        public void onTrue()
        {
            Log.Info("Opened PatchManager");
            visible = true;
            LoadAllPatches();
        }

        public void onFalse()
        {
            Log.Info("Closed PatchManager");
            visible = false;
        }

        public void OnGUI()
        {
            if ( HighLogic.CurrentGame.Parameters.CustomParams<PM>().EnabledForSave || HighLogic.CurrentGame.Parameters.CustomParams<PM>().alwaysShow)
            {
                if (visible)
                {
                    int windowId = GUIUtility.GetControlID(FocusType.Native);
                    windowPosition = GUILayout.Window(windowId, windowPosition, drawWindow, "Patch Manager");
                }
                if (restartMsg)
                {
                    int windowId = GUIUtility.GetControlID(FocusType.Native);
                    windowPosition = GUILayout.Window(windowId, windowPosition, drawRestartWindow, "Restart Message");
                }
            }
        }

        void LoadAllPatches()
        {
            Log.Info("PatchManager.OnLoad");
            //try {
            availablePatches.Clear();
            var availableNodes = GameDatabase.Instance.GetConfigNodes(CONFIG_NODENAME);
            if (availableNodes.Count() > 0)
            {
                //load
                Log.Info("PatchManager loaded configs, count: " + availableNodes.Count().ToString());
                foreach (var n in availableNodes)
                {
                    PatchInfo pi = new PatchInfo();
                    pi.modName = n.GetValue("modName");
                    pi.patchName = n.GetValue("patchName");
                    pi.srcPath = n.GetValue("srcPath");
                    pi.shortDescr = n.GetValue("shortDescr");
                    pi.longDescr = n.GetValue("longDescr");
                    pi.dependencies = n.GetValue("dependencies");
                    pi.icon = n.GetValue("icon");
                    pi.author = n.GetValue("author");
                    pi.destPath = n.GetValue("destPath");

                    if (pi.destPath == null || pi.destPath == "")
                        pi.destPath = DEFAULT_PATCH_DIRECTORY;
                    else
                        pi.destPath = KSP_DIR + "GameData/" + pi.destPath;
                    pi.fname = pi.srcPath.Substring(pi.srcPath.LastIndexOf('/'));                    

                    bool bd = Directory.Exists(pi.destPath);
                    if (bd)
                    {
                        string s = pi.destPath + pi.fname;
                        pi.enabled = File.Exists(s);
                    }
                    else
                    {
                        pi.enabled = false;
                        DirectoryInfo di = Directory.CreateDirectory(pi.destPath);
                        // Shouldn't ever happen, but if it does, create the directory
                    }
                        pi.toggle = false;
                    Log.Info("pi.enabled: " + pi.enabled.ToString());

                    if (dependenciesOK(pi))
                        availablePatches.Add(pi);
                    else
                        Log.Error("Dependencies not satisfied for: " + pi.modName);
                }
            }
            else
            {
                Log.Info("PatchManager no loaded configs");
            }
        }

        bool dependenciesOK(PatchInfo pi)
        {
            List<string> stringList = pi.dependencies.Split(',').ToList();
            // First check to see if it's a DLL
            foreach (var s in stringList)
            {
                var s1 = s.Trim();
                Log.Info("Checking for dependency: " + s1);
                if (hasMod(s1))
                    return true;

                // Now check to see if it's a directory in GameData
                var s2 = KSP_DIR + "GameData/" + s1;
                Log.Info("Checking for directory: " + s2);
                if (Directory.Exists(s2))
                    return true;
            }
            return false;
        }

        public static List<String> installedMods = new List<String>();
        void buildModList()
        {
            Log.Info("buildModList");
            //https://github.com/Xaiier/Kreeper/blob/master/Kreeper/Kreeper.cs#L92-L94 <- Thanks Xaiier!
            foreach (AssemblyLoader.LoadedAssembly a in AssemblyLoader.loadedAssemblies)
            {
                string name = a.name;
                Log.Info(string.Format("Loading assembly: {0}", name));
                installedMods.Add(name);
            }
        }

        bool hasMod(string modIdent)
        {
            if (installedMods.Count == 0)
                buildModList();
            return installedMods.Contains(modIdent);
        }

    }
}