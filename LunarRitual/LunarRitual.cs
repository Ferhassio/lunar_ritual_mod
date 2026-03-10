using BepInEx;
using BepInEx.Configuration;
using R2API;

namespace LunarRitual
{
	[BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
	public class LunarRitual : BaseUnityPlugin
	{
		public const string PluginGUID = PluginAuthor + "." + PluginName;
		public const string PluginAuthor = "LunarRitual";
		public const string PluginName = "LunarRitual";
		public const string PluginVersion = "1.0.0";

		public static ConfigEntry<float> shardChance { get; set; }
		public static ConfigEntry<float> shardMultiplier { get; set; }
		public static ConfigEntry<int> startingShards { get; set; }
		public static ConfigEntry<bool> teamShards { get; set; }
		public static ConfigEntry<bool> noShardDroplet { get; set; }
		public static ConfigEntry<bool> resetShards { get; set; }

		public static PluginInfo pluginInfo;

		public void Awake()
		{
			pluginInfo = Info;

			shardChance = Config.Bind("Genesis Shards", "Initial Shard Chance", 1.0f, "Chance for first genesis shard to be dropped (0-1).");
			shardMultiplier = Config.Bind("Genesis Shards", "Shard Chance Multiplier", 0.5f, "Value that chance is multiplied by after a shard is dropped (0-1).");
			startingShards = Config.Bind("Genesis Shards", "Starting Shards", 0, "Shards that each player has at the start of a run, if 'Reset Shards Each Run' is enabled.");
			teamShards = Config.Bind("Genesis Shards", "Distribute Shards", true, "All allies receive a genesis shard when one is dropped.");
			noShardDroplet = Config.Bind("Genesis Shards", "No Shard Droplets", false, "Enemies emit a genesis shard effect instead of the regular droplet that is manually picked up.");
			resetShards = Config.Bind("Genesis Shards", "Reset Shards Each Run", false, "Genesis shards are reset at the start of a run to the value determined by 'Starting Shards'.");

			noShardDroplet.Value = false; //For Debug only
			shardChance.Value = 1.0f; //For debug only

			Log.Init(Logger);

			GenesisShards.InitializeGenesisShardPickup();

			GenesisShards.LoadShards();

			Hooks.Init();

			GenesisShardsUI.Initialize();

			RitualMenu.Initialize();

			if (RiskOfOptionsCompatibility.enabled)
			{
				RiskOfOptionsCompatibility.OptionsInit();
			}

			Log.Info("[LunarRitual] loaded successfully!");
		}

		public void OnDestroy()
		{
			GenesisShards.SaveShards();
		}
	}
}
