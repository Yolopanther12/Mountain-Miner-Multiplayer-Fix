using Harmony;
using System;
using System.Reflection;
using Verse;

// Fixes the Mountain Miner mod so it does not cause desyncs when used with the multiplayer mod.
// https://github.com/Zetrith/Multiplayer

namespace MountainMinerMultiplayerFix
{
	[StaticConstructorOnStartup]
	public class HarmonyPatches
	{
		static HarmonyPatches()
		{
			var harmony = HarmonyInstance.Create("net.whniwwd.rimworld.mountainminermultiplayerfix");

			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}

	[HarmonyPatch]
	static class PatchClass1
	{
		// Used by Harmony to figure out what method it is we're patching. We need to do this because the
		// method we want to patch is in a private class, so we can't know the type at compile time and thus
		// can't use it in the annotation above.
		static MethodInfo TargetMethod()
		{
			return AccessTools.TypeByName("MountainMiner.Building_MountainDrill").GetMethod("Drill");
		}


		// Checks to see if the Mountain Miner mod is even loaded. If it's not loaded then there's no reason
		// to do any patching. Since the Mountain Miner mod doesn't use Harmony (thus there's no harmony id) the
		// only way to check for it is to look at the loaded mods and see if it's there by its name.
		static bool Prepare()
		{
			bool found = false;

			// https://rimworldwiki.com/wiki/Modding_Tutorials/Compatibility_with_DLLs
			try
			{
				((Action)(() =>
				{
					if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Mountain Miner 1.0"))
					{
						found = true;
					}
				}))();
			}
			catch (TypeLoadException) { }

			if(found)
			{
				Log.Message("[Mountain Miner Multiplayer Fix] Prefix Patching MountainMiner.Building_MountainDrill.Drill");
			}
			else
			{
				Log.Warning("[Mountain Miner Multiplayer Fix] 'Mountain Miner 1.0' Not Found");
			}

			return found;
		}


		static bool Prefix(object __instance, float miningPoints)
		{
			// There's probably a better way to do this than constantly creating a Traverse every time this method is called.
			Traverse m_traverse = Traverse.Create(__instance);

			m_traverse.Field("progress").SetValue(miningPoints + m_traverse.Field("progress").GetValue<float>());

			if (Rand.Range(0, 1000) == 0)
			{
				m_traverse.Method("ProduceLump").GetValue();
			}

			// Don't want to execute the original Drill method. It contains code that desyncs multiplayer clients. Everything
			// it does is replicated here and fixed for multiplayer.
			return false;
		}
	}
}
