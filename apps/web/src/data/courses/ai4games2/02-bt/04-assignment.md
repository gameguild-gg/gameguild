# Behavior Trees — Assignment

Refer the repo [GameGuild AI4Games Repo](https://github.com/gameguild-gg/ai4games) for the full project context. And read the [GameGuild AI4Games Course](https://gameguild.gg/p/ai4games2) for detailed explanations.

Implement the missing pieces of a minimal Behavior Tree system for a guard AI.

## What You Need to Implement in `bt.h`

**Composite Nodes:**

- `Selector::tick()` — Try children in order until one returns Success or Running; otherwise return Failure
- `Sequence::tick()` — Run children in order; if any returns Failure or Running, stop and return that status; otherwise return Success

**Leaf Nodes (Conditions):**

- `CanSeePlayer::tick()` — Return Success if `m_ctx.canSeePlayer` is true, else Failure
- `HasAmmo::tick()` — Return Success if `m_ctx.ammo > 0`, else Failure

**Leaf Nodes (Actions):**

- `Shoot::tick()` — Decrement ammo, print `"Bang! Ammo: <new_ammo>"`, return Success
- `Patrol::tick()` — Print `"Patrolling"`, return Running
- `Chase::tick()` — Print `"Chasing"`, return Running

**Factory:**

- `createGuardBT(GuardContext&)` — Build and return the root node of the tree below:

```
Selector (root)
├── Sequence (Combat)
│   ├── Condition: CanSeePlayer?
│   └── Selector (Attack)
│       ├── Sequence (Shoot)
│       │   ├── Condition: HasAmmo?
│       │   └── Action: Shoot
│       └── Action: Chase
└── Action: Patrol
```

## Input/Output Format

Input: First line `canSeePlayer=<true|false> ammo=<int>`, then commands per line:

- `see` / `nosee` — Update visibility
- `ammo=<N>` — Set ammo
- `tick` — Execute one tree tick and print leaf outputs + `ROOT:<Success|Failure|Running>`

The `runBT()` function is already complete and handles all parsing/printing.

## bt.h

```cpp
#ifndef BT_H
#define BT_H

#include <iostream>
#include <memory>
#include <sstream>
#include <string>
#include <vector>

namespace BT
{

    // ============================================================================
    // HINT: Behavior Trees basics
    // - Status values
    // - Node interface with tick()
    // - Composite nodes: Selector, Sequence
    // - Leaf nodes: Conditions (no Running) and Actions (can Running)
    // ============================================================================

    enum class Status
    {
        Success,
        Failure,
        Running
    };

    class Node
    {
    public:
        virtual ~Node() = default;
        // We will implement tick to drive node behavior and write outputs when needed
        virtual Status tick(std::ostream &out) = 0; // pure virtual
    };

    using NodePtr = std::shared_ptr<Node>;

    // COMPOSITES -----------------------------------------------------------------
    // HINT: Selector = "Try children until one works (OR logic)"
    // Semantics:
    // - Try each child in order
    // - If a child returns Success or Running, STOP and return that status
    // - Only if ALL children return Failure, return Failure
    // Think: "First plan that doesn't fail wins"
    class Selector : public Node
    {
    public:
        void add(NodePtr child) { m_children.push_back(std::move(child)); }

        Status tick(std::ostream &out) override
        {
            // TODO: Implement Selector semantics
            // Loop through children, tick each one
            // If any child returns Success or Running, return immediately
            // If all fail, return Failure
            throw std::runtime_error("Selector::tick not implemented");
        }

    private:
        std::vector<NodePtr> m_children;
    };

    class Sequence : public Node
    {
    public:
        void add(NodePtr child) { m_children.push_back(std::move(child)); }

        Status tick(std::ostream &out) override
        {
            // TODO: Implement Sequence semantics
            // Loop through children, tick each one
            // If any child returns Failure or Running, return immediately
            // If all succeed, return Success
            throw std::runtime_error("Sequence::tick not implemented");
        }

    private:
        std::vector<NodePtr> m_children;
    };

    // CONTEXT --------------------------------------------------------------------
    // The guard state used by leaves
    struct GuardContext
    {
        bool canSeePlayer = false;
        int ammo = 0;
    };

    // LEAVES ---------------------------------------------------------------------
    class CanSeePlayer : public Node
    {
    public:
        explicit CanSeePlayer(GuardContext &ctx) : m_ctx(ctx) {}
        Status tick(std::ostream & /*out*/) override
        {
            // todo: return Success if m_ctx.canSeePlayer is true, else Failure
            throw std::runtime_error("CanSeePlayer::tick not implemented");
        }

    private:
        GuardContext &m_ctx;
    };

    class HasAmmo : public Node
    {
    public:
        explicit HasAmmo(GuardContext &ctx) : m_ctx(ctx) {}
        Status tick(std::ostream & /*out*/) override
        {
            // todo: return Success if m_ctx.ammo > 0, else Failure
            throw std::runtime_error("HasAmmo::tick not implemented");
        }

    private:
        GuardContext &m_ctx;
    };

    class Shoot : public Node
    {
    public:
        explicit Shoot(GuardContext &ctx) : m_ctx(ctx) {}
        Status tick(std::ostream &out) override
        {
            // Todo:
            // - consume one ammo,
            // - print "Bang! Ammo: <remaining>"
            // - return Success
            throw std::runtime_error("Shoot::tick not implemented");
        }

    private:
        GuardContext &m_ctx;
    };

    class Patrol : public Node
    {
    public:
        Status tick(std::ostream &out) override
        {
            // todo: Print "Patrolling" and return Running
            throw std::runtime_error("Patrol::tick not implemented");
        }
    };

    class Chase : public Node
    {
    public:
        Status tick(std::ostream &out) override
        {
            // TODO: Print "Chasing\n" and return Running
            throw std::runtime_error("Chase::tick not implemented");
        }
    };

    // FACTORY --------------------------------------------------------------------
    // TODO: Build the fixed tree described in README
    // Selector(Root)
    //  ├─ Sequence(Combat)
    //  │   ├─ CanSeePlayer
    //  │   └─ Selector(Attack)
    //  │       ├─ Sequence(Shoot)
    //  │       │   ├─ HasAmmo
    //  │       │   └─ Shoot
    //  │       └─ Chase
    //  └─ Patrol
    // Return the root node
    inline NodePtr createGuardBT(GuardContext &ctx)
    {
        // Build the tree as specified in README
        auto root = std::make_shared<Selector>();

        // todo: keep going...
        // hint:
        // auto combat = std::make_shared<???>();
        // combat->add(std::make_shared<???>(ctx));
        // ???

        return root;
    }

    // RUNNER ---------------------------------------------------------------------
    // Parse input and execute ticks. See README for expected I/O.
    inline std::string runBT(const std::string &input)
    {
        std::istringstream in(input);
        std::ostringstream out;

        GuardContext ctx{};

        // Parse first line: canSeePlayer=<true|false> ammo=<int>
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
                if (key == "canSeePlayer")
                {
                    ctx.canSeePlayer = (val == "true" || val == "1");
                }
                else if (key == "ammo")
                {
                    ctx.ammo = std::stoi(val);
                }
            }
        }

        auto root = createGuardBT(ctx);

        // Process remaining lines commands
        std::string line;
        while (std::getline(in, line))
        {
            if (line.empty())
                continue;
            if (line == "see")
            {
                ctx.canSeePlayer = true;
            }
            else if (line == "nosee")
            {
                ctx.canSeePlayer = false;
            }
            else if (line.rfind("ammo=", 0) == 0)
            {
                try
                {
                    ctx.ammo = std::stoi(line.substr(5));
                }
                catch (...)
                {
                    // ignore bad ammo values
                }
            }
            else if (line == "tick")
            {
                Status s = root->tick(out);
                switch (s)
                {
                case Status::Success:
                    out << "ROOT:Success\n";
                    break;
                case Status::Failure:
                    out << "ROOT:Failure\n";
                    break;
                case Status::Running:
                    out << "ROOT:Running\n";
                    break;
                }
            }
            else
            {
                // ignore unknown commands
            }
        }

        return out.str();
    }

} // namespace BT

#endif // BT_H
```
