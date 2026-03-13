using System;
using System.Collections.Generic;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace LunarRitual
{
	public static class RitualMenu
	{
		private static bool initialized;
		private static bool shownThisRun;
		private static readonly HashSet<ulong> serverConsumedThisRun = new HashSet<ulong>();

		private static GameObject uiRoot;
		private static GameObject bootstrapObj;
		private static StageStartBootstrap bootstrap;
		private static HGTextMeshProUGUI shardsText;
		private static HGTextMeshProUGUI ritualTitleText;
		private static HGTextMeshProUGUI ritualDescText;
		private static Button smallBtn;
		private static Button mediumBtn;
		private static Button grandBtn;
		private static Canvas canvas;

		private static readonly int SmallCost = 1;
		private static readonly int MediumCost = 5;
		private static readonly int GrandCost = 10;

		private static ExplicitPickupDropTable tier1DropTable;
		private static ExplicitPickupDropTable tier2DropTable;
		private static ExplicitPickupDropTable tier3DropTable;

		private static RitualType selectedRitual = RitualType.Essence;

		public static void Initialize()
		{
			if (initialized) return;
			initialized = true;

			NetworkingAPI.RegisterMessageType<RitualOfEssenceRequest>();
			NetworkingAPI.RegisterMessageType<RitualOfEgoRequest>();
			NetworkingAPI.RegisterMessageType<RitualOfLightnessRequest>();
			Log.Info("[LunarRitual] RitualMenu initialized + message registered");

			EnsureBootstrap();

			Run.onRunStartGlobal += OnRunStart;
			Run.onRunDestroyGlobal += OnRunDestroy;
			Stage.onStageStartGlobal += OnStageStartGlobal;
		}

		private static void OnRunStart(Run run)
		{
			shownThisRun = false;
			if (NetworkServer.active)
			{
				serverConsumedThisRun.Clear();
			}
		}

		private static void OnRunDestroy(Run run)
		{
			shownThisRun = false;
			if (NetworkServer.active)
			{
				serverConsumedThisRun.Clear();
			}
			DestroyUI();
		}

		private static void OnStageStartGlobal(Stage stage)
		{
			if (shownThisRun) return;
			if (Run.instance == null) return;
			if (Run.instance.stageClearCount != 0) return; // only once per run, on first stage

			EnsureBootstrap();
			bootstrap.BeginTryShow();
		}

		private static void EnsureBootstrap()
		{
			if (bootstrap != null) return;
			if (bootstrapObj != null)
			{
				bootstrap = bootstrapObj.GetComponent<StageStartBootstrap>();
				if (bootstrap != null) return;
			}

			bootstrapObj = new GameObject("LunarRitual_RitualMenuBootstrap");
			UnityEngine.Object.DontDestroyOnLoad(bootstrapObj);
			bootstrap = bootstrapObj.AddComponent<StageStartBootstrap>();
		}

		private static void CreateAndShowIfNeeded()
		{
			if (shownThisRun) return;
			if (NetworkUser.readOnlyLocalPlayersList == null || NetworkUser.readOnlyLocalPlayersList.Count <= 0) return;

			var user = NetworkUser.readOnlyLocalPlayersList[0];
			if (user == null) return;
			if (!user.master) return;
			if (user.master.GetBody() == null) return; // wait until the run is actually playable

			shownThisRun = true;
			CreateUI();
			RefreshShardsText();
		}

		private class StageStartBootstrap : MonoBehaviour
		{
			private float remaining;

			public void BeginTryShow()
			{
				// try for a few seconds, to survive load hiccups / late spawning
				remaining = 8f;
				enabled = true;
			}

			private void Awake()
			{
				enabled = false;
			}

			private void Update()
			{
				if (shownThisRun)
				{
					enabled = false;
					return;
				}

				remaining -= Time.unscaledDeltaTime;
				if (remaining <= 0f)
				{
					enabled = false;
					return;
				}

				CreateAndShowIfNeeded();
			}
		}

		private static void CreateUI()
		{
			if (uiRoot != null) return;

			GameObject canvasObj = new GameObject("LunarRitual_RitualMenuCanvas");
			canvas = canvasObj.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 200;
			
			var scaler = canvasObj.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0.5f;
			
			canvasObj.AddComponent<GraphicRaycaster>();

			uiRoot = canvasObj;

			var bg = new GameObject("Background");
			bg.transform.SetParent(uiRoot.transform, false);
			var bgImage = bg.AddComponent<Image>();
			bgImage.color = new Color(0f, 0f, 0f, 0.55f);
			var bgRect = bg.GetComponent<RectTransform>();
			bgRect.anchorMin = new Vector2(0f, 0f);
			bgRect.anchorMax = new Vector2(1f, 1f);
			bgRect.offsetMin = Vector2.zero;
			bgRect.offsetMax = Vector2.zero;

			var panel = new GameObject("Panel");
			panel.transform.SetParent(bg.transform, false);
			var panelImage = panel.AddComponent<Image>();
			panelImage.color = new Color(0.08f, 0.05f, 0.12f, 0.95f);
			var panelRect = panel.GetComponent<RectTransform>();
			panelRect.anchorMin = new Vector2(0.5f, 0.5f);
			panelRect.anchorMax = new Vector2(0.5f, 0.5f);
			panelRect.pivot = new Vector2(0.5f, 0.5f);
			panelRect.sizeDelta = new Vector2(900f, 500f);
			panelRect.anchoredPosition = Vector2.zero;

			var titleObj = new GameObject("Title");
			titleObj.transform.SetParent(panel.transform, false);
			var title = titleObj.AddComponent<HGTextMeshProUGUI>();
			title.fontSize = 42;
			title.color = new Color(0.92f, 0.85f, 1f, 1f);
			title.fontStyle = TMPro.FontStyles.Bold;
			title.alignment = TMPro.TextAlignmentOptions.Left;
			title.text = "Lunar Ritual";
			if (title.font == null)
			{
				title.font = Resources.Load<TMPro.TMP_FontAsset>("fonts & materials/LiberationSans SDF");
			}
			var titleRect = titleObj.GetComponent<RectTransform>();
			titleRect.anchorMin = new Vector2(0f, 1f);
			titleRect.anchorMax = new Vector2(1f, 1f);
			titleRect.pivot = new Vector2(0.5f, 1f);
			titleRect.sizeDelta = new Vector2(-50f, 60f);
			titleRect.anchoredPosition = new Vector2(0f, -20f);

			var shardsObj = new GameObject("ShardsText");
			shardsObj.transform.SetParent(panel.transform, false);
			shardsText = shardsObj.AddComponent<HGTextMeshProUGUI>();
			shardsText.fontSize = 28;
			shardsText.color = new Color(1f, 0.55f, 0.55f, 1f);
			shardsText.fontStyle = TMPro.FontStyles.Bold;
			shardsText.alignment = TMPro.TextAlignmentOptions.Left;
			shardsText.text = "Genesis Shards: ?";
			if (shardsText.font == null)
			{
				shardsText.font = Resources.Load<TMPro.TMP_FontAsset>("fonts & materials/LiberationSans SDF");
			}
			var shardsRect = shardsObj.GetComponent<RectTransform>();
			shardsRect.anchorMin = new Vector2(0f, 1f);
			shardsRect.anchorMax = new Vector2(1f, 1f);
			shardsRect.pivot = new Vector2(0.5f, 1f);
			shardsRect.sizeDelta = new Vector2(-50f, 45f);
			shardsRect.anchoredPosition = new Vector2(0f, -90f);

			// Ritual selector row
			var ritualTabs = new GameObject("RitualTabs");
			ritualTabs.transform.SetParent(panel.transform, false);
			var tabsRect = ritualTabs.AddComponent<RectTransform>();
			tabsRect.anchorMin = new Vector2(0.5f, 1f);
			tabsRect.anchorMax = new Vector2(0.5f, 1f);
			tabsRect.pivot = new Vector2(0.5f, 1f);
			tabsRect.sizeDelta = new Vector2(800f, 56f);
			tabsRect.anchoredPosition = new Vector2(0f, -140f);

			CreateTabButton(ritualTabs.transform, "Essence", new Vector2(-240f, 0f), () => SelectRitual(RitualType.Essence));
			CreateTabButton(ritualTabs.transform, "Ego", new Vector2(0f, 0f), () => SelectRitual(RitualType.Ego));
			CreateTabButton(ritualTabs.transform, "Lightness", new Vector2(240f, 0f), () => SelectRitual(RitualType.Lightness));

			var ritualTitleObj = new GameObject("RitualTitle");
			ritualTitleObj.transform.SetParent(panel.transform, false);
			ritualTitleText = ritualTitleObj.AddComponent<HGTextMeshProUGUI>();
			ritualTitleText.fontSize = 32;
			ritualTitleText.color = Color.white;
			ritualTitleText.fontStyle = TMPro.FontStyles.Bold;
			ritualTitleText.alignment = TMPro.TextAlignmentOptions.Left;
			if (ritualTitleText.font == null)
			{
				ritualTitleText.font = Resources.Load<TMPro.TMP_FontAsset>("fonts & materials/LiberationSans SDF");
			}
			var ritualTitleRect = ritualTitleObj.GetComponent<RectTransform>();
			ritualTitleRect.anchorMin = new Vector2(0f, 1f);
			ritualTitleRect.anchorMax = new Vector2(1f, 1f);
			ritualTitleRect.pivot = new Vector2(0.5f, 1f);
			ritualTitleRect.sizeDelta = new Vector2(-50f, 45f);
			ritualTitleRect.anchoredPosition = new Vector2(0f, -205f);

			var ritualDescObj = new GameObject("RitualDesc");
			ritualDescObj.transform.SetParent(panel.transform, false);
			ritualDescText = ritualDescObj.AddComponent<HGTextMeshProUGUI>();
			ritualDescText.fontSize = 22;
			ritualDescText.color = new Color(0.9f, 0.9f, 0.95f, 1f);
			ritualDescText.alignment = TMPro.TextAlignmentOptions.Left;
			if (ritualDescText.font == null)
			{
				ritualDescText.font = Resources.Load<TMPro.TMP_FontAsset>("fonts & materials/LiberationSans SDF");
			}
			var ritualDescRect = ritualDescObj.GetComponent<RectTransform>();
			ritualDescRect.anchorMin = new Vector2(0f, 1f);
			ritualDescRect.anchorMax = new Vector2(1f, 1f);
			ritualDescRect.pivot = new Vector2(0.5f, 1f);
			ritualDescRect.sizeDelta = new Vector2(-50f, 90f);
			ritualDescRect.anchoredPosition = new Vector2(0f, -255f);

			var buttonsRow = new GameObject("ButtonsRow");
			buttonsRow.transform.SetParent(panel.transform, false);
			var rowRect = buttonsRow.AddComponent<RectTransform>();
			rowRect.anchorMin = new Vector2(0.5f, 0f);
			rowRect.anchorMax = new Vector2(0.5f, 0f);
			rowRect.pivot = new Vector2(0.5f, 0f);
			rowRect.sizeDelta = new Vector2(800f, 80f);
			rowRect.anchoredPosition = new Vector2(0f, 24f);

			smallBtn = CreateOfferButton(buttonsRow.transform, "Small Offering (1)", new Vector2(-270f, 0f), () => RequestSelectedRitual(OfferingTier.Small));
			mediumBtn = CreateOfferButton(buttonsRow.transform, "Medium Offering (5)", new Vector2(0f, 0f), () => RequestSelectedRitual(OfferingTier.Medium));
			grandBtn = CreateOfferButton(buttonsRow.transform, "Grand Offering (10)", new Vector2(270f, 0f), () => RequestSelectedRitual(OfferingTier.Grand));

			CreateCloseButton(panel.transform);

			SelectRitual(RitualType.Essence);
		}

		private static Button CreateOfferButton(Transform parent, string label, Vector2 anchoredPos, Action onClick)
		{
			var btnObj = new GameObject(label.Replace(" ", "_"));
			btnObj.transform.SetParent(parent, false);

			var img = btnObj.AddComponent<Image>();
			img.color = new Color(0.18f, 0.12f, 0.28f, 1f);

			var btn = btnObj.AddComponent<Button>();
			btn.onClick.AddListener(() => onClick?.Invoke());

			var rect = btnObj.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = new Vector2(240f, 64f);
			rect.anchoredPosition = anchoredPos;

			var textObj = new GameObject("Text");
			textObj.transform.SetParent(btnObj.transform, false);
			var text = textObj.AddComponent<HGTextMeshProUGUI>();
			text.fontSize = 20;
			text.color = Color.white;
			text.fontStyle = TMPro.FontStyles.Bold;
			text.alignment = TMPro.TextAlignmentOptions.Center;
			text.text = label;
			if (text.font == null)
			{
				text.font = Resources.Load<TMPro.TMP_FontAsset>("fonts & materials/LiberationSans SDF");
			}
			var textRect = textObj.GetComponent<RectTransform>();
			textRect.anchorMin = new Vector2(0f, 0f);
			textRect.anchorMax = new Vector2(1f, 1f);
			textRect.offsetMin = Vector2.zero;
			textRect.offsetMax = Vector2.zero;

			return btn;
		}

		private static void CreateCloseButton(Transform panel)
		{
			var closeObj = new GameObject("CloseButton");
			closeObj.transform.SetParent(panel, false);
			var img = closeObj.AddComponent<Image>();
			img.color = new Color(0.12f, 0.12f, 0.12f, 1f);

			var btn = closeObj.AddComponent<Button>();
			btn.onClick.AddListener(Close);

			var rect = closeObj.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(1f, 1f);
			rect.anchorMax = new Vector2(1f, 1f);
			rect.pivot = new Vector2(1f, 1f);
			rect.sizeDelta = new Vector2(130f, 48f);
			rect.anchoredPosition = new Vector2(-20f, -20f);

			var textObj = new GameObject("Text");
			textObj.transform.SetParent(closeObj.transform, false);
			var text = textObj.AddComponent<HGTextMeshProUGUI>();
			text.fontSize = 20;
			text.color = Color.white;
			text.fontStyle = TMPro.FontStyles.Bold;
			text.alignment = TMPro.TextAlignmentOptions.Center;
			text.text = "Close";
			if (text.font == null)
			{
				text.font = Resources.Load<TMPro.TMP_FontAsset>("fonts & materials/LiberationSans SDF");
			}
			var textRect = textObj.GetComponent<RectTransform>();
			textRect.anchorMin = new Vector2(0f, 0f);
			textRect.anchorMax = new Vector2(1f, 1f);
			textRect.offsetMin = Vector2.zero;
			textRect.offsetMax = Vector2.zero;
		}

		private static void CreateTabButton(Transform parent, string label, Vector2 anchoredPos, Action onClick)
		{
			var btnObj = new GameObject($"Tab_{label}");
			btnObj.transform.SetParent(parent, false);

			var img = btnObj.AddComponent<Image>();
			img.color = new Color(0.12f, 0.08f, 0.18f, 1f);

			var btn = btnObj.AddComponent<Button>();
			btn.onClick.AddListener(() => onClick?.Invoke());

			var rect = btnObj.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = new Vector2(230f, 48f);
			rect.anchoredPosition = anchoredPos;

			var textObj = new GameObject("Text");
			textObj.transform.SetParent(btnObj.transform, false);
			var text = textObj.AddComponent<HGTextMeshProUGUI>();
			text.fontSize = 20;
			text.color = Color.white;
			text.fontStyle = TMPro.FontStyles.Bold;
			text.alignment = TMPro.TextAlignmentOptions.Center;
			text.text = label;
			if (text.font == null)
			{
				text.font = Resources.Load<TMPro.TMP_FontAsset>("fonts & materials/LiberationSans SDF");
			}
			var textRect = textObj.GetComponent<RectTransform>();
			textRect.anchorMin = new Vector2(0f, 0f);
			textRect.anchorMax = new Vector2(1f, 1f);
			textRect.offsetMin = Vector2.zero;
			textRect.offsetMax = Vector2.zero;
		}

		private static void SelectRitual(RitualType ritual)
		{
			selectedRitual = ritual;
			if (ritualTitleText == null || ritualDescText == null) return;

			switch (selectedRitual)
			{
				case RitualType.Essence:
					ritualTitleText.text = "Ritual of Essence";
					ritualDescText.text = "Receive a random item.\n1: Common • 5: Uncommon • 10: Legendary";
					SetOfferingButtonsActive(true, true, true);
					break;
				case RitualType.Ego:
					ritualTitleText.text = "Ritual of Ego";
					ritualDescText.text = "Gain stacks of Egocentrism (LunarSun).\n1: 1–3 • 5: 4–6 • 10: 7–10";
					SetOfferingButtonsActive(true, true, true);
					break;
				case RitualType.Lightness:
					ritualTitleText.text = "Ritual of Lightness";
					ritualDescText.text = "Gain stacks of Hopoo Feathers.\n1: 1 • 5: 2–4 • 10: 5–10";
					SetOfferingButtonsActive(true, true, true);
					break;
			}
		}

		private static void SetOfferingButtonsActive(bool small, bool medium, bool grand)
		{
			if (smallBtn) smallBtn.interactable = small;
			if (mediumBtn) mediumBtn.interactable = medium;
			if (grandBtn) grandBtn.interactable = grand;
		}

		private static void RequestSelectedRitual(OfferingTier tier)
		{
			switch (selectedRitual)
			{
				case RitualType.Essence:
					RequestEssence(tier);
					break;
				case RitualType.Ego:
					RequestEgo(tier);
					break;
				case RitualType.Lightness:
					RequestLightness(tier);
					break;
			}
		}

		private static void RequestEssence(OfferingTier tier)
		{
			Log.Info($"[LunarRitual] Ritual of Essence clicked: {tier}. NetworkServer.active={NetworkServer.active}");
			RefreshShardsText();

			if (NetworkUser.readOnlyLocalPlayersList == null || NetworkUser.readOnlyLocalPlayersList.Count <= 0)
			{
				Log.Warning("[LunarRitual] Ritual click ignored: no local players");
				return;
			}
			var user = NetworkUser.readOnlyLocalPlayersList[0];
			if (user == null)
			{
				Log.Warning("[LunarRitual] Ritual click ignored: local user is null");
				return;
			}

			Log.Info($"[LunarRitual] Ritual click: local steamId={user.id.value}, netId={user.netId}");

			var msg = new RitualOfEssenceRequest
			{
				networkUserNetId = user.netId,
				tier = tier
			};

			if (NetworkServer.active)
			{
				Log.Info("[LunarRitual] Handling ritual locally (host)");
				msg.OnReceived();
			}
			else
			{
				Log.Info("[LunarRitual] Sending ritual request to server");
				msg.Send(NetworkDestination.Server);
			}

			InvokeDelayed(0.15f, RefreshShardsText);
			Close();
		}

		private static void RequestEgo(OfferingTier tier)
		{
			Log.Info($"[LunarRitual] Ritual of Ego clicked: {tier}. NetworkServer.active={NetworkServer.active}");
			RefreshShardsText();

			if (NetworkUser.readOnlyLocalPlayersList == null || NetworkUser.readOnlyLocalPlayersList.Count <= 0)
			{
				Log.Warning("[LunarRitual] Ritual click ignored: no local players");
				return;
			}
			var user = NetworkUser.readOnlyLocalPlayersList[0];
			if (user == null)
			{
				Log.Warning("[LunarRitual] Ritual click ignored: local user is null");
				return;
			}

			Log.Info($"[LunarRitual] Ritual click: local steamId={user.id.value}, netId={user.netId}");

			var msg = new RitualOfEgoRequest
			{
				networkUserNetId = user.netId,
				tier = tier
			};

			if (NetworkServer.active)
			{
				Log.Info("[LunarRitual] Handling ritual locally (host)");
				msg.OnReceived();
			}
			else
			{
				Log.Info("[LunarRitual] Sending ritual request to server");
				msg.Send(NetworkDestination.Server);
			}

			InvokeDelayed(0.15f, RefreshShardsText);
			Close();
		}

		private static void RequestLightness(OfferingTier tier)
		{
			Log.Info($"[LunarRitual] Ritual of Lightness clicked: {tier}. NetworkServer.active={NetworkServer.active}");
			RefreshShardsText();

			if (NetworkUser.readOnlyLocalPlayersList == null || NetworkUser.readOnlyLocalPlayersList.Count <= 0)
			{
				Log.Warning("[LunarRitual] Ritual click ignored: no local players");
				return;
			}
			var user = NetworkUser.readOnlyLocalPlayersList[0];
			if (user == null)
			{
				Log.Warning("[LunarRitual] Ritual click ignored: local user is null");
				return;
			}

			Log.Info($"[LunarRitual] Ritual click: local steamId={user.id.value}, netId={user.netId}");

			var msg = new RitualOfLightnessRequest
			{
				networkUserNetId = user.netId,
				tier = tier
			};

			if (NetworkServer.active)
			{
				Log.Info("[LunarRitual] Handling ritual locally (host)");
				msg.OnReceived();
			}
			else
			{
				Log.Info("[LunarRitual] Sending ritual request to server");
				msg.Send(NetworkDestination.Server);
			}

			InvokeDelayed(0.15f, RefreshShardsText);
			Close();
		}

		private static void RefreshShardsText()
		{
			if (shardsText == null) return;
			if (NetworkUser.readOnlyLocalPlayersList == null || NetworkUser.readOnlyLocalPlayersList.Count <= 0) return;
			var user = NetworkUser.readOnlyLocalPlayersList[0];
			if (user == null) return;
			ulong steamId = user.id.value;
			shardsText.text = $"Genesis Shards: {GenesisShards.GetShards(steamId)}";
		}

		private static void Close()
		{
			DestroyUI();
		}

		private static void DestroyUI()
		{
			if (uiRoot != null)
			{
				UnityEngine.Object.Destroy(uiRoot);
				uiRoot = null;
				shardsText = null;
			}

			if (canvas != null)
			{
				UnityEngine.Object.Destroy(canvas.gameObject);
				canvas = null;
			}
		}

		private static void InvokeDelayed(float delay, Action action)
		{
			if (!uiRoot) return;
			uiRoot.AddComponent<DelayedInvoker>().Init(delay, action);
		}

		private class DelayedInvoker : MonoBehaviour
		{
			private float t;
			private Action action;

			public void Init(float delay, Action action)
			{
				t = delay;
				this.action = action;
			}

			private void Update()
			{
				t -= Time.unscaledDeltaTime;
				if (t > 0f) return;
				try { action?.Invoke(); }
				finally { Destroy(this); }
			}
		}

		private enum OfferingTier : byte
		{
			Small = 0,
			Medium = 1,
			Grand = 2
		}

		private struct RitualOfEssenceRequest : INetMessage
		{
			public NetworkInstanceId networkUserNetId;
			public OfferingTier tier;

			public void Serialize(NetworkWriter writer)
			{
				writer.Write(networkUserNetId);
				writer.Write((byte)tier);
			}

			public void Deserialize(NetworkReader reader)
			{
				networkUserNetId = reader.ReadNetworkId();
				tier = (OfferingTier)reader.ReadByte();
			}

			public void OnReceived()
			{
				Log.Info($"[LunarRitual] RitualOfEssenceRequest.OnReceived. NetworkServer.active={NetworkServer.active}, netId={networkUserNetId.Value}, tier={tier}");
				if (!NetworkServer.active)
				{
					Log.Warning("[LunarRitual] OnReceived ignored: not server");
					return;
				}

				GameObject userObj = NetworkServer.FindLocalObject(networkUserNetId);
				if (!userObj)
				{
					Log.Warning("[LunarRitual] OnReceived: FindLocalObject returned null (bad netId?)");
					return;
				}

				NetworkUser user = userObj.GetComponent<NetworkUser>();
				if (!user)
				{
					Log.Warning("[LunarRitual] OnReceived: NetworkUser component missing on object");
					return;
				}

				ulong steamId = user.id.value;
				if (serverConsumedThisRun.Contains(steamId))
				{
					Log.Info($"[LunarRitual] OnReceived: ritual already consumed this run. steamId={steamId}");
					return;
				}
				int cost = tier switch
				{
					OfferingTier.Small => SmallCost,
					OfferingTier.Medium => MediumCost,
					OfferingTier.Grand => GrandCost,
					_ => 0
				};

				if (cost <= 0)
				{
					Log.Warning("[LunarRitual] OnReceived: cost <= 0 (invalid tier)");
					return;
				}

				int shards = GenesisShards.GetShards(steamId);
				if (shards < cost)
				{
					Log.Info($"[LunarRitual] OnReceived: not enough shards. steamId={steamId} shards={shards} cost={cost}");
					return;
				}

				var master = user.master;
				if (!master || master.inventory == null)
				{
					Log.Warning("[LunarRitual] OnReceived: user.master or inventory is null");
					return;
				}

				ItemIndex item = RitualOfEssenceServerRollItem(tier);
				if (item == ItemIndex.None)
				{
					Log.Warning("[LunarRitual] OnReceived: failed to roll item (ItemIndex.None)");
					return;
				}

				Log.Info($"[LunarRitual] OnReceived: granting item={item} cost={cost} steamId={steamId}");
				GenesisShards.RemoveShards(steamId, cost);
				master.inventory.GiveItem(item, 1);
				serverConsumedThisRun.Add(steamId);

				GenesisShards.SaveShards();
				GenesisShardsUI.RefreshUI();
			}
		}

		private struct RitualOfEgoRequest : INetMessage
		{
			public NetworkInstanceId networkUserNetId;
			public OfferingTier tier;

			public void Serialize(NetworkWriter writer)
			{
				writer.Write(networkUserNetId);
				writer.Write((byte)tier);
			}

			public void Deserialize(NetworkReader reader)
			{
				networkUserNetId = reader.ReadNetworkId();
				tier = (OfferingTier)reader.ReadByte();
			}

			public void OnReceived()
			{
				Log.Info($"[LunarRitual] RitualOfEgoRequest.OnReceived. NetworkServer.active={NetworkServer.active}, netId={networkUserNetId.Value}, tier={tier}");
				if (!NetworkServer.active) return;

				GameObject userObj = NetworkServer.FindLocalObject(networkUserNetId);
				if (!userObj) return;

				NetworkUser user = userObj.GetComponent<NetworkUser>();
				if (!user) return;

				ulong steamId = user.id.value;
				if (serverConsumedThisRun.Contains(steamId))
				{
					Log.Info($"[LunarRitual] Ego: ritual already consumed this run. steamId={steamId}");
					return;
				}

				int cost = tier switch
				{
					OfferingTier.Small => SmallCost,
					OfferingTier.Medium => MediumCost,
					OfferingTier.Grand => GrandCost,
					_ => 0
				};
				if (cost <= 0) return;

				int shards = GenesisShards.GetShards(steamId);
				if (shards < cost)
				{
					Log.Info($"[LunarRitual] Ego: not enough shards. steamId={steamId} shards={shards} cost={cost}");
					return;
				}

				var master = user.master;
				if (!master || master.inventory == null) return;

				ItemIndex egoItem = ItemCatalog.FindItemIndex("LunarSun");
				if (egoItem == ItemIndex.None) return;

				int min = tier switch
				{
					OfferingTier.Small => 1,
					OfferingTier.Medium => 4,
					OfferingTier.Grand => 7,
					_ => 0
				};
				int maxInclusive = tier switch
				{
					OfferingTier.Small => 3,
					OfferingTier.Medium => 6,
					OfferingTier.Grand => 10,
					_ => 0
				};
				if (min <= 0 || maxInclusive < min) return;

				int stacks = Run.instance != null
					? Run.instance.treasureRng.RangeInt(min, maxInclusive + 1)
					: UnityEngine.Random.Range(min, maxInclusive + 1);

				Log.Info($"[LunarRitual] Ego: granting {stacks}x {egoItem} cost={cost} steamId={steamId}");
				GenesisShards.RemoveShards(steamId, cost);
				master.inventory.GiveItem(egoItem, stacks);
				serverConsumedThisRun.Add(steamId);

				GenesisShards.SaveShards();
				GenesisShardsUI.RefreshUI();
			}
		}

		private struct RitualOfLightnessRequest : INetMessage
		{
			public NetworkInstanceId networkUserNetId;
			public OfferingTier tier;

			public void Serialize(NetworkWriter writer)
			{
				writer.Write(networkUserNetId);
				writer.Write((byte)tier);
			}

			public void Deserialize(NetworkReader reader)
			{
				networkUserNetId = reader.ReadNetworkId();
				tier = (OfferingTier)reader.ReadByte();
			}

			public void OnReceived()
			{
				Log.Info($"[LunarRitual] RitualOfLightnessRequest.OnReceived. NetworkServer.active={NetworkServer.active}, netId={networkUserNetId.Value}, tier={tier}");
				if (!NetworkServer.active) return;

				GameObject userObj = NetworkServer.FindLocalObject(networkUserNetId);
				if (!userObj) return;

				NetworkUser user = userObj.GetComponent<NetworkUser>();
				if (!user) return;

				ulong steamId = user.id.value;
				if (serverConsumedThisRun.Contains(steamId))
				{
					Log.Info($"[LunarRitual] Lightness: ritual already consumed this run. steamId={steamId}");
					return;
				}

				int cost = tier switch
				{
					OfferingTier.Small => SmallCost,
					OfferingTier.Medium => MediumCost,
					OfferingTier.Grand => GrandCost,
					_ => 0
				};
				if (cost <= 0) return;

				int shards = GenesisShards.GetShards(steamId);
				if (shards < cost)
				{
					Log.Info($"[LunarRitual] Lightness: not enough shards. steamId={steamId} shards={shards} cost={cost}");
					return;
				}

				var master = user.master;
				if (!master || master.inventory == null) return;

				ItemIndex featherItem = ItemCatalog.FindItemIndex("Feather");
				if (featherItem == ItemIndex.None) return;

				int min = tier switch
				{
					OfferingTier.Small => 1,
					OfferingTier.Medium => 2,
					OfferingTier.Grand => 5,
					_ => 0
				};
				int maxInclusive = tier switch
				{
					OfferingTier.Small => 1,
					OfferingTier.Medium => 4,
					OfferingTier.Grand => 10,
					_ => 0
				};
				if (min <= 0 || maxInclusive < min) return;

				int stacks = Run.instance != null
					? Run.instance.treasureRng.RangeInt(min, maxInclusive + 1)
					: UnityEngine.Random.Range(min, maxInclusive + 1);

				Log.Info($"[LunarRitual] Lightness: granting {stacks}x {featherItem} cost={cost} steamId={steamId}");
				GenesisShards.RemoveShards(steamId, cost);
				master.inventory.GiveItem(featherItem, stacks);
				serverConsumedThisRun.Add(steamId);

				GenesisShards.SaveShards();
				GenesisShardsUI.RefreshUI();
			}
		}

		private static ItemIndex RitualOfEssenceServerRollItem(OfferingTier tier)
		{
			if (Run.instance == null) return ItemIndex.None;

			ItemTier desiredTier = tier switch
			{
				OfferingTier.Small => ItemTier.Tier1,
				OfferingTier.Medium => ItemTier.Tier2,
				OfferingTier.Grand => ItemTier.Tier3,
				_ => ItemTier.NoTier
			};

			if (desiredTier == ItemTier.NoTier) return ItemIndex.None;

			// Robust roll: pick a random item from ItemCatalog by tier.
			// This avoids relying on addressable drop tables that can vary across builds.
			List<ItemIndex> candidates = new List<ItemIndex>(128);
			foreach (ItemIndex idx in ItemCatalog.allItems)
			{
				ItemDef def = ItemCatalog.GetItemDef(idx);
				if (def == null) continue;
				if (def.hidden) continue;
				if (def.tier != desiredTier) continue;
				candidates.Add(idx);
			}

			if (candidates.Count == 0)
			{
				Log.Warning($"[LunarRitual] Essence roll: no candidates for tier={desiredTier}");
				return ItemIndex.None;
			}

			int roll = Run.instance.treasureRng.RangeInt(0, candidates.Count);
			ItemIndex chosen = candidates[roll];
			Log.Info($"[LunarRitual] Essence roll: tier={desiredTier} candidates={candidates.Count} chosen={chosen}");
			return chosen;
		}

		private static void EnsureDropTablesLoaded()
		{
			if (tier1DropTable && tier2DropTable && tier3DropTable) return;
			try
			{
				if (!tier1DropTable)
					tier1DropTable = Addressables.LoadAssetAsync<ExplicitPickupDropTable>("RoR2/Base/Common/dtTier1Item.asset").WaitForCompletion();
				if (!tier2DropTable)
					tier2DropTable = Addressables.LoadAssetAsync<ExplicitPickupDropTable>("RoR2/Base/Common/dtTier2Item.asset").WaitForCompletion();
				if (!tier3DropTable)
					tier3DropTable = Addressables.LoadAssetAsync<ExplicitPickupDropTable>("RoR2/Base/Common/dtTier3Item.asset").WaitForCompletion();
			}
			catch (Exception e)
			{
				Log.Error($"[LunarRitual] Failed to load drop tables for Ritual of Essence: {e.Message}");
			}
		}

		private enum RitualType : byte
		{
			Essence = 0,
			Ego = 1,
			Lightness = 2
		}
	}
}

