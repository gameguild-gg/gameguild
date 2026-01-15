# Week 03 Quiz - Utility AI

---

!!! quiz
{
"title": "Question 1",
"question": "In a utility-based AI system using most recommended scoring methods, if an action has three considerations scoring 0.8, 0.0, and 0.9 respectively, what is the final utility score for that action?",
"options": [
"0.57 (the average)",
"0.0 (the product)",
"0.9 (the highest)",
"0.8 (the first non-zero)"
],
"answers": ["0.0 (the product)"]
}
!!!

---

!!! quiz
{
"title": "Question 2",
"question": "True or False: In the Infinite Axis Utility System (IAUS), adding a new consideration to an action requires rebalancing all other consideration weights in the system.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 3",
"question": "Which response curve would you use to create a behavior that has a sharp threshold—almost no response until input reaches ~0.5, then rapidly increases to maximum?",
"options": [
"Linear: y = x",
"Quadratic: y = x²",
"Logistic: y = 1 / (1 + e^(-k(x-0.5)))",
"Inverse quadratic: y = 1 - (1-x)²"
],
"answers": ["Logistic: y = 1 / (1 + e^(-k(x-0.5)))"]
}
!!!

---

!!! quiz
    {
      "title": "Question 4",
      "question": "Response curves transform raw input values into __________ scores, typically in the range [0, 1].",
      "options": [
        "normalized",
        "utility",
        "weighted",
        "absolute"
      ],
      "answers": ["normalized", "utility"]
    }
!!!

---

!!! quiz
{
"title": "Question 5",
"question": "In The Sims, when a character's Hunger need is very low, the utility score for \"Eat\" becomes very high. This is an example of:",
"options": [
"Dual-utility reasoning",
"Need-based utility with inverted input",
"Bucket selection",
"Hierarchical task networks"
],
"answers": ["Need-based utility with inverted input"]
}
!!!

---

!!! quiz
{
"title": "Question 6",
"question": "What is a key disadvantage of utility-based AI compared to behavior trees?",
"options": [
"Cannot handle more than 5 actions",
"Harder to trace exactly why a specific decision was made",
"Requires more memory than behavior trees",
"Cannot respond to changes in game state"
],
"answers": ["Harder to trace exactly why a specific decision was made"]
}
!!!

---

!!! quiz
{
"title": "Question 7",
"question": "What is the primary advantage of utility-based AI over behavior trees for character decision-making?",
"options": [
"Faster runtime performance",
"Easier to debug with visual tools",
"Smoother transitions between many competing behaviors based on continuous scores",
"Simpler implementation with fewer lines of code"
],
"answers": ["Smoother transitions between many competing behaviors based on continuous scores"]
}
!!!

---

!!! quiz
{
"title": "Question 8",
"question": "Which of the following is an example of a consideration in a utility AI system?",
"options": [
"A response curve equation",
"The final selected action",
"Distance to the nearest enemy",
"The behavior tree root node"
],
"answers": ["Distance to the nearest enemy"]
}
!!!

---

!!! quiz
{
"title": "Question 9",
"question": "An AI character should prefer targets that are closer. Given distance normalized to [0, 1] where 0 = closest and 1 = farthest, which curve makes close targets score HIGH?",
"options": [
"y = x (linear)",
"y = 1 - x (inverted linear)",
"y = x² (quadratic)",
"y = e^x (exponential growth)"
],
"answers": ["y = 1 - x (inverted linear)"]
}
!!!

---

!!! quiz
{
"title": "Question 10",
"question": "True or False: Utility AI systems are best suited for decisions with only 2-3 possible actions, while behavior trees excel when there are dozens of potential behaviors.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!
