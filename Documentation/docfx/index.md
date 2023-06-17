# TimberAssembly

## Summary

TimberAssembly is an innovative project that marries the power of Grasshopper and Unity to facilitate the generation of timber frame assemblies using reclaimed timber. This undertaking leans heavily on the capabilities of Unity's ML-Agent toolkit, employing a reinforcement learning agent to intelligently manipulate and modify a timber frame structure. The ultimate goal is to discover the optimal form that maximize the use of salvaged timber.

The backbone of this project is a Grasshopper script, which serves a dual role. First, it is used to generate a target structure (timber frame) that the agent then learns from and interacts with. Second, it compares and evaluates the extent to which the generated structure utilizes salvaged timber in relation to the target structure. 

A set of metrics are implemented to gauge the success of each iteration - time efficiency, labour efficiency, material efficiency, the total volume of salvage timber used, and the volume of salvage timber left unused. These values are calculated into a final score that is fed back into Unity. The ML-Agent utilizes this feedback to observe, learn and adapt, receiving rewards or penalties based on the score value.

TimberAssembly is developed by [Zeke Zhang](https://github.com/sean1832) as part of his Casual Research Assistantship at [RMIT University](https://www.rmit.edu.au/). Please note that the project is currently in its developmental stages and is not yet ready for production use.

## Roadmap
- Create documentation for end users and developers
- Create a universal match function, improve the quality of the match function
- Optimization
- Visualization of resulted timber frame assembly with salvaged timber.
- Visualization of the agent's learning process