using PeterHan.PLib.Options;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace StorageRefrigeratorThresholds
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("https://github.com/llunak/oni-storagerefrigeratorthresholds")]
    [ConfigFile(SharedConfigLocation: true)]
    public sealed class Options : SingletonOptions< Options >, IOptions
    {
        [Option("Reduced Smart Storage Bin Required Power", "Halves the required power for Smart Storage Bin to 30W.")]
        [JsonProperty]
        public bool ReducedSmartStorageBinRequiredPower { get; set; } = false;

        public override string ToString()
        {
            return string.Format("StorageRefrigeratorThresholds.Options[reducedsmartstoragebinrequiredpower={0}]",
                ReducedSmartStorageBinRequiredPower );
        }

        public void OnOptionsChanged()
        {
            // 'this' is the Options instance used by the options dialog, so set up
            // the actual instance used by the mod. MemberwiseClone() is enough to copy non-reference data.
            Instance = (Options) this.MemberwiseClone();
        }

        public IEnumerable<IOptionsEntry> CreateOptions()
        {
            return null;
        }
    }
}
