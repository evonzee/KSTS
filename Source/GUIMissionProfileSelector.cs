﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace KSTS
{
    class GUIMissionProfileSelector
    {
        public const string SELECTED_DETAILS_ALTITUDE = "altitude";
        public const string SELECTED_DETAILS_PAYLOAD = "payload";

        private Vector2 scrollPos = Vector2.zero;
        private int selectedIndex = -1;
        public MissionProfile selectedProfile = null;

        public double? filterMass = null;
        public double? filterAltitude = null;
        public int? filterCrewCapacity = null;
        public bool? filterRoundTrip = null;
        public List<string> filterDockingPortTypes = null;
        public CelestialBody filterBody = null;
        public MissionProfileType? filterMissionType = null;

        // Makes sure that the cached settings are still valid (eg if the player has deleted the selected profile):
        private void CheckInternals()
        {
            if (!MissionController.missionProfiles.Values.Contains(selectedProfile) || selectedIndex < 0 || selectedIndex >= MissionController.missionProfiles.Count)
            {
                selectedProfile = null;
                selectedIndex = -1;
            }
        }

        // Displays the currently selected mission-profile and returns true, if the player has deselected the profile:
        public bool DisplaySelected(string showDetails=SELECTED_DETAILS_ALTITUDE)
        {
            CheckInternals();
            if (this.selectedProfile == null) return true;
            GUILayout.BeginHorizontal();
            GUILayout.Label("<size=14><b>Mission Profile:</b></size>", new GUIStyle(GUI.labelStyle) { stretchWidth = true });

            var details = "N/A";
            if (showDetails == SELECTED_DETAILS_ALTITUDE) details = "Max Altitude: " + GUI.FormatAltitude(selectedProfile.maxAltitude);
            else if (showDetails == SELECTED_DETAILS_PAYLOAD) details = "Max Payload: " + this.selectedProfile.payloadMass.ToString("0.00t");
            if (GUILayout.Button("<size=14><color=#F9FA86><b>" + this.selectedProfile.profileName + "</b></color> ("+details+")</size>", new GUIStyle(GUI.buttonStyle) { alignment = TextAnchor.MiddleRight, stretchWidth = false, fixedWidth = 320 }))
            {
                this.selectedProfile = null; // Back to the previous selection
                this.selectedIndex = -1;
            }
            GUILayout.EndHorizontal();
            return this.selectedProfile == null;
        }

        // Shows a list of all available mission-profiles and returns true, if the player has selected one:
        public bool DisplayList()
        {
            CheckInternals();
            GUILayout.Label("<size=14><b>Mission Profile:</b></size>");
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUI.scrollStyle);
            var green = "#00FF00";
            var red = "#FF0000";

            // Show a list with all possible mission-profiles:
            if (MissionController.missionProfiles.Count == 0)
            {
                GUILayout.Label("No recordings found, switch to a new vessel to start recording a mission.");
            }
            else
            {
                var contents = new List<GUIContent>();
                var invalidIndices = new List<int>(); // Profiles which fall out of the defined filters will get noted here.
                var index = 0;
                foreach (var missionProfile in MissionController.missionProfiles.Values)
                {
                    var isValidProfile = true;
                    var color = "";

                    // Build the descriptive text with highlighting:
                    var description = "<color=#F9FA86><b>" + missionProfile.profileName + "</b></color> <color=#FFFFFF>(" + missionProfile.vesselName + ")\n";
                    description += "<b>Mass:</b> " + missionProfile.launchMass.ToString("0.0t") + ", Cost: " + missionProfile.launchCost.ToString("#,##0√") + ", ";

                    // One-Way or Round-Trip:
                    var missionRouteDetails = "";
                    if (missionProfile.oneWayMission) missionRouteDetails = "one-way";
                    else missionRouteDetails = "round-trip";
                    if (this.filterRoundTrip != null)
                    {
                        if (this.filterRoundTrip != missionProfile.oneWayMission) { isValidProfile = false; color = red; }
                        else color = green;
                        missionRouteDetails = "<color=" + color + ">" + missionRouteDetails + "</color>";
                    }
                    description += missionRouteDetails + "\n";

                    // Mission-Type:
                    var missionType = MissionProfile.GetMissionProfileTypeName(missionProfile.missionType);
                    if (this.filterMissionType != null)
                    {
                        if (this.filterMissionType != missionProfile.missionType) { isValidProfile = false; color = red; }
                        else color = green;
                        missionType = "<color=" + color + ">" + missionType + "</color>";
                    }
                    description += "<b>Type:</b> " + missionType + ", ";

                    description += "<b>Duration:</b> " + GUI.FormatDuration(missionProfile.missionDuration) + "\n";

                    // Docking-Ports:
                    var dockingPorts = "";
                    if (missionProfile.missionType == MissionProfileType.TRANSPORT || this.filterDockingPortTypes != null)
                    {
                        var hasFittingPort = false;
                        var portNumber = 0;
                        if (missionProfile.dockingPortTypes != null)
                        {
                            foreach (var portType in missionProfile.dockingPortTypes)
                            {
                                if (portNumber > 0) dockingPorts += ", ";
                                if (this.filterDockingPortTypes != null && this.filterDockingPortTypes.Contains(portType))
                                {
                                    hasFittingPort = true;
                                    dockingPorts += "<color=" + green + ">" + TargetVessel.TranslateDockingPortName(portType) + "</color>";
                                }
                                else dockingPorts += TargetVessel.TranslateDockingPortName(portType);
                                portNumber++;
                            }
                        }
                        if (portNumber == 0) dockingPorts = "N/A";
                        if (this.filterDockingPortTypes != null && !hasFittingPort)
                        {
                            dockingPorts = "<color=" + red + ">" + dockingPorts + "</color>";
                            isValidProfile = false;
                        }
                    }
                    if (dockingPorts != "") description += "<b>Docking-Ports:</b> " + dockingPorts + "\n";

                    // Payload:
                    var payloadMass = missionProfile.payloadMass.ToString("0.0t");
                    if (this.filterMass != null)
                    {
                        // We only display one digit after the pount, so we should round here to avoid confustion:
                        if (Math.Round((double)this.filterMass, 1) > Math.Round(missionProfile.payloadMass, 1)) { isValidProfile = false; color = red; }
                        else color = green;
                        payloadMass = "<color=" + color + ">" + payloadMass + "</color>";
                    }
                    description += "<b>Payload:</b> " + payloadMass;

                    // Body:
                    var bodyName = missionProfile.bodyName;
                    if (this.filterBody != null)
                    {
                        if (this.filterBody.bodyName != missionProfile.bodyName) { isValidProfile = false; color = red; }
                        else color = green;
                        bodyName = "<color=" + color + ">" + bodyName + "</color>";
                    }
                    description += " to " + bodyName;

                    // Altitude:
                    var maxAltitude = GUI.FormatAltitude(missionProfile.maxAltitude);
                    if (this.filterAltitude != null)
                    {
                        if (this.filterAltitude > missionProfile.maxAltitude) { isValidProfile = false; color = red; }
                        else color = green;
                        maxAltitude = "<color=" + color + ">" + maxAltitude + "</color>";
                    }
                    description += " @ " + maxAltitude + "\n";

                    // Crew-Capacity:
                    var crewCapacity = missionProfile.crewCapacity.ToString("0");
                    if (this.filterCrewCapacity != null)
                    {
                        if (this.filterCrewCapacity > missionProfile.crewCapacity) { isValidProfile = false; color = red; }
                        else color = green;
                        crewCapacity = "<color=" + color + ">" + crewCapacity + "</color>";
                    }
                    description += "<b>Crew-Capacity:</b> " + crewCapacity;

                    description += "</color>";
                    contents.Add(new GUIContent(description, GUI.GetVesselThumbnail(missionProfile.vesselName)));

                    if (!isValidProfile) invalidIndices.Add(index);
                    index++;
                }

                var newSelection = GUILayout.SelectionGrid(selectedIndex, contents.ToArray(), 1, GUI.selectionGridStyle);
                if (newSelection != selectedIndex && !invalidIndices.Contains(newSelection))
                {
                    selectedIndex = newSelection;
                    selectedProfile = MissionController.missionProfiles.Values.ToList()[selectedIndex];
                }
            }

            GUILayout.EndScrollView();
            return this.selectedProfile != null;
        }
    }
}
