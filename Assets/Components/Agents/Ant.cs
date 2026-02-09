using UnityEngine;
using Antymology.Terrain;

namespace Antymology.Agents
{
    /// <summary>
    /// Base class for ant agents. Handles health, movement, and basic behaviors.
    /// </summary>
    public class Ant : MonoBehaviour
    {
        #region Fields

        /// <summary>
        /// Current health of the ant.
        /// </summary>
        protected float health;

        /// <summary>
        /// Maximum health for this ant.
        /// </summary>
        protected float maxHealth;

        /// <summary>
        /// Current world position (block coordinates).
        /// </summary>
        protected Vector3Int worldPosition;

        /// <summary>
        /// Target position for movement interpolation.
        /// </summary>
        protected Vector3 targetWorldPosition;

        /// <summary>
        /// Is the ant currently moving?
        /// </summary>
        protected bool isMoving = false;

        /// <summary>
        /// Is this ant alive?
        /// </summary>
        protected bool isAlive = true;

        /// <summary>
        /// Movement progress for interpolation.
        /// </summary>
        protected float movementProgress = 0f;

        #endregion

        #region Properties

        public float Health => health;
        public float MaxHealth => maxHealth;
        public Vector3Int WorldPosition => worldPosition;
        public bool IsAlive => isAlive;
        public virtual bool IsQueen => false;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the ant with a starting position and health.
        /// </summary>
        public virtual void Initialize(Vector3Int startPosition, float startHealth)
        {
            worldPosition = startPosition;
            maxHealth = startHealth;
            health = startHealth;
            isAlive = true;
            isMoving = false;

            // Set visual position
            transform.position = new Vector3(startPosition.x, startPosition.y, startPosition.z);
            targetWorldPosition = transform.position;

            // Register with manager
            AntManager.Instance.RegisterAnt(this);
        }

        #endregion

        #region Update

        /// <summary>
        /// Called every frame by Unity.
        /// </summary>
        protected virtual void Update()
        {
            if (!isAlive) return;

            // Handle smooth movement interpolation
            if (isMoving)
            {
                movementProgress += Time.deltaTime * AntConfiguration.Instance.MovementSpeed;
                transform.position = Vector3.Lerp(transform.position, targetWorldPosition, movementProgress);

                if (movementProgress >= 1f)
                {
                    isMoving = false;
                    movementProgress = 0f;
                    transform.position = targetWorldPosition;
                }
            }
        }

        /// <summary>
        /// Called every simulation timestep. Handles health decay and behavior.
        /// </summary>
        public virtual void SimulationUpdate()
        {
            if (!isAlive) return;

            // Apply health decay
            ApplyHealthDecay();

            // Make decisions and perform actions
            MakeDecision();

            // Check for death
            if (health <= 0)
            {
                Die();
            }
        }

        #endregion

        #region Health Management

        /// <summary>
        /// Apply health decay based on current block type.
        /// </summary>
        protected virtual void ApplyHealthDecay()
        {
            float decayAmount = AntConfiguration.Instance.HealthDecayRate;

            // Check if standing on acidic block (2x decay)
            AbstractBlock currentBlock = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);
            if (currentBlock is AcidicBlock)
            {
                decayAmount *= 2f;
            }

            health -= decayAmount;
        }

        /// <summary>
        /// Add health to this ant (clamped to max).
        /// </summary>
        public void AddHealth(float amount)
        {
            health = Mathf.Min(health + amount, maxHealth);
        }

