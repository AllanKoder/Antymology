using UnityEngine;
using Antymology.Terrain;

namespace Antymology.Agents
{
    /// <summary>
    /// Queen ant - capable of producing nest blocks at the cost of health.
    /// </summary>
    public class QueenAnt : Ant
    {
        #region Properties

        public override bool IsQueen => true;

        #endregion

        #region Initialization

        public override void Initialize(Vector3Int startPosition, float startHealth)
        {
            base.Initialize(startPosition, startHealth);
        }

        #endregion

        #region Actions

        /// <summary>
        /// Queen's decision making - similar to worker but can also produce nests.
        /// </summary>
        protected override void MakeDecision()
        {
            // Base behavior (movement, eating)
            base.MakeDecision();

            // Try to produce nest if health is sufficient
            float nestCost = maxHealth * AntConfiguration.Instance.NestProductionHealthCost;
            if (health > nestCost * 1.5f && Random.value < Genome.queenBuildProbability) // Only produce if well above cost
            {
                TryProduceNest();
            }
        }

        /// <summary>
        /// Attempt to produce a nest block below the queen.
        /// </summary>
        protected bool TryProduceNest()
        {
            // Check the block below
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);

            // Can only place nest on solid, non-container blocks or replace air
            bool canPlace = false;
            
            if (blockBelow is AirBlock)
            {
                canPlace = true;
            }
            else if (!(blockBelow is ContainerBlock) && !(blockBelow is NestBlock))
            {
                // Can replace other blocks with nest
                canPlace = true;
            }

            if (!canPlace) return false;

            // Calculate health cost
            float healthCost = maxHealth * AntConfiguration.Instance.NestProductionHealthCost;

            // Check if queen has enough health
            if (health <= healthCost) return false;

            // Produce nest block
            RemoveHealth(healthCost);
            WorldManager.Instance.SetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z, new NestBlock());
            
            // Notify manager of nest production
            AntManager.Instance.OnNestProduced();

            return true;
        }

        #endregion
    }
}
