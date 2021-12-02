using System;
using BepInEx;
using Logger = BepInEx.Logging.Logger;
using PolyTechFramework;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine.UI;

namespace SpeedrunUtils
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    // Specify the mod as a dependency of PTF
    [BepInDependency(PolyTechMain.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    // This Changes from BaseUnityPlugin to PolyTechMod.
    // This superclass is functionally identical to BaseUnityPlugin, so existing documentation for it will still work.
    public class SpeedrunUtils : PolyTechMod
    {
        public new const string
            PluginGuid = "polytech.SpeedrunUtils",
            PluginName = "Speedrun Utils",
            PluginVersion = "1.0.0";
        
        public static SpeedrunUtils instance;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> speed, windowX, windowY, windowW, windowH;
        private Rect windowRect = new Rect(20, 20, 250, 150);
        float frameCount, timeElapsed;
        long buildTimeStart, buildTimeEnd;
        Harmony harmony;
        void Awake()
        {
			if (instance == null) instance = this;
            // Use this if you wish to make the mod trigger cheat mode ingame.
            // Set this true if your mod effects physics or allows mods that you can't normally do.
            isCheat = false;
           
            modEnabled = Config.Bind("Speedrun Utils", "Enable/Disable Mod", true, "Enable Mod");
            speed = Config.Bind("Speedrun Utils", "Simulation Speed for calculated time", 3f, "3.0 = 300% speed");
            
            windowX = Config.Bind("Speedrun Utils", "windowX", 20f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Browsable = false }));
            windowY = Config.Bind("Speedrun Utils", "windowY", 20f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Browsable = false }));
            windowW = Config.Bind("Speedrun Utils", "window width", 250f);
            windowH = Config.Bind("Speedrun Utils", "window height", 150f);

            windowX.SettingChanged += (s, e) => updateWindowRect();
            windowY.SettingChanged += (s, e) => updateWindowRect();
            windowW.SettingChanged += (s, e) => updateWindowRect();
            windowH.SettingChanged += (s, e) => updateWindowRect();

            updateWindowRect();

            isEnabled = modEnabled.Value;

            modEnabled.SettingChanged += onEnableDisable;

            harmony = new Harmony("org.bepinex.plugins.SpeedrunUtils");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            this.authors = new string[] {"Conqu3red"};

            PolyTechMain.registerMod(this);
        }  


        void updateWindowRect() {
            windowRect.x = windowX.Value;
            windowRect.y = windowY.Value;
            windowRect.width = windowW.Value;
            windowRect.height = windowH.Value;
        }

        public void onEnableDisable(object sender, EventArgs e)
        {
            this.isEnabled = modEnabled.Value;

            if (modEnabled.Value)
            {
                enableMod();
            }
            else
            {
                disableMod();
            }
        }
        public override void enableMod() 
        {
            modEnabled.Value = true;
        }
        public override void disableMod() 
        {
            modEnabled.Value = false;
        }

        private bool shouldRun() {
            return modEnabled.Value && PolyTechMain.ptfInstance.isEnabled;
        }

        private void OnGUI(){
            if (shouldRun()) {
                windowRect = GUI.Window(0, windowRect, DoUtilsWindow, "Speedrun Utils");
                windowX.Value = windowRect.x;
                windowY.Value = windowRect.y;
                windowW.Value = windowRect.width;
                windowH.Value = windowRect.height;
            }
        }

        [HarmonyPatch(typeof(BridgePhysics), "StartSimulation")]
        static class StartPatch {
            public static void Prefix() {
                if (instance.shouldRun() && Main.m_Instance != null) {
                    instance.frameCount = Main.m_Instance.m_World.frameCount;
                    instance.timeElapsed = Main.m_Instance.m_World.timeElapsed;
                }
            }
        }

        [HarmonyPatch(typeof(PolyPhysics.World), "FixedUpdate_Manual")]
        static class FrameUpdatePatch {
            public static void Prefix() {
                if (instance.shouldRun() && Main.m_Instance != null && instance.frameCount <= Main.m_Instance.m_World.frameCount && !GameStateSim.m_LevelPassed) {
                    instance.frameCount = Main.m_Instance.m_World.frameCount;
                    instance.timeElapsed = Main.m_Instance.m_World.timeElapsed;
                }
            }
        }

        [HarmonyPatch(typeof(GameStateBuild), "Enter")]
        static class BuildEnterPatch {
            public static void Prefix(GameState prevState) {
                if (instance.shouldRun()) {
                    instance.buildTimeStart = DateTime.Now.Ticks;
                    instance.buildTimeEnd = 0;
                }
            }
        }

        [HarmonyPatch(typeof(GameStateBuild), "Exit")]
        static class BuildExitPatch {
            public static void Prefix(GameState nextState) {
                if (instance.shouldRun()) {
                    instance.buildTimeEnd = DateTime.Now.Ticks;
                }
            }
        }

        
        

        private void DoUtilsWindow(int windowID) {
            var tspan = new TimeSpan((buildTimeEnd == 0 ? DateTime.Now.Ticks : buildTimeEnd) - buildTimeStart);
            GUILayout.Label($"Build time: {tspan.TotalSeconds : 0.00}s");
            
            GUILayout.Label($"Simulation Frames: {frameCount}");
            GUILayout.Label($"Simulation time (at {Utils.FormatPercentage(speed.Value)}): {timeElapsed / speed.Value : 0.00}s");
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    
    }
}