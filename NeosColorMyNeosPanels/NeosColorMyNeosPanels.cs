using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;
using System.Reflection;

namespace ColorMyNeosPanels
{
    public class ColorMyNeosPanels : NeosMod
    {
        public override string Name => "ColorMyNeosPanels";
        public override string Author => "Nytra";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/Nytra/NeosColorMyNeosPanels";
        private static Random rngTimeSeeded = new Random();
        private static Random rng;
        public static ModConfiguration Config;
        private static FieldInfo workerInspectorField = typeof(WorkerInspector).GetField("_targetWorker", BindingFlags.NonPublic | BindingFlags.Instance);

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> MOD_ENABLED = new ModConfigurationKey<bool>("MOD_ENABLED", "Mod Enabled:", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<float> SATURATION = new ModConfigurationKey<float>("SATURATION", "Saturation:", () => 1f);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<float> VALUE = new ModConfigurationKey<float>("VALUE", "Value:", () => 1f);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> COMP_NAME_BASED = new ModConfigurationKey<bool>("COMP_NAME_BASED", "[Worker Inspector] Color by Worker Name (Or random if false):", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<SceneInspectorMode> SCENE_INSPECTOR_MODE = new ModConfigurationKey<SceneInspectorMode>("SCENE_INSPECTOR_MODE", "[Scene Inspector] Mode:", () => SceneInspectorMode.ComponentView);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> USE_INSPECTOR_MODE = new ModConfigurationKey<bool>("USE_INSPECTOR_MODE", "[Scene Inspector] Use Mode (Random if false):", () => true);

        private enum SceneInspectorMode
        {
            ComponentView,
            Root
        }

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("owo.Nytra.ColorMyNeosPanels");
            Config = GetConfiguration();
            harmony.PatchAll();
        }

        static void SetPanelColorWithRNG(NeosPanel panel, Random rand)
        {
            panel.Color = new ColorHSV((float)rand.NextDouble(), Config.GetValue(SATURATION), Config.GetValue(VALUE), 1f).ToRGB();
        }

        static Slot GetSceneInspectorTarget(SceneInspector sceneInspector)
        {
            if (Config.GetValue(SCENE_INSPECTOR_MODE) == SceneInspectorMode.ComponentView)
            {
                return sceneInspector.ComponentView.RawTarget ?? sceneInspector.Root.RawTarget;
            }
            else
            {
                return sceneInspector.Root.RawTarget;
            }
        }
		
        [HarmonyPatch(typeof(NeosPanel), "OnAttach")]
        class ModNameGoesHerePatch
        {
            public static bool Prefix(NeosPanel __instance)
            {
                if (Config.GetValue(MOD_ENABLED) && __instance.Slot.ReferenceID.User == __instance.LocalUser.AllocationID)
                {
                    __instance.RunInUpdates(0, () => 
                    {
                        var sceneInspector = __instance.Slot.GetComponent<SceneInspector>();
                        var workerInspector = __instance.Slot.GetComponentInChildren<WorkerInspector>();
                        rng = rngTimeSeeded;

                        // SCENE INSPECTOR

                        if (sceneInspector != null)
                        {
                            var sceneInspectorMode = Config.GetValue(SCENE_INSPECTOR_MODE);
                            SyncRef<Slot> targetSyncRef = null;
                            if (sceneInspectorMode == SceneInspectorMode.ComponentView)
                            {
                                targetSyncRef = sceneInspector.ComponentView;
                            }
                            else 
                            {
                                targetSyncRef = sceneInspector.Root;
                            }

                            if (Config.GetValue(USE_INSPECTOR_MODE))
                            {
                                Slot targetSlot;
                                targetSyncRef.Changed += (iChangeable) =>
                                {
                                    targetSlot = GetSceneInspectorTarget(sceneInspector);
                                    rng = new Random(targetSlot.ReferenceID.GetHashCode());
                                    SetPanelColorWithRNG(__instance, rng);
                                };

                                targetSlot = GetSceneInspectorTarget(sceneInspector);
                                rng = new Random(targetSlot.ReferenceID.GetHashCode());
                            }
                            else
                            {
                                rng = rngTimeSeeded;
                            }
                        }

                        // WORKER INSPECTOR

                        if (Config.GetValue(COMP_NAME_BASED) && workerInspector != null)
                        {
                            var workerRef = workerInspectorField.GetValue(workerInspector) as SyncRef<Worker>;
                            rng = new Random(workerRef.Target.ReferenceID.GetHashCode());
                        }

                        // END

                        SetPanelColorWithRNG(__instance, rng);
                    });
                }
                return true;
            }
        }
    }
}