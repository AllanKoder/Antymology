using System.Collections.Generic;
using Antymology.Terrain;
using UnityEngine;

namespace Antymology.Agents
{
    /// <summary>
    /// Manages all ants in the simulation. Tracks positions and coordinates ant behavior.
    /// </summary>
    public class AntManager : Singleton<AntManager>
    {
        #region Fields

        /// <summary>
        /// All active ants in the simulation.
        /// </summary>
        private List<Ant> allAnts = new List<Ant>();

        /// <summary>
        /// Dictionary mapping world positions to lists of ants at that position.
        /// </summary>
        private Dictionary<Vector3Int, List<Ant>> antPositions = new Dictionary<Vector3Int, List<Ant>>();

        /// <summary>
        /// Reference to the queen ant.
        /// </summary>
        private QueenAnt queenAnt;

        /// <summary>
        /// Total number of nests produced.
        /// </summary>
        private int totalNestsProduced = 0;

        /// <summary>
        /// Current generation number.
        /// </summary>
        private int currentGeneration = 0;

        /// <summary>
        /// Timestep counter for simulation updates.
        /// </summary>
        private float timestepAccumulator = 0f;

        #endregion

        #region Properties

        public int TotalNestsProduced => totalNestsProduced;
        public int CurrentGeneration => currentGeneration;
        public int AliveAntCount => allAnts.Count;
        public QueenAnt Queen => queenAnt;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // Accumulate time for discrete simulation updates
            float dt = Time.deltaTime * (Antymology.Simulation.SimulationSettings.FastMode ? Antymology.Simulation.SimulationSettings.TimeScaleMultiplier : 1f);
            timestepAccumulator += dt;

            // Add scaled delta time to accumulator (so fast mode speeds up progress proportionally)
            // and compute how many simulation steps to run this frame.
            float scaledDt = Time.deltaTime * (Antymology.Simulation.SimulationSettings.FastMode ? Antymology.Simulation.SimulationSettings.TimeScaleMultiplier : 1f);
            timestepAccumulator += scaledDt;

            if (Antymology.Simulation.SimulationSettings.FastMode)
            {
                int steps = Mathf.FloorToInt(timestepAccumulator / AntConfiguration.Instance.timestepDuration);
                if (steps > 0)
                {
                    // safety cap
                    int cap = 2000;
                    if (steps > cap) steps = cap;
                    for (int i = 0; i < steps; i++) SimulationUpdate();
                    timestepAccumulator -= steps * AntConfiguration.Instance.timestepDuration;
                }
            }
            else
            {
                if (timestepAccumulator >= AntConfiguration.Instance.timestepDuration)
                {
                    timestepAccumulator -= AntConfiguration.Instance.timestepDuration;
                    SimulationUpdate();
                }
            }
        }

        #endregion

        #region Simulation


        /// <summary>
        /// Called every simulation timestep. Updates all ants.
        /// </summary>
        private void SimulationUpdate()
        {
            // Update all ants (they'll make decisions and perform actions)
            // Use a copy to avoid modification during iteration
            List<Ant> antsCopy = new List<Ant>(allAnts);
            foreach (Ant ant in antsCopy)
            {
                if (ant != null && ant.IsAlive)
                {
                    ant.SimulationUpdate();
                }
            }

            // Let the EvolutionManager handle evolutionary progress if present
            if (EvolutionManager.Instance != null)
            {
                EvolutionManager.Instance.Tick();
            }
        }

        #endregion

        #region Ant Management

        /// <summary>
        /// Register an ant with the manager.
        /// </summary>
        public void RegisterAnt(Ant ant)
        {
            if (!allAnts.Contains(ant))
            {
                allAnts.Add(ant);
                AddAntToPosition(ant, ant.WorldPosition);

                if (ant.IsQueen)
                {
                    queenAnt = ant as QueenAnt;
                }
            }
        }

        /// <summary>
        /// Unregister an ant (called when ant dies).
        /// </summary>
        public void UnregisterAnt(Ant ant)
        {
            allAnts.Remove(ant);
            RemoveAntFromPosition(ant, ant.WorldPosition);

            if (ant.IsQueen)
            {
                queenAnt = null;
            }
        }

