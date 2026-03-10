using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace LunarRitual
{
	public static class GenesisShards
	{
		private static readonly string ShardsSavePath = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "genesis_shards.json");
		private static Dictionary<ulong, int> playerShards = new Dictionary<ulong, int>();

		public static PickupIndex GenesisShardPickupIndex = PickupIndex.none;
		public static MiscPickupDef GenesisShardPickupDef { get; private set; }

		private static bool isInitialized = false;

		public static void InitializeGenesisShardPickup()
		{
			if (isInitialized)
			{
				Log.Warning("[LunarRitual] GenesisShard already initialized, skipping...");
				return;
			}

			try
			{
				LanguageAPI.Add("GENESIS_SHARD_NAME", "Genesis Shard");
				LanguageAPI.Add("GENESIS_SHARD_PICKUP", "Genesis Shard");
				LanguageAPI.Add("GENESIS_SHARD_DESC", "A fragment of lunar energy. Collect shards to perform powerful rituals.");
				LanguageAPI.Add("GENESIS_SHARD_LORE", "The Genesis Shards are remnants of the Moon's power, scattered across the worlds.");

				GenesisShardPickupDef = ScriptableObject.CreateInstance<GenesisShardMiscPickupDef>();
				GenesisShardPickupDef.name = "GenesisShard";
				GenesisShardPickupDef.nameToken = "GENESIS_SHARD_NAME";
				GenesisShardPickupDef.descriptionToken = "GENESIS_SHARD_DESC";
				ContentAddition.AddMiscPickupDef(GenesisShardPickupDef);

				On.RoR2.PickupCatalog.Init += (orig) =>
				{
					var result = orig();
					InitializePickupIndex();
					return result;
				};

				Log.Info("[LunarRitual] GenesisShard MiscPickupDef registered via ContentAddition");
			}
			catch (Exception ex)
			{
				Log.Error($"[LunarRitual] Failed to initialize GenesisShard MiscPickupDef: {ex.Message}");
				Log.Error($"[LunarRitual] Stack trace: {ex.StackTrace}");
				return;
			}

			isInitialized = true;
			Log.Info("[LunarRitual] GenesisShard initialized successfully");
		}

		public static void InitializePickupIndex()
		{
			if (GenesisShardPickupDef == null)
			{
				Log.Error("[LunarRitual] Cannot initialize PickupIndex - GenesisShardPickupDef is null");
				return;
			}

			try
			{
				MiscPickupIndex miscPickupIndex = GenesisShardPickupDef.miscPickupIndex;
				GenesisShardPickupIndex = PickupCatalog.FindPickupIndex(miscPickupIndex);
				if (GenesisShardPickupIndex != PickupIndex.none)
				{
					Log.Info($"[LunarRitual] GenesisShard PickupIndex found: {GenesisShardPickupIndex}");
				}
				else
				{
					Log.Error("[LunarRitual] Failed to find PickupIndex for GenesisShard");
				}
			}
			catch (Exception ex)
			{
				Log.Error($"[LunarRitual] Exception while getting PickupIndex: {ex.Message}");
			}
		}

		public static void AddShards(ulong userId, int amount)
		{
			if (!playerShards.ContainsKey(userId))
			{
				playerShards[userId] = 0;
			}
			playerShards[userId] += amount;
			Log.Info($"[LunarRitual] Added {amount} Genesis Shard(s) to user {userId}. Total: {playerShards[userId]}");
		}

		public static int GetShards(ulong userId)
		{
			return playerShards.ContainsKey(userId) ? playerShards[userId] : 0;
		}

		public static void SetShards(ulong userId, int amount)
		{
			playerShards[userId] = amount;
			Log.Info($"[LunarRitual] Set Genesis Shards for user {userId} to {amount}");
		}

		public static void RemoveShards(ulong userId, int amount)
		{
			if (playerShards.ContainsKey(userId))
			{
				playerShards[userId] = Math.Max(0, playerShards[userId] - amount);
				Log.Info($"[LunarRitual] Removed {amount} Genesis Shard(s) from user {userId}. Total: {playerShards[userId]}");
			}
		}

		public static void LoadShards()
		{
			if (!File.Exists(ShardsSavePath))
			{
				Log.Info("[LunarRitual] No existing Genesis Shards save file found. Starting with 0 shards.");
				return;
			}

			try
			{
				string json = File.ReadAllText(ShardsSavePath);
				playerShards = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(json);
				Log.Info($"[LunarRitual] Loaded Genesis Shards for {playerShards.Count} player(s)");
			}
			catch (Exception ex)
			{
				Log.Error($"[LunarRitual] Failed to load Genesis Shards: {ex.Message}");
			}
		}

		public static void SaveShards()
		{
			try
			{
				string json = JsonConvert.SerializeObject(playerShards, Formatting.Indented);
				File.WriteAllText(ShardsSavePath, json);
				Log.Info("[LunarRitual] Genesis Shards saved successfully");
			}
			catch (Exception ex)
			{
				Log.Error($"[LunarRitual] Failed to save Genesis Shards: {ex.Message}");
			}
		}
	}

	public class GenesisShardMiscPickupDef : MiscPickupDef
	{
		public override void GrantPickup(ref PickupDef.GrantContext context)
		{
			NetworkUser user = context.body?.master?.playerCharacterMasterController?.networkUser;
			if (user != null)
			{
				ulong userId = user.id.value;
				GenesisShards.AddShards(userId, 1);
				Log.Warning($"[LunarRitual] Genesis Shard collected by player {userId} via GrantPickup");
				GenesisShardsUI.RefreshUI();
			}
		}
	}
}