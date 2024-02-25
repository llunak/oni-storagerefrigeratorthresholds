using HarmonyLib;
using UnityEngine;
using KSerialization;
using STRINGS;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace StorageRefrigeratorThresholds
{
    // Implementing thresholds requires using IActivateRangeTarget for a class.
    // Since StorageLockerSmart and Refrigerator cannot be changed, create
    // a new class to handle this, patch the original classes to communicate
    // with this one.
    // The rest is using the same logic as BatterySmart or SmartReservoir,
    // only activate and deactivate must be swapped (since here green means full).
    // But provide an option to not swap the logic, which has also additional
    // advantages (swapped logic requires not gate for detecting not-enough,
    // which causes false alarms on game start or when power goes out, both
    // of which result in red signal, negated to green and activating notifier).
    public abstract class ThresholdsBase : KMonoBehaviour, IActivationRangeTarget, ISim4000ms
    {
        [Serialize]
        private bool sendGreenOnLow = false;

        // Note that the naming is a mess, BatterySmart and SmartReservoir make
        // Activate propery get and set deactivateValue and vice versa,
        // and "activate" in the interface actually usually means "high"
        // and "deactivate" means "low", regardless of what it does.
        // Keep the high/low meaning here, so activate is high regardless of function.
        [Serialize]
        private int activateValue = 100;

        [Serialize]
        private int deactivateValue = 99;

        [Serialize]
        private bool activated;

        public bool? LastSetFlag { get; set; } = null; // Last value set to the logic port.

        public bool SendGreenOnLow
        {
            get
            {
                return sendGreenOnLow;
            }
            set
            {
                sendGreenOnLow = value;
                UpdateLogicCircuit();
                UpdateLogicPortTooltip();
            }
        }

        public float ActivateValue
        {
            get
            {
                return activateValue;
            }
            set
            {
                activateValue = (int)value;
                UpdateLogicCircuit();
            }
        }

        public float DeactivateValue
        {
            get
            {
                return deactivateValue;
            }
            set
            {
                deactivateValue = (int)value;
                UpdateLogicCircuit();
            }
        }

        public float MinValue => 0f;

        public float MaxValue => 100f;

        public bool UseWholeNumbers => true;

        public string ActivateTooltip => sendGreenOnLow
            ? STRINGS.STORAGEREFRIGERATORTHRESHOLDS.ACTIVATE_TOOLTIP_GREENONLOW
            : STRINGS.STORAGEREFRIGERATORTHRESHOLDS.ACTIVATE_TOOLTIP;

        public string DeactivateTooltip => sendGreenOnLow
            ? STRINGS.STORAGEREFRIGERATORTHRESHOLDS.DEACTIVATE_TOOLTIP_GREENONLOW
            : STRINGS.STORAGEREFRIGERATORTHRESHOLDS.DEACTIVATE_TOOLTIP;

        // These strings are reusable.
        public string ActivationRangeTitleText => BUILDINGS.PREFABS.SMARTRESERVOIR.SIDESCREEN_TITLE;

        // "High Threshold:"
        public string ActivateSliderLabelText => BUILDINGS.PREFABS.SMARTRESERVOIR.SIDESCREEN_DEACTIVATE;

        // "Low Threshold:"
        public string DeactivateSliderLabelText => BUILDINGS.PREFABS.SMARTRESERVOIR.SIDESCREEN_ACTIVATE;

        public bool UpdateLogicState(float percentFull)
        {
            float num = Mathf.RoundToInt(percentFull * 100f);
            if (activated)
            {
                if (sendGreenOnLow ? (num >= (float)activateValue) : (num <= (float)deactivateValue))
                    activated = false;
            }
            else
            {
                if (sendGreenOnLow ? (num <= (float)deactivateValue) : (num >= (float)activateValue))
                    activated = true;
            }
            return activated;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            fastMap[ gameObject ] = this;
            LastSetFlag = null;
            UpdateLogicPortTooltip();
        }

        protected override void OnCleanUp()
        {
            fastMap.Remove( gameObject );
            base.OnCleanUp();
        }

        private void UpdateLogicPortTooltip()
        {
            LogicPorts ports = GetComponent< LogicPorts >();
            ports.outputPortInfo[ 0 ].activeDescription = sendGreenOnLow
                ? STRINGS.STORAGEREFRIGERATORTHRESHOLDS.LOGIC_PORT_ACTIVE_GREENONLOW
                : STRINGS.STORAGEREFRIGERATORTHRESHOLDS.LOGIC_PORT_ACTIVE;
            ports.outputPortInfo[ 0 ].inactiveDescription = sendGreenOnLow
                ? STRINGS.STORAGEREFRIGERATORTHRESHOLDS.LOGIC_PORT_INACTIVE_GREENONLOW
                : STRINGS.STORAGEREFRIGERATORTHRESHOLDS.LOGIC_PORT_INACTIVE;
        }

        protected abstract void UpdateLogicCircuit();

        // GetComponent() calls may add up being somewhat expensive when called often,
        // so instead cache the mapping.
        private static Dictionary< GameObject, ThresholdsBase > fastMap
            = new Dictionary< GameObject, ThresholdsBase >();

        public static ThresholdsBase Get( GameObject gameObject )
        {
            if( fastMap.TryGetValue( gameObject, out ThresholdsBase thresholds ))
                return thresholds;
            return null;
        }

        // Operational.IsOperational returns false if the building is rocket-controlled
        // and restricted. That means that restricted storage inside a rocket would
        // always signal false, and so any automation is-full checks for it would be useless.
        // So check for everything else except for restriction.
        public bool IsActuallyOperational( Operational operational )
        {
            if( operational.IsOperational )
                return true;
            foreach( KeyValuePair< Operational.Flag, bool > flag in operational.Flags )
            {
                if( !flag.Value && flag.Key != RocketUsageRestriction.rocketUsageAllowed )
                    return false;
            }
            return true;
        }

        // If inside a rocket, and thus possibly restricted, it is necessary to check periodically.
        // The reason is that Operational signals changes only when its operational status changes,
        // so when restricted (and thus not operational) and something else such as loss of power
        // also makes it inoperational, this change won't be signalled, but IsActuallyOperational()
        // and thus the logic handling should change.
        public void Sim4000ms(float dt)
        {
            if(!gameObject.GetMyWorld().IsModuleInterior)
                return;
            UpdateLogicCircuit();
        }
    }

    public class StorageThresholds : ThresholdsBase
    {
        delegate void UpdateLogicAndActiveStateDelegate( StorageLockerSmart locker );
        private static readonly UpdateLogicAndActiveStateDelegate updateLogicAndActiveStateMethod
            = AccessTools.MethodDelegate<UpdateLogicAndActiveStateDelegate>(
                AccessTools.Method(typeof(StorageLockerSmart),"UpdateLogicAndActiveState"));

#pragma warning disable CS0649
        [MyCmpGet]
        private StorageLockerSmart storageLockerSmart;
#pragma warning restore CS0649

        protected override void UpdateLogicCircuit()
        {
            updateLogicAndActiveStateMethod( storageLockerSmart );
        }
    }

    // OnCopySettings() is inherited from StorageLocker in StorageLockerSmart,
    // so it needs to be patched there. For normal StorageLocker objects it will not change
    // anything, because they do not include StorageThresholds.
    [HarmonyPatch(typeof(StorageLocker))]
    public class StorageLocker_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(OnCopySettings))]
        public static void OnCopySettings(StorageLockerSmart __instance, object data)
        {
            GameObject otherGameObject = (GameObject)data;
            if (otherGameObject != null)
            {
                StorageThresholds component = __instance.gameObject.GetComponent<StorageThresholds>();
                StorageThresholds otherComponent = otherGameObject.GetComponent<StorageThresholds>();
                if (component != null && otherComponent != null)
                {
                    component.SendGreenOnLow = otherComponent.SendGreenOnLow;
                    component.ActivateValue = otherComponent.ActivateValue;
                    component.DeactivateValue = otherComponent.DeactivateValue;
                }
            }
        }
    }

    [HarmonyPatch(typeof(StorageLockerSmart))]
    public class StorageLockerSmart_Patch
    {
        delegate float FloatDelegate(FilteredStorage storage);
        private static readonly FloatDelegate getAmountStoredMethod
            = AccessTools.MethodDelegate<FloatDelegate>(
                AccessTools.Method(typeof(FilteredStorage),"GetAmountStored"));
        private static readonly FloatDelegate getMaxCapacityMethod
            = AccessTools.MethodDelegate<FloatDelegate>(
                 AccessTools.Method(typeof(FilteredStorage),"GetMaxCapacityMinusStorageMargin"));

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UpdateLogicAndActiveState))]
        public static bool UpdateLogicAndActiveState(StorageLockerSmart __instance, FilteredStorage ___filteredStorage,
            Operational ___operational, LogicPorts ___ports)
        {
            ThresholdsBase component = ThresholdsBase.Get(__instance.gameObject );
            if( component == null )
                return true;
            float stored = getAmountStoredMethod(___filteredStorage);
            float capacity = getMaxCapacityMethod(___filteredStorage);
            bool isOperational = component.IsActuallyOperational( ___operational );
            bool num = component.UpdateLogicState( stored / capacity );
            bool flag = num && isOperational;
            if( flag != component.LastSetFlag )
            {
                ___ports.SendSignal(FilteredStorage.FULL_PORT_ID, flag ? 1 : 0);
                component.LastSetFlag = flag;
            }
            ___filteredStorage.SetLogicMeter(flag);
            ___operational.SetActive(isOperational);
            return false; // skip the original
        }
    }

    [HarmonyPatch(typeof(StorageLockerSmartConfig))]
    public class StorageLockerSmartConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<StorageThresholds>();
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CreateBuildingDef))]
        public static void CreateBuildingDef( ref BuildingDef __result )
        {
            if( Options.Instance.ReducedSmartStorageBinRequiredPower )
                __result.EnergyConsumptionWhenActive = 20f;
        }
    }

    public class RefrigeratorThresholds : ThresholdsBase
    {
        delegate void UpdateLogicCircuitMethod( Refrigerator refrigerator );
        private static readonly UpdateLogicCircuitMethod updateLogicCircuitMethod
            = AccessTools.MethodDelegate<UpdateLogicCircuitMethod>(
                AccessTools.Method(typeof(Refrigerator),"UpdateLogicCircuit"));

#pragma warning disable CS0649
        [MyCmpGet]
        private Refrigerator refrigerator;
#pragma warning restore CS0649

        protected override void UpdateLogicCircuit()
        {
            updateLogicCircuitMethod( refrigerator );
        }

    }

    [HarmonyPatch(typeof(Refrigerator))]
    public class Refrigerator_Patch
    {
        delegate float FloatDelegate(FilteredStorage storage);
        private static readonly FloatDelegate getAmountStoredMethod
            = AccessTools.MethodDelegate<FloatDelegate>(
                AccessTools.Method(typeof(FilteredStorage),"GetAmountStored"));
        private static readonly FloatDelegate getMaxCapacityMethod
            = AccessTools.MethodDelegate<FloatDelegate>(
                AccessTools.Method(typeof(FilteredStorage),"GetMaxCapacityMinusStorageMargin"));

        [HarmonyPostfix]
        [HarmonyPatch(nameof(OnCopySettings))]
        public static void OnCopySettings(Refrigerator __instance, object data)
        {
            GameObject otherGameObject = (GameObject)data;
            if (otherGameObject != null)
            {
                RefrigeratorThresholds component = __instance.gameObject.GetComponent<RefrigeratorThresholds>();
                RefrigeratorThresholds otherComponent = otherGameObject.GetComponent<RefrigeratorThresholds>();
                if (component != null && otherComponent != null)
                {
                    component.SendGreenOnLow = otherComponent.SendGreenOnLow;
                    component.ActivateValue = otherComponent.ActivateValue;
                    component.DeactivateValue = otherComponent.DeactivateValue;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UpdateLogicCircuit))]
        public static bool UpdateLogicCircuit(Refrigerator __instance, FilteredStorage ___filteredStorage,
            Operational ___operational, LogicPorts ___ports)
        {
            ThresholdsBase component = ThresholdsBase.Get(__instance.gameObject );
            if( component == null )
                return true;
            float stored = getAmountStoredMethod(___filteredStorage);
            float capacity = getMaxCapacityMethod(___filteredStorage);
            bool isOperational = component.IsActuallyOperational( ___operational );
            bool num = component.UpdateLogicState( stored / capacity );
            bool flag = num && isOperational;
            if( flag != component.LastSetFlag )
            {
                ___ports.SendSignal(FilteredStorage.FULL_PORT_ID, flag ? 1 : 0);
                component.LastSetFlag = flag;
            }
            ___filteredStorage.SetLogicMeter(flag);
            return false; // skip the original
        }
    }

    [HarmonyPatch(typeof(RefrigeratorConfig))]
    public class RefrigeratorConfig_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<RefrigeratorThresholds>();
        }
    }

    // Optionally support storage from other mods.
    public class OtherMods_Patch
    {
        public static void Patch( Harmony harmony )
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

        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<RefrigeratorThresholds>();
        }
    }
}
