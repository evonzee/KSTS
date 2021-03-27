using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using KSP.Localization;

namespace KSTS
{
    public delegate bool TryParse<T>(string str, out T value);

    public static class Extensions
    {
        [ConditionalAttribute("DEBUG")]
        public static void Log(string message)
        {
            UnityEngine.Debug.Log("HyperEdit: " + message);
        }

        public static void TryGetValue<T>(this ConfigNode node, string key, ref T value, TryParse<T> tryParse)
        {
            var strvalue = node.GetValue(key);
            if (strvalue == null)
                return;
            if (tryParse == null)
            {
                // `T` better be `string`...
                value = (T)(object)strvalue;
                return;
            }

            if (tryParse(strvalue, out var temp) == false)
            {
                return;
            }
            value = temp;
        }

        public static void RealCbUpdate(this CelestialBody body)
        {
            body.CBUpdate();
            try
            {
                body.resetTimeWarpLimits();
            }
            catch (NullReferenceException)
            {
                Log("resetTimeWarpLimits threw NRE " + (TimeWarp.fetch == null ? "as expected" : "unexpectedly"));
            }

            // CBUpdate doesn't update hillSphere
            // http://en.wikipedia.org/wiki/Hill_sphere
            var orbit = body.orbit;
            var cubedRoot = Math.Pow(body.Mass / orbit.referenceBody.Mass, 1.0 / 3.0);
            body.hillSphere = orbit.semiMajorAxis * (1.0 - orbit.eccentricity) * cubedRoot;

            // Nor sphereOfInfluence
            // http://en.wikipedia.org/wiki/Sphere_of_influence_(astrodynamics)
            body.sphereOfInfluence = orbit.semiMajorAxis * Math.Pow(body.Mass / orbit.referenceBody.Mass, 2.0 / 5.0);
        }

        public static void PrepVesselTeleport(this Vessel vessel)
        {
            if (vessel.Landed)
            {
                vessel.Landed = false;
                Log("Set ActiveVessel.Landed = false");
            }
            if (vessel.Splashed)
            {
                vessel.Splashed = false;
                Log("Set ActiveVessel.Splashed = false");
            }
            if (vessel.landedAt != string.Empty)
            {
                vessel.landedAt = string.Empty;
                Log("Set ActiveVessel.landedAt = \"\"");
            }
            var parts = vessel.parts;
            if (parts != null)
            {
                var killcount = 0;
                foreach (var part in parts.Where(part => part.Modules.OfType<LaunchClamp>().Any()).ToList())
                {
                    killcount++;
                    part.Die();
                }
                if (killcount != 0)
                {
                    Log($"Removed {killcount} launch clamps from {Localizer.Format( vessel.vesselName)}");
                }
            }
        }

        /// <summary>
        /// Sphere of Influence.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public static double Soi(this CelestialBody body)
        {
            var radius = body.sphereOfInfluence * 0.95;
            if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0 || radius > 200000000000)
            {
                radius = 200000000000; // jool apo = 72,212,238,387
            }
            return radius;
        }

        public static double Mod(this double x, double y)
        {
            var result = x % y;
            if (result < 0)
            {
                result += y;
            }
            return result;
        }

        public static string VesselToString(this Vessel vessel)
        {
            if (FlightGlobals.fetch != null && FlightGlobals.ActiveVessel == vessel)
            {
                return "Active vessel";
            }
            return Localizer.Format(vessel.vesselName);
        }

        public static string OrbitDriverToString(this OrbitDriver driver)
        {
            if (driver == null)
            {
                return null;
            }
            if (driver.celestialBody != null)
            {
                return driver.celestialBody.bodyName;
            }
            if (driver.vessel != null)
            {
                return driver.vessel.VesselToString();
            }
            if (!string.IsNullOrEmpty(driver.name))
            {
                return driver.name;
            }
            return "Unknown";
        }

        /// <summary>
        /// Convert Celestial Body to human readable form.
        /// </summary>
        /// <param name="body">Celestial Body</param>
        /// <returns>The name of the Celestial Body.</returns>
        public static string CbToString(this CelestialBody body)
        {
            return body.bodyName;
        }

        public static bool CbTryParse(string bodyName, out CelestialBody body)
        {
            body = FlightGlobals.Bodies == null ? null : FlightGlobals.Bodies.FirstOrDefault(cb => cb.name == bodyName);
            return body != null;
        }

    }
}
