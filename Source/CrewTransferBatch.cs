using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSTS
{
    public static class CrewTransferBatch
    {
        static bool same_crew_member(ProtoCrewMember a, ProtoCrewMember b)
        { return a.name == b.name && a.trait == b.trait; }

        #region Vessel

        public static bool moveCrew(Vessel toV, List<ProtoCrewMember> crew, bool spawn = true)
        {
            if (crew.Count == 0) return false;
            var moved = new List<ProtoCrewMember>(crew.Capacity);
            foreach (var kerbal in crew)
            {
                var toP = toV.parts.Find(p => p.CrewCapacity > p.protoModuleCrew.Count);
                if (toP == null) break;
                move_crew(kerbal, toP);
                moved.Add(kerbal);
            }
            if (moved.Count > 0)
            {
                Vessel.CrewWasModified(toV);
                if (spawn) respawnCrew(toV);
            }
            return moved.Count == crew.Count;
        }

        public static bool moveCrew(Vessel fromV, Vessel toV, List<ProtoCrewMember> crew, bool spawn = true)
        {
            if (crew.Count == 0) return false;
            var moved = new List<ProtoCrewMember>(crew.Capacity);
            foreach (var kerbal in crew)
            {
                Part fromP = null;
                ProtoCrewMember real_kerbal = null;
                foreach (var p in fromV.Parts)
                {
                    real_kerbal = p.protoModuleCrew.Find(c => same_crew_member(c, kerbal));
                    if (real_kerbal != null)
                    {
                        fromP = p;
                        break;
                    }
                }
                if (real_kerbal == null) continue;
                var toP = toV.parts.Find(p => p.CrewCapacity > p.protoModuleCrew.Count);
                if (toP == null) break;
                move_crew(real_kerbal, toP, fromP);
                moved.Add(real_kerbal);
            }
            if (moved.Count > 0)
            {
                Vessel.CrewWasModified(fromV, toV);
                if (spawn) respawnCrew(fromV, toV);
            }
            return moved.Count == crew.Count;
        }

        public static bool moveCrew(Vessel fromV, Vessel toV, bool spawn = true)
        {
            var all = true;
            var moved = false;
            foreach (var fromP in fromV.parts)
            {
                while (fromP.protoModuleCrew.Count > 0)
                {
                    var toP = toV.parts.Find(p => p.CrewCapacity > p.protoModuleCrew.Count);
                    if (toP == null) break;
                    move_crew(fromP.protoModuleCrew[0], toP, fromP);
                    moved = true;
                }
                if (fromP.protoModuleCrew.Count > 0)
                { all = false; break; }
            }
            if (moved)
            {
                Vessel.CrewWasModified(fromV, toV);
                if (spawn) respawnCrew(fromV, toV);
            }
            return all;
        }

        public static bool moveCrew(Vessel fromV, Part toP, bool spawn = true)
        {
            if (toP.CrewCapacity <= toP.protoModuleCrew.Count) return false;
            var all = true;
            var moved = false;
            foreach (var fromP in fromV.parts)
            {
                while (toP.protoModuleCrew.Count < toP.CrewCapacity && fromP.protoModuleCrew.Count > 0)
                {
                    move_crew(fromP.protoModuleCrew[0], toP, fromP);
                    moved = true;
                }
                if (fromP.protoModuleCrew.Count > 0)
                { all = false; break; }
            }
            if (moved)
            {
                Vessel.CrewWasModified(fromV, toP.vessel);
                if (spawn) respawnCrew(fromV, toP.vessel);
            }
            return all;
        }

        public static bool moveCrew(Part fromP, Part toP, bool spawn = true)
        {
            if (fromP.protoModuleCrew.Count == 0 ||
               toP.CrewCapacity <= toP.protoModuleCrew.Count) return false;
            while (toP.protoModuleCrew.Count < toP.CrewCapacity && fromP.protoModuleCrew.Count > 0)
                move_crew(fromP.protoModuleCrew[0], toP, fromP);
            var moved = fromP.protoModuleCrew.Count > 0;
            if (moved)
            {
                Vessel.CrewWasModified(fromP.vessel, toP.vessel);
                if (spawn) respawnCrew(fromP.vessel, toP.vessel);
            }
            return moved;
        }

        static void move_crew(ProtoCrewMember crew, Part toP, Part fromP = null)
        {
            toP.AddCrewmember(crew);
            fromP?.RemoveCrewmember(crew);
            if (fromP != null)
            {
                GameEvents.onCrewTransferred.Fire(new GameEvents.HostedFromToAction<ProtoCrewMember, Part>(crew, fromP, toP));
            }
            else
            {
                Debug.LogWarning($"Dodged exception that might have come from transfering directly from the roster. From part: {fromP?.name} To part: {toP.name}");
                GameEvents.onCrewTransferred.Fire(new GameEvents.HostedFromToAction<ProtoCrewMember, Part>(crew, toP, toP));
            }
        }

        public static void respawnCrew(Vessel V)
        {
            V.DespawnCrew();
            V.StartCoroutine(CallbackUtil.DelayedCallback(1, FlightGlobals.ActiveVessel.SpawnCrew));
        }

        public static void respawnCrew(Vessel fromV, Vessel toV)
        {
            fromV.DespawnCrew();
            toV.DespawnCrew();
            toV.StartCoroutine(CallbackUtil.DelayedCallback(1, FlightGlobals.ActiveVessel.SpawnCrew));
        }
        #endregion
    }
}