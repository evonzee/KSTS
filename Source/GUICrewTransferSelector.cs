﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace KSTS
{
    class GUICrewTransferSelector
    {
        public List<string> crewToDeliver = null;
        public List<string> crewToCollect = null;

        private Vessel targetVessel = null;
        private CachedShipTemplate targetTemplate = null;
        private MissionProfile missionProfile = null;

        public GUICrewTransferSelector(Vessel targetVessel, MissionProfile missionProfile)
        {
            this.targetVessel = targetVessel;
            this.missionProfile = missionProfile;
            crewToDeliver = new List<string>();
            crewToCollect = new List<string>();
        }

        public GUICrewTransferSelector(CachedShipTemplate targetTemplate, MissionProfile missionProfile)
        {
            this.targetTemplate = targetTemplate;
            this.missionProfile = missionProfile;
            crewToDeliver = new List<string>();
            crewToCollect = new List<string>();
        }

        // Returns a list of all kerbals on the crew-roster that can be transported:
        public static List<ProtoCrewMember> GetCrewRoster()
        {
            var roster = new List<ProtoCrewMember>();
            foreach (var crewMember in HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Available)) roster.Add(crewMember);
            foreach (var crewMember in HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Tourist, ProtoCrewMember.RosterStatus.Available)) roster.Add(crewMember);
            return roster;
        }

        // Shows a list of all available crew-members which the player can choose to transport and returns true, if the selection is valid:
        public bool DisplayList()
        {
            var targetCrewCapacity = 0;
            if (targetVessel != null) targetCrewCapacity = TargetVessel.GetCrewCapacity(targetVessel);
            else if (targetTemplate != null) targetCrewCapacity = targetTemplate.GetCrewCapacity();

            if (missionProfile.crewCapacity == 0 && missionProfile.missionType == MissionProfileType.TRANSPORT) // We only care about the seats on the transport-vessel during transport-missions.
            {
                GUILayout.Label("There are no available seats in the selected mission-profile.");
                return true;
            }
            else if (targetCrewCapacity == 0) // If the target has no seats, we can't transport anyone.
            {
                GUILayout.Label("The selected target-vessel can not hold any crew-members.");
                return true;
            }
            else
            {
                // Target-vessel summary:
                var targetOverload = false;
                string headline;
                if (targetVessel != null) // Existing vessel (in- & outboud transfers possible)
                {
                    // Display capacity and transfer deltas:
                    var targetVesselCrew = TargetVessel.GetCrew(targetVessel);
                    if (targetVesselCrew.Count + crewToDeliver.Count - crewToCollect.Count > targetCrewCapacity) targetOverload = true;
                    headline = "<b>" + Localizer.Format(targetVessel.vesselName) + ":</b> " + targetVesselCrew.Count.ToString() + "/" + targetCrewCapacity.ToString();
                    var transfers = " inbound: " + crewToDeliver.Count.ToString("+#;-#;0") + ", outbound: " + (-crewToCollect.Count).ToString("+#;-#;0");
                    if (targetOverload) transfers = "<color=#FF0000>" + transfers + "</color>";
                    GUILayout.Label(headline + transfers);

                    // Display Crew that is stationed on the target vessel:
                    foreach (var kerbonaut in targetVesselCrew)
                    {
                        var details = " <b>" + kerbonaut.name + "</b> (Level " + kerbonaut.experienceLevel.ToString() + " " + kerbonaut.trait + ")";
                        if (missionProfile.oneWayMission || MissionController.GetKerbonautsMission(kerbonaut.name) != null || missionProfile.missionType != MissionProfileType.TRANSPORT) GUILayout.Label(" • " + details); // Do not transport kerbals, which are flagged for another mission or there isn't even a return-trip or transport happening
                        else
                        {
                            var selected = GUILayout.Toggle(crewToCollect.Contains(kerbonaut.name), details);
                            if (selected && !crewToCollect.Contains(kerbonaut.name)) crewToCollect.Add(kerbonaut.name);
                            else if (!selected && crewToCollect.Contains(kerbonaut.name)) crewToCollect.Remove(kerbonaut.name);
                        }
                    }
                    GUILayout.Label("");
                }
                else if (targetTemplate != null) // New vessel (only inbound transfers possible)
                {
                    // Display capacity:
                    if (crewToDeliver.Count > targetCrewCapacity) targetOverload = true;
                    headline = "<b>" + targetTemplate.template.shipName + ":</b> ";
                    var seats = crewToDeliver.Count.ToString() + " / " + targetCrewCapacity.ToString() + " seat";
                    if (targetCrewCapacity != 1) seats += "s";
                    if (targetOverload) seats = "<color=#FF0000>" + seats + "</color>";
                    GUILayout.Label(headline + seats);
                }

                // Display Transport-vessel summary, if this is a transport-mission:
                var transportOutboundOverload = false;
                var transportInboundOverload = false;
                if (missionProfile.missionType == MissionProfileType.TRANSPORT)
                {
                    if (crewToDeliver.Count > missionProfile.crewCapacity) transportOutboundOverload = true;
                    if (crewToCollect.Count > missionProfile.crewCapacity) transportInboundOverload = true;

                    headline = "<b>" + Localizer.Format(missionProfile.vesselName) + ":</b> ";
                    var outbound = "outbound: " + crewToDeliver.Count.ToString() + "/" + missionProfile.crewCapacity.ToString();
                    if (transportOutboundOverload) outbound = "<color=#FF0000>" + outbound + "</color>";
                    var inbound = "";
                    if (!missionProfile.oneWayMission)
                    {
                        inbound = ", inbound: " + crewToCollect.Count.ToString() + "/" + missionProfile.crewCapacity.ToString();
                        if (transportInboundOverload) inbound = "<color=#FF0000>" + inbound + "</color>";
                    }
                    else inbound += ", inbound: -";
                    GUILayout.Label(headline + outbound + inbound);
                }

                // Display crew-rowster:
                foreach (var kerbonaut in GetCrewRoster())
                {
                    var details = " <b>" + kerbonaut.name + "</b> (Level " + kerbonaut.experienceLevel.ToString() + " " + kerbonaut.trait.ToString() + ")";
                    if (MissionController.GetKerbonautsMission(kerbonaut.name) != null) GUILayout.Label(" • " + details); // Do not transport kerbals, which are flagged for another mission
                    else
                    {
                        var selected = GUILayout.Toggle(crewToDeliver.Contains(kerbonaut.name), details);
                        if (selected && !crewToDeliver.Contains(kerbonaut.name)) crewToDeliver.Add(kerbonaut.name);
                        else if (!selected && crewToDeliver.Contains(kerbonaut.name)) crewToDeliver.Remove(kerbonaut.name);
                    }
                }

                // Check if the selection is valid (it neither overloads the target nor the transport):
                if (!targetOverload && !transportOutboundOverload && !transportInboundOverload) return true;
                return false;
            }
        }
    }
}
