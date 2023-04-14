using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;

namespace StorageRefrigeratorThresholds
{
    public class Mod : KMod.UserMod2
    {
        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<KMod.Mod> mods)
        {
            OtherMods_Patch.Patch( harmony );
        }
    }
}
