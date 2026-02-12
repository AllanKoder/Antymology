# Antymology

An ant colony simulation and evolutionary sandbox.

![Ant colony simulation preview](Images/Ants.gif)

Table of contents
- Project overview
- Features implemented (high level)
- Technical architecture
- Key engineering changes made (portfolio highlights)
- How to run and test (development instructions)
- Controls and UI
- Known issues and testing notes
- Contact / next steps

Project overview

Antymology is an experimental agent-based simulation written in Unity that models ant behaviour in a discrete 3D block world. The simulation supports an evolutionary loop that breeds and evaluates ant behaviour genomes to maximize the number of nest blocks produced in the world. The environment contains reusable terrain chunks, a variety of block types (mulch, container, acid, air, etc.), a queen agent that produces nests, and worker agents that forage, dig, eat, and build.

The project is useful as a research playground for emergent behaviour, evolution experiments, and performance trade-offs in simulation systems.

Features implemented and maintained (high level)
- Discrete block-based world with chunked storage and regeneration (WorldManager).
- Agents (Ants) with behaviour genomes controlling move/dig/eat/build probabilities and action cooldowns.
    - follows the rules as defined in the assignment specifications
- Queen ant with a queen-only genome parameter set for nest production cost and cooldown.
- Evolution loop (EvolutionManager) evaluating genomes by spawning colonies and selecting top performers.
- UI for runtime metrics: nests produced, generation number, alive ants, evolved best genome, current testing genome, and explicit text indicators for "Non-Evolving Mode" and "Fast Forwarding".
- Fast-mode simulation acceleration that increases evaluation throughput while keeping visual interpolation smooth.
- Non-evolving observation mode: stop evolution and spawn the current Best genome for indefinite observation.
- Safety and robustness improvements (null checks, index guards) and improved bounds checks in world code.

## Technical architecture

-a Assets/Components with modules:
  - Agents: Ant, AntManager, EvolutionManager, QueenAnt
  - UI: UIManager, SimulationUI, TextMeshPro-based HUD elements.

- Simulation model:
  - Running each Genome: Instead of doing parallel worlds. It was much easier to evaluate each best genome, one step at a time. Then, we can choose the elite 2 genomes as the model for the next population.
  - Discrete timesteps: AntManager collects delta time, accumulates into discrete timesteps (timestepDuration) and advances the logical simulation one or more steps per frame. Fast mode scales delta-time accumulation to run more logical timesteps per frame so genome evaluations complete faster.
  - Visual interpolation: Ant movement visually interpolates between discrete block positions; this interpolation is scaled for fast-mode so visuals remain coherent when the simulation steps run quickly.
  - Modes: Fast mode for speeding up the evolution process, Non-evolving mode for just observing the best genome discovered thus far.

## Highlights

1) WorldManager correctness and safety
- Fixed out-of-bounds checks and neighbour chunk update logic (bug where updateX was used instead of updateZ when checking Z neighbours).
- Consolidated local chunk GetBlock/SetBlock into single world-coordinate implementations to reduce duplication and centralize bounds handling.
- Added null-safety for Blocks to avoid NullReferenceExceptions when world data is not yet initialized.

2) Evolution and evaluation speed
- Reworked AntManager timestep handling to support a fast-forward mode that scales simulation progress (more logical steps per frame) while preserving a smooth visual experience.
- Ensured evolution (EvolutionManager) consumes more evaluation time in fast mode so "time moves faster" truly accelerates genome evaluation throughput rather than only speeding visuals.

3) Ant behaviour and building
- Implemented build actions for workers (place a ContainerBlock below/at position and move up), and queen-only nest-production behavior driven by queen-specific genome parameters.
- Added build cooldowns and health costs to balance behaviour.

4) UI and developer ergonomics
- Added UI fields showing Best evolved genome and the currently testing genome; added non-evolving and fast-forward text indicators (so reviewers can immediately see simulation mode).
- Changed fast-mode camera handling so the Camera component remains enabled instead of toggling the GameObject—this prevents display issues.
- Added a global keyboard toggle (N) for Non-Evolving Mode and F for Fast Mode to ease demonstration and manual testing.

5) Robustness and safety
- Guarded EvolutionManager.Tick against index out-of-range and ensured fitness array lengths match population size.
- Clamped genome mutation values during evolution to valid ranges.

## How to run (developer instructions)

Open this repository in Unity (6000.3.x).

### Controls
- F: Toggle Fast Mode (increases simulation throughput; UI shows Fast Forwarding: ON).
- N: Toggle Non-Evolving Mode (stops evolution and spawns the Best genome colony for observation; UI shows Non-Evolving Mode ON).
- Use the camera in the scene to inspect ant behaviour while simulation runs.

### Recommendations
- Do some fast mode before trying out the Non-evolving mode and viewing the best


## Emergent Properties
