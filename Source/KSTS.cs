﻿using System;
using System.Linq;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using StageRecovery;

using static KSTS.Statics;


namespace KSTS
{
    public class Saveable
    {
        // Returns a config-node which contains all public attributes of this object to be saved:
        public ConfigNode CreateConfigNode(string name)
        {
            var node = new ConfigNode(name);
            var fields = this.GetType().GetFields();
            foreach (var field in fields)
            {
                if (!field.IsPublic) continue; // Only public attributes should contain persistent values worth saving.
                if (field.IsLiteral) continue; // Don't save constants.
                if (field.GetValue(this) == null) continue;

                // Save all elements of the list by creating multiple config-nodes with the same name:
                if (field.FieldType == typeof(List<string>))
                {
                    var list = (List<string>)field.GetValue(this);
                    if (list != null) foreach (var element in list)
                    {
                        node.AddValue(field.Name.ToString(), element);
                    }
                }
                // Save dictionary-values in a sub-node:
                else if (field.FieldType == typeof(Dictionary<string, double>))
                {
                    var dictNode = node.AddNode(field.Name.ToString());
                    foreach (var item in (Dictionary<string, double>)field.GetValue(this))
                    {
                        dictNode.AddValue(item.Key, item.Value.ToString());
                    }
                }
                // Use orbit helper-class to save compley orbit-object:
                else if (field.FieldType == typeof(Orbit))
                {
                    node.AddNode(GUIOrbitEditor.SaveOrbitToNode((Orbit)field.GetValue(this)));
                }
                // Default; save as string:
                else node.AddValue(field.Name.ToString(), field.GetValue(this));
            }
            return node;
        }

