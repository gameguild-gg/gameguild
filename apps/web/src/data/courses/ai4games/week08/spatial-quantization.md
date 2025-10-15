# Enhanced Pathfinding

::: note

This is a group project. You can make groups of 2 or more, but for every extra member, you have to add more features and list them in the project report.

:::

## Activity

1. Use any game engine you want;
    1. I recommend you using my Unity boilerplate with the 2021 version so you can have a CI/CD automation for the project to build and publish targeting WebGL. Read the README.md there to enable CI/CD on unity. And talk to me if anything fails. [UnityBoilerplate](https://github.com/gameguild-gg/UnityBoilerplate/)
    2. If you want to use another engine, you have to make sure that it has a way to export the project to WebGL. (Latest Unreal to do that is 4.23, and Godot does that, but if you want to use C# the latest one is 3.8). 
        1. Add me and the TAs to the repo if you make it private.
2. Create any type of grid (e.g. 2D grid, 3D grid, hexagonal grid), we will use that for spatial quantization. 
    1. As bonus extra points, you may want to create a way to analyze the environment and create your very own grid based on the obstacles and terrain. Ex.: create a flat(or not) terrain, allow the user to place obstacles as cubes, and then create a grid that represents the free space aka.: build the NavMesh.
3. Create debug interfaces. Allow the user to:
    1. Button  to reset the board to empty;
    2. Placement selection to place obstacles, set weights or set the type of tile that have embedded weight on the board;
    3. Button to set the source and target points for the pathfinding;
    4. Button to run the pathfinding algorithm and visualize the result.
    5. Button to place one agent and make it walk using the Follow-Path Steering behavior.
4. Initialize the board empty with the given size from the debugger interface.
5. Invite the user to configure the board by placing tiles or setting weights or configurations on the grid or scenario.
6. Implement at least 2 enhancements for the pathfinding algorithm. Ex.:
    1. Non-GridGraph 
    2. Path Smoothing 
    3. Hierarchical Pathfinding 
    4. Dynamic Pathfinding 
    5. Interruptible Pathfinding 
    6. Pathfinder Pooling 
    7. Information reuse 
    8. Flow-Field: If you use this  you can create an alternative FollowFlowField steering. 
    9. [Anisotropic A* Pathfinding](https://jflynn.xyz/portfolio/houdini-anisotropic-procedural-roads/)
7. The user should be able to visualize the path by color or any other way you think is appropriate. ex.:
    1. Color the tiles that are part of the path in a different color.
    2. If using flowfields, visualize the flowfield by coloring the tiles in a way that shows the direction of the flow.
8. The agent should be able to move using the Follow-Path Steering behavior or any other steering behavior you think is appropriate, such as the FollowFlowField steering.

## Grading

- 5 Points - builds for OpenGL and is published on Itchio or on any other platform you want (ex.: github pages)
- 5 points - the video recording explaining the code and what you have done
- 10 Points – uses good OOP Architecture
- 5 points – Follow-path steering behavior implementation
- 5 points - General Pathfinding algorithm implementation
- 10 points - Debug interface implementation
- 20 points - Selection and Implementation of Enhanced Techniques

