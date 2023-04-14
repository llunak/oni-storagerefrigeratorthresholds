using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;

namespace StorageRefrigeratorThresholds
{
    public class Mod : KMod.UserMod2
    {
        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<KMod.Mod> mods)
        {
            string[] methods =
            {
                // Freezer
                "Psyko.Freezer.FreezerConfig",
                // Dupes Refrigeration
                "Advanced_Refrigeration.CompressorUnitConfig",
                "Advanced_Refrigeration.FridgeAdvancedConfig",
                "Advanced_Refrigeration.FridgeBlueConfig",
                "Advanced_Refrigeration.FridgePodConfig",
                "Advanced_Refrigeration.FridgeRedConfig",
                "Advanced_Refrigeration.FridgeYellowConfig",
                "Advanced_Refrigeration.HightechBigFridgeConfig",
                "Advanced_Refrigeration.HightechSmallFridgeConfig",
                "Advanced_Refrigeration.SimpleFridgeConfig",
                "Advanced_Refrigeration.SpaceBoxConfig",
            };
            foreach( string method in methods )
            {
                MethodInfo info = AccessTools.Method( method + ":DoPostConfigureComplete");
                if( info != null )
                    harmony.Patch( info, prefix: new HarmonyMethod( typeof( OtherMods_Patch ).GetMethod( "DoPostConfigureComplete" )));
            }
        }
    }
}
