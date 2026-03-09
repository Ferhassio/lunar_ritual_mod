using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
		}
	}

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
}
