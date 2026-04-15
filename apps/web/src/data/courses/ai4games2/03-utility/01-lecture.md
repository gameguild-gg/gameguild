# Utility AI Systems

### Scoring-Based Decision Making

---

## Today's Agenda

- What is Utility AI?
- Core Components
- Response Curves
- Combining Considerations
- Utility vs Behavior Trees
- Case Studies
- Debugging & Tuning

---

## The Problem with BTs

Behavior Trees work well for **priority-based** decisions:

```
Selector
├── FleeIfLowHealth
├── AttackIfEnemyVisible
└── Patrol
```

But what if **everything matters a little**?

---

## When Priorities Aren't Enough

- Should I eat, sleep, or socialize?
- Which enemy should I attack?
- Where should I take cover?

These decisions need to **weigh multiple factors simultaneously**

---

# What is Utility AI?

---

## Definition

> A decision-making system where each possible action receives a **numerical score** based on the current game state, and the highest-scoring action is selected.

---

## The Core Idea

```
For each possible action:
    score = evaluate(gameState)

Select action with highest score
```

Simple concept. Powerful results.

---

## Key Insight

Instead of asking **"Should I do this?"** (boolean)

We ask **"How much do I want to do this?"** (scalar)

---

# Core Components

---

## The Utility Pipeline

```
Input → Normalization → Response Curve → Score
```

Multiple scores are **combined** to produce final utility

---

## Considerations

A **consideration** is a single factor that influences a decision

Examples:

- Distance to target
- Current health percentage
- Ammo remaining
- Time since last action

---

## Normalization

Raw inputs must be **normalized** to [0, 1]

```c++
# Distance: 0-100 meters
normalized_distance = distance / 100.0

# Health: 0-250 HP
normalized_health = current_hp / max_hp
```

Why? **Comparability across different inputs**

---

# Response Curves

---

## What Are Response Curves?

Functions that **transform** normalized inputs into utility scores

They let designers control **how much** an input matters at different values

---

## Linear

`y = x`

