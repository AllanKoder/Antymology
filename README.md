# Antymology

An ant colony simulation and evolutionary sandbox.

[Overview](./Images/overview.png)

Table of contents
- Project overview
- Features implemented (high level)
- Technical architecture
- Key engineering changes made (portfolio highlights)
- How to run and test (development instructions)
- Controls and UI
- Known issues and testing notes
- Contact / next steps

## Project overview

We are modelling ant behaviour in a discrete 3D block world. The simulation supports an evolutionary loop that breeds and evaluates ant behaviour genomes to maximize the number of nest blocks produced in the world. The environment contains reusable terrain chunks, a variety of block types (mulch, container, acid, air, etc.), a queen agent that produces nests, and worker agents that forage, dig, eat, and build.

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

- A Assets/Components with modules:
  - Agents: Ant, AntManager, EvolutionManager, QueenAnt
  - UI: UIManager, SimulationUI, TextMeshPro-based HUD elements.

- Simulation model:
  - Running each Genome: Instead of doing parallel worlds. It was much easier to evaluate each best genome, one step at a time. Then, we can choose the elite 2 genomes as the model for the next population.
  - Discrete timesteps: AntManager collects delta time, accumulates into discrete timesteps (timestepDuration) and advances the logical simulation one or more steps per frame. Fast mode scales delta-time accumulation to run more logical timesteps per frame so genome evaluations complete faster.
  - Visual interpolation: Ant movement visually interpolates between discrete block positions; this interpolation is scaled for fast-mode so visuals remain coherent when the simulation steps run quickly.
  - Modes: Fast mode for speeding up the evolution process, Non-evolving mode for just observing the best genome discovered thus far.

## Highlights

1) Evolution and evaluation speed
- Reworked AntManager timestep handling to support a fast-forward mode that scales simulation progress (more logical steps per frame) while preserving a smooth visual experience.
- Ensured evolution (EvolutionManager) consumes more evaluation time in fast mode so "time moves faster" truly accelerates genome evaluation throughput rather than only speeding visuals.

2) Ant behaviour and building
- Implemented build actions for workers (place a Green Block below/at position and move up), and queen-only nest-production behavior driven by queen-specific genome parameters.
- Added build cooldowns and health costs to balance behaviour.

3) UI and developer ergonomics
- Added UI fields showing Best evolved genome and the currently testing genome; added non-evolving and fast-forward text indicators (so reviewers can immediately see simulation mode).
- Changed fast-mode camera handling so the Camera component remains enabled instead of toggling the GameObject—this prevents display issues.
- Added a global keyboard toggle (N) for Non-Evolving Mode and (F) for Fast Mode to ease demonstration and manual testing.

## How to run (developer instructions)

Open this repository in Unity (6000.3.x).

### Controls
- F: Toggle Fast Mode (increases simulation throughput; UI shows Fast Forwarding: ON).
- N: Toggle Non-Evolving Mode (stops evolution and spawns the Best genome colony for observation; UI shows Non-Evolving Mode ON).
- Use the camera in the scene to inspect ant behaviour while simulation runs.

### Recommendations
- Do some fast mode before trying out the Non-evolving mode and viewing the best

## Emergent Properties

### Important Changes
Ants commonly exibihited the behavior of digging into holes - from a combination of eating Mulch and digging down - resulting in their slow and depressing deaths. This was a tragic and common behavior with the ants, and therefore, I created a new property for ants: the ability to build upwards. Building a grass block for ants takes up 10% of their health and allows for more interesting emergent properties. This also prevented tragic deaths.

### Evolution Changes
Ants originally just dug about, usually digging into their holes - with no escape or anywhere to go - and end up dying in a hole. After some evolutions, the ants either start digging less (not eating less as they still needed food), or they started moving more. The probabilities for movement was as high as 50% sometimes. This made it diffiuclt to stay in the same area, and prevented falling into a hole.

There was a balance of trying to eat as much mulch as possible, to both build more queen nests and share health with the queen. As a result, the movement Genome became the most important factor: ants needed to travel in order to find more mulch and prevent falling into a hole.

Interestingly, ants converge to prefer the genome of digging over the genome of eating. It doesn't make much sense as Ants should be eating. However, this is an emergent property.

The queen builds her nests as often as possible, with a high probabilty of at least 10%.

Usually a queen dies by digging too much into a hole and ends up unable to get back out.
[Queen Death](./Images/queen_death.png)

### Local Maxes
Hitting a local max was common, as there are only 6 genomes being evaluated. Sometimes the build ticks for a queen nest was very high:

[Queen In a Local Max](./Images/local_max.png)

### Patterns

The queen would travel around, and create nests in a somewhat random pattern, typically a circle from the spawning location. However, the queen would typically benefit from wandering and getting further from the original nest position. This way, she would be able to eat more mulch and prevent digging the original nest blocks that she has placed (yes, she can dig her own nests).

The other ants would terraform the world around them very gradually. They would make somewhat random dug out patterns, with the occasional structures sticking out:

[Ant structure](./Images/ant_pattern.png)

## Improvements to future iterations
As it was quite a tight deadline, and difficult enough to implement a genetic algorithm in Unity, I did not get a chance to create any advanced agents. Here are some things I would've done if I had the time:
- Implement a neural network for complex ant behaviors, as the ant behavior is currently prefined and customized via probability. The genetic algorithm genomes could be used to fine tune this neural network instead of only messing around with probabilities.
- Have pheromones deposited for communication between Ants. Currently ants are very independent of each other. The only real form of interaction between them as of now is the placement of blocks, and the sharing of health with others.
- I've noticed that when I add more parameters to the genomes, the ants started to do worse in many ways. They were falling into many local maximums of optimization. Essentially, It became harder to fine tune the ant behavior. When I had less parameters, the ants were able to live for a very long time, and produce around 60 nests until death. However, now, we have the ants dying more often as it's harder to fine tune the parameters that actually matter.
