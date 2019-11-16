﻿// MutationTracker.cs created by Nick M(Iron Wolf)  Pawnmorph on 09/12/2019 9:11 AM
// last updated 09/12/2019  9:11 AM

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pawnmorph.Hediffs;
using Pawnmorph.Utilities;
using UnityEngine;
using Verse;
using static Pawnmorph.DebugUtils.DebugLogUtils; 
namespace Pawnmorph
{
    /// <summary> Tracker comp for tracking the current influence a pawn has of a given morph. </summary>
    public class MutationTracker : ThingComp, IEnumerable<KeyValuePair<MorphDef, float>>
    {
        private Dictionary<MorphDef, float> _influenceLookup = new Dictionary<MorphDef, float>();
        private List<VTuple<MorphDef, float>> _normalizedInfluencesCache = new List<VTuple<MorphDef, float>>();
        private bool _reCalcInfluences = true;
        private float _totalNormalizedInfluence;

        /// <summary>
        /// Gets the total number of mutations on the pawn being tracked.
        /// </summary>
        /// <value>
        /// The mutations count.
        /// </value>
        public int MutationsCount { get; private set; }
        /// <summary> Get the current influence associated with the given key. </summary>
        public float this[MorphDef key] => _influenceLookup.TryGetValue(key);

        /// <summary>
        /// Gets the total normalized influence of all morphs on the tracked pawn 
        /// </summary>
        /// <value>
        /// The total normalized influence.
        /// </value>
        public float TotalNormalizedInfluence //cache this to help with performance 
        {
            get
            {
                if (_reCalcInfluences)
                {
                    _totalNormalizedInfluence = NormalizedInfluences.Sum(f => f.second); 
                }

                return _totalNormalizedInfluence;
            }
        }

        /// <summary> An enumerable collection of influences normalized against each other and the remaining human influence. </summary>
        public IEnumerable<VTuple<MorphDef, float>> NormalizedInfluences //cache this to help with performance 
        {
            get
            {
                if (_reCalcInfluences)
                {
                    _normalizedInfluencesCache.Clear();
                    _normalizedInfluencesCache.AddRange(CalculateNormalizedInfluences());

                    _totalNormalizedInfluence = _normalizedInfluencesCache.Sum(f => f.second); 
                }

                return _normalizedInfluencesCache; 
            }
        }

        /// <summary> The morph with the most influence on this pawn, not necessarily the morph the pawn currently is. </summary>
        [CanBeNull] public MorphDef HighestInfluence { get; private set; }

        private IEnumerable<VTuple<MorphDef, float>> CalculateNormalizedInfluences()
        {
            float accum = 0;
            foreach (KeyValuePair<MorphDef, float> keyValuePair in _influenceLookup)
            {
                accum += keyValuePair.Value
                       / keyValuePair.Key.TotalInfluence; //use the normalized value so we're comparing apples to apples 
                //some morphs have more unique parts then others after all 
            }

            accum += Pawn.GetHumanInfluence(true) * MorphUtilities.HUMAN_CHANGE_FACTOR; //add in the remaining human influence, if any


            foreach (KeyValuePair<MorphDef, float> keyValuePair in _influenceLookup)
            {
                float nVal;
                if (accum < 0.0001f) nVal = 0; //prevent division by zero 
                else
                    nVal = keyValuePair.Value
                         / (accum * keyValuePair.Key.TotalInfluence); //now normalize the morph influences against the total 
                yield return new VTuple<MorphDef, float>(keyValuePair.Key, nVal);
            }
        }


        /// <summary> All mutations the pawn has. </summary>
        public IEnumerable<Hediff_AddedMutation> AllMutations =>
            Pawn.health.hediffSet.hediffs.OfType<Hediff_AddedMutation>();

        /// <summary>
        /// Initializes this instance with given props.
        /// </summary>
        /// this is call just after it is added to the parent, so other comps may or may not be added yet 
        /// <param name="props">The props.</param>
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            Assert(parent is Pawn, "parent is Pawn"); 
        }

