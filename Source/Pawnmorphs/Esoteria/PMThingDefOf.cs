﻿// PMThingDefOf.cs modified by Iron Wolf for Pawnmorph on 09/08/2019 9:46 AM
// last updated 09/08/2019  9:46 AM

using RimWorld;
using Verse;
#pragma warning disable 1591
namespace Pawnmorph
{
    /// <summary> Static container for commonly referenced thing defs. </summary>
    [DefOf]
    public static class PMThingDefOf
    {
        static PMThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ThingDef));
        }

        public static ThingDef Plant_ChaoBulb;
        public static ThingDef Plant_GnarledTree; 
    }
}