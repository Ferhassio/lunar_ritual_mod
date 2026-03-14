using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;

namespace LunarRitual
{
	public static class RiskOfOptionsCompatibility
	{
		private static bool? _enabled;

		public static bool enabled
		{
			get
			{
				if (_enabled == null)
				{
					_enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
				}
				return (bool)_enabled;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void OptionsInit()
		{
			Log.Info("Risk of Options detected, adding options...");
			
			// Main settings
			AddMainOptions();
			
			// Debug settings
			AddDebugOptions();
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private static void AddMainOptions()
		{
			// shardChance - Step slider for percentage (0-100%), default 1%
			ModSettingsManager.AddOption(new StepSliderOption(LunarRitual.shardChance,
				new StepSliderConfig
				{
					min = 0f,
					max = 100f,
					increment = 0.01f
				}));
			
			// teamShards - Checkbox, default false
			ModSettingsManager.AddOption(new CheckBoxOption(LunarRitual.teamShards));
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private static void AddDebugOptions()
		{
			// shardMultiplier
			ModSettingsManager.AddOption(new StepSliderOption(LunarRitual.shardMultiplier,
				new StepSliderConfig
				{
					min = 0f,
					max = 1f,
					increment = 0.01f
				}));
			
			// startingShards
			ModSettingsManager.AddOption(new IntSliderOption(LunarRitual.startingShards,
				new IntSliderConfig
				{
					min = 0,
					max = 100
				}));
			
			// noShardDroplet
			ModSettingsManager.AddOption(new CheckBoxOption(LunarRitual.noShardDroplet));
			
			// resetShards
			ModSettingsManager.AddOption(new CheckBoxOption(LunarRitual.resetShards));
		}
	}

	/* ProperSave temporarily disabled
	public static class ProperSaveCompatibility
	{
		private static bool? _enabled;

		public static bool enabled
		{
			get
			{
				if (_enabled == null)
				{
					_enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ProperSave");
				}
				return (bool)_enabled;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void AddEvent(Action<Dictionary<string, object>> action)
		{
			ProperSave.SaveFile.OnGatherSaveData += action;
		}

		public static bool IsLoading
		{
			[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
			get { return ProperSave.Loading.IsLoading; }
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static string GetModdedData(string name)
		{
			return ProperSave.Loading.CurrentSave.GetModdedData<string>(name);
		}
	}

	public class GenesisShards_ProperSaveObj
	{
		public Dictionary<ulong, int> playerShards;
		public GenesisShards_ProperSaveObj(Dictionary<ulong, int> playerShards)
		{
			this.playerShards = playerShards;
		}
	}
	*/
}