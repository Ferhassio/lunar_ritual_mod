using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LunarRitual
{
	public class GenesisShardsUI : MonoBehaviour
	{
		private static GameObject uiObject;
		private static HGTextMeshProUGUI shardsText;
		private static Canvas canvas;
		private static bool initialized = false;

		public static void Initialize()
		{
			if (initialized) return;
			initialized = true;

			Run.onRunStartGlobal += OnRunStart;
			Run.onRunDestroyGlobal += OnRunDestroy;
			SceneDirector.onPostPopulateSceneServer += OnSceneLoaded;
		}

		private static void OnSceneLoaded(SceneDirector director)
		{
			if (uiObject == null)
			{
				CreateUI();
			}
		}

		private static void OnRunStart(Run run)
		{
			CreateUI();
		}

		private static void OnRunDestroy(Run run)
		{
			DestroyUI();
		}

		private static void CreateUI()
		{
			if (uiObject != null) return;

			CreateStandaloneUI();
			UpdateShardsDisplay();
			Log.Info("[LunarRitual] Genesis Shards UI created");
		}

		private static void CreateStandaloneUI()
		{
			GameObject canvasObj = new GameObject("GenesisShardsCanvas");
			canvas = canvasObj.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 100;
			canvasObj.AddComponent<CanvasScaler>();
			canvasObj.AddComponent<GraphicRaycaster>();
			uiObject = canvasObj;

			GameObject shardsContainer = new GameObject("ShardsContainer");
			shardsContainer.transform.SetParent(uiObject.transform, false);

			RectTransform containerRect = shardsContainer.AddComponent<RectTransform>();
			containerRect.anchorMin = new Vector2(0f, 1f);
			containerRect.anchorMax = new Vector2(0f, 1f);
			containerRect.pivot = new Vector2(0f, 1f);
			containerRect.sizeDelta = new Vector2(300f, 60f);
			containerRect.anchoredPosition = new Vector2(10f, -10f);
			containerRect.localRotation = Quaternion.identity;

			GameObject shardsIcon = new GameObject("ShardsIcon");
			shardsIcon.transform.SetParent(shardsContainer.transform, false);

			Image iconImage = shardsIcon.AddComponent<Image>();
			iconImage.sprite = CreateShardIcon();

			RectTransform iconRect = shardsIcon.GetComponent<RectTransform>();
			iconRect.anchorMin = new Vector2(0f, 0.5f);
			iconRect.anchorMax = new Vector2(0f, 0.5f);
			iconRect.pivot = new Vector2(0f, 0.5f);
			iconRect.sizeDelta = new Vector2(40f, 40f);
			iconRect.anchoredPosition = new Vector2(0f, 0f);

			GameObject shardsTextObj = new GameObject("ShardsText");
			shardsTextObj.transform.SetParent(shardsContainer.transform, false);

			shardsText = shardsTextObj.AddComponent<HGTextMeshProUGUI>();
			shardsText.fontSize = 24;
			shardsText.color = new Color(36f / 255f, 9f / 255f, 53f / 255f);
			shardsText.fontStyle = TMPro.FontStyles.Bold;
			shardsText.alignment = TMPro.TextAlignmentOptions.Left;
			shardsText.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
			if (shardsText.font == null)
			{
				shardsText.font = Resources.Load<TMPro.TMP_FontAsset>("fonts & materials/LiberationSans SDF");
			}

			RectTransform textRect = shardsTextObj.GetComponent<RectTransform>();
			textRect.anchorMin = new Vector2(0f, 0f);
			textRect.anchorMax = new Vector2(1f, 1f);
			textRect.pivot = new Vector2(0f, 0.5f);
			textRect.sizeDelta = new Vector2(-50f, 40f);
			textRect.anchoredPosition = new Vector2(50f, 0f);

			shardsContainer.SetActive(true);
			shardsIcon.SetActive(true);
			shardsTextObj.SetActive(true);

			UpdateShardsDisplay();

			Log.Info("[LunarRitual] Genesis Shards UI created (standalone)");
		}

		private static Sprite CreateShardIcon()
		{
			Texture2D texture = new Texture2D(32, 32);
			Color[] pixels = new Color[32 * 32];
			
			for (int i = 0; i < pixels.Length; i++)
			{
				int x = i % 32;
				int y = i / 32;
				float centerX = 16f;
				float centerY = 16f;
				float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
				
				if (distance < 15f)
				{
					float alpha = 1f - (distance / 15f);
					pixels[i] = new Color(36f / 255f, 9f / 255f, 53f / 255f, alpha);
				}
				else
				{
					pixels[i] = new Color(0f, 0f, 0f, 0f);
				}
			}
			
			texture.SetPixels(pixels);
			texture.Apply();
			
			return Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f));
		}

		public static void UpdateShardsDisplay()
		{
			if (shardsText == null)
			{
				Log.Warning("[LunarRitual] UpdateShardsDisplay: shardsText is null");
				return;
			}

			ulong steamId = 0;
			if (NetworkUser.readOnlyLocalPlayersList.Count > 0)
			{
				steamId = NetworkUser.readOnlyLocalPlayersList[0].id.value;
				Log.Warning($"[LunarRitual] UpdateShardsDisplay: Local player steamId: {steamId}");
			}
			else
			{
				Log.Warning("[LunarRitual] UpdateShardsDisplay: No local players found");
			}
			
			int shardCount = GenesisShards.GetShards(steamId);
			shardsText.text = $"Genesis Shards: {shardCount}";
			Log.Warning($"[LunarRitual] UpdateShardsDisplay: Set text to 'Genesis Shards: {shardCount}'");
		}

		public static void RefreshUI()
		{
			if (uiObject != null)
			{
				UpdateShardsDisplay();
			}
		}

		private static void DestroyUI()
		{
			if (uiObject != null)
			{
				Object.Destroy(uiObject);
				uiObject = null;
				shardsText = null;
			}

			if (canvas != null)
			{
				Object.Destroy(canvas.gameObject);
				canvas = null;
			}
		}
	}
}