        // Creates a new object from the given config-node to fill all of the objects public attributes:
        public static object CreateFromConfigNode(ConfigNode node, object obj)
        {
            var fields = obj.GetType().GetFields();
            foreach (var field in fields)
            {
                if (!field.IsPublic) continue; // Only public attributes should contain persistent values worth saving.
                if (field.IsLiteral) continue; // Don't load constants.
                if (!node.HasValue(field.Name.ToString()) && !node.HasNode(field.Name.ToString())) continue; // Should only happen when the savegame is from a different version.
                if (field.FieldType == typeof(double)) field.SetValue(obj, double.Parse(node.GetValue(field.Name.ToString())));
                else if (field.FieldType == typeof(MissionType)) field.SetValue(obj, Enum.Parse(typeof(MissionType), node.GetValue(field.Name.ToString())));
                else if (field.FieldType == typeof(MissionProfileType)) field.SetValue(obj, Enum.Parse(typeof(MissionProfileType), node.GetValue(field.Name.ToString())));
                else if (field.FieldType == typeof(FlightRecordingStatus)) field.SetValue(obj, Enum.Parse(typeof(FlightRecordingStatus), node.GetValue(field.Name.ToString())));
                else if (field.FieldType == typeof(int)) field.SetValue(obj, int.Parse(node.GetValue(field.Name.ToString())));
                else if (field.FieldType == typeof(bool)) field.SetValue(obj, bool.Parse(node.GetValue(field.Name.ToString())));
                else if (field.FieldType == typeof(Guid) || field.FieldType == typeof(Guid?)) field.SetValue(obj, new Guid(node.GetValue(field.Name.ToString())));
                // Restore String-Lists by loading all nodes with the name of this field:
                else if (field.FieldType == typeof(List<string>))
                {
                    var list = new List<string>();
                    foreach (var value in node.GetValues(field.Name.ToString()))
                    {
                        list.Add(value);
                    }
                    field.SetValue(obj, list);
                }
                // Restore the dictionary by loading all the values in the sub-node:
                else if (field.FieldType == typeof(Dictionary<string, double>))
                {
                    var dict = new Dictionary<string, double>();
                    var dictNode = node.GetNode(field.Name.ToString());
                    foreach (ConfigNode.Value item in dictNode.values)
                    {
                        dict.Add(item.name, double.Parse(item.value));
                    }
                    field.SetValue(obj, dict);
                }
                // Load orbit via the helper-class:
                else if (field.FieldType == typeof(Orbit)) field.SetValue(obj, GUIOrbitEditor.CreateOrbitFromNode(node.GetNode(field.Name.ToString())));
                // Fallback; try to store the value as string:
                else field.SetValue(obj, node.GetValue(field.Name.ToString()));
            }
            return obj;
        }
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class KSTS : UnityEngine.MonoBehaviour
    {
        private static bool initialized = false;
        public static Dictionary<string, AvailablePart> partDictionary = null;
        public static Dictionary<string, PartResourceDefinition> resourceDictionary = null;
        public static Dictionary<string, string> stageParentDictionary = null;
        private string lastActiveVesselId;

        // Is called when this Addon is first loaded to initializes all values (eg registration of event-handlers and creation
        // of original-stats library).
        public void Awake()
        {
            try
            {
                FlightRecorder.Initialize();
                MissionController.Initialize();

                // Build dictionary of all parts for easier access:
                if (KSTS.partDictionary == null)
                {
                    KSTS.partDictionary = new Dictionary<string, AvailablePart>();
                    foreach (var part in PartLoader.LoadedPartsList)
                    {
                        if (KSTS.partDictionary.ContainsKey(part.name.ToString()))
                        {
                            Debug.LogError("duplicate part-name '" + part.name.ToString() + "'");
                            continue;
                        }
                        KSTS.partDictionary.Add(part.name.ToString(), part);
                    }
                }

                // Build a dictionay of all resources for easier access:
                if (KSTS.resourceDictionary == null)
                {
                    KSTS.resourceDictionary = new Dictionary<string, PartResourceDefinition>();
                    foreach (var resourceDefinition in PartResourceLibrary.Instance.resourceDefinitions)
                    {
                        KSTS.resourceDictionary.Add(resourceDefinition.name.ToString(), resourceDefinition);
                    }
                }

                // Invoke the timer-function every second to run background-code:
                if (!IsInvoking("Timer"))
                {
                    InvokeRepeating("Timer", 1, 1);
                }

                // In case the Stage Recovery Mod is installed, add lists and handlers to track the separation and recovery of stages:
                if (StageRecoveryAPI.StageRecoveryAvailable && KSTS.stageParentDictionary == null)
                {
                    Log.Warning("detected stage recovery mod");
                    stageParentDictionary = new Dictionary<string, string>();
                    StageRecoveryAPI.AddRecoverySuccessEvent((vessel, array, str) =>
                    {
                        if (StageRecoveryAPI.StageRecoveryEnabled)
                        {
                            FlightRecorder.OnStageRecovered(vessel.id.ToString(), array[1]);
                        }
                    });

                    GameEvents.onStageSeparation.Add(new EventData<EventReport>.OnEvent(this.onStageSeparation));
                    GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(this.onVesselModified));
                }

                // Execute the following code only once:
                if (KSTS.initialized) return;
                DontDestroyOnLoad(this);
                KSTS.initialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError("Awake(): " + e.ToString());
            }
        }

        // Helper-function to allow us to access the vessel-id in the "onStageSeparation" which detached the most recent stage:
        private void onVesselModified(Vessel data)
        {
            lastActiveVesselId = data.id.ToString();
        }

        // Fired for each stage on separation:
        private void onStageSeparation(EventReport data)
        {
            var stageVesselId = data.origin.vessel.id.ToString();
            Log.Warning("detected stage separation (" + stageVesselId + " from " + lastActiveVesselId + ")");
            stageParentDictionary.Add(stageVesselId, lastActiveVesselId);
        }

        public class StageRecoveredEventArgs : EventArgs
        {
            public Vessel Vessel { get; set; }
            public float FundsRecovered { get; set; }
        }