        /// <summary>
        /// Remove health from this ant.
        /// </summary>
        public void RemoveHealth(float amount)
        {
            health = Mathf.Max(health - amount, 0);
            if (health <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Transfer health to another ant (zero-sum).
        /// </summary>
        public void ShareHealthWith(Ant otherAnt, float amount)
        {
            if (!isAlive || !otherAnt.IsAlive) return;

            float actualAmount = Mathf.Min(amount, health);
            RemoveHealth(actualAmount);
            otherAnt.AddHealth(actualAmount);
        }

        #endregion

        #region Actions

        /// <summary>
        /// Override this to implement decision-making behavior.
        /// </summary>
        protected virtual void MakeDecision()
        {
            // Base implementation: random walk
            if (!isMoving && Random.value < 0.1f)
            {
                // Try to move in a random direction
                Vector3Int[] directions = new Vector3Int[]
                {
                    Vector3Int.right,
                    Vector3Int.left,
                    Vector3Int.forward,
                    new Vector3Int(0, 0, -1), // back
                    Vector3Int.up,
                    Vector3Int.down
                };

                Vector3Int randomDir = directions[Random.Range(0, directions.Length)];
                TryMove(randomDir);
            }

            // Try to eat if on mulch and low health
            if (health < maxHealth * 0.5f)
            {
                TryEat();
            }
        }

        /// <summary>
        /// Attempt to move in a direction.
        /// </summary>
        protected bool TryMove(Vector3Int direction)
        {
            if (isMoving) return false;

            Vector3Int targetPos = worldPosition + direction;

            // Check bounds
            if (!IsValidPosition(targetPos)) return false;

            // Check height difference constraint
            int currentHeight = GetGroundHeight(worldPosition.x, worldPosition.z);
            int targetHeight = GetGroundHeight(targetPos.x, targetPos.z);

            if (Mathf.Abs(targetHeight - currentHeight) > AntConfiguration.Instance.MaxHeightDifference)
            {
                return false;
            }

            // Check if target position is solid (can't move into solid blocks)
            AbstractBlock targetBlock = WorldManager.Instance.GetBlock(targetPos.x, targetPos.y, targetPos.z);
            if (targetBlock.isVisible())
            {
                return false;
            }

            // Valid move - update position
            AntManager.Instance.UpdateAntPosition(this, worldPosition, targetPos);
            worldPosition = targetPos;
            targetWorldPosition = new Vector3(targetPos.x, targetPos.y, targetPos.z);
            isMoving = true;
            movementProgress = 0f;

            return true;
        }

        /// <summary>
        /// Attempt to consume mulch at current position.
        /// </summary>
        protected bool TryEat()
        {
            // Check if standing on mulch
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);
            
            if (blockBelow is MulchBlock)
            {
                // Check if another ant is also on this mulch
                if (AntManager.Instance.GetAntsAtPosition(worldPosition).Count > 1)
                {
                    return false; // Can't eat if sharing mulch
                }

                // Consume mulch
                AddHealth(AntConfiguration.Instance.MulchHealthRestore);
                WorldManager.Instance.SetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z, new AirBlock());
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempt to dig the block below.
        /// </summary>
        protected bool TryDig()
        {
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);

            // Can't dig container blocks or air
            if (blockBelow is ContainerBlock || blockBelow is AirBlock)
            {
                return false;
            }

            // Dig the block
            WorldManager.Instance.SetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z, new AirBlock());
            return true;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Check if a position is valid (within world bounds).
        /// </summary>
        protected bool IsValidPosition(Vector3Int pos)
        {
            int maxX = ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter;
            int maxY = ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter;
            int maxZ = ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter;

            return pos.x >= 0 && pos.x < maxX &&
                   pos.y >= 0 && pos.y < maxY &&
                   pos.z >= 0 && pos.z < maxZ;
        }

        /// <summary>
        /// Get the ground height at an x,z coordinate (highest non-air block).
        /// </summary>
        protected int GetGroundHeight(int x, int z)
        {
            int maxY = ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter;
            
            for (int y = maxY - 1; y >= 0; y--)
            {
                AbstractBlock block = WorldManager.Instance.GetBlock(x, y, z);
                if (block.isVisible())
                {
                    return y;
                }
            }
            return 0;
        }

        /// <summary>
        /// Handle ant death.
        /// </summary>
        protected virtual void Die()
        {
            isAlive = false;
            AntManager.Instance.UnregisterAnt(this);
            gameObject.SetActive(false);
        }

        #endregion
    }
}
