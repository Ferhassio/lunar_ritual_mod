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
		public static GameObject genesisShardPrefab;

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

			On.RoR2.Run.Start += (orig, self) =>
			{
				GenesisShardMiscPickupDef.ClearProcessedPickups();
				orig(self);
			};

			Log.Warning("[LunarRitual] GenesisShard MiscPickupDef registered via ContentAddition");
			}
			catch (Exception ex)
			{
				Log.Error($"[LunarRitual] Failed to initialize GenesisShard MiscPickupDef: {ex.Message}");
				Log.Error($"[LunarRitual] Stack trace: {ex.StackTrace}");
				return;
			}

			isInitialized = true;
			Log.Warning("[LunarRitual] GenesisShard initialized successfully");
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
					Log.Warning($"[LunarRitual] GenesisShard PickupIndex found: {GenesisShardPickupIndex}");
					UpdateGenesisShardPrefab();
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

		public static void UpdateGenesisShardPrefab()
		{
			Log.Warning("[LunarRitual] UpdateGenesisShardPrefab called");
			if (GenesisShardPickupIndex == PickupIndex.none)
			{
				Log.Error("[LunarRitual] Cannot update prefab - GenesisShardPickupIndex is not initialized");
				return;
			}

			try
			{
				PickupDef genesisShardDef = GenesisShardPickupIndex.pickupDef;
				Log.Warning($"[LunarRitual] GenesisShardPickupDef found, name: {genesisShardDef.nameToken}, pickupIndex: {GenesisShardPickupIndex.value}");
				
				genesisShardDef.nameToken = "GENESIS_SHARD_NAME";
				genesisShardDef.interactContextToken = "Pick up Genesis Shard";
				genesisShardDef.baseColor = new Color32(255, 100, 100, 255);
				genesisShardDef.darkColor = new Color32(180, 50, 50, 255);

				GameObject lunarCoinPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarCoin/PickupLunarCoin.prefab").WaitForCompletion();
				if (lunarCoinPrefab != null)
				{
					Log.Warning($"[LunarRitual] LunarCoin prefab loaded: {lunarCoinPrefab.name}");
					
					genesisShardPrefab = GameObject.Instantiate(lunarCoinPrefab);
					genesisShardPrefab.name = "GenesisShardPickup";
					Log.Warning($"[LunarRitual] GenesisShard prefab instantiated");

					MeshRenderer meshRenderer = genesisShardPrefab.transform.Find("Coin5Mesh").GetComponent<MeshRenderer>();
					if (meshRenderer != null)
					{
						Log.Warning("[LunarRitual] Coin5Mesh found, setting material");
						Material material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matLunarCoinPlaceholder.mat").WaitForCompletion();
						if (material != null)
						{
							meshRenderer.material = material;
							Color genesisColor = new Color(1f, 0.4f, 0.4f, 1f);
							meshRenderer.material.SetColor("_Color", genesisColor);
							meshRenderer.material.SetColor("_EmColor", new Color(1f, 0.2f, 0.2f, 1f));
							Log.Warning("[LunarRitual] Material set successfully");
						}
						else
						{
							Log.Error("[LunarRitual] Failed to load matLunarCoinPlaceholder material");
						}
					}
					else
					{
						Log.Error("[LunarRitual] Coin5Mesh not found in prefab");
					}

					Light light = genesisShardPrefab.GetComponentInChildren<Light>();
					if (light != null)
					{
						light.color = new Color(1f, 0.4f, 0.4f, 1f);
						Log.Warning("[LunarRitual] Light color set");
					}
					else
					{
						Log.Warning("[LunarRitual] Light component not found");
					}

					GenericPickupController pickupController = genesisShardPrefab.GetComponent<GenericPickupController>();
					if (pickupController == null)
					{
						pickupController = genesisShardPrefab.AddComponent<GenericPickupController>();
						Log.Warning("[LunarRitual] Added GenericPickupController to prefab");
					}
					
					if (pickupController != null)
					{
						pickupController.pickupIndex = GenesisShardPickupIndex;
						Log.Warning($"[LunarRitual] GenericPickupController pickupIndex set to {GenesisShardPickupIndex.value}");
						
						Collider collider = genesisShardPrefab.GetComponentInChildren<Collider>();
						if (collider != null)
						{
							collider.isTrigger = false;
							Log.Warning("[LunarRitual] Collider trigger disabled - manual pickup only");
						}
					}
					else
					{
						Log.Error("[LunarRitual] GenericPickupController not found on prefab!");
					}

					Log.Warning("[LunarRitual] GenesisShard prefab created and assigned successfully");
				}
				else
				{
					Log.Error("[LunarRitual] Failed to load LunarCoin prefab");
				}
			}
			catch (Exception ex)
			{
				Log.Error($"[LunarRitual] Exception while updating GenesisShard prefab: {ex.Message}");
				Log.Error($"[LunarRitual] Stack trace: {ex.StackTrace}");
			}
		}

		public static void AddShards(ulong userId, int amount)
		{
			if (!playerShards.ContainsKey(userId))
			{
				playerShards[userId] = 0;
			}
			playerShards[userId] += amount;
			Log.Warning($"[LunarRitual] Added {amount} Genesis Shard(s) to user {userId}. Total: {playerShards[userId]}");
		}

		public static int GetShards(ulong userId)
		{
			return playerShards.ContainsKey(userId) ? playerShards[userId] : 0;
		}

		public static void SetShards(ulong userId, int amount)
		{
			playerShards[userId] = amount;
			Log.Warning($"[LunarRitual] Set Genesis Shards for user {userId} to {amount}");
		}

		public static void RemoveShards(ulong userId, int amount)
		{
			if (playerShards.ContainsKey(userId))
			{
				playerShards[userId] = Math.Max(0, playerShards[userId] - amount);
				Log.Warning($"[LunarRitual] Removed {amount} Genesis Shard(s) from user {userId}. Total: {playerShards[userId]}");
			}
		}

		public static void LoadShards()
		{
			if (!File.Exists(ShardsSavePath))
			{
				Log.Warning("[LunarRitual] No existing Genesis Shards save file found. Starting with 0 shards.");
				return;
			}

			try
			{
				string json = File.ReadAllText(ShardsSavePath);
				playerShards = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(json);
				Log.Warning($"[LunarRitual] Loaded Genesis Shards for {playerShards.Count} player(s)");
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
				Log.Warning("[LunarRitual] Genesis Shards saved successfully");
			}
			catch (Exception ex)
			{
				Log.Error($"[LunarRitual] Failed to save Genesis Shards: {ex.Message}");
			}
		}
	}

	public class GenesisShardMiscPickupDef : MiscPickupDef
	{
		public static void ClearProcessedPickups()
		{
		}

		public override void GrantPickup(ref PickupDef.GrantContext context)
		{
			Log.Warning("[LunarRitual] GrantPickup called for Genesis Shard");
			Log.Warning($"[LunarRitual] context.body: {(context.body != null ? context.body.name : "null")}");
			Log.Warning($"[LunarRitual] context.pickupIndex: {context.pickupIndex.value}");
			
			NetworkUser user = Util.LookUpBodyNetworkUser(context.body);
			if (user != null)
			{
				ulong userId = user.id.value;
				Log.Warning($"[LunarRitual] Genesis Shard collected by player {userId} via GrantPickup");
				GenesisShards.AddShards(userId, 1);
				GenesisShardsUI.RefreshUI();
			}
			else
			{
				Log.Error("[LunarRitual] GrantPickup: NetworkUser is null! context.body is null or invalid");
			}
		}
	}
}