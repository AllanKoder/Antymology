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
        public static Ant.BehaviorGenome DefaultGenome => new Ant.BehaviorGenome(0.2f, 0.002f, 0.01f);

        private List<Ant.BehaviorGenome> population;
        private List<int> fitnesses;
        private int evalIndex = 0;
        private int evalStepsRemaining = 0;
        private bool isEvolving = false;

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
        }

        /// <summary>
        /// Called once per simulation timestep from AntManager to progress evaluations.
        /// </summary>
        public void Tick()
        {
            if (!isEvolving) return;
            evalStepsRemaining--;
            if (evalStepsRemaining <= 0)
            {
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
                    Random.Range(0.02f, 0.2f),
                    Random.Range(0.005f, 0.2f),
                    Random.Range(0.005f, 0.05f)
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
                newPop.Add(child);
            }

            int bestFitness = 0;
            foreach (int f in fitnesses) if (f > bestFitness) bestFitness = f;

            population = newPop;
            fitnesses = new List<int>(new int[population.Count]);

            Debug.Log($"EvolutionManager: Evolved to next population. Best previous fitness: {bestFitness}");
        }
    }
}