        /// <summary>
        /// Update an ant's position in the tracking dictionary.
        /// </summary>
        public void UpdateAntPosition(Ant ant, Vector3Int oldPosition, Vector3Int newPosition)
        {
            RemoveAntFromPosition(ant, oldPosition);
            AddAntToPosition(ant, newPosition);
        }

        /// <summary>
        /// Get all ants at a specific position.
        /// </summary>
        public List<Ant> GetAntsAtPosition(Vector3Int position)
        {
            if (antPositions.ContainsKey(position))
            {
                return new List<Ant>(antPositions[position]);
            }
            return new List<Ant>();
        }

        /// <summary>
        /// Called when a nest is produced.
        /// </summary>
        public void OnNestProduced()
        {
            totalNestsProduced++;
        }

        #endregion

        #region Position Tracking

        /// <summary>
        /// Add an ant to the position tracking dictionary.
        /// </summary>
        private void AddAntToPosition(Ant ant, Vector3Int position)
        {
            if (!antPositions.ContainsKey(position))
            {
                antPositions[position] = new List<Ant>();
            }
            antPositions[position].Add(ant);
        }

        /// <summary>
        /// Remove an ant from the position tracking dictionary.
        /// </summary>
        private void RemoveAntFromPosition(Ant ant, Vector3Int position)
        {
            if (antPositions.ContainsKey(position))
            {
                antPositions[position].Remove(ant);
                if (antPositions[position].Count == 0)
                {
                    antPositions.Remove(position);
                }
            }
        }

        #endregion

        #region Generation Management

        /// <summary>
        /// Spawn initial generation of ants.
        /// </summary>
        public void SpawnGeneration()
        {
            // Delegate to EvolutionManager to start evolutionary runs
            if (EvolutionManager.Instance != null)
            {
                EvolutionManager.Instance.StartEvolution();
            }
            else
            {
                // Fallback: spawn a single, non-evolving generation
                ClearAllAnts();
                totalNestsProduced = 0;

                Vector3Int spawnCenter = FindSafeSpawnLocation();
                for (int i = 0; i < AntConfiguration.Instance.WorkerAntCount; i++)
                {
                    Vector3Int spawnPos = FindNearbySpawnPosition(spawnCenter, AntConfiguration.Instance.SpawnRadius);
                    SpawnWorkerAnt(spawnPos, EvolutionManager.DefaultGenome);
                }
                Vector3Int queenSpawnPos = FindNearbySpawnPosition(spawnCenter, 2);
                SpawnQueenAnt(queenSpawnPos, EvolutionManager.DefaultGenome);

                currentGeneration++;
            }
        }



        /// <summary>
        /// Spawn a worker ant at a position.
        /// </summary>
        private void SpawnWorkerAnt(Vector3Int position, Ant.BehaviorGenome genome)
        {
            GameObject antObj = Instantiate(WorldManager.Instance.antPrefab, this.transform);
            Ant ant = antObj.GetComponent<Ant>();
            ant.Initialize(position, AntConfiguration.Instance.MaxWorkerHealth);
            ant.Genome = genome;
        }

        /// <summary>
        /// Spawn a queen ant at a position.
        /// </summary>
        private void SpawnQueenAnt(Vector3Int position, Ant.BehaviorGenome genome)
        {
            GameObject queenObj = Instantiate(WorldManager.Instance.queenAntPrefab, this.transform);
            QueenAnt queen = queenObj.AddComponent<QueenAnt>();
            queen.Initialize(position, AntConfiguration.Instance.MaxQueenHealth);
            queen.Genome = genome;
        }

        /// <summary>
        /// Clear all ants from the simulation.
        /// </summary>
        public void ClearAllAnts()
        {
            foreach (Ant ant in allAnts)
            {
                if (ant != null)
                {
                    Destroy(ant.gameObject);
                }
            }
            allAnts.Clear();
            antPositions.Clear();
            queenAnt = null;
        }