![Linear Curve](<https://quickchart.io/chart?bkg=white&c={type:'line',data:{labels:[0,0.02,0.04,0.06,0.08,0.1,0.12,0.14,0.16,0.18,0.2,0.22,0.24,0.26,0.28,0.3,0.32,0.34,0.36,0.38,0.4,0.42,0.44,0.46,0.48,0.5,0.52,0.54,0.56,0.58,0.6,0.62,0.64,0.66,0.68,0.7,0.72,0.74,0.76,0.78,0.8,0.82,0.84,0.86,0.88,0.9,0.92,0.94,0.96,0.98,1],datasets:[{label:'y=x',data:[0,0.02,0.04,0.06,0.08,0.1,0.12,0.14,0.16,0.18,0.2,0.22,0.24,0.26,0.28,0.3,0.32,0.34,0.36,0.38,0.4,0.42,0.44,0.46,0.48,0.5,0.52,0.54,0.56,0.58,0.6,0.62,0.64,0.66,0.68,0.7,0.72,0.74,0.76,0.78,0.8,0.82,0.84,0.86,0.88,0.9,0.92,0.94,0.96,0.98,1],borderColor:'rgb(75,192,192)',borderWidth:3,fill:false,pointRadius:0,tension:0.4}]},options:{plugins:{legend:{display:false}},scales:{x:{title:{display:true,text:'Input'},grid:{display:false}},y:{title:{display:true,text:'Output'},min:0,max:1,grid:{color:'rgba(0,0,0,0.1)'}}}}}>)

Uniform response across all values

---

## Quadratic

$$y = x^2$$

![Quadratic Curve](<https://quickchart.io/chart?bkg=white&c={type:'line',data:{labels:[0,0.02,0.04,0.06,0.08,0.1,0.12,0.14,0.16,0.18,0.2,0.22,0.24,0.26,0.28,0.3,0.32,0.34,0.36,0.38,0.4,0.42,0.44,0.46,0.48,0.5,0.52,0.54,0.56,0.58,0.6,0.62,0.64,0.66,0.68,0.7,0.72,0.74,0.76,0.78,0.8,0.82,0.84,0.86,0.88,0.9,0.92,0.94,0.96,0.98,1],datasets:[{label:'y=x²',data:[0,0.0004,0.0016,0.0036,0.0064,0.01,0.0144,0.0196,0.0256,0.0324,0.04,0.0484,0.0576,0.0676,0.0784,0.09,0.1024,0.1156,0.1296,0.1444,0.16,0.1764,0.1936,0.2116,0.2304,0.25,0.2704,0.2916,0.3136,0.3364,0.36,0.3844,0.4096,0.4356,0.4624,0.49,0.5184,0.5476,0.5776,0.6084,0.64,0.6724,0.7056,0.7396,0.7744,0.81,0.8464,0.8836,0.9216,0.9604,1],borderColor:'rgb(255,99,132)',borderWidth:3,fill:false,pointRadius:0,tension:0.4}]},options:{plugins:{legend:{display:false}},scales:{x:{title:{display:true,text:'Input'},grid:{display:false}},y:{title:{display:true,text:'Output'},min:0,max:1,grid:{color:'rgba(0,0,0,0.1)'}}}}}>)

Low inputs matter **less**, high inputs matter **more**

---

## Inverse Quadratic

$$y = 1 - (1-x)^2$$

![Inverse Quadratic Curve](<https://quickchart.io/chart?bkg=white&c={type:'line',data:{labels:[0,0.02,0.04,0.06,0.08,0.1,0.12,0.14,0.16,0.18,0.2,0.22,0.24,0.26,0.28,0.3,0.32,0.34,0.36,0.38,0.4,0.42,0.44,0.46,0.48,0.5,0.52,0.54,0.56,0.58,0.6,0.62,0.64,0.66,0.68,0.7,0.72,0.74,0.76,0.78,0.8,0.82,0.84,0.86,0.88,0.9,0.92,0.94,0.96,0.98,1],datasets:[{label:'y=1-(1-x)²',data:[0,0.0396,0.0784,0.1164,0.1536,0.19,0.2256,0.2604,0.2944,0.3276,0.36,0.3916,0.4224,0.4524,0.4816,0.51,0.5376,0.5644,0.5904,0.6156,0.64,0.6636,0.6864,0.7084,0.7296,0.75,0.7696,0.7884,0.8064,0.8236,0.84,0.8556,0.8704,0.8844,0.8976,0.91,0.9216,0.9324,0.9424,0.9516,0.96,0.9676,0.9744,0.9804,0.9856,0.99,0.9936,0.9964,0.9984,0.9996,1],borderColor:'rgb(54,162,235)',borderWidth:3,fill:false,pointRadius:0,tension:0.4}]},options:{plugins:{legend:{display:false}},scales:{x:{title:{display:true,text:'Input'},grid:{display:false}},y:{title:{display:true,text:'Output'},min:0,max:1,grid:{color:'rgba(0,0,0,0.1)'}}}}}>)

Rapid initial response, then **diminishing returns**

---

## Logistic (Sigmoid)

$$y = \frac{1}{1 + e^{-k(x-0.5)}}$$

![Logistic Curve](<https://quickchart.io/chart?bkg=white&c={type:'line',data:{labels:[0,0.02,0.04,0.06,0.08,0.1,0.12,0.14,0.16,0.18,0.2,0.22,0.24,0.26,0.28,0.3,0.32,0.34,0.36,0.38,0.4,0.42,0.44,0.46,0.48,0.5,0.52,0.54,0.56,0.58,0.6,0.62,0.64,0.66,0.68,0.7,0.72,0.74,0.76,0.78,0.8,0.82,0.84,0.86,0.88,0.9,0.92,0.94,0.96,0.98,1],datasets:[{label:'Sigmoid',data:[0.0067,0.0082,0.0100,0.0122,0.0149,0.0181,0.0220,0.0267,0.0323,0.0391,0.0474,0.0573,0.0691,0.0832,0.1000,0.1200,0.1437,0.1717,0.2047,0.2437,0.2895,0.3433,0.4058,0.4778,0.5498,0.6225,0.6945,0.7616,0.8176,0.8638,0.9003,0.9288,0.9505,0.9669,0.9791,0.9879,0.9940,0.9975,0.9991,0.9997,0.9999,1.0000,1.0000,1.0000,1.0000,1.0000,1.0000,1.0000,1.0000,1.0000,1.0000],borderColor:'rgb(153,102,255)',borderWidth:3,fill:false,pointRadius:0,tension:0.4}]},options:{plugins:{legend:{display:false}},scales:{x:{title:{display:true,text:'Input'},grid:{display:false}},y:{title:{display:true,text:'Output'},min:0,max:1,grid:{color:'rgba(0,0,0,0.1)'}}}}}>)

**Threshold behavior**: sharp transition around midpoint

---

## Choosing the Right Curve

| Behavior              | Curve             |
| --------------------- | ----------------- |
| Proportional response | Linear            |
| Ignore low values     | Quadratic         |
| Diminishing returns   | Inverse Quadratic |
| Sharp threshold       | Logistic          |
| Periodic preference   | Sine              |

---

# Combining Considerations

---

## Multiple Factors

An action's utility often depends on **several** considerations:

**"Attack Enemy"** might consider:

- Distance to enemy
- Enemy health
- My health
- Ammo remaining

---

## Multiplication

$$U = c_1 \times c_2 \times c_3 \times ... \times c_n$$

**Key property**: Any zero = entire action scores zero

```python
score = distance_score * health_score * ammo_score
# If ammo_score = 0, action is eliminated
```

---

## Multiplication: Pros & Cons

✅ Natural "veto" behavior  
✅ All considerations must be satisfied  
✅ Supports early-out optimization

❌ Many factors = very small scores  
❌ Hard to tune relative importance

---

## Averaging

$$U = \frac{c_1 + c_2 + c_3 + ... + c_n}{n}$$

**Key property**: No single factor can eliminate an action

```python
score = (distance_score + health_score + ammo_score) / 3
```

---

## Weighted Averaging

$$U = w_1 c_1 + w_2 c_2 + ... + w_n c_n$$

Where $\sum w_i = 1$

Allows **tuning importance** of each consideration

---

## Hybrid Approaches

In practice: **multiply mandatory, average optional**

```python
# Mandatory: must have ammo and line of sight
mandatory = ammo_score * los_score

# Optional: prefer closer, weaker enemies
optional = 0.6 * distance_score + 0.4 * enemy_health_score

final_score = mandatory * optional
```

---

# IAUS Architecture

### Infinite Axis Utility System

---

## The Problem with Fixed Weights

Traditional weighted systems require **rebalancing** when adding new considerations

IAUS solves this through **normalization**

---

## IAUS Principles

1. All inputs normalized to [0, 1]
2. All outputs normalized to [0, 1]
3. Response curves are **reusable**
4. New considerations integrate without rebalancing

---

## Modular Design

```
Action: Attack
├── Consideration: Distance → InverseLinear → 0.8
├── Consideration: EnemyHP → Linear → 0.3
└── Consideration: MyAmmo → Threshold → 1.0

Final: 0.8 × 0.3 × 1.0 = 0.24
```

Add/remove considerations freely

---

# Utility vs Behavior Trees

---

## When to Use BTs

✅ Clear priority ordering  
✅ Discrete, distinct behaviors  
✅ Easy to visualize and debug  
✅ Predictable, testable

---

## When to Use Utility

✅ Many competing behaviors  
✅ Smooth transitions needed  
✅ Context-dependent priorities  
✅ Emergent, believable behavior

---

## Comparison Table

| Aspect           | Behavior Tree | Utility AI                  |
| ---------------- | ------------- | --------------------------- |
| Selection        | First success | Highest score               |
| Debugging        | Visual trace  | Score inspection            |
| Adding behaviors | Insert node   | Add action + considerations |
| Transitions      | Abrupt        | Smooth                      |
| Predictability   | High          | Medium                      |

---

## Hybrid Systems

Many games combine both:

- **BT** for high-level behavior structure
- **Utility** for specific decisions within nodes

```
Selector
├── CombatBehavior (uses utility to pick target)
├── SocialBehavior (uses utility to pick activity)
└── IdleBehavior
```

---

# Case Study: The Sims

---

## Needs System

Each Sim has **needs** (motives):

![needs](https://miro.medium.com/v2/resize:fit:1100/format:webp/1*5x_a_ZjuJ3Jj1k3kJntQLw.gif)
[img source](https://medium.com/@tarikthefirst/living-life-like-the-sims-cc6a3d84ff79)

Hunger, Energy, Social, Hygiene, Bladder, Fun ...

---

## Need Decay

Needs **decrease over time**

Low needs create **high utility** for related actions

```python
eat_utility = 1.0 - hunger_level  # Inverted!
```

---

## Emergent Behavior

No scripted schedules—Sims **choose** based on utility

- Wake up → Energy need satisfied
- Get hungry → Eat utility rises
- Eat → Hunger satisfied, other needs now dominate

**Behavior emerges from competing utilities**

---

## Advertising

Objects **advertise** which needs they satisfy

```
Refrigerator:
  - Hunger: 0.8
  - Fun: 0.0

TV:
  - Hunger: 0.0
  - Fun: 0.7
```

Sims compare advertisements against current needs

---

# Case Study:

## Guild Wars 2

![gw2](https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/1284210/header.jpg)

---

## Scale

- 25+ creature types
- 50+ considerations per agent
- Thousands of simultaneous NPCs

---

## Modular Considerations

Reusable "building blocks":

- `PreferCloseTargets`
- `AvoidFriendlyFire`
- `PreferLowHealthTargets`
- `RequireLineOfSight`

Mix and match for different creature behaviors

---

## Early-Out Optimization

```python
for consideration in action.considerations:
    score *= consideration.evaluate()
    if score == 0:
        break  # No need to evaluate remaining
```

**Mandatory** considerations evaluated first

---

## Performance Results

- 100+ behavior patterns defined
- New creatures created in **minutes**
- Consistent, tunable AI across entire game

---

# Debugging & Tuning

---

## The Challenge

Why did the AI do **that**?

With BTs: follow the tree execution path  
With Utility: examine **all scores**

---

## Score Visualization

Essential tools:

- Real-time score display
- Historical score graphs
- Winning action highlighting
- Consideration breakdown

---

## Common Issues

**Behavior never triggers**

- Check for zero-scoring considerations
- Verify input normalization range

**Wrong action wins**

- Compare scores across actions
- Adjust response curves

---

## Tuning Response Curves

```
Before: Linear → AI too aggressive at range

After: Quadratic → AI only aggressive up close
```

Small curve changes = **significant behavior changes**

---

## Tuning Tips

1. Start with linear curves
2. Observe problematic behaviors
3. Adjust one curve at a time
4. Test edge cases (0%, 50%, 100% inputs)
5. Playtest extensively

---

# Implementation Sketch

---

## Basic Structure

```cpp
class Consideration {
    float Evaluate(GameState state);
    ResponseCurve curve;
    InputSource input;
};

class Action {
    vector<Consideration> considerations;
    float GetUtility(GameState state);
    void Execute();
};
```

---

## Evaluation Loop

```cpp
Action* SelectAction(vector<Action>& actions,
                     GameState& state) {
    Action* best = nullptr;
    float bestScore = 0;

    for (auto& action : actions) {
        float score = action.GetUtility(state);
        if (score > bestScore) {
            bestScore = score;
            best = &action;
        }
    }
    return best;
}
```

---

## Response Curve Examples

```cpp
float Linear(float x) {
    return x;
}

float Quadratic(float x) {
    return x * x;
}

float InverseQuadratic(float x) {
    return 1 - (1-x) * (1-x);
}

float Logistic(float x, float k = 10) {
    return 1.0f / (1.0f + exp(-k * (x - 0.5f)));
}
```

---

# Summary

---

## Key Takeaways

1. Utility AI scores actions numerically
2. Considerations are individual factors
3. Response curves shape how inputs affect scores
4. Multiplication enables "veto" behavior
5. IAUS provides modular, scalable architecture

---

## When to Choose Utility

- Many potential actions
- Smooth, believable transitions
- Context-sensitive decisions
- Emergent behavior desired

---

## Next Steps

- Experiment with Desmos curves
- Watch Dave Mark's GDC talks
- Try implementing a simple utility system
- Consider hybrid BT + Utility approaches

---

# Questions?

### Thursday: Hands-on Utility Tuning

---

# References

- Dave Mark, "An Introduction to Utility Theory" - Game AI Pro
- Mike Lewis, "Choosing Effective Utility-Based Considerations" - Game AI Pro 3
- Dave Mark, GDC 2010: "Improving AI Decision Modeling Through Utility Theory"
- Dave Mark & Mike Lewis, GDC 2015: "Building a Better Centaur"
