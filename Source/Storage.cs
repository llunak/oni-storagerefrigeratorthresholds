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
    public abstract class ThresholdsBase : KMonoBehaviour, IActivationRangeTarget
    {
        [Serialize]
        private int activateValue = 100;

        [Serialize]
        private int deactivateValue = 99;

        [Serialize]
        private bool activated;

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

        public string ActivateTooltip => STRINGS.STORAGEREFRIGERATORTHRESHOLDS.ACTIVATE_TOOLTIP;

        public string DeactivateTooltip => STRINGS.STORAGEREFRIGERATORTHRESHOLDS.DEACTIVATE_TOOLTIP;

        // These strings are reusable, except that activate and deactivate are inverted.
        public string ActivationRangeTitleText => BUILDINGS.PREFABS.SMARTRESERVOIR.SIDESCREEN_TITLE;

        public string ActivateSliderLabelText => BUILDINGS.PREFABS.SMARTRESERVOIR.SIDESCREEN_DEACTIVATE;

        public string DeactivateSliderLabelText => BUILDINGS.PREFABS.SMARTRESERVOIR.SIDESCREEN_ACTIVATE;

        public bool UpdateLogicState(float percentFull)
        {
            float num = Mathf.RoundToInt(percentFull * 100f);
            if (activated)
            {
                if (num <= (float)deactivateValue)
                    activated = false;
            }
            else if (num >= (float)activateValue)
                activated = true;
            return activated;
        }

        protected abstract void UpdateLogicCircuit();
    }

    public class StorageThresholds : ThresholdsBase
    {
        private static readonly MethodInfo updateLogicAndActiveStateMethod
            = AccessTools.Method(typeof(StorageLockerSmart),"UpdateLogicAndActiveState");

        protected override void UpdateLogicCircuit()
        {
            updateLogicAndActiveStateMethod.Invoke( GetComponent<StorageLockerSmart>(), null );
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
                    component.ActivateValue = otherComponent.ActivateValue;
                    component.DeactivateValue = otherComponent.DeactivateValue;
                }
            }
        }
    }

    [HarmonyPatch(typeof(StorageLockerSmart))]
    public class StorageLockerSmart_Patch
    {
        private static readonly MethodInfo getAmountStoredMethod
            = AccessTools.Method(typeof(FilteredStorage),"GetAmountStored");
        private static readonly MethodInfo getMaxCapacityMethod
         = AccessTools.Method(typeof(FilteredStorage),"GetMaxCapacityMinusStorageMargin");

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UpdateLogicAndActiveState))]
        public static bool UpdateLogicAndActiveState(StorageLockerSmart __instance, FilteredStorage ___filteredStorage,
            Operational ___operational, LogicPorts ___ports)
        {
            StorageThresholds component = __instance.gameObject.GetComponent<StorageThresholds>();
            if( component == null )
                return true;
            float stored = (float) getAmountStoredMethod.Invoke(___filteredStorage, null );
            float capacity = (float) getMaxCapacityMethod.Invoke(___filteredStorage, null );
            bool num = component.UpdateLogicState( stored / capacity );
            bool isOperational = ___operational.IsOperational;
            bool flag = num && isOperational;
            ___ports.SendSignal(FilteredStorage.FULL_PORT_ID, flag ? 1 : 0);
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
    }

    public class RefrigeratorThresholds : ThresholdsBase
    {
        private static readonly MethodInfo updateLogicCircuitMethod
            = AccessTools.Method(typeof(Refrigerator),"UpdateLogicCircuit");

        protected override void UpdateLogicCircuit()
        {
            updateLogicCircuitMethod.Invoke( GetComponent<Refrigerator>(), null );
        }
    }

    [HarmonyPatch(typeof(Refrigerator))]
    public class Refrigerator_Patch
    {
        private static readonly MethodInfo getAmountStoredMethod
            = AccessTools.Method(typeof(FilteredStorage),"GetAmountStored");
        private static readonly MethodInfo getMaxCapacityMethod
         = AccessTools.Method(typeof(FilteredStorage),"GetMaxCapacityMinusStorageMargin");

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
            RefrigeratorThresholds component = __instance.gameObject.GetComponent<RefrigeratorThresholds>();
            if( component == null )
                return true;
            float stored = (float) getAmountStoredMethod.Invoke(___filteredStorage, null );
            float capacity = (float) getMaxCapacityMethod.Invoke(___filteredStorage, null );
            bool num = component.UpdateLogicState( stored / capacity );
            bool isOperational = ___operational.IsOperational;
            bool flag = num && isOperational;
            ___ports.SendSignal(FilteredStorage.FULL_PORT_ID, flag ? 1 : 0);
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
