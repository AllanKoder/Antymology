using UnityEngine;
using UnityEngine.UI;
using Antymology.Terrain;

namespace Antymology.Agents
{
    /// <summary>
    /// Base class for ant agents. Handles health, movement, and basic behaviors.
    /// </summary>


    public class Ant : MonoBehaviour
    {
        #region Fields

        // UI for the health bar
        protected Slider healthSlider;

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
        /// Last horizontal direction the ant moved (used for facing)
        /// </summary>
        protected Vector3 lastMoveDirection = Vector3.forward;

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
        protected int digCooldownRemaining = 0;
        protected int eatCooldownRemaining = 0;
        protected int buildCooldownRemaining = 0;

        #endregion

        #region Properties

        public float Health => health;
        public float MaxHealth => maxHealth;
        public Vector3Int WorldPosition => worldPosition;
        public bool IsAlive => isAlive;
        public virtual bool IsQueen => false;

        // Behavior genome controlling probabilities and speed
        public struct BehaviorGenome
        {
            public float moveProbability;
            public float digProbability;
            public float eatProbability;
            public float buildProbability;
            public float queenBuildProbability;
            public int ticksBetweenDigs;
            public int ticksBetweenEats;
            public int ticksBetweenBuilds;
            public int ticksBetweenQueenBuilds;

            public BehaviorGenome(float moveProb, float digProb, float eatProb, float buildProb = 0f, float queenBuildProb = 0f, int ticksBetweenDigs = 3, int ticksBetweenEats = 3, int ticksBetweenBuilds = 8, int ticksBetweenQueenBuilds = 20)
            {
                this.moveProbability = moveProb;
                this.digProbability = digProb;
                this.eatProbability = eatProb;
                this.buildProbability = buildProb;
                this.queenBuildProbability = queenBuildProb;
                this.ticksBetweenDigs = ticksBetweenDigs;
                this.ticksBetweenEats = ticksBetweenEats;
                this.ticksBetweenBuilds = ticksBetweenBuilds;
                this.ticksBetweenQueenBuilds = ticksBetweenQueenBuilds;
            }

            public override string ToString()
            {
                return $"move:{moveProbability:F2} dig:{digProbability:F2} eat:{eatProbability:F2} build:{buildProbability:F2} queenBuild:{queenBuildProbability:F2} digs:({ticksBetweenDigs}) eats:({ticksBetweenEats}) builds:({ticksBetweenBuilds}) qbuilds:({ticksBetweenQueenBuilds})";
            }
        }

        public BehaviorGenome Genome { get; set; }

        #endregion

        #region Initialization

        public void Awake()
        {
            healthSlider = GetComponentInChildren<Slider>();
        }

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

            // Default genome comes from EvolutionManager so defaults are centralized
            Genome = EvolutionManager.DefaultGenome;

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
            healthSlider.value = Health / MaxHealth;
            if (!isAlive) return;

            // Handle smooth movement interpolation
            if (isMoving)
            {
                Vector3 visualTargetPosition = targetWorldPosition + Vector3.down * 0.5f;
                // Scale visual interpolation by fast-mode multiplier so visuals keep up with accelerated simulation timesteps
                movementProgress += Time.deltaTime * AntConfiguration.Instance.MovementSpeed * (Antymology.Simulation.SimulationSettings.FastMode ? Antymology.Simulation.SimulationSettings.TimeScaleMultiplier : 1f);
                transform.position = Vector3.Lerp(transform.position, visualTargetPosition, movementProgress);

                if (movementProgress >= 1f)
                {
                    isMoving = false;
                    movementProgress = 0f;
                    transform.position = visualTargetPosition;
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

            // Decrement any action cooldowns
            if (digCooldownRemaining > 0) digCooldownRemaining--;
            if (eatCooldownRemaining > 0) eatCooldownRemaining--;
            if (buildCooldownRemaining > 0) buildCooldownRemaining--;

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
            // HealthDecayRate is specified in health units per second; scale by timestep duration so decay is proportional to real time
            float decayAmount = AntConfiguration.Instance.HealthDecayRate * AntConfiguration.Instance.timestepDuration;

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
            // Base implementation: random walk controlled by genome
            if (!isMoving && Random.value < Genome.moveProbability)
            {
                // Try to move in a random horizontal direction (4-way)
                Vector3Int[] directions = new Vector3Int[]
                {
                    Vector3Int.right,
                    Vector3Int.left,
                    Vector3Int.forward,
                    new Vector3Int(0, 0, -1) // back
                };

                Vector3Int randomDir = directions[Random.Range(0, directions.Length)];
                TryMove(randomDir);
            }
            else
            {
                // If there's diggable material beneath, attempt to dig with genome probability
                AbstractBlock blockBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);
                bool validBlock = blockBelow != null && !(blockBelow is ContainerBlock) && !(blockBelow is AirBlock);
                if (!isMoving && validBlock && Random.value < Genome.digProbability && digCooldownRemaining <= 0)
                {
                    TryDig();
                }
                else if (Random.value < Genome.eatProbability && eatCooldownRemaining <= 0)
                {
                    // Try to eat if on mulch and low; set cooldown when successful
                    if (TryEat()) eatCooldownRemaining = Genome.ticksBetweenEats;
                }
                // Attempt to build at current position (place ContainerBlock at current y, then move up)
                else if (!isMoving && Genome.buildProbability > 0f && Random.value < Genome.buildProbability && buildCooldownRemaining <= 0)
                {
                    TryBuildUp();
                }
            }
        }

