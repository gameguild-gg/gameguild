# Week 11 Quiz — Goal-Oriented Action Planning

!!! quiz
{
"title": "Question 1",
"question": "In the STRIPS planning formalism, what does the quadruple ⟨P, O, I, G⟩ represent?",
"options": [
"Propositions, Operators, Initial state, Goal state",
"Position, Orientation, Inventory, Goals",
"Players, Objects, Items, Games",
"Priority, Options, Input, Generation"
],
"answers": ["Propositions, Operators, Initial state, Goal state"]
}
!!!

!!! quiz
{
"title": "Question 2",
"question": "Why did Jeff Orkin replace FSMs with GOAP for F.E.A.R.'s AI?",
"options": [
"FSMs were too slow at runtime for the game's frame budget",
"FSMs required O(n²) transition authoring that became unmanageable at ~80 states",
"FSMs could not handle pathfinding on navigation meshes",
"FSMs did not support animations"
],
"answers": ["FSMs required O(n²) transition authoring that became unmanageable at ~80 states"]
}
!!!

!!! quiz
{
"title": "Question 3",
"question": "In GOAP, how is the world state typically represented?",
"options": [
"A list of booleans indexed by position",
"A single integer encoding all conditions",
"A graph of connected state nodes",
"Key-value pairs representing conditions and their values"
],
"answers": ["Key-value pairs representing conditions and their values"]
}
!!!

!!! quiz
{
"title": "Question 4",
"question": "F.E.A.R. reduced all NPC animation behavior to just three FSM states. Which are they?",
"options": [
"GoTo, Animate, UseSmartObject",
"Idle, Combat, Flee",
"Patrol, Chase, Attack",
"Move, Shoot, Cover"
],
"answers": ["GoTo, Animate, UseSmartObject"]
}
!!!

!!! quiz
{
"title": "Question 5",
"question": "Why does F.E.A.R.'s GOAP implementation use backward (regressive) planning instead of forward planning?",
"options": [
"Backward planning produces shorter plans",
"Forward planning cannot use A*",
"Goals have fewer conditions than the full world state, so backward search starts narrower",
"Backward planning does not require a heuristic function"
],
"answers": ["Goals have fewer conditions than the full world state, so backward search starts narrower"]
}
!!!

!!! quiz
{
"title": "Question 6",
"question": "In F.E.A.R., both FlankEnemy (cost 5) and MoveToRange (cost 2) produce the effect 'enemyInRange = true'. What behavior does this create?",
"options": [
"The planner always chooses flanking for realism",
"The planner randomly selects between the two actions",
"The planner prefers the cheaper direct approach, but automatically discovers flanking when the direct path is blocked",
"Both actions always execute in sequence"
],
"answers": ["The planner prefers the cheaper direct approach, but automatically discovers flanking when the direct path is blocked"]
}
!!!

!!! quiz
{
"title": "Question 7",
"question": "How does squad coordination actually work in F.E.A.R.'s AI?",
"options": [
"A central coordinator assigns tactical roles to each NPC",
"NPCs send messages to each other before executing their plans",
"Squad behavior is entirely scripted with predefined formations",
"Each NPC plans independently; verbal commands like 'flanking!' are triggered after the planning decision"
],
"answers": ["Each NPC plans independently; verbal commands like 'flanking!' are triggered after the planning decision"]
}
!!!

!!! quiz
{
"title": "Question 8",
"question": "What heuristic does a GOAP planner use, and why is it admissible?",
"options": [
"Count of unsatisfied goal conditions — each requires at least one action, so it never overestimates",
"Manhattan distance — it always underestimates physical distance",
"Sum of all action costs — it provides an upper bound on plan cost",
"Number of available actions — more actions mean more planning needed"
],
"answers": ["Count of unsatisfied goal conditions — each requires at least one action, so it never overestimates"]
}
!!!

!!! quiz
{
"title": "Question 9",
"question": "Which advantage does GOAP have over traditional FSMs when adding new NPC behaviors?",
"options": [
"GOAP is always faster at runtime than FSMs",
"GOAP uses less memory than FSMs",
"GOAP eliminates the need for any state machine",
"New actions can be added independently without modifying existing transitions"
],
"answers": ["New actions can be added independently without modifying existing transitions"]
}
!!!

!!! quiz
{
"title": "Question 10",
"question": "What triggers runtime replanning in a GOAP system?",
"options": [
"Replanning occurs at fixed time intervals regardless of world state",
"Replanning is triggered when a precondition is no longer met, a higher-priority goal activates, or sensors detect a significant state change",
"Replanning only happens when the current plan completes successfully",
"The plan is regenerated every frame for maximum accuracy"
],
"answers": ["Replanning is triggered when a precondition is no longer met, a higher-priority goal activates, or sensors detect a significant state change"]
}
!!!
