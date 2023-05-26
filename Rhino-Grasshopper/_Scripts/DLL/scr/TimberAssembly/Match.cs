﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TimberAssembly.Entities;

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
        public List<Pair> ExactMatch(out Remain remains)
        {
            remains = new Remain();
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
        /// WARNING: This is a slow method!
        /// Two subjects from the remainders of ExactMatch are combined to match one target.
        /// </summary>
        /// <param name="previousRemains">Remainders from ExactMatch</param>
        /// <param name="remains">Output remainders</param>
        public List<Pair> SecondMatchSlow(Remain previousRemains, out Remain remains)
        {
            remains = new Remain();
            List<Agent> remainTargets = previousRemains.Targets.ToList();
            List<Agent> remainSalvages = previousRemains.Subjects.ToList();

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

                        if (ComputeMatch.IsAgentSecondMatched(target, salvage1, salvage2, Tolerance))
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
        /// Two subjects from the remainder of ExactMatch are combined to match one target.
        /// </summary>
        /// <param name="previousRemains">Remainder from ExactMatch</param>
        /// <param name="remains">Output remainder</param>
        public List<Pair> SecondMatchFast(Remain previousRemains, out Remain remains)
        {
            remains = new Remain();
            List<Agent> remainTargets = previousRemains.Targets.ToList();
            List<Agent> remainSalvages = previousRemains.Subjects.ToList();

            Dictionary<(Agent target, Agent firstAgent), Agent> pairDict = new Dictionary<(Agent target, Agent firstAgent), Agent>();

            // Loop over targets and the first agents to find pairs that match the criteria
            foreach (var target in remainTargets)
            {
                for (int i = 0; i < remainSalvages.Count; i++)
                {
                    var salvage1 = remainSalvages[i];
                    for (int j = i + 1; j < remainSalvages.Count; j++)
                    {
                        var salvage2 = remainSalvages[j];
                        if (ComputeMatch.IsAgentSecondMatched(target, salvage1, salvage2, Tolerance))
                        {
                            pairDict[(target, salvage1)] = salvage2;
                            break;
                        }
                    }
                }
            }

            HashSet<Agent> matchedSubjects = new HashSet<Agent>();
            List<Pair> pairs = new List<Pair>();

            // Loop over the dictionary to create the pairs and remove the matched agents
            foreach (var pair in pairDict)
            {
                Pair newPair = new Pair(pair.Key.target, new List<Agent> { pair.Key.firstAgent, pair.Value });
                pairs.Add(newPair);

                matchedSubjects.Add(pair.Key.firstAgent);
                matchedSubjects.Add(pair.Value);

                remainTargets.Remove(pair.Key.target);
                remainSalvages.Remove(pair.Key.firstAgent);
                remainSalvages.Remove(pair.Value);
            }

            remains.Targets = remainTargets;
            remains.Subjects = remainSalvages;

            return pairs;
        }

        public List<Pair> CutToTarget(Remain previousRemains, out Remain remain)
        {
            // Bin unpacking 

            // initialize
            remain = new Remain();
            List<Agent> resultOffcuts = new List<Agent>();
            List<Pair> results = new List<Pair>();

            HashSet<Agent> usedSubjects = new HashSet<Agent>();
            HashSet<Agent> usedTargets = new HashSet<Agent>();

            List<Agent> targets;
            List<Agent> subjects;
            try
            {
                targets = previousRemains.Targets.ToList();
                subjects = previousRemains.Subjects.ToList();

            }
            catch (NullReferenceException e)
            {
                return null;
            }

            // matching...
            foreach (var target in targets)
            {
                double minDiff = double.MaxValue;
                Agent matchedSubject = null;

                foreach (var subject in subjects)
                {
                    if (usedSubjects.Contains(subject) || usedTargets.Contains(target)) continue;

                    if (subject.Volume() >= target.Volume() && subject.Volume() - target.Volume() < minDiff)
                    {
                        minDiff = subject.Volume() - target.Volume();
                        matchedSubject = subject;
                    }
                }

                if (matchedSubject != null)
                {
                    var residuals = new Pair(target, new List<Agent>(){matchedSubject}).CalculateResiduals();
                    resultOffcuts.AddRange(residuals);
                    results.Add(new Pair(target, new List<Agent>(){matchedSubject}));

                    usedSubjects.Add(matchedSubject);
                    usedTargets.Add(target);
                }
            }

            remain.Subjects = resultOffcuts;
            return results;
        }

        /// <summary>
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
                return null;
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

                    Dimension difference = Dimension.Subtract(target.Dimension, salvage.Dimension);
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
    }
}
