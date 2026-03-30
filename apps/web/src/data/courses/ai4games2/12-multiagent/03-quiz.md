# Week 12 Quiz — Multi-Agent Coordination

!!! quiz
{
"title": "Question 1",
"question": "What is the core 'coordination problem' when multiple identical GOAP agents plan independently against the same target?",
"options": [
"All agents converge on the same optimal plan, producing identical behavior",
"Each agent's plan takes too long to compute, exceeding the frame budget",
"Agents cannot find any valid plan because goals conflict with each other",
"The A* heuristic becomes inadmissible with multiple agents"
],
"answers": ["All agents converge on the same optimal plan, producing identical behavior"]
}
!!!

!!! quiz
{
"title": "Question 2",
"question": "Which coordination approach do most shipped games use, and why?",
"options": [
"Centralized — a single commander decides everything for maximum control",
"Hybrid/Hierarchical — high-level coordination with low-level autonomy balances control and emergence",
"Decentralized — each agent plans independently for maximum emergence",
"Random — agents randomly choose different actions to avoid convergence"
],
"answers": ["Hybrid/Hierarchical — high-level coordination with low-level autonomy balances control and emergence"]
}
!!!

!!! quiz
{
"title": "Question 3",
"question": "In the Observer pattern, what is the 'lapsed listener' problem?",
"options": [
"Observers receive events too slowly and miss important updates",
"The subject forgets to send notifications after a certain number of events",
"A destroyed observer remains registered, causing a dangling pointer crash when the subject notifies",
"Observers receive events in a random order instead of the correct sequence"
],
"answers": ["A destroyed observer remains registered, causing a dangling pointer crash when the subject notifies"]
}
!!!

!!! quiz
{
"title": "Question 4",
"question": "What is the primary advantage of an Event Queue over the direct Observer pattern for game AI communication?",
"options": [
"It uses less memory than the Observer pattern",
"It guarantees that events are never dropped",
"It allows observers to modify events before they are delivered",
"It decouples communication in time, preventing frame spikes from expensive handlers"
],
"answers": ["It decouples communication in time, preventing frame spikes from expensive handlers"]
}
!!!

!!! quiz
{
"title": "Question 5",
"question": "Three NPCs spot the same enemy within 100ms. What does event aggregation produce?",
"options": [
"A single ENEMY_CONFIRMED event with averaged position and high confidence",
"Three separate ENEMY_SPOTTED events delivered to all handlers",
"The first event is delivered and the other two are silently dropped",
"All three events are queued and delivered on the next frame"
],
"answers": ["A single ENEMY_CONFIRMED event with averaged position and high confidence"]
}
!!!

!!! quiz
{
"title": "Question 6",
"question": "In a publish-subscribe system, a medic NPC subscribes only to 'ally_wounded' events. What happens when an ENEMY_SPOTTED event is published?",
"options": [
"The medic receives the event but ignores it in its handler",
"The medic does not receive the event at all — the broker filters it out",
"The system throws an error because no handler exists for that event type",
"The event is queued until the medic subscribes to ENEMY_SPOTTED"
],
"answers": ["The medic does not receive the event at all — the broker filters it out"]
}
!!!

!!! quiz
{
"title": "Question 7",
"question": "The blackboard architecture was originally developed for which system?",
"options": [
"The F.E.A.R. combat AI system at Monolith Productions",
"The Killzone 2 multiplayer bot system at Guerrilla Games",
"The HEARSAY-II speech recognition system at Carnegie Mellon University in the 1970s",
"The Unreal Engine AI framework at Epic Games"
],
"answers": ["The HEARSAY-II speech recognition system at Carnegie Mellon University in the 1970s"]
}
!!!

!!! quiz
{
"title": "Question 8",
"question": "What are the three components of a blackboard architecture?",
"options": [
"Publisher, Subscriber, and Broker",
"Subject, Observer, and Event Queue",
"Strategic Layer, Tactical Layer, and Individual Layer",
"Blackboard (shared data store), Knowledge Sources (specialist modules), and Control Shell (moderator/scheduler)"
],
"answers": ["Blackboard (shared data store), Knowledge Sources (specialist modules), and Control Shell (moderator/scheduler)"]
}
!!!

!!! quiz
{
"title": "Question 9",
"question": "What is the role of the control shell in a blackboard architecture?",
"options": [
"It decides which knowledge source runs next and prevents scheduling conflicts",
"It stores and retrieves data for knowledge sources",
"It directly communicates messages between knowledge sources",
"It displays the blackboard contents for debugging"
],
"answers": ["It decides which knowledge source runs next and prevents scheduling conflicts"]
}
!!!

!!! quiz
{
"title": "Question 10",
"question": "Why is staleness checking important when querying a blackboard?",
"options": [
"To save memory by removing old entries automatically",
"To prevent agents from acting on outdated information, such as an enemy position posted 10 seconds ago",
"To ensure the blackboard never exceeds a maximum number of entries",
"To prioritize recent entries over older ones for display purposes"
],
"answers": ["To prevent agents from acting on outdated information, such as an enemy position posted 10 seconds ago"]
}
!!!

