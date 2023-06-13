using HarmonyLib;
using UnityEngine;
using System.Reflection;
using PeterHan.PLib.UI;
using STRINGS;

namespace StorageRefrigeratorThresholds
{
    // Thresholds themselves are handled by ActiveRangeSideScreen,
    // this only shows 'Send Green Signal When Low' checkbox
    // to convert whether to invert the green/red signals
    // and should go directly below ActiveRangeSideScreen content.
    public class ThresholdsSideScreen : SideScreenContent
    {
        private GameObject checkbox;

        private ThresholdsBase target;

        protected override void OnPrefabInit()
        {
            var margin = new RectOffset(4, 4, 4, 4);
            var baseLayout = gameObject.GetComponent<BoxLayoutGroup>();
            if (baseLayout != null)
                baseLayout.Params = new BoxLayoutParams()
                {
                    Alignment = TextAnchor.MiddleLeft,
                    Margin = margin,
                };
            PPanel panel = new PPanel("MainPanel")
            {
                Direction = PanelDirection.Horizontal,
                Margin = margin,
                Spacing = 4,
                FlexSize = Vector2.right
            };
            PCheckBox checkboxField = new PCheckBox( "checkbox" )
            {
                    Text = STRINGS.STORAGEREFRIGERATORTHRESHOLDS.CHECKBOX,
                    ToolTip = STRINGS.STORAGEREFRIGERATORTHRESHOLDS.CHECKBOX_TOOLTIP,
                    OnChecked = OnCheck,
                    TextStyle = PUITuning.Fonts.TextDarkStyle
            };
            checkboxField.AddOnRealize((obj) => checkbox = obj);
            panel.AddChild( checkboxField );
            panel.AddTo( gameObject );
            ContentContainer = gameObject;
            base.OnPrefabInit();
            UpdateState();
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target.GetComponent<StorageThresholds>() != null
                || target.GetComponent<RefrigeratorThresholds>() != null;
        }

        public override void SetTarget(GameObject new_target)
        {
            if (new_target == null)
            {
                Debug.LogError("Invalid gameObject received");
                return;
            }
            target = new_target.GetComponent<StorageThresholds>();
            if (target == null)
                target = new_target.GetComponent<RefrigeratorThresholds>();
            if (target == null)
            {
                Debug.LogError("The gameObject received does not contain a ThresholdsBase component");
                return;
            }
            UpdateState();
        }

        public void UpdateState()
        {
            if( target == null || checkbox == null )
                return;
            PCheckBox.SetCheckState( checkbox, target.sendGreenOnLow
                ? PCheckBox.STATE_CHECKED : PCheckBox.STATE_UNCHECKED );
        }

        public void OnCheck( GameObject source, int state )
        {
            int newState = state == PCheckBox.STATE_CHECKED ? PCheckBox.STATE_UNCHECKED : PCheckBox.STATE_CHECKED;
            PCheckBox.SetCheckState( checkbox, newState );
            target.sendGreenOnLow = ( newState == PCheckBox.STATE_CHECKED );
            target.UpdateLogicCircuit();
            UpdateActiveRangeSideScreenTooltips();
        }

        private static readonly MethodInfo refreshTooltipsMethod
            = AccessTools.Method(typeof(ActiveRangeSideScreen),"RefreshTooltips");

        private void UpdateActiveRangeSideScreenTooltips()
        {
            GameObject parent = PUIUtils.GetParent( gameObject );
            if( parent == null ) // huh?
                return;
            PUIUtils.DebugObjectTree(parent);
            // The game object is called 'Activation...' and not 'Active...'.
            Transform transform = parent.transform.Find("ActivationRangeSideScreen");
            if( transform == null )
                return;
            ActiveRangeSideScreen screen = transform.gameObject.GetComponent<ActiveRangeSideScreen>();
            if( screen == null )
                return;
            refreshTooltipsMethod.Invoke( screen, null );
        }

        public override string GetTitle()
        {
            return "";
        }
    }


    [HarmonyPatch(typeof(DetailsScreen))]
    [HarmonyPatch("OnPrefabInit")]
    public static class DetailsScreen_OnPrefabInit_Patch
    {
        public static void Postfix()
        {
            PUIUtils.AddSideScreenContentWithOrdering<ThresholdsSideScreen>(
                typeof(ActiveRangeSideScreen).FullName, false);
        }
    }
}
