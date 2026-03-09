using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;

namespace LunarRitual
{
	public class Hooks
	{
		private static GameObject shardEffectPrefab;
		private static Dictionary<ulong, float> playerShardChance = new Dictionary<ulong, float>();

		public static void Init()
		{
			On.RoR2.Run.Awake += CreateShardEffectPrefab;
			On.RoR2.Run.Start += InitializePlayers;
			On.RoR2.PlayerCharacterMasterController.Awake += OnPlayerAwake;
			On.RoR2.GlobalEventManager.OnCharacterDeath += OnCharacterDeath;
			On.RoR2.Run.OnDestroy += SaveShardsOnRunEnd;
			On.RoR2.NetworkUser.RpcAwardLunarCoins += OnLunarCoinPickedUp;

			if (ProperSaveCompatibility.enabled)
			{
				ProperSaveCompatibility.AddEvent(SaveShardsProperSave);
			}

			Log.Info("[LunarRitual] Hooks initialized");
		}

		private static void CreateShardEffectPrefab(On.RoR2.Run.orig_Awake orig, Run self)
		{
			orig(self);

			if (shardEffectPrefab == null)
			{
				shardEffectPrefab = new GameObject("GenesisShardEffect");
				shardEffectPrefab.AddComponent<NetworkIdentity>();
				shardEffectPrefab.AddComponent<GenesisShardEffect>();
				GameObject.DontDestroyOnLoad(shardEffectPrefab);
				shardEffectPrefab.SetActive(false);
			}

			PickupDef coinDef = PickupCatalog.FindPickupIndex("LunarCoin.Coin0").pickupDef;
			if (coinDef != null)
			{
				coinDef.nameToken = "Genesis Shard";
				coinDef.interactContextToken = "Pick up Genesis Shard";
				coinDef.baseColor = new Color32(36, 9, 53, 255);
				coinDef.darkColor = new Color32(36, 9, 53, 255);
			}

			Log.Info("[LunarRitual] Shard effect prefab created");
		}

		private static void InitializePlayers(On.RoR2.Run.orig_Start orig, Run self)
		{
			if (!NetworkServer.active)
			{
				orig(self);
				return;
			}

			bool isLoading = false;
			if (ProperSaveCompatibility.enabled)
			{
				if (ProperSaveCompatibility.IsLoading) isLoading = true;
			}

			if (!isLoading && LunarRitual.resetShards.Value)
			{
				foreach (var user in NetworkUser.readOnlyInstancesList)
				{
					ulong steamId = user.id.value;
					GenesisShards.SetShards(steamId, LunarRitual.startingShards.Value);
				}
			}
			else if (isLoading)
			{
				string jsonString = ProperSaveCompatibility.GetModdedData("GenesisShardsObj");
				if (!string.IsNullOrEmpty(jsonString))
				{
					GenesisShards.LoadShardsFromProperSave(jsonString);
				}
			}

			orig(self);
		}

		private static void OnPlayerAwake(On.RoR2.PlayerCharacterMasterController.orig_Awake orig, PlayerCharacterMasterController self)
		{
			orig(self);

			NetworkUser user = self.networkUser;
			if (user != null)
			{
				ulong steamId = user.id.value;
				if (!playerShardChance.ContainsKey(steamId))
				{
					playerShardChance[steamId] = LunarRitual.shardChance.Value;
				}
			}
		}

		private static void OnCharacterDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
		{
			orig(self, damageReport);

			if (!NetworkServer.active) return;

			Log.Warning("[LunarRitual] OnCharacterDeath called - checking if shard should drop");

			if (damageReport == null || damageReport.attackerMaster == null)
			{
				Log.Warning("[LunarRitual] damageReport or attackerMaster is null");
				return;
			}

			CharacterMaster attackerMaster = damageReport.attackerMaster;

			CharacterBody attackerBody = attackerMaster.GetBody();
			TeamComponent attackerTeam = attackerBody?.GetComponent<TeamComponent>();
			if (attackerTeam == null || attackerTeam.teamIndex != TeamIndex.Player)
			{
				Log.Warning("[LunarRitual] Attacker is not on player team");
				return;
			}

			Log.Warning($"[LunarRitual] Attacker is player: {attackerMaster.GetBody()?.GetUserName() ?? "Unknown"}");

			NetworkUser attackerUser = attackerMaster.playerCharacterMasterController?.networkUser;
			if (attackerUser == null)
			{
				Log.Warning("[LunarRitual] playerCharacterMasterController.networkUser is null, trying fallback");
				foreach (NetworkUser user in NetworkUser.readOnlyLocalPlayersList)
				{
					if (user.master == attackerMaster)
					{
						attackerUser = user;
						break;
					}
				}
				if (attackerUser == null)
				{
					Log.Warning("[LunarRitual] Could not find attackerUser");
					return;
				}
			}

			ulong steamId = attackerUser.id.value;
			Log.Warning($"[LunarRitual] Attacker steamId: {steamId}");

			if (!playerShardChance.ContainsKey(steamId))
			{
				playerShardChance[steamId] = LunarRitual.shardChance.Value;
				Log.Warning($"[LunarRitual] Initial shard chance set to {LunarRitual.shardChance.Value}");
			}

			float roll = UnityEngine.Random.Range(0f, 1f);
			Log.Warning($"[LunarRitual] Roll: {roll:F3}, Chance: {playerShardChance[steamId]:F3}");
			
			if (roll < playerShardChance[steamId])
			{
				Log.Warning("[LunarRitual] SHARD DROP TRIGGERED!");
				
				if (LunarRitual.noShardDroplet.Value)
				{
					Log.Warning("[LunarRitual] Using auto-collect mode (no droplet)");
					if (LunarRitual.teamShards.Value)
					{
						foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
						{
							NetworkUser user = instance.networkUser;
							if (user != null)
							{
								ulong playerId = user.id.value;
								GenesisShards.AddShards(playerId, 1);
								Log.Warning($"[LunarRitual] Added 1 shard to player {playerId}");
							}
						}
					}
					else
					{
						GenesisShards.AddShards(steamId, 1);
						Log.Warning($"[LunarRitual] Added 1 shard to attacker {steamId}");
					}
				}
				else
				{
					Log.Warning("[LunarRitual] Creating physical shard droplet");
					SpawnShardDroplet(damageReport);
					Log.Warning($"[LunarRitual] Droplet created");
				}

				Log.Info($"[LunarRitual] Genesis Shard dropped by {attackerMaster.GetBody()?.GetUserName() ?? "Unknown"}");

				GenesisShardsUI.RefreshUI();
			}
			else
			{
				Log.Warning("[LunarRitual] Shard drop failed (roll was too high)");
			}
		}

