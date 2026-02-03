# Introduction to Pandas

### Python Data Analysis Library

---

## What is Pandas?

- Open source Python library for **data manipulation and analysis**
- Created by **Wes McKinney** in 2008
- Name refers to "**Pan**el **Da**ta" and "Python Data Analysis"

---

## Why Use Pandas?

- 📊 Easy data loading (CSV, Excel, SQL, JSON)
- 🧹 Powerful data cleaning tools
- 🔍 Intuitive data exploration
- ⚡ Fast operations on large datasets
- 📈 Integration with visualization libraries

---

## Installation

```python
pip install pandas
```

```python
import pandas as pd
import numpy as np
```

---

# Pandas Series

---

## What is a Series?

A **one-dimensional** labeled array

Think of it as a **single column** in a spreadsheet

```
Index | Value
------+-------
  0   |  10
  1   |  20
  2   |  30
  3   |  40
```

---

## Creating an Empty Series

```python
import pandas as pd

series_empty = pd.Series()
print(series_empty)
```

Output:

```
Series([], dtype: float64)
```

---

## Creating Series from a List

```python
my_list = [1, 4, 5, 2, 7, 9, 8]

my_series = pd.Series(my_list)
print(my_series)
```

Output:

```
0    1
1    4
2    5
3    2
4    7
5    9
6    8
dtype: int64
```

---

## Series with Different Data Types

**Strings:**

```python
names = ["Luffy", "Zoro", "Sanji", "Nami"]
crew = pd.Series(names)
```

**Booleans:**

```python
flags = [True, False, False, True]
bool_series = pd.Series(flags)
```

---

## Accessing Elements

```python
my_series = pd.Series([1, 4, 5, 2, 7, 9, 8])

# Single element
print(my_series[1])  # Output: 4

# Multiple elements
print(my_series[[1, 3, 4]])
```

Output:

```
1    4
3    2
4    7
dtype: int64
```

---

# Custom Indexing

---

## Creating Custom Index

```python
my_list = [1, 4, 5, 2, 7, 9, 8]

my_series = pd.Series(
    data=my_list,
    index=["a", "b", "c", "d", "e", "f", "g"]
)
print(my_series)
```

Output:

```
a    1
b    4
c    5
d    2
e    7
f    9
g    8
dtype: int64
```

---

## Accessing with Custom Index

```python
# Access by label
print(my_series["c"])  # Output: 5

# Access multiple
print(my_series[["a", "e", "g"]])
```

Output:

```
a    1
e    7
g    8
dtype: int64
```

---

## Creating Series from Dictionary

```python
sushi_data = {
    "Salmon": "Orange",
    "Tuna": "Red",
    "Eel": "Brown"
}

sushi_series = pd.Series(sushi_data)
print(sushi_series)
```

Output:

```
Salmon    Orange
Tuna         Red
Eel        Brown
dtype: object
```

---

## Dictionary Keys = Index

```python
# Access by key (index)
print(sushi_series["Tuna"])  # Output: Red

# View all indices
print(sushi_series.index)
# Index(['Salmon', 'Tuna', 'Eel'], dtype='object')
```

---

# Series Attributes

---

## Common Attributes

```python
s = pd.Series([100, 200, 300, 400, 500, 600],
              index=['a', 'b', 'c', 'd', 'e', 'f'])
```

| Attribute  | Output            | Description        |
| ---------- | ----------------- | ------------------ |
| `s.values` | `[100 200 ...]`   | NumPy array        |
| `s.index`  | `['a', 'b', ...]` | Index labels       |
| `s.dtype`  | `int64`           | Data type          |
| `s.size`   | `6`               | Number of elements |
| `s.shape`  | `(6,)`            | Dimensions         |

---

# Series Methods

---

## Statistical Methods

```python
s = pd.Series([100, 200, 300, 400, 500, 600])

print(s.sum())   # 2100
print(s.mean())  # 350.0
print(s.min())   # 100
print(s.max())   # 600
print(s.std())   # 187.08
```

---

## Sorting Methods

```python
s = pd.Series([1, 4, 4, 1, 7],
              index=["b", "t", "n", "e", "y"])

# Sort by index
s_sorted = s.sort_index()

# Sort by values
s_sorted = s.sort_values()
```

---

## Value Counts

```python
s = pd.Series([1, 4, 4, 1, 7, 1, 8])

print(s.value_counts())
```

Output:

```
1    3
4    2
7    1
8    1
dtype: int64
```

---

# NumPy Integration

---

## NumPy + Pandas

```python
import numpy as np

# Create array from 5 to 100, step 5
arr = np.arange(5, 101, 5)

# Convert to Series
s = pd.Series(arr)

print(s.sum())   # Sum
print(s.mean())  # Mean
print(s.std())   # Standard deviation
```

---

## Filtering Data

```python
arr = np.arange(5, 101, 5)
s = pd.Series(arr)

# Elements divisible by both 3 and 5
filtered = s[(s % 3 == 0) & (s % 5 == 0)]
print(filtered)
```

Output:

```
2     15
5     30
8     45
11    60
14    75
17    90
dtype: int64
```

---

# Practical Example

---

## Student Scores Analysis

```python
# Load student data
student_ids = [101, 102, 103, 104, 105]
scores = [88, 78, 64, 92, 57]

grades = pd.Series(scores, index=student_ids)
print(grades)
```

Output:

```
101    88
102    78
103    64
104    92
105    57
dtype: int64
```

---

## Finding Top Scorer

```python
# Top scorer
top_score = grades.max()
top_student = grades.idxmax()

print(f"Top scorer: Student {top_student}")
print(f"Score: {top_score}")
```

Output:

```
Top scorer: Student 104
Score: 92
```

---

## Students Below Average

```python
avg = grades.mean()
below_avg = grades[grades < avg]

print(f"Class average: {avg:.1f}")
print("Below average:")
print(below_avg)
```

Output:

```
Class average: 75.8
Below average:
103    64
105    57
dtype: int64
```

---

## Concatenating Series

```python
series1 = pd.Series([10, 20, 30], index=['a', 'b', 'c'])
series2 = pd.Series([40, 50], index=['d', 'e'])
combined = pd.concat([series1, series2])
print(combined)
```

---

# Summary

---

## Key Takeaways

✅ **Series** = 1D labeled array

✅ Create from **lists**, **arrays**, or **dictionaries**

✅ **Custom indexing** for meaningful labels

✅ Built-in **statistical methods**

✅ Seamless **NumPy integration**

---

## What's Next?

📊 **DataFrames** - 2D data structures

🧹 **Data Cleaning** - Handle missing values

📈 **Data Visualization** - matplotlib & seaborn

🤖 **Data Analysis** - Real-world projects

---

# Questions?

### 🐼 Happy Data Analysis!

---

## Resources

- [Pandas Documentation](https://pandas.pydata.org/docs/)
- [NumPy Documentation](https://numpy.org/devdocs/user/)
- [10 Minutes to Pandas](https://pandas.pydata.org/docs/user_guide/10min.html)
