# Week 02 Quiz - Behavior Trees

!!! quiz
{
"title": "Event-Driven BT Advantage",
"question": "What is the primary advantage of event-driven Behavior Trees over traditional tick-based implementations?",
"options": [
"They are easier to debug in production",
"They allow more complex tree structures",
"They only re-evaluate when relevant state changes, reducing unnecessary processing",
"They eliminate the need for Selector nodes"
],
"answers": ["They only re-evaluate when relevant state changes, reducing unnecessary processing"]
}
!!!

!!! quiz
{
"title": "Deep Nesting Anti-Pattern",
"question": "True or False: A well-designed Behavior Tree should have deep nesting (10+ levels) to capture all possible decision nuances.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

!!! quiz
{
"title": "Unreal Abort Mode: Lower Priority",
"question": "In Unreal Engine's BT implementation, what does the 'Lower Priority' abort mode do?",
"options": [
"Aborts the current node if a higher-priority branch becomes valid",
"Prevents any node from being aborted",
"Aborts lower-priority branches when the current decorator's condition becomes true",
"Restarts the entire tree from the root"
],
"answers": ["Aborts the current node if a higher-priority branch becomes valid"]
}
!!!

!!! quiz
{
"title": "Sequence Running Resume Behavior",
"question": "When a Sequence node has a child that returns Running, what should the Sequence do to avoid restarting from the first child next frame?",
"options": ["remember/store; resume/continue", "reset; restart", "ignore; skip", "increment; retry", "abort; reset"],
"answers": ["remember/store; resume/continue"]
}
!!!

!!! quiz
{
"title": "BT Anti-Pattern: Global State",
"question": "Which of the following is a common Behavior Tree design anti-pattern?",
"options": [
"Using Selectors to choose between mutually exclusive behaviors",
"Returning Running from actions that span multiple frames",
"Creating reusable subtrees for common behavior patterns",
"Having leaf nodes directly modify global game state without abstraction"
],
"answers": ["Having leaf nodes directly modify global game state without abstraction"]
}
!!!

!!! quiz
{
"title": "Decorator Capabilities",
"question": "True or False: Decorator nodes can invert results, repeat execution, add cooldowns, or limit execution time.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

!!! quiz
{
"title": "Running Action Without Aborts",
"question": "A guard is in a Running 'SearchArea' action. The player steps into view. In a standard tick-based BT without abort modes, what happens?",
"options": [
"The tree immediately switches to Combat",
"SearchArea continues until it completes or fails, then Combat is evaluated",
"Both SearchArea and Attack execute simultaneously",
"The tree returns Failure"
],
"answers": ["SearchArea continues until it completes or fails, then Combat is evaluated"]
}
!!!

!!! quiz
{
"title": "Subtree Composition Benefits",
"question": "Why is subtree composition preferred over copy-pasting node structures across multiple character types?",
"options": [
"It makes trees intentionally deeper to capture more nuance",
"It forces every character to share the exact same behavior with no variation",
"It centralizes maintenance, reduces duplication, and scales by reusing named behaviors",
"It removes the need for designers to understand tree structure"
],
"answers": ["It centralizes maintenance, reduces duplication, and scales by reusing named behaviors"]
}
!!!

!!! quiz
{
"title": "Least Effective Debugging Approach",
"question": "When debugging a misbehaving BT in production, which approach is least effective?",
"options": [
"Recording and replaying decision sequences to reproduce issues",
"Reading the tree in code and mentally simulating execution",
"Visualizing the tree with active node highlighting in real-time",
"Logging status returns at each node with timestamps"
],
"answers": ["Reading the tree in code and mentally simulating execution"]
}
!!!

!!! quiz
{
"title": "Selector Fallback Validity",
"question": "True or False: A Selector containing Actions that can fail is a valid pattern for implementing fallback behaviors.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

!!! quiz
{
"title": "God Node Anti-Pattern",
"question": "What problem does the 'god node' anti-pattern describe?",
"options": [
"A single node that contains too much logic, making multiple decisions internally",
"A node that always returns Success regardless of game state",
"A Selector with more than 5 children",
"Using the same node class for both Conditions and Actions"
],
"answers": ["A single node that contains too much logic, making multiple decisions internally"]
}
!!!
