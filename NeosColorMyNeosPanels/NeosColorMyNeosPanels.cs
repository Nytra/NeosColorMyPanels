using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;

namespace ColorMyNeosPanels
{
	public class ColorMyNeosPanels : NeosMod
	{
		public override string Name => "ColorMyPanels";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/Nytra/NeosColorMyPanels";

		private static Random rngTimeSeeded = new Random();
		public static ModConfiguration Config;
		private const string SEP_STRING = "<size=0></size>";

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> MOD_ENABLED = new ModConfigurationKey<bool>("MOD_ENABLED", "Mod Enabled:", () => true);
		
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_SEP_1 = new ModConfigurationKey<dummy>("DUMMY_SEP_1", SEP_STRING, () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> USE_STATIC_COLOR = new ModConfigurationKey<bool>("USE_STATIC_COLOR", "Use Static Color (Random if false):", () => false);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<color> STATIC_COLOR = new ModConfigurationKey<color>("STATIC_COLOR", "Static Color:", () => new color(1f, 1f, 1f, 0.5f));

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_SEP_2 = new ModConfigurationKey<dummy>("DUMMY_SEP_2", SEP_STRING, () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<float> SATURATION = new ModConfigurationKey<float>("SATURATION", "Saturation:", () => 1f);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<float> VALUE = new ModConfigurationKey<float>("VALUE", "Value:", () => 1f);

		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("owo.Nytra.ColorMyPanels");
			Config = GetConfiguration();
			harmony.PatchAll();
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
						color c;
						if (Config.GetValue(USE_STATIC_COLOR))
						{
							c = Config.GetValue(STATIC_COLOR);
						}
						else
						{
							c = new ColorHSV((float)rngTimeSeeded.NextDouble(), Config.GetValue(SATURATION), Config.GetValue(VALUE), 1f).ToRGB();
						}
						ClampColor(ref c);
						__instance.Color = c;
					});
				}

				return true;
			}
		}
	}
}