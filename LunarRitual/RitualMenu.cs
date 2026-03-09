using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine.Networking;

namespace LunarRitual
{
    public class RitualMenu : MonoBehaviour
    {
        /*
        private static GameObject menuPanel;
        private static bool menuOpened = false;
        private static bool optionSelected = false;
        
        private static Dictionary<ulong, int> playerOfferings = new Dictionary<ulong, int>();
        */
        
        public static void Initialize()
        {
            /*
            Run.onRunStartGlobal += OnRunStart;
            RoR2Application.onLoad += CreateMenuUI;
            */
            Log.Info($"[LunarRitual] RitualMenu initialized (disabled for testing)");
        }
        
        /*
        private static void CreateMenuUI()
        {
            if (menuPanel != null) return;
            
            menuPanel = new GameObject("RitualMenuPanel");
            menuPanel.AddComponent<Canvas>();
            menuPanel.AddComponent<CanvasScaler>();
            menuPanel.AddComponent<GraphicRaycaster>();
            
            Canvas canvas = menuPanel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            
            RectTransform rectTransform = menuPanel.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            
            CreateMenuContent();
            
            menuPanel.SetActive(false);
        }
        
        private static void CreateMenuContent()
        {
            GameObject background = new GameObject("Background");
            background.transform.SetParent(menuPanel.transform);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);
            
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.25f, 0.15f);
            bgRect.anchorMax = new Vector2(0.75f, 0.85f);
            bgRect.sizeDelta = Vector2.zero;
            
            GameObject titleTextObj = new GameObject("TitleText");
            titleTextObj.transform.SetParent(menuPanel.transform);
            TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "RITUAL OFFERING";
            titleText.fontSize = 48;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.9f, 0.8f, 0.5f);
            titleText.alignment = TextAlignmentOptions.Center;
            
            RectTransform titleRect = titleTextObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.3f, 0.75f);
            titleRect.anchorMax = new Vector2(0.7f, 0.85f);
            titleRect.sizeDelta = Vector2.zero;
            
            GameObject shardsTextObj = new GameObject("ShardsText");
            shardsTextObj.transform.SetParent(menuPanel.transform);
            TextMeshProUGUI shardsText = shardsTextObj.AddComponent<TextMeshProUGUI>();
            shardsText.text = "Available Shards: 0";
            shardsText.fontSize = 32;
            shardsText.color = Color.cyan;
            shardsText.alignment = TextAlignmentOptions.Center;
            
            RectTransform shardsRect = shardsTextObj.GetComponent<RectTransform>();
            shardsRect.anchorMin = new Vector2(0.3f, 0.68f);
            shardsRect.anchorMax = new Vector2(0.7f, 0.74f);
            shardsRect.sizeDelta = Vector2.zero;
            
            CreateOfferingButton(1, "Entity Offering", "1/5/10 shards\nRandom/Common/Rare Item", new Vector2(0.28f, 0.55f), new Vector2(0.42f, 0.66f), OnEntityOffering);
            CreateOfferingButton(2, "Ego Offering", "1/5/10 shards\n1-3/4-6/7-10 Egocentrism", new Vector2(0.58f, 0.55f), new Vector2(0.72f, 0.66f), OnEgoOffering);
            CreateOfferingButton(3, "Blessing Offering", "Based on shards\nStat bonuses", new Vector2(0.28f, 0.43f), new Vector2(0.42f, 0.54f), OnBlessingOffering);
            CreateOfferingButton(4, "Lightness Offering", "1/5/10 shards\n1/2-4/5-10 Hopoo Feathers", new Vector2(0.58f, 0.43f), new Vector2(0.72f, 0.54f), OnLightnessOffering);
            CreateOfferingButton(5, "Heresy Offering", "10 shards\nHeretic Unlock Items", new Vector2(0.28f, 0.31f), new Vector2(0.42f, 0.42f), OnHeresyOffering);
            CreateOfferingButton(6, "Submission Offering", "10 shards\nHappiest Mask", new Vector2(0.58f, 0.31f), new Vector2(0.72f, 0.42f), OnSubmissionOffering);
            CreateOfferingButton(7, "Greed Offering", "1/5/10 shards\n100-200/500-1000/1000-5000 Gold", new Vector2(0.38f, 0.19f), new Vector2(0.62f, 0.30f), OnGreedOffering);
        }
        
        private static void CreateOfferingButton(int id, string title, string description, Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            GameObject buttonObj = new GameObject($"OfferingButton_{id}");
            buttonObj.transform.SetParent(menuPanel.transform);
            
            Button button = buttonObj.AddComponent<Button>();
            button.onClick.AddListener(() => OnOfferingClick(id, onClick));
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.15f, 0.3f, 0.9f);
            buttonImage.type = Image.Type.RoundedRectangle;
            
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.sizeDelta = Vector2.zero;
            
            GameObject titleTextObj = new GameObject($"Title_{id}");
            titleTextObj.transform.SetParent(buttonObj.transform);
            TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;
            
            RectTransform titleRect = titleTextObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.6f);
            titleRect.anchorMax = new Vector2(0.9f, 0.9f);
            titleRect.sizeDelta = Vector2.zero;
            
            GameObject descTextObj = new GameObject($"Description_{id}");
            descTextObj.transform.SetParent(buttonObj.transform);
            TextMeshProUGUI descText = descTextObj.AddComponent<TextMeshProUGUI>();
            descText.text = description;
            descText.fontSize = 12;
            descText.color = new Color(0.8f, 0.8f, 0.8f);
            descText.alignment = TextAlignmentOptions.Center;
            
            RectTransform descRect = descTextObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.1f, 0.1f);
            descRect.anchorMax = new Vector2(0.9f, 0.55f);
            descRect.sizeDelta = Vector2.zero;
        }
        
        private static void OnRunStart(Run run)
        {
            menuOpened = false;
            optionSelected = false;
            playerOfferings.Clear();
            OpenMenu();
        }
        
        private static void OpenMenu()
        {
            if (menuOpened || optionSelected) return;
            
            UpdateShardsDisplay();
            menuPanel.SetActive(true);
            menuOpened = true;
            
            PauseGame();
        }
        
        private static void UpdateShardsDisplay()
        {
            ulong localSteamId = GetLocalPlayerSteamId();
            int shards = GenesisShards.GetShards(localSteamId);
            
            Transform shardsText = menuPanel.transform.Find("ShardsText");
            if (shardsText != null)
            {
                TextMeshProUGUI text = shardsText.GetComponent<TextMeshProUGUI>();
                text.text = $"Available Shards: {shards}";
            }
        }
        
        private static void OnOfferingClick(int offeringId, Action onExecute)
        {
            ulong localSteamId = GetLocalPlayerSteamId();
            int shards = GenesisShards.GetShards(localSteamId);
            
            if (shards <= 0)
            {
                Log.LogWarning("Not enough shards!");
                return;
            }
            
            if (optionSelected)
            {
                Log.LogWarning("Offering already selected!");
                return;
            }
            
            optionSelected = true;
            playerOfferings[localSteamId] = offeringId;
            
            onExecute?.Invoke();
            
            CloseMenu();
        }
        
        private static void OnEntityOffering()
        {
            ulong localSteamId = GetLocalPlayerSteamId();
            int shards = GenesisShards.GetShards(localSteamId);
            
            int tier = shards >= 10 ? 2 : (shards >= 5 ? 1 : 0);
            int cost = tier == 2 ? 10 : (tier == 1 ? 5 : 1);
            
            GenesisShards.RemoveShards(localSteamId, cost);
            GiveRandomItem(tier);
            
            Log.LogInfo($"Entity Offering: {cost} shards spent, Tier {tier} item granted");
        }
        
        private static void OnEgoOffering()
        {
            ulong localSteamId = GetLocalPlayerSteamId();
            int shards = GenesisShards.GetShards(localSteamId);
            
            int tier = shards >= 10 ? 2 : (shards >= 5 ? 1 : 0);
            int cost = tier == 2 ? 10 : (tier == 1 ? 5 : 1);
            int stacks = tier == 2 ? UnityEngine.Random.Range(7, 11) : (tier == 1 ? UnityEngine.Random.Range(4, 7) : UnityEngine.Random.Range(1, 4));
            
            GenesisShards.RemoveShards(localSteamId, cost);
            GiveEgocentrismStacks(stacks);
            
            Log.LogInfo($"Ego Offering: {cost} shards spent, {stacks} Egocentrism stacks granted");
        }
        
        private static void OnBlessingOffering()
        {
            ulong localSteamId = GetLocalPlayerSteamId();
            int shards = GenesisShards.GetShards(localSteamId);
            
            GenesisShards.RemoveShards(localSteamId, shards);
            ApplyStatBonuses(shards);
            
            Log.LogInfo($"Blessing Offering: {shards} shards spent, stat bonuses applied");
        }
        
        private static void OnLightnessOffering()
        {
            ulong localSteamId = GetLocalPlayerSteamId();
            int shards = GenesisShards.GetShards(localSteamId);
            
            int tier = shards >= 10 ? 2 : (shards >= 5 ? 1 : 0);
            int cost = tier == 2 ? 10 : (tier == 1 ? 5 : 1);
            int stacks = tier == 2 ? UnityEngine.Random.Range(5, 11) : (tier == 1 ? UnityEngine.Random.Range(2, 5) : 1);
            
            GenesisShards.RemoveShards(localSteamId, cost);
            GiveHopooFeatherStacks(stacks);
            
            Log.LogInfo($"Lightness Offering: {cost} shards spent, {stacks} Hopoo Feathers granted");
        }
        
        private static void OnHeresyOffering()
        {
            ulong localSteamId = GetLocalPlayerSteamId();
            int shards = GenesisShards.GetShards(localSteamId);
            
            if (shards < 10)
            {
                Log.LogWarning("Not enough shards for Heresy Offering!");
                return;
            }
            
            GenesisShards.RemoveShards(localSteamId, 10);
            GiveHereticItems();
            
            Log.LogInfo("Heresy Offering: 10 shards spent, Heretic items granted");
        }
        
        private static void OnSubmissionOffering()
        {
            ulong localSteamId = GetLocalPlayerSteamId();
            int shards = GenesisShards.GetShards(localSteamId);
            
            if (shards < 10)
            {
                Log.LogWarning("Not enough shards for Submission Offering!");
                return;
            }
            
            GenesisShards.RemoveShards(localSteamId, 10);
            GiveHappiestMask();
            
            Log.LogInfo("Submission Offering: 10 shards spent, Happiest Mask granted");
        }
        
        private static void OnGreedOffering()
        {
            ulong localSteamId = GetLocalPlayerSteamId();
            int shards = GenesisShards.GetShards(localSteamId);
            
            int tier = shards >= 10 ? 2 : (shards >= 5 ? 1 : 0);
            int cost = tier == 2 ? 10 : (tier == 1 ? 5 : 1);
            int gold = tier == 2 ? UnityEngine.Random.Range(1000, 5001) : (tier == 1 ? UnityEngine.Random.Range(500, 1001) : UnityEngine.Random.Range(100, 201));
            
            GenesisShards.RemoveShards(localSteamId, cost);
            GiveGold(gold);
            
            Log.LogInfo($"Greed Offering: {cost} shards spent, {gold} gold granted");
        }
        
        private static void GiveRandomItem(int tier)
        {
            if (!NetworkServer.active) return;
            
            var pickupList = tier switch
            {
                0 => Run.instance.availableTier1DropList,
                1 => Run.instance.availableTier2DropList,
                2 => Run.instance.availableTier3DropList,
                _ => Run.instance.availableTier1DropList
            };
            
            if (pickupList != null && pickupList.Count > 0)
            {
                PickupIndex pickup = pickupList[UnityEngine.Random.Range(0, pickupList.Count)];
                PlayerCharacterMasterController localPlayer = PlayerCharacterMasterController.instances[0];
                if (localPlayer != null && localPlayer.master.inventory != null)
                {
                    PickupDropletController.CreatePickupDroplet(pickup, localPlayer.master.GetBody().transform.position, Vector3.up * 10f);
                }
            }
        }
        
        private static void GiveEgocentrismStacks(int stacks)
        {
            if (!NetworkServer.active) return;
            
            PlayerCharacterMasterController localPlayer = PlayerCharacterMasterController.instances[0];
            if (localPlayer != null && localPlayer.master.inventory != null)
            {
                ItemIndex egocentrismIndex = RoR2Content.Items.AttackSpeedOnCrit.itemIndex;
                localPlayer.master.inventory.GiveItem(egocentrismIndex, stacks);
            }
        }
        
        private static void ApplyStatBonuses(int shards)
        {
            if (!NetworkServer.active) return;
            
            PlayerCharacterMasterController localPlayer = PlayerCharacterMasterController.instances[0];
            if (localPlayer != null)
            {
                float damageBonus = shards * 0.02f;
                float healthBonus = shards * 0.01f;
                
                localPlayer.master.GetBody().baseDamage *= (1f + damageBonus);
                localPlayer.master.GetBody().baseMaxHealth *= (1f + healthBonus);
            }
        }
        
        private static void GiveHopooFeatherStacks(int stacks)
        {
            if (!NetworkServer.active) return;
            
            PlayerCharacterMasterController localPlayer = PlayerCharacterMasterController.instances[0];
            if (localPlayer != null && localPlayer.master.inventory != null)
            {
                ItemIndex featherIndex = RoR2Content.Items.JumpBoost.itemIndex;
                localPlayer.master.inventory.GiveItem(featherIndex, stacks);
            }
        }
        
        private static void GiveHereticItems()
        {
            if (!NetworkServer.active) return;
            
            PlayerCharacterMasterController localPlayer = PlayerCharacterMasterController.instances[0];
            if (localPlayer != null && localPlayer.master.inventory != null)
            {
                ItemIndex[] hereticItems = 
                {
                    RoR2Content.Items.BoostAttackSpeedAndMoveSpeed.itemIndex,
                    RoR2Content.Items.AutoCastEquipment.itemIndex,
                    RoR2Content.Items.ChainLightning.itemIndex,
                    RoR2Content.Items.BleedOnHitAndExplode.itemIndex
                };
                
                foreach (ItemIndex itemIndex in hereticItems)
                {
                    localPlayer.master.inventory.GiveItem(itemIndex, 1);
                }
            }
        }
        
        private static void GiveHappiestMask()
        {
            if (!NetworkServer.active) return;
            
            PlayerCharacterMasterController localPlayer = PlayerCharacterMasterController.instances[0];
            if (localPlayer != null && localPlayer.master.inventory != null)
            {
                ItemIndex maskIndex = RoR2Content.Items.GhostOnKill.itemIndex;
                localPlayer.master.inventory.GiveItem(maskIndex, 1);
            }
        }
        
        private static void GiveGold(int amount)
        {
            if (!NetworkServer.active) return;
            
            PlayerCharacterMasterController localPlayer = PlayerCharacterMasterController.instances[0];
            if (localPlayer != null)
            {
                localPlayer.master.GiveMoney((uint)amount);
            }
        }
        
        private static void CloseMenu()
        {
            menuPanel.SetActive(false);
            ResumeGame();
        }
        
        private static void PauseGame()
        {
            Time.timeScale = 0f;
        }
        
        private static void ResumeGame()
        {
            Time.timeScale = 1f;
        }
        
        private static ulong GetLocalPlayerSteamId()
        {
            if (NetworkUser.readOnlyLocalPlayersList.Count > 0)
            {
                return NetworkUser.readOnlyLocalPlayersList[0].id.value;
            }
            return 0;
        }
        */
    }
}