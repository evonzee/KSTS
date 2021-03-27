﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if true

namespace KSTS
{


    /////////////////////////////////
    /// Following from:  
    /// http://forum.kerbalspaceprogram.com/threads/119609-Manually-generating-ship-thumbnail
    /// 
    public class ThumbnailHelper
    {
        /// <summary>
        /// Generates a thumbnail exactly like the one KSP generates automatically.
        /// Behaves exactly like ShipConstruction.CaptureThumbnail() but allows customizing the resolution.
        /// </summary>
        public static void CaptureThumbnail(ShipConstruct ship, int resolution, string saveFolder, string craftName)
        {
            if (ship.shipFacility != EditorFacility.VAB)
            {
                CraftThumbnail.TakeSnaphot(ship, resolution, saveFolder, craftName, 35, 135, 35, 135, 0.9f);
            }
            else
            {
                CraftThumbnail.TakeSnaphot(ship, resolution, saveFolder, craftName, 45, 45, 45, 45, 0.9f);
            }
        }



        public static void CaptureThumbnail(ShipConstruct ship, int resolution,
                float elevation, float azimuth, float pitch, float heading, float fov, string saveFolder, string craftName)
        {
            CraftThumbnail.TakeSnaphot(ship, resolution, saveFolder, craftName, elevation, azimuth, pitch, heading, fov);
        }
#if false
		/// <summary>
		/// Builds the path to the auto-generated thumbnail for the given ship.
		/// </summary>
		public static string GetCraftThumbnailPath(ShipConstruct ship)
		{
			return string.Format("thumbs/{0}_{1}_{2}.png", HighLogic.SaveFolder, ShipConstruction.GetShipsSubfolderFor(ship.shipFacility), ship.shipName);
		}
#endif
    }
}
# endif