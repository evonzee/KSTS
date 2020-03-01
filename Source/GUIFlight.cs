using System;
using UnityEngine;
using ClickThroughFix;

namespace KSTS
{
    // Helper-Class to draw the window in the flight scene:
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GUIFlight : UnityEngine.MonoBehaviour
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
                Debug.LogError("KSTSGUIFlight.OnWindow(): " + e.ToString());
            }
        }
    }
}
