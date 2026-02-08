# Assignment 3: Antymology

As we\'ve seen in class, ants exhibit very interesting behaviour. From finding the shortest path to building bridges out of bodies ants have evolved to produce complex emergents from very simple rules. For your assignment you will need to create a species of ant which is capable of generating the biggest nest possible.

I have already created the base code you will use for the assignment. Currently the simulation environment is devoid of any dynamic behaviour and exists only as a landscape. You will need to extend the functionality of what I have written in order to produce \"intelligent\" behaviour. Absolutely no behaviour has been added to this project so you are free to implement whatever you want however you want, with only a few stipulations.

![Ants](Images/Ants.gif)

## Goal

The only goal you have is to implement some sort of evolutionary algorithm which maximises nest production. You are in complete control over how your ants breed, make choices, and interact with the environment. Because of this, your mark is primarily going to be reflective of how much effort it looks like you put into this vs. how well your agents maximise their fitness (I.e. don\'t worry about having your ants perform exceptionally well).

## Current Code
My code is currently broken into 4 components (found within the components folder)
1. Agents
2. Configuration
3. Terrain
4. UI

You are able to experience it generating an environment by simply running the project once you have loaded it into unity.

### Agents
The agents component is currently empty. This is where you will place most of your code. The component will be responsible for moving ants, digging, making nests, etc. You will need to come up with a system for how ants interact within the world, as well as how you will be maximising their fitness (see ant behaviour).

### Configuration
This is the component responsible for configuring the system. For example, currently there exists a file called ConfigurationManager which holds the values responsible for world generation such as the dimensions of the world, and the seed used in the RNG. As you build parameters into your system, you will need to place your necesarry configuration components in here.

### Terrain
The terrain memory, generation, and display all take place in the terrain component. The main WorldManager is responsible for generating everything.

### UI
This is where all UI components will go. Currently only a fly camera, and a camera-controlled map editor are present here.

## Requirements

### Admin
 - This assignment must be implemented using Unity 2019or above (see appendix)
 - Your code must be maintained in a github (or other similar git environment) repository.
 - You must fork from this repo to start your project.
 - You will be marked for your commit messages as well as the frequency with which you commit. Committing everything at once will receive a letter grade reduction (A →A-).
 - All project documentation should be provided via a Readme.md file found in your repo. Write it as if I was an employer who wanted to see a portfolio of your work. By that I mean write it as if I have no idea what the project is. Describe it in detail. Include images/gifs.

### Interface
- The camera must be usable in play-mode so as to allow the grader the ability to look at what is happening in the scene.
- You must create a basic UI which shows the current number of nest blocks in the world

### Ant Behaviour
- Ants must have some measure of health. When an ants health hits 0, it dies and needs to be removed from the simulation
- Every timestep, you must reduce each ants health by some fixed amount
- Ants can refill their health by consuming Mulch blocks. To consume a mulch block, and ant must be directly ontop of a mulch block. After consuming, the mulch block must be removed from the world.
- Ants cannot consume mulch if another ant is also on the same mulch block
- When moving from one black to another, ants are not allowed to move to a block that is greater than 2 units in height difference
- Ants are able to dig up parts of the world. To dig up some of the world, an ant must be directly ontop of the block. After digging, the block is removed from the map
- Ants cannot dig up a block of type ContainerBlock
- Ants standing on an AcidicBlock will have the rate at which their health decreases multiplied by 2.
- Ants may give some of their health to other ants occupying the same space (must be a zero-sum exchange)
- Among your ants must exists a singular queen ant who is responsible for producing nest blocks
- Producing a single nest block must cost the queen 1/3rd of her maximum health.
- No new ants can be created during each evaluation phase (you are allowed to create as many ants as you need for each new generation though).

## Tips
Initially you should first come up with some mechanism which each ant uses to interact with the environment. For the beginning phases your ants should behave completely randomly, at least until you have gotten it so that your ants don't break the pre-defined behaviour above.

Once you have the interaction mechanism nailed down, begin thinking about how you will get your ants to change over time. One approach might be to use a neural network to dictate ant behaviour

https://youtu.be/zIkBYwdkuTk

another approach might be to use phermone deposits (I\'ve commented how you could achieve this in the code for the AirBlock) and have your genes be what action should be taken for different phermone concentrations, etc.

## Submission
Export your project as a Unity package file. Submit your Unity package file and additional document using the D2L system under the corresponding entry in Assessments/Dropbox. Inlude in the message a link to your git repo where you did your work.
