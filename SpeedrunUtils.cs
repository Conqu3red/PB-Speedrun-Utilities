using System;
using BepInEx;
using Logger = BepInEx.Logging.Logger;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine.UI;

namespace SpeedrunUtils
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class SpeedrunUtils : BaseUnityPlugin
    {
        public const string
            PluginGuid = "polytech.SpeedrunUtils",
            PluginName = "Speedrun Utils",
            PluginVersion = "1.0.2";
        
        public static SpeedrunUtils instance;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> speed, windowX, windowY, windowW, windowH;
        private Rect windowRect = new Rect(20, 20, 250, 150);
        float frameCount, timeElapsed;
        float buildTimeStart, buildTimeEnd;
        Harmony harmony;
        void Awake()
        {
			if (instance == null) instance = this;
            // Use this if you wish to make the mod trigger cheat mode ingame.
            // Set this true if your mod effects physics or allows mods that you can't normally do.
           
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

            harmony = new Harmony("org.bepinex.plugins.SpeedrunUtils");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }  

        bool imGuiHackyRectUpdatePrevention = false;

        void updateWindowRect() {
            if (imGuiHackyRectUpdatePrevention) return;
            windowRect.x = windowX.Value;
            windowRect.y = windowY.Value;
            windowRect.width = windowW.Value;
            windowRect.height = windowH.Value;
        }

        private bool shouldRun() {
            return modEnabled.Value;
        }

        private void OnGUI(){
            if (shouldRun()) {
                windowRect = GUI.Window(
                    GUIUtility.GetControlID(FocusType.Passive),
                    windowRect,
                    DoUtilsWindow,
                    "Speedrun Utils"
                );
                imGuiHackyRectUpdatePrevention = true;
                windowX.Value = windowRect.x;
                windowY.Value = windowRect.y;
                windowW.Value = windowRect.width;
                windowH.Value = windowRect.height;
                imGuiHackyRectUpdatePrevention = false;
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
                    instance.buildTimeStart = Time.unscaledTime;
                    instance.buildTimeEnd = 0;
                }
            }
        }

        [HarmonyPatch(typeof(GameStateSim), "StartSimulation")]
        static class BuildExitPatch {
            public static void Prefix() {
                if (instance.shouldRun()) {
                    instance.buildTimeEnd = Time.unscaledTime;
                }
            }
        }

        
        

        private void DoUtilsWindow(int windowID) {
            // TODO: switch everything to adding Time.unscaledDeltaTime each frame??
            var tspan = (buildTimeEnd == 0 ? Time.unscaledTime : buildTimeEnd) - buildTimeStart;
            GUILayout.Label($"Build time: {tspan : 0.00}s");
            
            GUILayout.Label($"Simulation Frames: {frameCount}");
            GUILayout.Label($"Simulation time (at {Utils.FormatPercentage(speed.Value)}): {timeElapsed / speed.Value : 0.00}s");
            GUI.DragWindow();
        }
    
    }
}