!!! quiz
{
"title": "Question 11",
"question": "When should you use a blackboard vs. an event queue for agent communication?",
"options": [
"Always use a blackboard — event queues are obsolete",
"Use an event queue for all communication — blackboards are too slow",
"Use a blackboard for persistent shared state queried repeatedly; use an event queue for one-time discrete notifications",
"Use a blackboard only for enemy positions and an event queue for everything else"
],
"answers": ["Use a blackboard for persistent shared state queried repeatedly; use an event queue for one-time discrete notifications"]
}
!!!

!!! quiz
{
"title": "Question 12",
"question": "In Killzone 2/3's hierarchical AI, what does the tactical layer do?",
"options": [
"Decides which objectives the entire faction should attack or defend",
"Handles individual NPC movement, aiming, and cover selection",
"Manages the game's strategic overview minimap",
"Assigns roles within a squad (suppressor, flanker, rusher) and coordinates squad-level tactics"
],
"answers": ["Assigns roles within a squad (suppressor, flanker, rusher) and coordinates squad-level tactics"]
}
!!!

!!! quiz
{
"title": "Question 13",
"question": "In Killzone's three-layer hierarchy, how often does each layer update?",
"options": [
"All three layers update every frame for maximum responsiveness",
"Strategic: every frame, Tactical: every 5 seconds, Individual: every 30 seconds",
"All layers update at the same fixed rate of once per second",
"Strategic: every 10-30 seconds, Tactical: every 1-5 seconds, Individual: every frame"
],
"answers": ["Strategic: every 10-30 seconds, Tactical: every 1-5 seconds, Individual: every frame"]
}
!!!

!!! quiz
{
"title": "Question 14",
"question": "What is the 'Kung-Fu Circle' problem in game AI?",
"options": [
"Without coordination, all NPCs attack simultaneously, overwhelming the player instantly",
"Enemies form a perfect circle around the player, making combat too predictable",
"NPCs get stuck in circular pathfinding loops around the player",
"Enemies take turns attacking one-at-a-time, looking unrealistically polite"
],
"answers": ["Without coordination, all NPCs attack simultaneously, overwhelming the player instantly"]
}
!!!

!!! quiz
{
"title": "Question 15",
"question": "How does a token system prevent the 'everyone rush the player' problem?",
"options": [
"It removes the attack action from most NPCs entirely",
"It limits how many agents can perform a specific action simultaneously by requiring a scarce token",
"It slows down NPC movement speed so they arrive at different times",
"It reduces the total number of NPCs spawned in the encounter"
],
"answers": ["It limits how many agents can perform a specific action simultaneously by requiring a scarce token"]
}
!!!

!!! quiz
{
"title": "Question 16",
"question": "How do token systems simplify difficulty scaling?",
"options": [
"By writing completely separate AI code for each difficulty level",
"By reducing the number of NPCs on easier difficulties",
"By changing only the token pool configuration (max tokens, cooldowns) while keeping the AI code identical across all difficulties",
"By increasing NPC health on harder difficulties"
],
"answers": ["By changing only the token pool configuration (max tokens, cooldowns) while keeping the AI code identical across all difficulties"]
}
!!!

!!! quiz
{
"title": "Question 17",
"question": "Why does companion AI require a model of the player's behavior?",
"options": [
"To control the player's movement and prevent them from going the wrong way",
"To decide when to trigger a cutscene or end the level",
"To adapt its behavior — hiding when the player sneaks, fighting when the player engages in combat",
"To display the player's current state on a HUD indicator"
],
"answers": ["To adapt its behavior — hiding when the player sneaks, fighting when the player engages in combat"]
}
!!!

!!! quiz
{
"title": "Question 18",
"question": "What does the companion AI do when the player gets too far ahead and the companion enters the 'panic zone' (>15m)?",
"options": [
"The companion gives up and despawns until the next checkpoint",
"The companion sprints at double speed toward the player",
"The companion calls out for the player to stop and wait",
"The companion teleports to a position the player cannot currently see"
],
"answers": ["The companion teleports to a position the player cannot currently see"]
}
!!!

!!! quiz
{
"title": "Question 19",
"question": "In F.E.A.R., at what point in the AI pipeline are voice lines like 'Flanking left!' selected?",
"options": [
"After the planner decides the action but before execution — it's retroactive narration, not real communication",
"Before the GOAP planner runs, to set the NPC's intent for the planner",
"During planning, as an explicit plan step between other actions",
"After the action is fully completed, as a status report"
],
"answers": ["After the planner decides the action but before execution — it's retroactive narration, not real communication"]
}
!!!

!!! quiz
{
"title": "Question 20",
"question": "Why does each Agent hold a pointer to a shared Blackboard instead of maintaining its own copy?",
"options": [
"Solely to save memory — the data is identical anyway",
"So that when one agent posts knowledge (e.g., enemy position), all squad members can immediately query it through the same data store",
"Because agents do not need any knowledge to make decisions",
"To prevent agents from accidentally reading each other's private data"
],
"answers": ["So that when one agent posts knowledge (e.g., enemy position), all squad members can immediately query it through the same data store"]
}
!!!
