﻿// MorphInjectorProperties.cs created by Iron Wolf for Pawnmorph on 09/13/2021 7:34 AM
// last updated 09/13/2021  7:34 AM

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pawnmorph.Composable.Hediffs;
using Pawnmorph.Hediffs.Composable;
using RimWorld;
using UnityEngine;
using Verse;

namespace Pawnmorph
{
    /// <summary>
    ///     properties for dynamically generated injectors generated by <see cref="MorphDef" />
    /// </summary>
    public class MorphInjectorProperties
    {
        private const string DEFAULT_TRADER_TAG = "ExoticMisc";

        /// <summary>
        /// The label for the generated injector 
        /// </summary>
        public string label;

        /// <summary>
        /// The work amount
        /// </summary>
        public int workAmount = 4000; 

        /// <summary>
        /// The stat bases for the injector 
        /// </summary>
        public List<StatModifier> statBases;
        /// <summary>
        /// The tech level of the injector 
        /// </summary>
        public TechLevel techLevel = TechLevel.Industrial;
        /// <summary>
        /// The trader tags for the injector 
        /// </summary>
        public List<string> traderTags;
        /// <summary>
        /// if to add the default trader tags to the trader tags list 
        /// </summary>
        public bool useDefaultTags = true;
        /// <summary>
        /// The cost list for the recipe
        /// ignored if <see cref="recipeMaker"/> is explicitly set 
        /// </summary>
        public List<ThingDefCountClass> costList;
        /// <summary>
        /// how much slurry is needed to make the injector 
        /// </summary>
        public int slurryCost = 3; //1 is for debug, make a reasonable default value 
        /// <summary>
        /// The neutroamine cost to make this injector 
        /// </summary>
        public int neutroamineCost = 4;
        /// <summary>
        /// The mutanite cost to make this injector 
        /// </summary>
        public int mutaniteCost = 0;


        /// <summary>
        /// list of additional outcome doers on the injector 
        /// </summary>
        public List<IngestionOutcomeDoer> outcomeDoers; 

        /// <summary>
        /// The graphic data for the injector 
        /// </summary>
        public GraphicData graphicData;

        [UsedImplicitly(ImplicitUseKindFlags.Assign)] RecipeMakerProperties recipeMaker;


        [Unsaved] private RecipeMakerProperties
            _recipeMakerGenerated;

        [Unsaved] private List<ThingDefCountClass> _costListGenerated;
        /// <summary>
        /// The description of the injector 
        /// </summary>
        public string description;

        /// <summary>
        /// Gets the recipe maker.
        /// </summary>
        /// <value>
        /// The recipe maker.
        /// </value>
        public RecipeMakerProperties RecipeMaker => _recipeMakerGenerated; 

        /// <summary>
        /// Gets the cost list.
        /// </summary>
        /// <value>
        /// The cost list.
        /// </value>
        [NotNull]
        public IReadOnlyList<ThingDefCountClass> CostList => _costListGenerated; 
        
        /// <summary>
        ///     gets all configuration errors with this instance.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ConfigErrors()
        {
            if (costList != null)
            {
                if (mutaniteCost > 0 && costList.Any(c => c.thingDef == PMThingDefOf.Mutanite))
                {
                    yield return $"{nameof(costList)} includes mutanite but {nameof(mutaniteCost)} is also set!";
                }

                if (slurryCost > 0 && costList.Any(c => c.thingDef == PMThingDefOf.MechaniteSlurry))
                    yield return $"{nameof(costList)} includes slurry but {nameof(slurryCost)} is also set!";

                if (neutroamineCost > 0 && costList.Any(c => c.thingDef == PMThingDefOf.Neutroamine))
                    yield return $"{nameof(costList)} includes neutromine but {nameof(neutroamineCost)} is also set!";
            }
        }



