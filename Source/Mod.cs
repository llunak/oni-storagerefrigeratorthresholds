using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace StorageRefrigeratorThresholds
{
    public class Mod : KMod.UserMod2
    {
        public override void OnLoad( Harmony harmony )
        {
            base.OnLoad( harmony );
            PUtil.InitLibrary( false );
            new POptions().RegisterOptions( this, typeof( Options ));
        }
        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<KMod.Mod> mods)
        {
            base.OnAllModsLoaded( harmony, mods );
            OtherMods_Patch.Patch( harmony );
        }
    }
}
