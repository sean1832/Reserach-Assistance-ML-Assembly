﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using TimberAssembly.Entities;
using TimberAssembly.Helper;

namespace TimberAssembly
{
    /// <summary>
    /// Matching algorithm for timber assembly.
    /// </summary>
    public class Match
    {
        public List<Agent> TargetAgents { get; set; }
        public List<Agent> SalvageAgents { get; set; }
        public double Tolerance { get; set; }

        public Match(List<Agent> targetAgents, List<Agent> salvageAgents, double tolerance = 0.01)
        {
            TargetAgents = targetAgents;
            SalvageAgents = salvageAgents;
            Tolerance = tolerance;
        }

        /// <summary>
        /// One subject is exactly matched to one target.
        /// </summary>
        /// <param name="remains">Output remainders</param>
        public List<Pair> ExactMatch(ref Remain remains)
        {
            List<Agent> remainTargets = TargetAgents.ToList();
            List<Agent> remainSalvages = SalvageAgents.ToList();

            List<Pair> pairs = new List<Pair>();
            foreach (var target in TargetAgents)
            {
                foreach (var salvage in SalvageAgents)
                {
                    if (!ComputeMatch.IsAgentExactMatched(target, salvage, Tolerance)) continue;

                    Pair pair = new Pair(target, new List<Agent>() { salvage });

                    pairs.Add(pair);

                    remainTargets.Remove(target);
                    remainSalvages.Remove(salvage);
                    break;
                }
            }
            remains.Targets = remainTargets;
            remains.Subjects = remainSalvages;

            return pairs;
        }

        /// <summary>
        /// Two subjects from the remainders are combined to match one target. 1 dimensional matching.
        /// </summary>
        /// <param name="remains">Output remainders</param>
        public List<Pair> DoubleMatch(ref Remain remains)
        {
            Remain previousRemains = remains;

            var (remainTargets, remainSalvages) = CloneAgents(previousRemains);

            List<Agent> matchedSubjects = new List<Agent>();

            List<Pair> pairs = new List<Pair>();
            foreach (var target in previousRemains.Targets)
            {
                bool isMatched = false;

                for (int i = 0; i < previousRemains.Subjects.Count; i++)
                {
                    if (isMatched) break;

                    var salvage1 = previousRemains.Subjects[i];
                    if (matchedSubjects.Contains(salvage1)) continue;

                    for (int j = i + 1; j < previousRemains.Subjects.Count; j++)
                    {
                        var salvage2 = previousRemains.Subjects[j];
                        if (matchedSubjects.Contains(salvage2)) continue;
                        if (ComputeMatch.IsAgentDoubleMatched(target, salvage1, salvage2, Tolerance))
                        {
                            isMatched = true;

                            Pair pair = new Pair(target, new List<Agent>() { salvage1, salvage2 });

                            pairs.Add(pair);

                            matchedSubjects.Add(salvage1);
                            matchedSubjects.Add(salvage2);

                            remainTargets.Remove(target);
                            remainSalvages.Remove(salvage1);
                            remainSalvages.Remove(salvage2);
                            break;
                        }
                    }
                }
            }
            remains.Targets = remainTargets;
            remains.Subjects = remainSalvages;

            return pairs;
        }

        /// <summary>
        /// Three subjects from the remainders are combined to match one target. 2 dimensional matching.
        /// </summary>
        /// <param name="remains">Output remainders</param>
        public List<Pair> TripleMatch(ref Remain remains)
        {
            Remain previousRemains = remains;
            var (remainTargets, remainSalvages) = CloneAgents(previousRemains);
            List<Agent> matchedSubjects = new List<Agent>();
            List<Pair> pairs = new List<Pair>();

            foreach (var target in remainTargets)
            {
                foreach (var salvage in remainSalvages)
                {
                    List<Agent> residuals = ComputeMatch.CalculateResiduals(target, salvage);
                }
            }
            throw new NotImplementedException();
        }


        /// <summary>
        /// Four subjects from the remainders are combined to match one target. 3 dimensional matching.
        /// </summary>
        /// <param name="remains">Output remainders</param>
        public List<Pair> QuadrupleMatch(ref Remain remains)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Cut the remainders to the target and create offcuts.
        /// (when target is smaller than subject)
        /// </summary>
        public List<Pair> CutToTarget(ref Remain remain)
        {
            Remain previousRemains = remain;
            var usedSubjects = new HashSet<Agent>();
            var usedTargets = new HashSet<Agent>();

            var (targets, subjects) = CloneAgents(previousRemains);

            if (targets == null || subjects == null)
            {
                return null;
            }

            var resultOffcuts = new List<Agent>();
            var results = new List<Pair>();
            List<Agent> remainTargets = targets.ToList();


            foreach (var target in targets)
            {
                var matchedSubject = FindBestMatch(target, subjects, usedSubjects, usedTargets);

                if (matchedSubject == null) continue;

                var residuals = ComputeMatch.CalculateResiduals(target, matchedSubject);
                resultOffcuts.AddRange(residuals);

                // mutate matched subject
                matchedSubject.Trimmed = residuals.Count;
                matchedSubject.Dimension = target.Dimension;

                results.Add(new Pair(target, new List<Agent> { matchedSubject }));

                usedSubjects.Add(matchedSubject);
                usedTargets.Add(target);
                remainTargets.Remove(target);
            }

            remain.Targets = remainTargets;
            remain.Subjects = resultOffcuts;

            return results;
        }

