using System;
using UnityEngine;
using ClickThroughFix;

namespace KSTS
{

    // Helper-Class to draw the window in the tracking-station scene:
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class GUITrackingStation : MonoBehaviour
    {
        static int winId;

        public void Start()
        {
            Mission.InitKAC();
            winId = SpaceTuxUtility.WindowHelper.NextWindowId("KSPS.GUITrackingStation");
        }

        public void OnGUI()
        {
            if (GUI.showGui)
            {
                GUI.windowPosition = ClickThruBlocker.GUILayoutWindow(winId, GUI.windowPosition, OnWindow, "", GUI.windowStyle);
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
                Debug.LogError("KSTSGUITrackingStation.OnWindow(): " + e.ToString());
            }
        }
    }
}
