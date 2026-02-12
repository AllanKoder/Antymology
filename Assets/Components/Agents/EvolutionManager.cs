using System.Collections.Generic;
using UnityEngine;

namespace Antymology.Agents
{
    /// <summary>
    /// Handles an evolutionary loop that evaluates genomes by spawning colonies and
    /// evolving them to maximize nest production.
    /// </summary>
    public class EvolutionManager : Singleton<EvolutionManager>
    {
        public int PopulationSize = 6;
        public int StepsPerEvaluation = 300;

        // Default genome used when a non-evolving fallback is needed or for seeds
        public static Ant.BehaviorGenome DefaultGenome => new Ant.BehaviorGenome(0.2f, 0.01f, 0.01f, 0.02f, 25, 25, 8);

        private List<Ant.BehaviorGenome> population;
        private List<int> fitnesses;
        private int evalIndex = 0;
        private int evalStepsRemaining = 0;
        private bool isEvolving = false;

        // Expose last-best genome for UI
        public Ant.BehaviorGenome BestGenome { get; private set; }

        /// <summary>
        /// Start the evolutionary process.
        /// </summary>
        public void StartEvolution()
        {
            if (isEvolving) return;
            InitializePopulation();
            evalIndex = 0;
            StartEvaluationGenome(evalIndex);
            isEvolving = true;
            // Inform AntManager that a new generation (initial) has started
            if (Antymology.Agents.AntManager.Instance != null)
                Antymology.Agents.AntManager.Instance.AdvanceGeneration();
        }

        /// <summary>
        /// Called once per simulation timestep from AntManager to progress evaluations.
        /// </summary>
        public void Tick()
        {
            if (!isEvolving) return;
            // Ensure population/fitness lists are valid
            if (population == null || population.Count == 0)
            {
                isEvolving = false;
                return;
            }
            if (fitnesses == null || fitnesses.Count != population.Count)
            {
                fitnesses = new System.Collections.Generic.List<int>(new int[population.Count]);
            }

            evalStepsRemaining--;
            if (evalStepsRemaining <= 0)
            {
                // guard index
                if (evalIndex < 0 || evalIndex >= fitnesses.Count)
                {
                    Debug.LogWarning($"EvolutionManager.Tick: evalIndex {evalIndex} out of range (0..{fitnesses.Count - 1}), resetting to 0");
                    evalIndex = 0;
                }

                // record fitness from AntManager
                fitnesses[evalIndex] = AntManager.Instance.TotalNestsProduced;

                // Advance
                evalIndex++;
                if (evalIndex < population.Count)
                {
                    StartEvaluationGenome(evalIndex);
                }
                else
                {
                    EvolvePopulation();
                    evalIndex = 0;
                    StartEvaluationGenome(evalIndex);
                }
            }
        }

        private void StartEvaluationGenome(int index)
        {
            if (population == null || index < 0 || index >= population.Count) return;
            // Restore fair environment (regenerate mulch/blocks to initial snapshot)
            if (Antymology.Terrain.WorldManager.Instance != null)
            {
                Antymology.Terrain.WorldManager.Instance.RestoreInitialWorld();
            }

            // Ask AntManager to spawn a colony configured with this genome
            AntManager.Instance.ClearAllAnts();
            AntManager.Instance.SpawnColonyWithGenome(population[index]);
            // reset step counter and nest counter
            evalStepsRemaining = StepsPerEvaluation;
            AntManager.Instance.ResetNestCount();

            Debug.Log($"EvolutionManager: Evaluating genome {index}/{population.Count}");
        }

        private void InitializePopulation()
        {
            population = new List<Ant.BehaviorGenome>();
            fitnesses = new List<int>();
            for (int i = 0; i < PopulationSize; i++)
            {
                Ant.BehaviorGenome g = new Ant.BehaviorGenome(
                    Random.Range(0.0f, 0.5f),
                    Random.Range(0.0f, 0.1f),
                    Random.Range(0.0f, 0.05f),
                    Random.Range(0.0f, 0.08f),
                    Random.Range(1, 6),
                    Random.Range(1, 6),
                    Random.Range(4, 12)
                );
                population.Add(g);
                fitnesses.Add(0);
            }
        }

        private void EvolvePopulation()
        {
            // Simple elitist selection: keep top 2
            List<int> indices = new List<int>();
            for (int i = 0; i < fitnesses.Count; i++) indices.Add(i);
            indices.Sort((a, b) => fitnesses[b].CompareTo(fitnesses[a]));

            List<Ant.BehaviorGenome> newPop = new List<Ant.BehaviorGenome>();

            int elites = Mathf.Min(2, population.Count);
            for (int i = 0; i < elites; i++)
            {
                newPop.Add(population[indices[i]]);
            }

            while (newPop.Count < population.Count)
            {
                Ant.BehaviorGenome parent = population[indices[Random.Range(0, elites)]];
                Ant.BehaviorGenome child = parent;
                child.moveProbability = Mathf.Clamp01(child.moveProbability + Random.Range(-0.02f, 0.02f));
                child.digProbability = Mathf.Clamp01(child.digProbability + Random.Range(-0.01f, 0.01f));
                child.eatProbability = Mathf.Clamp01(child.eatProbability + Random.Range(-0.01f, 0.01f));
                int newDigTicks = child.ticksBetweenDigs + Random.Range(-1, 2);
                child.ticksBetweenDigs = (int)Mathf.Clamp(newDigTicks, 1, 20);
                int newEatTicks = child.ticksBetweenEats + Random.Range(-1, 2);
                child.ticksBetweenEats = (int)Mathf.Clamp(newEatTicks, 1, 20);
                newPop.Add(child);
            }

            int bestFitness = 0;
            int bestIndex = 0;
            for (int i = 0; i < fitnesses.Count; i++)
            {
                if (fitnesses[i] > bestFitness)
                {
                    bestFitness = fitnesses[i];
                    bestIndex = i;
                }
            }

            // store the best genome we just selected (if available)
            if (indices.Count > 0)
            {
                BestGenome = population[indices[0]];
            }

            population = newPop;
            fitnesses = new List<int>(new int[population.Count]);

            // inform AntManager that we advanced to the next generation
            if (Antymology.Agents.AntManager.Instance != null)
                Antymology.Agents.AntManager.Instance.AdvanceGeneration();

            Debug.Log($"EvolutionManager: Evolved to next population. Best previous fitness: {bestFitness}");
        }
    }
}