		private static void SpawnShardDroplet(DamageReport damageReport)
		{
			Vector3 spawnPos = damageReport.victimBody.corePosition;
			Vector3 dropPosition = spawnPos + Vector3.up * 2f;

			PickupIndex shardIndex = PickupCatalog.FindPickupIndex("LunarCoin.Coin0");

			PickupDropletController.CreatePickupDroplet(shardIndex, dropPosition, Vector3.zero);

			Log.Warning($"[LunarRitual] Shard droplet created at {dropPosition}");
		}

		private static void OnLunarCoinPickedUp(On.RoR2.NetworkUser.orig_RpcAwardLunarCoins orig, RoR2.NetworkUser self, uint count)
		{
			orig(self, count);

			if (NetworkServer.active && count > 0)
			{
				ulong steamId = self.id.value;
				GenesisShards.AddShards(steamId, (int)count);
				Log.Warning($"[LunarRitual] Lunar coin picked up, added {count} Genesis Shards to player {steamId}");
				GenesisShardsUI.RefreshUI();
			}
		}

		private static void SaveShardsOnRunEnd(On.RoR2.Run.orig_OnDestroy orig, Run self)
		{
			GenesisShards.SaveShards();
			playerShardChance.Clear();
			orig(self);
		}

		private static void SaveShardsProperSave(Dictionary<string, object> dict)
		{
			string jsonString = GenesisShards.GetShardsForProperSave();
			dict.Add("GenesisShardsObj", jsonString);
		}
	}

	public class GenesisShardEffect : MonoBehaviour
	{
		private float duration = 2f;
		private float radius = 2f;
		private ParticleSystem particleSystem;
		private Light light;

		public void Initialize(float enemyRadius)
		{
			radius = enemyRadius * 2f;
			CreateParticleSystem();
			CreateLight();
		}

		private void CreateParticleSystem()
		{
			GameObject particleObj = new GameObject("ShardParticles");
			particleObj.transform.SetParent(transform, false);

			particleSystem = particleObj.AddComponent<ParticleSystem>();

			var main = particleSystem.main;
			main.duration = duration;
			main.loop = false;
			main.startLifetime = duration;
			main.startSpeed = 3f;
			main.startSize = 0.5f;
			main.startColor = new Color(0.8f, 0.2f, 1f, 1f);
			main.maxParticles = 50;
			main.gravityModifier = 0.5f;

			var emission = particleSystem.emission;
			emission.rateOverTime = 0f;
			emission.SetBursts(new ParticleSystem.Burst[]
			{
				new ParticleSystem.Burst(0f, 20)
			});

			var shape = particleSystem.shape;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = radius * 0.5f;

			var colorOverLifetime = particleSystem.colorOverLifetime;
			var gradient = new Gradient();
			gradient.SetKeys(
				new GradientColorKey[] { new GradientColorKey(new Color(0.8f, 0.2f, 1f), 0f), new GradientColorKey(new Color(0.5f, 0.1f, 0.8f), 1f) },
				new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
			);
			colorOverLifetime.color = gradient;
		}

		private void CreateLight()
		{
			GameObject lightObj = new GameObject("ShardLight");
			lightObj.transform.SetParent(transform, false);
			lightObj.transform.position = Vector3.zero;

			light = lightObj.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = new Color(0.8f, 0.2f, 1f);
			light.range = radius * 3f;
			light.intensity = 2f;
		}

		private void Update()
		{
			if (light != null)
			{
				light.intensity = Mathf.Lerp(2f, 0f, Time.deltaTime / duration);
			}
		}
	}
}
