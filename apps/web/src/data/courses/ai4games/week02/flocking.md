# Flocking agents behavior formal assignment

You are in charge of implementing some functions to make some AI agents flock together in a game. After finishing it, you will be one step further to render it in a game engine, and start making reactive NPCs and enemies. You will learn the basic concepts needed to code and customize your own AI behaviors.

- [Presentation](https://docs.google.com/presentation/d/1OBEY-tb_ubgoq6Mk9lEsCFaYLINni3oPwjH8iAXEQQM/edit?usp=sharing)

- For the formal assignment, you have to follow this [repo](https://github.com/gameguild-gg/ai4games/);
- For the interactive assignment, you can code in any language and/or game engine you want, but I recommend you to use this one I provided on this [repo](https://github.com/gameguild-gg/mobagen). If you want extra experience, you may want to explore buiding bare minimum with C++, CMake and sdl: [SDL3-CPM-CMake-Example](https://github.com/gameguild-gg/SDL3-CPM-CMake-Example), or with raylib [](https://github.com/gameguild-gg/raylib-cpm-cmake-boilerplate);
- I don't recommend using Game Engines for this specific assignment. Historically, students fail on the implementation of the double buffering and the math operations. But if you are confident, go ahead.

::: danger "Notes on imprecision"

On the formal assignment, the automated tests results may differ somehow because of floating point imprecison, so don't worry much. If you cannot make it pass 100% of the tests, explain how you tried to solve it and what you think is wrong. I will evaluate your code based on your explanation.

If you find an issue on my formal description or on the tests, send a PR and I will give you extra points.

:::

## What is flocking?

Flocking is a behavior that is observed in birds, fish and other animals that move in groups. It is a very simple behavior that can be implemented with a few lines of code. The idea is that each agent will try to move towards the center of mass of the group (cohesion), and will try to align its velocity with the average velocity of the group (AKA alignment). In addition, each agent will try to avoid collisions with other agents (AKA avoidance).

::: note "Formal Notation Review"

- \( \vec{F} \) means a vector \( F \) that has components. In a 2 dimensional vector it will hold \( F_x \) and \( F_y \). For example, if \( F_x = 1 \) and \( F_y = 3 \), then \( \vec{F} = (1,3) \)
- Simple math operations between vectors are done component-wise. For example, if \( \vec{F} = (1,1) \) and \( \vec{G} = (2,2) \), then \( \vec{F} + \vec{G} = (3,3) \)
- The notation \( \overrightarrow{P*{1}P*{2}} \) means the vector that goes from \( P_1 \) to \( P_2 \). It is the same as \( P_2-P_1 \)
- The modulus notation means the length (magnitude) of the vector. \( |\vec{F}| = \sqrt{F_x^2+F_y^2} \) For example, if \( \vec{F} = (1,1) \), then \( |\vec{F}| = \sqrt{2} \)
- The hat ^ notation means the normalized vector(magnitude is 1) of the vector. \( \hat{F} = \frac{\vec{F}}{|\vec{F}|} \) For example, if \( \vec{F} = (1,1) \), then \( \hat{F} = (\frac{1}{\sqrt{2}},\frac{1}{\sqrt{2}}) \)
- The hat notation over 2 points means the normalized vector that goes from the first point to the second point. \( \widehat{P_1P_2} = \frac{\overrightarrow{P_1P_2}}{|\overrightarrow{P_1P_2}|} \) For example, if \( P_1 = (0,0) \) and \( P_2 = (1,1) \), then \( \widehat{P_1P_2} = (\frac{1}{\sqrt{2}},\frac{1}{\sqrt{2}}) \)
- The sum \( \sum \) notation means the sum of all elements in the list going from `0` to `n-1`. Ex. \( \sum*{i=0}^{n-1} \vec{V_i} = \vec{V_0} + \vec{V_1} + \vec{V_2} + ... + \vec{V*{n-1}} \)

:::

It is your job to implement those 3 behaviors following the ruleset below:

### Cohesion

Apply a force towards the center of mass of the group.

1. The \( n \) neighbors of an agent are all the other agents that are within a certain radius \( r_c \)( `<` operation ) of the agent. It doesn't include the agent itself;
2. Compute the location of the center of mass of the group (\( P\_{CM} \));
3. Compute the force that will move the agent towards the center of mass(\( \overrightarrow{F_c} \)); The farther the agent is from the center of mass, the force increases linearly up to the limit of the cohesion radius \( r_c \).

![cohesion](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Falignment.png)

\[
P*{CM} = \frac{\sum*{i=0}^{n-1} P_i}{n}
\]

\[
\overrightarrow{F*{c}} = \begin{cases}
\frac{ \overrightarrow{P*{agent}P*{CM}} }{r_c} & \text{if } |\overrightarrow{P*{agent}P*{CM}}| \leq r_c \\
0 & \text{if } |\overrightarrow{P*{agent}P\_{CM}}| > r_c
\end{cases}
\]

![cohesion](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Falignment.gif)

:: tip

Note that the maximum magnitude of \( \overrightarrow{F_c} \) is 1. Inclusive. This value can be multiplied by a constant \( K_c \) to increase or decrease the cohesion force to looks more appealing.

:::

### Separation

It will move the agent away from other agents when they get too close.

1. The \( n \) neighbors of an agent are all the other agents that are within the separation radius \( r_s \) of the agent;
2. If the distance to a neighbor is less than the separation radius, then the agent will move away from it inversely proportionally to the distance between them.
3. Accumulate the forces that will move the agent away from each neighbor (\( \overrightarrow{F*{s}} \)). And then, clamp the force to a maximum value of \( F*{Smax} \).

![separation](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Fseparation.png)

\[
\overrightarrow{F*s} = \sum*{i=0}^{n-1} \begin{cases}
\frac{\widehat{P_aP_i}}{|\overrightarrow{P_aP_i}|} & \text{if } 0 < |\overrightarrow{P_aP_i}| \leq r_s \\
0 & \text{if } |\overrightarrow{P_aP_i}| = 0 \lor |\overrightarrow{P_aP_i}| > r_s
\end{cases}
\]

::: tip

Here you can see that if we have more than one neighbor and one of them is way too close, the force will be very high and make the influence of the other neighbors irrelevant. This is the expected behavior.

:::

The force will go near infinite when the distance between the agent and the \( n \) neighbor is 0. To avoid this, after accumulating all the influences from every neighbor, the force will be clamped to a maximum magnitude of \( F\_{Smax} \).

\[
\overrightarrow{F*{s}} = \begin{cases}
\overrightarrow{F_s} & \text{if } |\overrightarrow{F_s}| \leq F*{Smax} \\
\widehat{F*s} \cdot F*{Smax} & \text{if } |\overrightarrow{F*s}| > F*{Smax}
\end{cases}
\]

::: tip

- You can implement those two math together, but it is better to isolate in two steps to make it easier to understand and debug.
- This is not an averaged force like the cohesion force, it is a sum of forces. So, the maximum magnitude of the force can be higher than 1.

:::

::: example "Separation Example"

![separation](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Fseparation.gif)

:::

### Alignment

It is the force that will align the velocity of the agent with the average velocity of the group.

1. The \( n \) neighbors of an agent are all the agents that are within the alignment radius \( r_a \) of the agent, including itself;
2. Compute the average velocity of the group (\( \overrightarrow{V\_{avg}} \));
3. Compute the force that will move the agent towards the average velocity (\( \overrightarrow{F\_{a}} \));

![alignment](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Falignment.png)

\[
\overrightarrow{V*{avg}} = \frac{\sum*{i=0}^{n-1} \vec{V_i}}{n}
\]

::: example "Alignment Example"

![alignment](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Falignment.gif)

:::

## Behavior composition

The force composition is made by a weighted sum of the influences of those 3 behaviors. This is the way we are going to work, this is not the only way to do it, nor the more correct. It is just a way to do it.

- \( \vec{F} = K_c \cdot \overrightarrow{F_c} + K_s \cdot \overrightarrow{F_s} + K_a \cdot \overrightarrow{F_a} \) `This is a weighted sum!`
- \( \overrightarrow{V*{new}} = \overrightarrow{V*{cur}} + \vec{F} \cdot \Delta t \) `This is a simplification!`
- \( P*{new} = P*{cur}+\overrightarrow{V\_{new}} \cdot \Delta t \) `This is an approximation!`

::: warning

A more precise way for representing the new position would be to use full equations of motion. But given timestep is usually very small and it even squared, it is acceptable to ignore it. But here they are anyway, just dont use them in this assignment:

- \( \overrightarrow{V*{new}} = \overrightarrow{V*{cur}}+\frac{\overrightarrow{F}}{m} \cdot \Delta t \)
- \( P*{new} = P*{cur}+\overrightarrow{V\_{cur}} \cdot \Delta t + \frac{\vec{F}}{m} \cdot \frac{\Delta t^2}{2} \)

Where:

- \( \overrightarrow{F} \) is the force applied to the agent;
- \( \overrightarrow{V} \) is the velocity of the agent;
- \( P \) is the position of the agent;
- \( m \) is the mass of the agent, here it is always 1;
- \( \Delta t \) is the time frame (1/FPS);
- \( cur \) is the current value of the variable;
- \( new \) is the new value of the variable to be used in the next frame.

The \( \overrightarrow{V*{new}} \) and \( P*{new} \) are the ones that will be used in the next frame and you will have to print to the console at the end of every single frame.

::: note

- For simplicity, we are going to assume that the mass of all agents is 1.
- In a real game simulation, it would be nice to apply some friction to the velocity of the agent to make it stop eventually or just clamp it to prevent the velocity get too high. But, for simplicity, we are going to ignore it.

:::

::: example "Combined behavior examples"

Alignment + Cohesion:

![alignment+cohesion](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Falignment_cohesion.gif)

Cohesion:

![separation+cohesion](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Fseparation_cohesion.gif)

Separation + Alignment:

![separation+alignment](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Fseparation_alignment.gif)

All 3:

![alignment+cohesion+separation](https://console-minio.gameguild.gg/api/v1/buckets/gameguild/objects/download?prefix=ai4games%2Fall3.gif)

:::

## Input

The input consists in a list of parameters followed by a list of agents. The parameters are:

- \( r_c \) - Cohesion radius
- \( r_s \) - Separation radius
- \( F\_{Smax} \) - Maximum separation force
- \( r_a \) - Alignment radius
- \( K_c \) - Cohesion constant
- \( K_s \) - Separation constant
- \( K_a \) - Alignment constant
- \( N \) - Number of agents

Every agent is represented by 4 values in the same line, separated by a space:

- \( x \) - X coordinate
- \( y \) - Y coordinate
- \( vx \) - X velocity
- \( vy \) - Y velocity

After reading the agent's data, the program should read the time frame (\( \Delta t \)), simulate the agents and then output the new position of the agents in the same sequence and format it was read. The program should keep reading the time frame and simulating the agents until the end of the input.

::: note "Data Types"

All values are double precision floating point numbers to improve consistency between different languages.

:::

### Input Example

In this example we are going to test only the cohesion behavior. The input is composed by the parameters and 2 agents.

```text
1.000 0.000 0.000 0.000 1.000 0.000 0.000 2
0.000 0.500 0.000 0.000
0.000 -0.500 0.000 0.000
0.125
```

## Output

The expected output is the position and velocity for each agent after the simulation step using the time frame. After printing each simulation step, the program should wait for the next time frame and then simulate the next step. All values should have exactly 3 decimal places and should be rounded to the nearest.

```text
0.000 0.484 0.000 -0.125
0.000 -0.484 0.000 0.125
```

## Grading

10 points total:

- 3 Points – by following standards;
- 2 Points – properly submitted in Canvas;
- 5 Points – passed on test cases;
