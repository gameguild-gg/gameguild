# Utility AI — Assignment

Refer the repo [GameGuild AI4Games Repo](https://github.com/gameguild-gg/ai4games) for the full project context. And read the [GameGuild AI4Games Course](https://gameguild.gg/p/ai4games2) for detailed explanations.

Implement the missing pieces of a minimal Utility AI system for a survival game character.

## What You Need to Implement in `utility.h`

**Response Curves:**

- `Linear::evaluate(float x)` — Return `x` unchanged
- `Quadratic::evaluate(float x)` — Return `x * x` (emphasizes high values)
- `InverseQuadratic::evaluate(float x)` — Return `1 - (1-x)²` (emphasizes low values, diminishing returns)
- `Logistic::evaluate(float x)` — Return `1 / (1 + e^(-10*(x-0.5)))` (sharp threshold at midpoint)

**Considerations:**

- `HungerConsideration::evaluate()` — Normalize `hunger` (0-100) to [0,1], apply curve, return score
- `EnergyConsideration::evaluate()` — Normalize `energy` (0-100) to [0,1], apply curve, return score
- `HealthConsideration::evaluate()` — Normalize `health` (0-100) to [0,1], apply curve, return score

**Actions:**

- `EatAction` — Should have high utility when hunger is high
- `SleepAction` — Should have high utility when energy is low
- `ExploreAction` — Should have high utility when health is good and energy is decent
- `RestAction` — Should have high utility when health is low

**Scoring System:**

- `UtilityAI::selectBestAction()` — Evaluate all actions, return the one with highest utility score

## The Character Actions

You need to configure these four actions with appropriate considerations:

```
EatAction:
  - HungerConsideration with InverseQuadratic (high hunger = high score)

SleepAction:
  - EnergyConsideration with InverseQuadratic (low energy = high score)

ExploreAction:
  - HealthConsideration with Quadratic (need good health)
  - EnergyConsideration with Quadratic (need decent energy)

RestAction:
  - HealthConsideration with InverseQuadratic (low health = high score)
```

The key insight: **Inverted** input means low values should score high. For hunger and health checks where low values should trigger actions, normalize as `1.0 - (value / max)`.

## Input/Output Format

Input: First line `hunger=<int> energy=<int> health=<int>`, then commands per line:

- `hunger=<N>` — Update hunger level (0-100, 0=starving, 100=full)
- `energy=<N>` — Update energy level (0-100, 0=exhausted, 100=energized)
- `health=<N>` — Update health level (0-100, 0=dying, 100=perfect)
- `decide` — Select and print best action: `ACTION:<name> UTILITY:<score>` with score formatted to 4 decimal places

Example:

```
hunger=80 energy=30 health=50
decide
hunger=20
decide
```

Output:

```
ACTION:Sleep UTILITY:0.5625
ACTION:Eat UTILITY:0.6400
```

The `runUtilityAI()` function is already complete and handles all parsing/printing.

```cpp
#ifndef UTILITY_H
#define UTILITY_H

#include <algorithm>
#include <cmath>
#include <iomanip>
#include <iostream>
#include <memory>
#include <sstream>
#include <string>
#include <vector>

namespace Utility
{

    // ============================================================================
    // HINT: Utility AI basics
    // - Response curves transform normalized inputs [0,1] to utility scores [0,1]
    // - Considerations combine context + curve to produce a score
    // - Actions combine multiple considerations (typically via multiplication)
    // - The AI selects the action with highest utility
    // ============================================================================

    // RESPONSE CURVES ------------------------------------------------------------
    // Base class for all response curves
    class ResponseCurve
    {
    public:
        virtual ~ResponseCurve() = default;
        // Takes normalized input [0,1], returns utility score [0,1]
        virtual float evaluate(float x) const = 0;
    };

    // Linear: y = x
    // Uniform response across all values
    class Linear : public ResponseCurve
    {
    public:
        float evaluate(float x) const override
        {
            // TODO: Return x unchanged
            throw std::runtime_error("Linear::evaluate not implemented");
        }
    };

    // Quadratic: y = x²
    // Emphasizes high values, low values matter less
    class Quadratic : public ResponseCurve
    {
    public:
        float evaluate(float x) const override
        {
            // TODO: Return x squared (x * x)
            throw std::runtime_error("Quadratic::evaluate not implemented");
        }
    };

    // Inverse Quadratic: y = 1 - (1-x)²
    // Rapid initial response, then diminishing returns
    class InverseQuadratic : public ResponseCurve
    {
    public:
        float evaluate(float x) const override
        {
            // TODO: Return 1 - (1-x)²
            // Hint: Calculate (1-x) first, square it, then subtract from 1
            throw std::runtime_error("InverseQuadratic::evaluate not implemented");
        }
    };

    // Logistic (Sigmoid): y = 1 / (1 + e^(-k*(x-0.5)))
    // Sharp threshold behavior around midpoint
    class Logistic : public ResponseCurve
    {
    public:
        explicit Logistic(float steepness = 10.0f) : m_k(steepness) {}

        float evaluate(float x) const override
        {
            // TODO: Return 1 / (1 + e^(-k*(x-0.5)))
            // Use std::exp() for the exponential function
            // Remember: m_k is the steepness parameter
            throw std::runtime_error("Logistic::evaluate not implemented");
        }

    private:
        float m_k; // Steepness parameter
    };

    // CONTEXT --------------------------------------------------------------------
    // The character state used by considerations
    struct CharacterContext
    {
        int hunger = 0; // 0=starving, 100=full
        int energy = 0; // 0=exhausted, 100=energized
        int health = 0; // 0=dying, 100=perfect
    };

    // CONSIDERATIONS -------------------------------------------------------------
    // Base class for all considerations
    class Consideration
    {
    public:
        explicit Consideration(std::shared_ptr<ResponseCurve> curve) : m_curve(std::move(curve)) {}
        virtual ~Consideration() = default;

        // Evaluate this consideration given current context
        // Returns a score in [0,1]
        virtual float evaluate(const CharacterContext &ctx) const = 0;

    protected:
        std::shared_ptr<ResponseCurve> m_curve;
    };

    // Hunger: When hungry (low value), eating should have high utility
    class HungerConsideration : public Consideration
    {
    public:
        explicit HungerConsideration(std::shared_ptr<ResponseCurve> curve, bool inverted = true)
            : Consideration(std::move(curve)), m_inverted(inverted) {}

        float evaluate(const CharacterContext &ctx) const override
        {
            // TODO:
            // 1. Normalize hunger (0-100) to [0,1] by dividing by 100.0f
            // 2. If m_inverted is true, invert it: normalized = 1.0f - normalized
            //    (This makes low hunger → high score, which triggers eating)
            // 3. Apply the response curve: m_curve->evaluate(normalized)
            // 4. Return the result
            throw std::runtime_error("HungerConsideration::evaluate not implemented");
        }

    private:
        bool m_inverted;
    };

    // Energy: When tired (low value), sleeping should have high utility
    class EnergyConsideration : public Consideration
    {
    public:
        explicit EnergyConsideration(std::shared_ptr<ResponseCurve> curve, bool inverted = true)
            : Consideration(std::move(curve)), m_inverted(inverted) {}

        float evaluate(const CharacterContext &ctx) const override
        {
            // TODO:
            // 1. Normalize energy (0-100) to [0,1] by dividing by 100.0f
            // 2. If m_inverted is true, invert it: normalized = 1.0f - normalized
            //    (This makes low energy → high score, which triggers sleeping)
            // 3. Apply the response curve: m_curve->evaluate(normalized)
            // 4. Return the result
            throw std::runtime_error("EnergyConsideration::evaluate not implemented");
        }

    private:
        bool m_inverted;
    };

    // Health: This one is NOT inverted by default - high health = high score
    class HealthConsideration : public Consideration
    {
    public:
        explicit HealthConsideration(std::shared_ptr<ResponseCurve> curve, bool inverted = false)
            : Consideration(std::move(curve)), m_inverted(inverted)
        {
        }

        float evaluate(const CharacterContext &ctx) const override
        {
            // TODO:
            // 1. Normalize health (0-100) to [0,1] by dividing by 100.0f
            // 2. If m_inverted is true, invert it: normalized = 1.0f - normalized
            //    - NOT inverted (false): high health → high score (for exploring)
            //    - Inverted (true): low health → high score (for resting)
            // 3. Apply the response curve: m_curve->evaluate(normalized)
            // 4. Return the result
            throw std::runtime_error("HealthConsideration::evaluate not implemented");
        }

    private:
        bool m_inverted;
    };

    // ACTIONS --------------------------------------------------------------------
    // Base class for all actions
    class Action
    {
    public:
        explicit Action(std::string name) : m_name(std::move(name)) {}
        virtual ~Action() = default;

        void addConsideration(std::shared_ptr<Consideration> consideration)
        {
            m_considerations.push_back(std::move(consideration));
        }

        // Calculate utility by combining all considerations
        float calculateUtility(const CharacterContext &ctx) const
        {
            // TODO: Implement IAUS multiplication approach
            // 1. Start with score = 1.0f
            // 2. Loop through m_considerations vector
            // 3. For each consideration, multiply score by consideration->evaluate(ctx)
            // 4. Return the final score
            // Note: If any consideration returns 0, the whole action scores 0 (veto behavior)
            throw std::runtime_error("Action::calculateUtility not implemented");
        }

        const std::string &getName() const { return m_name; }

    protected:
        std::string m_name;
        std::vector<std::shared_ptr<Consideration>> m_considerations;
    };

    // Specific actions
    class EatAction : public Action
    {
    public:
        EatAction() : Action("Eat") {}
    };

    class SleepAction : public Action
    {
    public:
        SleepAction() : Action("Sleep") {}
    };

    class ExploreAction : public Action
    {
    public:
        ExploreAction() : Action("Explore") {}
    };

    class RestAction : public Action
    {
    public:
        RestAction() : Action("Rest") {}
    };

    // UTILITY AI SYSTEM ----------------------------------------------------------
    class UtilityAI
    {
    public:
        void addAction(std::shared_ptr<Action> action)
        {
            m_actions.push_back(std::move(action));
        }

        // Select the action with highest utility
        std::shared_ptr<Action> selectBestAction(const CharacterContext &ctx) const
        {
            // TODO:
            // 1. If m_actions is empty, return nullptr
            // 2. Initialize bestAction = nullptr and bestUtility = -1.0f
            // 3. Loop through all actions in m_actions
            // 4. For each action, calculate its utility: action->calculateUtility(ctx)
            // 5. If utility >= bestUtility (use >= to prefer later actions on ties):
            //    - Update bestUtility to this utility
            //    - Update bestAction to this action
            // 6. Return bestAction
            throw std::runtime_error("UtilityAI::selectBestAction not implemented");
        }

    private:
        std::vector<std::shared_ptr<Action>> m_actions;
    };

    // FACTORY --------------------------------------------------------------------
    // Build the complete utility AI system with all actions and considerations
    inline std::unique_ptr<UtilityAI> createSurvivalAI()
    {
        auto ai = std::make_unique<UtilityAI>();

        // TODO: Create and configure all four actions in this order:
        // Order matters for tie-breaking!
        //
        // 1. EatAction:
        //    - Create with std::make_shared<EatAction>()
        //    - Add HungerConsideration with InverseQuadratic curve (inverted by default)
        //    - Add to ai with ai->addAction()
        //
        // 2. SleepAction:
        //    - Create with std::make_shared<SleepAction>()
        //    - Add EnergyConsideration with InverseQuadratic curve (inverted by default)
        //    - Add to ai
        //
        // 3. RestAction:
        //    - Create with std::make_shared<RestAction>()
        //    - Add HealthConsideration with InverseQuadratic curve
        //    - IMPORTANT: Pass true as second argument to invert (low health = high score)
        //    - Add to ai
        //
        // 4. ExploreAction:
        //    - Create with std::make_shared<ExploreAction>()
        //    - Add HealthConsideration with Quadratic curve
        //      IMPORTANT: Pass false as second argument (high health = high score)
        //    - Add EnergyConsideration with Quadratic curve
        //      IMPORTANT: Pass false as second argument (high energy = high score)
        //    - Add to ai (add last so it wins ties when all stats are perfect)
        //
        // Example pattern:
        //   auto eatAction = std::make_shared<EatAction>();
        //   eatAction->addConsideration(
        //       std::make_shared<HungerConsideration>(std::make_shared<InverseQuadratic>()));
        //   ai->addAction(eatAction);

        return ai;
    }

    // RUNNER ---------------------------------------------------------------------
    // Parse input and execute decisions. See README for expected I/O.
    inline std::string runUtilityAI(const std::string &input)
    {
        std::istringstream in(input);
        std::ostringstream out;

        CharacterContext ctx{};

        // Parse first line: hunger=<int> energy=<int> health=<int>
        std::string firstLine;
        if (!std::getline(in, firstLine))
        {
            return out.str();
        }
        {
            std::istringstream init(firstLine);
            std::string token;
            while (init >> token)
            {
                auto pos = token.find('=');
                if (pos == std::string::npos)
                    continue;
                auto key = token.substr(0, pos);
                auto val = token.substr(pos + 1);
                if (key == "hunger")
                {
                    ctx.hunger = std::stoi(val);
                }
                else if (key == "energy")
                {
                    ctx.energy = std::stoi(val);
                }
                else if (key == "health")
                {
                    ctx.health = std::stoi(val);
                }
            }
        }

        auto ai = createSurvivalAI();

        // Process remaining line commands
        std::string line;
        while (std::getline(in, line))
        {
            if (line.empty())
                continue;
            if (line.rfind("hunger=", 0) == 0)
            {
                try
                {
                    ctx.hunger = std::stoi(line.substr(7));
                }
                catch (...)
                {
                    // ignore bad values
                }
            }
            else if (line.rfind("energy=", 0) == 0)
            {
                try
                {
                    ctx.energy = std::stoi(line.substr(7));
                }
                catch (...)
                {
                    // ignore bad values
                }
            }
            else if (line.rfind("health=", 0) == 0)
            {
                try
                {
                    ctx.health = std::stoi(line.substr(7));
                }
                catch (...)
                {
                    // ignore bad values
                }
            }
            else if (line == "decide")
            {
                auto bestAction = ai->selectBestAction(ctx);
                if (bestAction)
                {
                    float utility = bestAction->calculateUtility(ctx);
                    out << "ACTION:" << bestAction->getName() << " UTILITY:" << std::fixed
                        << std::setprecision(4) << utility << "\n";
                }
            }
            else
            {
                // ignore unknown commands
            }
        }

        return out.str();
    }

} // namespace Utility

#endif // UTILITY_H
```
