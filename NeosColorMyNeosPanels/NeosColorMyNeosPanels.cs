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
		private static ModConfigurationKey<bool> USE_STATIC_COLOR = new ModConfigurationKey<bool>("USE_STATIC_COLOR", "Use Static Color (Overrides everything else):", () => false);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<color> STATIC_COLOR = new ModConfigurationKey<color>("STATIC_COLOR", "Static Color:", () => new color(1f, 1f, 1f, 0.5f));
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<float> SATURATION = new ModConfigurationKey<float>("SATURATION", "Saturation:", () => 1f);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<float> VALUE = new ModConfigurationKey<float>("VALUE", "Value:", () => 1f);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> WI_USE_WORKER = new ModConfigurationKey<bool>("WI_USE_WORKER", "[Worker Inspector] Color by Worker RefID (Random if false):", () => true);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> SI_USE_FACTOR = new ModConfigurationKey<bool>("SI_USE_FACTOR", "[Scene Inspector] Use Factor (Static random if false):", () => false);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<SI_Factor_Enum> SI_FACTOR = new ModConfigurationKey<SI_Factor_Enum>("SI_FACTOR", "[Scene Inspector] Factor:", () => SI_Factor_Enum.ComponentView);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> SI_USE_RANDOM_COMP_VIEW = new ModConfigurationKey<bool>("SI_USE_RANDOM_COMP_VIEW", "[Scene Inspector] Use time-seeded RNG if factor is ComponentView:", () => false);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<int> RANDOM_SEED = new ModConfigurationKey<int>("RANDOM_SEED", "Random Seed:", () => 0);

		private enum SI_Factor_Enum
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
			if (Config.GetValue(SI_FACTOR) == SI_Factor_Enum.ComponentView)
			{
				return sceneInspector.ComponentView.RawTarget ?? sceneInspector.World.RootSlot;
			}
			else
			{
				return sceneInspector.Root.RawTarget;
			}
		}

		static Random GetRNGForSceneInspector(SceneInspector sceneInspector)
		{
			if (Config.GetValue(SI_USE_RANDOM_COMP_VIEW) && Config.GetValue(SI_FACTOR) == SI_Factor_Enum.ComponentView)
			{
				return rngTimeSeeded;
			}
			else
			{
				Slot targetSlot = GetSceneInspectorTarget(sceneInspector);
				return new Random(targetSlot.ReferenceID.GetHashCode() + Config.GetValue(RANDOM_SEED));
			}
		}

		static void ClampColor(ref color c)
		{
			c = c.SetR(Math.Min(Math.Max(c.r, 0f), 1f));
			c = c.SetG(Math.Min(Math.Max(c.g, 0f), 1f));
			c = c.SetB(Math.Min(Math.Max(c.b, 0f), 1f));
			c = c.SetA(Math.Min(Math.Max(c.a, 0f), 1f));
		}
		
		[HarmonyPatch(typeof(NeosPanel), "OnAttach")]
		class ColorMyNeosPanelsPatch
		{
			public static bool Prefix(NeosPanel __instance)
			{
				if (Config.GetValue(MOD_ENABLED) && __instance.Slot.ReferenceID.User == __instance.LocalUser.AllocationID)
				{
					__instance.RunSynchronously(() =>
					{
						if (Config.GetValue(USE_STATIC_COLOR))
						{
							color c = Config.GetValue(STATIC_COLOR);
							ClampColor(ref c);
							__instance.Color = c;
							return;
						}

						var sceneInspector = __instance.Slot.GetComponent<SceneInspector>();
						var workerInspector = __instance.Slot.GetComponentInChildren<WorkerInspector>();
						rng = rngTimeSeeded;

						// SCENE INSPECTOR

						if (sceneInspector != null)
						{
							if (Config.GetValue(SI_USE_FACTOR))
							{
								sceneInspector.ComponentView.Changed += (iChangeable) =>
								{
									if (Config.GetValue(SI_FACTOR) != SI_Factor_Enum.ComponentView || !Config.GetValue(SI_USE_FACTOR)) return;
									rng = GetRNGForSceneInspector(sceneInspector);
									SetPanelColorWithRNG(__instance, rng);
								};

								sceneInspector.Root.Changed += (iChangeable) =>
								{
									if (Config.GetValue(SI_FACTOR) != SI_Factor_Enum.Root || !Config.GetValue(SI_USE_FACTOR)) return;
									rng = GetRNGForSceneInspector(sceneInspector);
									SetPanelColorWithRNG(__instance, rng);
								};

								rng = GetRNGForSceneInspector(sceneInspector);
							}
						}

						// WORKER INSPECTOR

						if (Config.GetValue(WI_USE_WORKER) && workerInspector != null)
						{
							var workerRef = workerInspectorField.GetValue(workerInspector) as SyncRef<Worker>;
							rng = new Random(workerRef.Target.ReferenceID.GetHashCode() + Config.GetValue(RANDOM_SEED));
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