        public void Timer()
        {
            try
            {
                // Don't update while not in game:
                if (HighLogic.LoadedScene == GameScenes.MAINMENU || HighLogic.LoadedScene == GameScenes.CREDITS || HighLogic.LoadedScene == GameScenes.SETTINGS) return;

                // Call all background-jobs:
                FlightRecorder.Timer();
                MissionController.Timer();
            }
            catch (Exception e)
            {
                Debug.LogError("Timer(): " + e.ToString());
            }
        }

        // Returns the currently avaialabel amount of funds:
        public static double GetFunds()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && Funding.Instance != null)
            {
                return Funding.Instance.Funds;
            }
            else
            {
                // When we are playing a sandbox-game we have "unlimited" funds:
                return 999999999;
            }
        }

        // Adds the given amount of funds in a career-game:
        public static void AddFunds(double funds)
        {
            // This only makes sense in career-mode:
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && Funding.Instance != null)
            {
                Funding.Instance.AddFunds(funds, TransactionReasons.VesselRollout);
            }
        }
    }

    // This class handels load- and save-operations.
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER)]
    class KSTSScenarioModule : ScenarioModule
    {
        public override void OnSave(ConfigNode node)
        {
            try
            {
                /*
                 * When transporting an available kerbal to a ship or recovering one from orbit, we manipulate the (unloaded) vessel's crew
                 * as well as the roster-status of the kerbal. This is apparently not expected by KSP's core functionality, because there
                 * seems to be a secret list of roster-status which is enforced when the game is safed:
                 * 
                 * [WRN 15:16:14.678] [ProtoCrewMember Warning]: Crewmember Sierina Kerman found inside a part but status is set as missing. Vessel must have failed to save earlier. Restoring assigned status.
                 * [WRN 15:17:42.913] [ProtoCrewMember Warning]: Crewmember Sierina Kerman found assigned but no vessels reference him. Sierina Kerman set as missing.
                 * 
                 * Afterwards these kerbals would be lost for the player, which is why we have to use the workaround below to revert these
                 * changes and make sure each kerbal has the correct status. This effectively disables the "missing" status as these kerbals
                 * will always respawn, but I haven't seen a valid use-case for this thus far, so it is probably fine.
                 */
                if (HighLogic.CurrentGame.CrewRoster.Count > 0 && FlightGlobals.Vessels.Count > 0)
                {
                    // Build a list of all Kerbals which are assigned to vessels:
                    var vesselCrewNames = new List<string>();
                    foreach (var vessel in FlightGlobals.Vessels)
                    {
                        foreach (var crewMember in TargetVessel.GetCrew(vessel)) vesselCrewNames.Add(crewMember.name);
                    }

                    // Build a list of all kerbals which we could have manipulated:
                    var kerbals = new List<ProtoCrewMember>();
                    foreach (var kerbal in HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew)) kerbals.Add(kerbal);
                    foreach (var kerbal in HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Tourist)) kerbals.Add(kerbal);

                    // Check those kerbals against our vessel-list and maybe restore their correct status:
                    foreach (var kerbal in kerbals)
                    {
                        if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Dead) continue;
                        if (vesselCrewNames.Contains(kerbal.name) && kerbal.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
                        {
                            Log.Warning("setting kerbal " + kerbal.name + " from " + kerbal.rosterStatus.ToString() + " to Assigned (see code for more info)");
                            kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                        }
                        else if (!vesselCrewNames.Contains(kerbal.name) && kerbal.rosterStatus != ProtoCrewMember.RosterStatus.Available)
                        {
                            Log.Warning("setting kerbal " + kerbal.name + " from " + kerbal.rosterStatus.ToString() + " to Available (see code for more info)");
                            kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                        }
                    }
                }

                FlightRecorder.SaveRecordings(node);
                MissionController.SaveMissions(node);
            }
            catch (Exception e)
            {
                Debug.LogError("OnSave(): " + e.ToString());
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            Log.Warning("KSTS: OnLoad");
            try
            {
                FlightRecorder.LoadRecordings(node);
                MissionController.LoadMissions(node);
                GUI.Reset();
            }
            catch (Exception e)
            {
                Debug.LogError("OnLoad(): " + e.ToString());
            }
        }
    }
}
