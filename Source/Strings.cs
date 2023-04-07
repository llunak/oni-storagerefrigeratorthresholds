using TUNING;

namespace STRINGS
{
    public class STORAGEREFRIGERATORTHRESHOLDS
    {
        public static LocString ACTIVATE_TOOLTIP = "Sends a " + UI.FormatAsAutomationState("Green Signal", UI.AutomationState.Active) + " when storage is <b>{0}%</b> full, until it is less than <b>{1}% (High Threshold)</b> full";

        public static LocString DEACTIVATE_TOOLTIP = "Sends a " + UI.FormatAsAutomationState("Red Signal", UI.AutomationState.Standby) + " when storage is less than <b>{0}%</b> full, until it is <b>{1}% (Low Threshold)</b> full";
    }
}