        /// <summary>
        ///     Resolves the references.
        /// </summary>
        public void ResolveReferences(string animal)
        {
            if (string.IsNullOrEmpty(label))
                label = "injectorLabel".Translate(animal);
            if (string.IsNullOrEmpty(description))
                description = "injectorDescription".Translate(animal);

            if (useDefaultTags)
            {
                traderTags = traderTags ?? new List<string>();
                if (!traderTags.Contains(DEFAULT_TRADER_TAG))
                    traderTags.Add(DEFAULT_TRADER_TAG);
            }

            _costListGenerated = new List<ThingDefCountClass>();
            if (costList != null) _costListGenerated.AddRange(costList);

            if (mutaniteCost > 0) _costListGenerated.Add(new ThingDefCountClass(PMThingDefOf.Mutanite, mutaniteCost));

            if (slurryCost > 0)
                _costListGenerated.Add(new ThingDefCountClass(PMThingDefOf.MechaniteSlurry, slurryCost));

            if (neutroamineCost > 0) _costListGenerated.Add(new ThingDefCountClass(PMThingDefOf.Neutroamine, neutroamineCost));

            if (graphicData == null)
                graphicData = new GraphicData
                {
                    texPath = "Things/Item/Drug/Specific",
                    graphicClass = typeof(Graphic_Single)
                };

            if (recipeMaker != null) _recipeMakerGenerated = recipeMaker;
            else
                _recipeMakerGenerated = new RecipeMakerProperties
                {
                    productCount = 1,
                    recipeUsers = new List<ThingDef> {PMThingDefOf.PM_InjectorLab},
                    researchPrerequisite = PMResearchProjectDefOf.Injectors,
                    useIngredientsForColor = false,
                    soundWorking = PMSoundDefOf.Recipe_CookMeal,
                    effectWorking = PMEffecterDefOf.Cook,
                    workSkill = SkillDefOf.Intellectual,
                    workSpeedStat = PMStatDefOf.DrugSynthesisSpeed,
                    workAmount = workAmount,
                };

            if (statBases == null) statBases = new List<StatModifier>();
            if (!statBases.Any(s => s.stat == StatDefOf.MarketValue))
            {
                StatModifier mkValue = new StatModifier()
                {
                    stat = StatDefOf.MarketValue,
                    value = CalculateMarketValue()
                };
                statBases.Add(mkValue);
            }
        }

        private const float WORK_TO_VALUE = 1; 
        private float CalculateMarketValue()
        {
            return CostList.Sum(s => s.count * s.thingDef.GetStatValueAbstract(StatDefOf.MarketValue))
                 + WORK_TO_VALUE * workAmount; 
        }
    }

    /// <summary>
    ///     properties for dynamically generated morph hediffs
    /// </summary>
    public class MorphHediffProperties
    {
        /// <summary>
        ///     The label color of the injector hediff
        /// </summary>
        public Color labelColor = new Color(0.3f, .26f, .71f);

        /// <summary>
        ///     The mutagen to use
        /// </summary>
        [CanBeNull] public MutagenDef mutagen;

        /// <summary>
        ///     The tf alert, if null no alert stage will be generated
        /// </summary>
        [CanBeNull] public StageAlert tfAlert;

        /// <summary>
        ///     The chance for this hediff to remove a non morph part chance
        /// </summary>
        public float removeNonMorphPartChance;

        /// <summary>
        ///     The tf settings to use, if null no tf stage will be generated
        /// </summary>
        [CanBeNull] public TFMiscSettings tfSettings;

        /// <summary>
        ///     The hunger rate factor
        /// </summary>
        public float hungerRateFactor;

        /// <summary>
        ///     The cap mods to use
        /// </summary>
        [CanBeNull] public List<PawnCapacityModifier> capMods;

        /// <summary>
        /// The aspect givers
        /// </summary>
        [CanBeNull] public List<AspectGiver> aspectGivers;

        /// <summary>
        /// The label of the generated hediff. 
        /// </summary>
        [CanBeNull]
        public string label;

        /// <summary>
        /// The description of the generated hediff 
        /// </summary>
        [CanBeNull]
        public string description;
    }
}