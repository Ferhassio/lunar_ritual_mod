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
		On.RoR2.GenericPickupController.OnInteractionBegin += OnPickupInteractionBegin;

		Log.Warning("[LunarRitual] Hooks initialized");
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

			Log.Warning("[LunarRitual] Shard effect prefab created");
		}

	private static void InitializePlayers(On.RoR2.Run.orig_Start orig, Run self)
		{
			if (!NetworkServer.active)
			{
				orig(self);
				return;
			}

			if (LunarRitual.resetShards.Value)
			{
				if (NetworkUser.readOnlyInstancesList != null)
				{
					foreach (var user in NetworkUser.readOnlyInstancesList)
					{
						ulong steamId = user.id.value;
						GenesisShards.SetShards(steamId, LunarRitual.startingShards.Value);
					}
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

				Log.Warning($"[LunarRitual] Genesis Shard dropped by {attackerMaster.GetBody()?.GetUserName() ?? "Unknown"}");

				GenesisShardsUI.RefreshUI();
			}
			else
			{
				Log.Warning("[LunarRitual] Shard drop failed (roll was too high)");
			}
		}

		private static void SpawnShardDroplet(DamageReport damageReport)
		{
			PickupIndex pickupIndex = GenesisShards.GenesisShardPickupIndex;

			if (pickupIndex == PickupIndex.none && GenesisShards.GenesisShardPickupDef != null)
			{
				try
				{
					MiscPickupIndex miscPickupIndex = GenesisShards.GenesisShardPickupDef.miscPickupIndex;
					pickupIndex = PickupCatalog.FindPickupIndex(miscPickupIndex);
					GenesisShards.GenesisShardPickupIndex = pickupIndex;
					Log.Warning($"[LunarRitual] Dynamically initialized GenesisShardPickupIndex: {pickupIndex}");
					if (pickupIndex == PickupIndex.none)
					{
						Log.Error("[LunarRitual] Failed to find PickupIndex for GenesisShard dynamically");
						return;
					}
				}
				catch (Exception ex)
				{
					Log.Error($"[LunarRitual] Exception while getting PickupIndex: {ex.Message}");
					return;
				}
			}

			if (pickupIndex == PickupIndex.none)
			{
				Log.Error("[LunarRitual] GenesisShardPickupIndex is not initialized and GenesisShardPickupDef is null!");
				return;
			}

			if (GenesisShards.genesisShardPrefab == null)
			{
				Log.Warning("[LunarRitual] genesisShardPrefab is null, calling UpdateGenesisShardPrefab");
				GenesisShards.UpdateGenesisShardPrefab();
				if (GenesisShards.genesisShardPrefab == null)
				{
					Log.Error("[LunarRitual] genesisShardPrefab is still null after UpdateGenesisShardPrefab!");
					return;
				}
			}

			Vector3 spawnPos = damageReport.victimBody.corePosition + Vector3.up * 3f;
			
			GameObject pickupGameObject = GameObject.Instantiate(GenesisShards.genesisShardPrefab, spawnPos, Quaternion.identity);
			GenericPickupController pickupController = pickupGameObject.GetComponent<GenericPickupController>();
			if (pickupController != null)
			{
				pickupController.pickupIndex = pickupIndex;
				pickupGameObject.transform.position = spawnPos;
				NetworkServer.Spawn(pickupGameObject);
				Log.Warning($"[LunarRitual] Genesis Shard pickup created directly at {spawnPos}");
			}
			else
			{
				Log.Error("[LunarRitual] Failed to get GenericPickupController from Genesis Shard prefab!");
				GameObject.Destroy(pickupGameObject);
			}
		}

		private static void OnPickupInteractionBegin(On.RoR2.GenericPickupController.orig_OnInteractionBegin orig, GenericPickupController self, Interactor activator)
	{
		Log.Warning($"[LunarRitual] OnPickupInteractionBegin called - pickupIndex: {self.pickupIndex.value}, GenesisShardPickupIndex: {GenesisShards.GenesisShardPickupIndex.value}");
		Log.Warning($"[LunarRitual] Is Genesis Shard: {self.pickupIndex == GenesisShards.GenesisShardPickupIndex}");
		Log.Warning($"[LunarRitual] NetworkServer.active: {NetworkServer.active}, self.gameObject: {(self.gameObject != null ? "exists" : "null")}");
		
		orig(self, activator);
		
		if (self.pickupIndex == GenesisShards.GenesisShardPickupIndex)
		{
			Log.Warning("[LunarRitual] Genesis Shard interaction begin detected, destroying object");
			if (NetworkServer.active && self.gameObject != null)
			{
				UnityEngine.Object.Destroy(self.gameObject);
				Log.Warning("[LunarRitual] Genesis Shard object destroyed");
			}
			else
			{
				Log.Error($"[LunarRitual] Cannot destroy Genesis Shard - NetworkServer.active: {NetworkServer.active}, gameObject exists: {self.gameObject != null}");
			}
		}
	}

		private static void SaveShardsOnRunEnd(On.RoR2.Run.orig_OnDestroy orig, Run self)
		{
			GenesisShards.SaveShards();
			playerShardChance.Clear();
			orig(self);
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

	public class GenesisShardMarker : MonoBehaviour
	{
	}
}
