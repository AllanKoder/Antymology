using UnityEngine;

/// <summary>
/// Configuration parameters for ant behavior and simulation.
/// </summary>
public class AntConfiguration : Singleton<AntConfiguration>
{
    [Header("Population Settings")]
    [Tooltip("Number of worker ants per generation")]
    public int WorkerAntCount = 20;

    [Header("Health Settings")]
    [Tooltip("Maximum health for worker ants")]
    public float MaxWorkerHealth = 100f;

    [Tooltip("Maximum health for queen ant")]
    public float MaxQueenHealth = 150f;

    [Tooltip("Health lost per timestep (normal)")]
    public float HealthDecayRate = 0.5f;

    [Tooltip("Health restored when consuming mulch")]
    public float MulchHealthRestore = 30f;

    [Header("Movement Settings")]
    [Tooltip("Maximum height difference for movement")]
    public int MaxHeightDifference = 2;

    [Tooltip("Movement speed (blocks per second)")]
    public float MovementSpeed = 2f;

    [Tooltip("Simulation Timestep")]
    public float timestepDuration = 0.1f;

    [Header("Queen Settings")]
    [Tooltip("Health cost for queen to produce one nest block (as fraction of max health)")]
    [Range(0f, 1f)]
    public float NestProductionHealthCost = 0.33f;

    [Header("Building Settings")]
    [Tooltip("Health cost for Ant to produce one container for building (as fraction of max health)")]
    [Range(0f, 1f)]
    public float ContainerProductionHealthCost = 0.10f;

    [Header("Spawn Settings")]
    [Tooltip("Spawn ants at this Y level or higher")]
    public int SpawnYLevel = 20;

    [Tooltip("Radius around spawn center to place ants")]
    public int SpawnRadius = 10;
}
