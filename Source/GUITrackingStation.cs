using System;
using UnityEngine;
using ClickThroughFix;

namespace KSTS
{

    // Helper-Class to draw the window in the tracking-station scene:
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class GUITrackingStation : UnityEngine.MonoBehaviour
    {
        public void OnGUI()
        {
            if (GUI.showGui)
            {
                GUI.windowPosition = ClickThruBlocker.GUILayoutWindow(100, GUI.windowPosition, OnWindow, "", GUI.windowStyle);
            }
        }

        private void OnWindow(int windowId)
        {
            try
            {
                GUI.DrawWindow();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSTS] KSTSGUITrackingStation.OnWindow(): " + e.ToString());
            }
        }
    }
}