        /// <summary>
        /// Spawn an entire colony using the given genome.
        /// </summary>
        public void SpawnColonyWithGenome(Ant.BehaviorGenome genome)
        {
            totalNestsProduced = 0;
            Vector3Int spawnCenter = FindSafeSpawnLocation();

            for (int i = 0; i < AntConfiguration.Instance.WorkerAntCount; i++)
            {
                Vector3Int spawnPos = FindNearbySpawnPosition(spawnCenter, AntConfiguration.Instance.SpawnRadius);
                SpawnWorkerAnt(spawnPos, genome);
            }

            Vector3Int queenSpawnPos = FindNearbySpawnPosition(spawnCenter, 2);
            SpawnQueenAnt(queenSpawnPos, genome);
        }

        /// <summary>
        /// Reset nest counter (used by EvolutionManager between evaluations).
        /// </summary>
        public void ResetNestCount()
        {
            totalNestsProduced = 0;
        }

        #endregion

        #region Spawn Helpers

        /// <summary>
        /// Find a safe spawn location (not on acidic blocks, with some mulch nearby).
        /// </summary>
        private Vector3Int FindSafeSpawnLocation()
        {
            int maxX = ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter;
            int maxZ = ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter;
            int maxY = ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter;

            // Try to find a good spot
            for (int attempt = 0; attempt < 100; attempt++)
            {
                int x = Random.Range(maxX / 4, maxX * 3 / 4);
                int z = Random.Range(maxZ / 4, maxZ * 3 / 4);

                // Find ground level
                int y = -1;
                for (int checkY = maxY - 1; checkY >= 0; checkY--)
                {
                    AbstractBlock block = Antymology.Terrain.WorldManager.Instance.GetBlock(x, checkY, z);
                    if (block.isVisible())
                    {
                        y = checkY + 1; // Spawn one block above ground
                        break;
                    }
                }

                if (y < AntConfiguration.Instance.SpawnYLevel) continue;

                // Check if not on acidic block
                AbstractBlock groundBlock = Antymology.Terrain.WorldManager.Instance.GetBlock(x, y - 1, z);
                if (!(groundBlock is Antymology.Terrain.AcidicBlock) && !(groundBlock is Antymology.Terrain.ContainerBlock))
                {
                    return new Vector3Int(x, y, z);
                }
            }

            // Fallback: choose center of world but place at actual ground level to avoid spawning in mid-air
            int centerX = maxX / 2;
            int centerZ = maxZ / 2;
            int spawnY = AntConfiguration.Instance.SpawnYLevel;
            for (int checkY = maxY - 1; checkY >= 0; checkY--)
            {
                AbstractBlock block = Antymology.Terrain.WorldManager.Instance.GetBlock(centerX, checkY, centerZ);
                if (block.isVisible())
                {
                    spawnY = checkY + 1;
                    break;
                }
            }
            // Ensure spawnY is within bounds
            spawnY = Mathf.Clamp(spawnY, 0, maxY - 1);
            return new Vector3Int(centerX, spawnY, centerZ);
        }

        /// <summary>
        /// Find a spawn position near a center point.
        /// </summary>
        private Vector3Int FindNearbySpawnPosition(Vector3Int center, int radius)
        {
            int offsetX = Random.Range(-radius, radius + 1);
            int offsetZ = Random.Range(-radius, radius + 1);
            
            Vector3Int pos = new Vector3Int(center.x + offsetX, center.y, center.z + offsetZ);

            int maxX = ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter;
            int maxZ = ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter;
            int maxY = ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter;

            // Clamp horizontal coordinates to world bounds to avoid spawning out of range
            pos.x = Mathf.Clamp(pos.x, 0, maxX - 1);
            pos.z = Mathf.Clamp(pos.z, 0, maxZ - 1);
            pos.y = Mathf.Clamp(pos.y, 0, maxY - 1);

            // Make sure we're on solid ground
            for (int y = pos.y; y >= 0; y--)
            {
                AbstractBlock block = Antymology.Terrain.WorldManager.Instance.GetBlock(pos.x, y, pos.z);
                if (block.isVisible())
                {
                    pos.y = y + 1;
                    break;
                }
            }

            // Ensure final y is within bounds
            pos.y = Mathf.Clamp(pos.y, 0, maxY - 1);

            return pos;
        }

        #endregion

        /// <summary>
        /// Returns a copy of the internal list of all ants (for UI/inspection purposes).
        /// </summary>
        public List<Ant> GetAllAnts()
        {
            return new List<Ant>(allAnts);
        }
    }
}
