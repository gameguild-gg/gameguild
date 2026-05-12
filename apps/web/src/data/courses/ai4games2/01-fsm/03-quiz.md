# Quiz 01 - Finite State Machines

**Topics:** FSM basics, State Pattern, Stack-based FSMs, Hierarchical FSMs

---

!!! quiz
{
"title": "Switch Statement Problems",
"question": "What is the main problem with implementing FSMs using `switch` statements?",
"options": [
"They cannot handle more than 3 states",
"They are slower than other implementations",
"They don't support transitions",
"They become unmanageable as complexity increases"
],
"answers": ["They become unmanageable as complexity increases"]
}
!!!

!!! quiz
{
"title": "State Pattern Methods",
"question": "In the State Pattern, what are the three core methods each state should implement?",
"options": [
"`onEnter()`, `execute()`, `onExit()`",
"`start()`, `stop()`, `reset()`",
"`init()`, `run()`, `destroy()`",
"`begin()`, `update()`, `end()`"
],
"answers": ["`onEnter()`, `execute()`, `onExit()`"]
}
!!!

!!! quiz
{
"title": "FSM Transition Timing",
"question": "True or False: In a basic FSM, when a transition condition is met, the state change happens immediately.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

!!! quiz
{
"title": "Stack-based FSM Data Structure",
"question": "What data structure does a Stack-based FSM (pushdown automaton) use to manage states?",
"options": ["Queue", "Stack", "Binary Tree", "Linked List"],
"answers": ["Stack"]
}
!!!

!!! quiz
{
"title": "FSM Transition Rules",
"question": "Given this FSM diagram, what triggers the transition from `Chase` to `Patrol`?\n `Patrol → Chase (sees player)` \n `Chase → Attack (in range)` \n `Attack → Chase (out of range)` \n `Chase → Patrol (loses player)`",
"options": [
"Player goes out of range",
"Player is in range",
"AI loses sight of the player",
"AI takes damage"
],
"answers": ["AI loses sight of the player"]
}
!!!

!!! quiz
{
"title": "Transition Map Advantages",
"question": "What is the primary advantage of using a transition map over hardcoded transitions?",
"options": [
"Faster execution speed",
"Less memory usage",
"Flexibility and maintainability",
"Better graphics rendering"
],
"answers": ["Flexibility and maintainability"]
}
!!!

!!! quiz
{
"title": "Stack-based FSM Use Cases",
"question": "When would you use a Stack-based FSM instead of a regular FSM?",
"options": [
"When you have only two states",
"When transitions are random",
"When you need faster performance",
"When states are transactional and should return to the previous state"
],
"answers": ["When states are transactional and should return to the previous state"]
}
!!!

!!! quiz
{
"title": "Hierarchical State Machine Entry",
"question": "In a Hierarchical State Machine (HSM), what happens when you enter a parent state?",
"options": [
"Its initial substate is automatically entered",
"All sibling states are also entered",
"The parent state is skipped",
"Nothing special occurs"
],
"answers": ["Its initial substate is automatically entered"]
}
!!!

!!! quiz
{
"title": "State Lifecycle Order",
"question": "In the State Pattern implementation, why is it important to call `onExit()` before `onEnter()` during a state change?",
"options": [
"To make the code run faster",
"To prevent memory leaks only",
"It's not important, order doesn't matter",
"To properly cleanup the current state before initializing the new one"
],
"answers": ["To properly cleanup the current state before initializing the new one"]
}
!!!

!!! quiz
{
"title": "HSM Behavior Inheritance",
"question": "True or False: Hierarchical State Machines (HSMs) allow child states to share common behavior defined in parent states.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!