        /// <summary>
        /// Attempt to move in a direction.
        /// </summary>
        protected bool TryMove(Vector3Int direction)
        {
            if (isMoving)
            {
                return false;
            }

            // Only use horizontal components and normalize to single-step
            int dx = Mathf.Clamp(direction.x, -1, 1);
            int dz = Mathf.Clamp(direction.z, -1, 1);
            if (dx == 0 && dz == 0)
                return false;

            int targetX = worldPosition.x + dx;
            int targetZ = worldPosition.z + dz;

            // Determine ground heights at current and target columns
            int currentGround = GetGroundHeight(worldPosition.x, worldPosition.z);
            int targetGround = GetGroundHeight(targetX, targetZ);

            // Block if height difference too large
            if (Mathf.Abs(targetGround - currentGround) > AntConfiguration.Instance.MaxHeightDifference)
            {
                return false;
            }

            int targetY = targetGround + 1; // ants occupy the block above ground
            Vector3Int targetPos = new Vector3Int(targetX, targetY, targetZ);

            // Check bounds
            if (!IsValidPosition(targetPos))
            {
                return false;
            }

            // Check if target space is free
            AbstractBlock targetBlock = WorldManager.Instance.GetBlock(targetPos.x, targetPos.y, targetPos.z);
            if (targetBlock.isVisible())
            {
                return false;
            }

            // Ensure there's support under target
            AbstractBlock belowTargetBlock = WorldManager.Instance.GetBlock(targetPos.x, targetPos.y - 1, targetPos.z);
            if (!belowTargetBlock.isVisible())
            {
                return false;
            }

            // Valid move - update position and start interpolation
            AntManager.Instance.UpdateAntPosition(this, worldPosition, targetPos);
            worldPosition = targetPos;
            targetWorldPosition = new Vector3(targetPos.x, targetPos.y, targetPos.z);
            // record movement direction for facing (horizontal only)
            lastMoveDirection = new Vector3(dx, 0, dz);
            if (lastMoveDirection.sqrMagnitude > float.Epsilon)
            {
                transform.rotation = Quaternion.LookRotation(lastMoveDirection);
            }
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

                // If support was removed, drop the ant down until it reaches solid ground.
                DropToGround();

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

            // Prevent digging if it would create a dangerous fall (no support two blocks down)
            if (worldPosition.y - 2 < 0)
            {
                // too close to bottom; don't dig
                return false;
            }
            AbstractBlock twoBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 2, worldPosition.z);
            if (!twoBelow.isVisible())
            {
                // digging would remove the last immediate support and cause a fall; avoid it
                return false;
            }

            // Dig the block (remove it)
            WorldManager.Instance.SetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z, new AirBlock());
            
            // Set dig cooldown from genome
            digCooldownRemaining = Genome.ticksBetweenDigs;

            // After digging, drop the ant down until it reaches solid ground.
            DropToGround();

            return true;
        }

        /// <summary>
        /// Attempt to build a container block at the ant's current position and move up onto it.
        /// </summary>
        protected bool TryBuildUp()
        {
            int x = worldPosition.x;
            int y = worldPosition.y;
            int z = worldPosition.z;

            // Can't build if at top of world
            int maxY = ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter;
            if (y + 1 >= maxY) return false;

            // Current space must be free (air) to place a block there
            AbstractBlock currentBlock = WorldManager.Instance.GetBlock(x, y, z);
            if (!(currentBlock is AirBlock)) return false;

            // Target space above must be free for the ant to occupy after building
            AbstractBlock aboveBlock = WorldManager.Instance.GetBlock(x, y + 1, z);
            if (!(aboveBlock is AirBlock)) return false;

            // Health cost (use same fraction as nest production)
            float healthCost = maxHealth * AntConfiguration.Instance.ContainerProductionHealthCost;
            if (health - healthCost <= 0f) return false; // won't build if it would kill the ant

            // Place container block at current position
            WorldManager.Instance.SetBlock(x, y, z, new GrassBlock());

            // Deduct health and set cooldown
            RemoveHealth(healthCost);
            buildCooldownRemaining = Genome.ticksBetweenBuilds;

            // Move the ant up one block
            Vector3Int oldPos = worldPosition;
            Vector3Int newPos = new Vector3Int(x, y + 1, z);
            AntManager.Instance.UpdateAntPosition(this, oldPos, newPos);
            worldPosition = newPos;
            targetWorldPosition = new Vector3(newPos.x, newPos.y, newPos.z);
            isMoving = true;
            movementProgress = 0f;

            return true;
        }

        #endregion

        /// <summary>
        /// Drop vertically until the ant reaches solid ground beneath it.
        /// Updates manager tracking and visual transform.
        /// </summary>
        protected void DropToGround()
        {
            while (worldPosition.y > 0 && !WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z).isVisible())
            {
                Vector3Int oldPos = worldPosition;
                Vector3Int newPos = new Vector3Int(worldPosition.x, worldPosition.y - 1, worldPosition.z);
                AntManager.Instance.UpdateAntPosition(this, oldPos, newPos);
                worldPosition = newPos;
                targetWorldPosition = new Vector3(newPos.x, newPos.y, newPos.z);
                transform.position = targetWorldPosition;
            }

            // Ensure movement interpolation does not override the teleport; stop any active movement
            isMoving = false;
            movementProgress = 0f;
        }

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