        /// <summary>
        /// Clone the agents from the previousRemains.
        /// </summary>
        /// <param name="previousRemains"></param>
        /// <returns>(Targets, Subjects)</returns>
        private (List<Agent>, List<Agent>) CloneAgents(Remain previousRemains)
        {
            try
            {
                List<Agent> targets = new List<Agent>(previousRemains.Targets);
                List<Agent> subjects = new List<Agent>(previousRemains.Subjects);
                return (targets, subjects);
            }
            catch (NullReferenceException)
            {
                return (null, null);
            }
        }

        private Agent FindBestMatch(Agent target, List<Agent> subjects, HashSet<Agent> usedSubjects, HashSet<Agent> usedTargets)
        {
            double minDiff = double.MaxValue;
            Agent matchedSubject = null;

            foreach (var subject in subjects)
            {
                if (usedSubjects.Contains(subject) || usedTargets.Contains(target)) continue;

                if (subject.Volume() < target.Volume() || subject.Volume() - target.Volume() >= minDiff) continue;

                minDiff = subject.Volume() - target.Volume();
                matchedSubject = subject;
            }

            return matchedSubject;
        }


        /// <summary>
        /// (DEPRECATED! Use ExtendToTarget Instead.) 
        /// Match the rest of the targets with the rest of the subjects.
        /// Introduce offcuts if necessary.
        /// </summary>
        /// <param name="previousRemains">Remainder from SecondMatch</param>
        public List<Pair> RemainMatch(Remain previousRemains)
        {
            List<Agent> remainTargets;
            List<Agent> remainSalvages;
            try
            {
                remainTargets = previousRemains.Targets.ToList();
                remainSalvages = previousRemains.Subjects.ToList();
            }
            catch (NullReferenceException e)
            {
                return new List<Pair>();
            }

            List<Pair> pairs = new List<Pair>();

            int count = 0;

            // Match each target with a suitable subject
            for (var i = 0; i < remainTargets.Count; i++)
            {
                var target = remainTargets[i];
                Dictionary<Agent, Dimension> potentialMatches = new Dictionary<Agent, Dimension>();

                foreach (Agent salvage in remainSalvages)
                {
                    if (salvage.Dimension.IsAnyLargerThan(target.Dimension)) continue;

                    Dimension difference = Dimension.GetDifference(target.Dimension, salvage.Dimension);
                    difference.Absolute();
                    potentialMatches.Add(salvage, difference);
                }

                var sortedMatches = potentialMatches.OrderBy(x => x.Value.Length + x.Value.Width + x.Value.Height)
                    .ToList();

                // check if sortedMatches is empty
                if (sortedMatches.Count == 0)
                {
                    continue; // skip this iteration if there are no potential matches
                }

                Agent selectedSalvage = sortedMatches[0].Key;
                Dimension remainingDimension = sortedMatches[0].Value;

                // check if all dimensions have value, if not, assign the value from target
                foreach (var prop in remainingDimension.GetType().GetProperties())
                {
                    if ((double)prop.GetValue(remainingDimension) != 0) continue;

                    if (prop.Name == "Length")
                    {
                        prop.SetValue(remainingDimension, target.Dimension.Length);
                    }
                    else if (prop.Name == "Width")
                    {
                        prop.SetValue(remainingDimension, target.Dimension.Width);
                    }
                    else if (prop.Name == "Height")
                    {
                        prop.SetValue(remainingDimension, target.Dimension.Height);
                    }
                }

                Pair pair = new Pair ()   
                {
                    Target = target,
                    Subjects = new List<Agent>
                    {
                        selectedSalvage,
                        new Agent() 
                        {
                            Name = $"NewTimber{count:D2}",
                            Dimension = remainingDimension
                        }
                    }
                };
                pairs.Add(pair);

                count++;

                remainSalvages.Remove(selectedSalvage);
            }
            return pairs;
        }

        /// <summary>
        /// Combining remainders with new subjects to match the targets. 
        /// (when target is larger than target)
        /// </summary>
        public List<Pair> ExtendToTarget(ref Remain remain)
        {
            Remain previousRemains = remain;
            List<Agent> preRemainTargets = previousRemains.Targets;
            List<Agent> remainTargets = new List<Agent>(preRemainTargets);

            List<Agent> remainSubjects = previousRemains.Subjects;

            List<Pair> pairs = new List<Pair>();

            for (var i = 0; i < preRemainTargets.Count; i++)
            {
                var target = preRemainTargets[i];
                (Agent closestSubject, Dimension different) = ComputeMatch.GetClosestAgent(target, remainSubjects);
                if (closestSubject != null)
                {
                    remainTargets.Remove(target);
                    remainSubjects.Remove(closestSubject);

                    Pair pair = new Pair(target, new List<Agent>()
                    {
                        closestSubject,
                        new Agent($"NewTimber{i:D2}", different, 1, true)
                    });
                    pairs.Add(pair);
                }
            }
            remain.Targets = remainTargets;
            remain.Subjects = remainSubjects;

            return pairs;
        }
    }
}