        /// <summary>
        /// Gets the normalized influence of the given morph 
        /// </summary>
        /// <param name="morph">The morph.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">morph</exception>
        public float GetNormalizedInfluence([NotNull] MorphDef morph)
        {
            if (morph == null) throw new ArgumentNullException(nameof(morph));
            return this[morph] / Mathf.Max(0.001f, morph.TotalInfluence); //prevent division by zero 
        }
        /// <summary>
        /// Gets the pawn this is tracking mutations for.
        /// </summary>
        /// <value>
        /// The pawn.
        /// </value>
        public Pawn Pawn => (Pawn) parent;

        /// <summary> Called to notify this tracker that a mutation has been added. </summary>
        public void NotifyMutationAdded([NotNull] Hediff_AddedMutation mutation)
        {
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            _reCalcInfluences = true;  //we will need to recalculate the total if requested 
            var comp = mutation.TryGetComp<Comp_MorphInfluence>();
            if (comp != null)
            {
                _influenceLookup[comp.Morph] = _influenceLookup.TryGetValue(comp.Morph) + comp.Influence;
            }

            MutationsCount += 1; 

            HighestInfluence = GetHighestInfluence(); 

            NotifyCompsAdded(mutation);
        }

        void NotifyCompsAdded(Hediff_AddedMutation mutation)
        {
            foreach (ThingComp parentAllComp in parent.AllComps)
            {
                if(parentAllComp == this) continue;
                if(!(parentAllComp is IMutationEventReceiver receiver)) continue;
                receiver.MutationAdded(mutation, this); 
            }
        }

        void NotifyCompsRemoved(Hediff_AddedMutation mutation)
        {
            foreach (ThingComp parentAllComp in parent.AllComps)
            {
                if (parentAllComp == this) continue;
                if (!(parentAllComp is IMutationEventReceiver receiver)) continue;
                receiver.MutationRemoved(mutation, this); 
            }
        }


        /// <summary> Called to notify this tracker that a mutation has been removed. </summary>
        public void NotifyMutationRemoved([NotNull] Hediff_AddedMutation mutation)
        {
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            _reCalcInfluences = true; //we will need to recalculate the total if requested 
            MutationsCount -= 1; 
            var comp = mutation.TryGetComp<Comp_MorphInfluence>();
            if (comp != null)
            {
                if (!_influenceLookup.ContainsKey(comp.Morph)) return;  
                var val = _influenceLookup[comp.Morph] - comp.Influence;
                if (Mathf.Abs(val) < 0.1f)
                {
                    _influenceLookup.Remove(comp.Morph); 
                }
                else
                {
                    _influenceLookup[comp.Morph] = val; 
                }
            }

            HighestInfluence = GetHighestInfluence();

            NotifyCompsRemoved(mutation); 
        }

        /// <summary>
        /// exposes this instances data after the parent.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Generate lookup dict manually during load for backwards compatibility.
                foreach (var comp in AllMutations.Select(mut => mut.TryGetComp<Comp_MorphInfluence>()))
                {
                  if(comp == null) continue;

                  var morph = comp.Morph;
                  _influenceLookup[morph] = _influenceLookup.TryGetValue(morph) + comp.Influence;
                }

                // Now find the highest influence.
                MorphDef hMorph = GetHighestInfluence();
                HighestInfluence = hMorph;
                _reCalcInfluences = true;


                MutationsCount = Pawn.health.hediffSet.hediffs.OfType<Hediff_AddedMutation>().Count(); 
            }
        }

        private MorphDef GetHighestInfluence()
        {
            MorphDef hMorph = null;
            float max = float.NegativeInfinity;
            foreach (KeyValuePair<MorphDef, float> keyValuePair in _influenceLookup)
            {
                if (max < keyValuePair.Value)
                {
                    hMorph = keyValuePair.Key;
                    max = keyValuePair.Value;
                }
            }

            return hMorph;
        }


        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        public IEnumerator<KeyValuePair<MorphDef, float>> GetEnumerator()
        {
            return _influenceLookup.GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _influenceLookup).GetEnumerator();
        }
    }
}