using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace LunarRitual
{
	public class GenesisShards
	{
		public static Dictionary<ulong, int> PlayerShards = new Dictionary<ulong, int>();

		public static void AddShards(ulong steamId, int amount)
		{
			Log.Warning($"[LunarRitual] AddShards called - steamId: {steamId}, amount: {amount}");
			if (!PlayerShards.ContainsKey(steamId))
			{
				PlayerShards[steamId] = 0;
				Log.Warning($"[LunarRitual] Created new shard entry for player {steamId}");
			}
			PlayerShards[steamId] += amount;
			Log.Warning($"[LunarRitual] Added {amount} Genesis Shards to player {steamId}. Total: {PlayerShards[steamId]}");
		}

		public static void RemoveShards(ulong steamId, int amount)
		{
			if (PlayerShards.ContainsKey(steamId))
			{
				PlayerShards[steamId] = Mathf.Max(0, PlayerShards[steamId] - amount);
				Log.Info($"[LunarRitual] Removed {amount} Genesis Shards from player {steamId}. Total: {PlayerShards[steamId]}");
			}
		}

		public static int GetShards(ulong steamId)
		{
			return PlayerShards.ContainsKey(steamId) ? PlayerShards[steamId] : 0;
		}

		public static void SetShards(ulong steamId, int amount)
		{
			PlayerShards[steamId] = amount;
			Log.Info($"[LunarRitual] Set Genesis Shards for player {steamId} to {amount}");
		}

		public static void SaveShards()
		{
			Dictionary<string, int> saveData = new Dictionary<string, int>();
			foreach (var kvp in PlayerShards)
			{
				saveData[kvp.Key.ToString()] = kvp.Value;
			}
			string jsonString = JsonConvert.SerializeObject(saveData);
			System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "genesis_shards.json"), jsonString);
			Log.Info($"[LunarRitual] Genesis Shards saved to file");
		}

		public static void LoadShards()
		{
			PlayerShards.Clear();
			string filePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "genesis_shards.json");
			string jsonString = "{}";
			if (System.IO.File.Exists(filePath))
			{
				jsonString = System.IO.File.ReadAllText(filePath);
			}
			try
			{
				Dictionary<string, int> saveData = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonString);
				foreach (var kvp in saveData)
				{
					if (ulong.TryParse(kvp.Key, out ulong steamId))
					{
						PlayerShards[steamId] = kvp.Value;
					}
				}
				Log.Info($"[LunarRitual] Genesis Shards loaded from file. {PlayerShards.Count} players found.");
			}
			catch (Exception ex)
			{
				Log.Error($"[LunarRitual] Failed to load Genesis Shards: {ex.Message}");
			}
		}

		public static void ResetShards()
		{
			PlayerShards.Clear();
			string filePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "genesis_shards.json");
			System.IO.File.WriteAllText(filePath, "{}");
			Log.Info($"[LunarRitual] Genesis Shards reset");
		}

		public static string GetShardsForProperSave()
		{
			Dictionary<string, int> saveData = new Dictionary<string, int>();
			foreach (var kvp in PlayerShards)
			{
				saveData[kvp.Key.ToString()] = kvp.Value;
			}
			return JsonConvert.SerializeObject(saveData);
		}

		public static void LoadShardsFromProperSave(string jsonString)
		{
			try
			{
				PlayerShards.Clear();
				Dictionary<string, int> saveData = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonString);
				foreach (var kvp in saveData)
				{
					if (ulong.TryParse(kvp.Key, out ulong steamId))
					{
						PlayerShards[steamId] = kvp.Value;
					}
				}
				Log.Info($"[LunarRitual] Genesis Shards loaded from ProperSave. {PlayerShards.Count} players found.");
			}
			catch (Exception ex)
			{
				Log.Error($"[LunarRitual] Failed to load Genesis Shards from ProperSave: {ex.Message}");
			}
		}
	}
}
