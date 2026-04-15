# Introduction to NumPy

### Numerical Python Library

---

## What is NumPy?

- **Num**erical **Py**thon
- Foundation for scientific computing in Python
- Provides **n-dimensional arrays** (ndarray)
- Fast mathematical operations on arrays

---

## Why Use NumPy?

- ⚡ **Fast** - Written in C, optimized for performance
- 🧮 **Efficient** - Less memory than Python lists
- 📐 **Powerful** - Broadcasting and vectorized operations
- 🔗 **Foundation** - Used by Pandas, SciPy, TensorFlow

---

## Installation & Import

```python
pip install numpy
```

```python
import numpy as np
```

---

# Creating Arrays

---

## From Python Lists

```python
import numpy as np

# 1D Array
arr1d = np.array([1, 2, 3, 4, 5])
print(arr1d)
# [1 2 3 4 5]

# 2D Array
arr2d = np.array([[1, 2, 3],
                  [4, 5, 6]])
print(arr2d)
```

---

## Using Built-in Functions

```python
# Array of zeros
zeros = np.zeros((3, 4))

# Array of ones
ones = np.ones((2, 3))

# Identity matrix
identity = np.eye(3)

# Empty array (uninitialized)
empty = np.empty((2, 2))
```

---

## Range Functions

```python
# arange: start, stop, step
arr = np.arange(0, 10, 2)
# [0 2 4 6 8]

# linspace: start, stop, num_points
arr = np.linspace(0, 1, 5)
# [0.   0.25 0.5  0.75 1.  ]
```

---

## Random Arrays

```python
# Random floats [0, 1)
rand = np.random.rand(3, 3)

# Random integers
randint = np.random.randint(1, 100, size=(3, 3))

# Normal distribution
normal = np.random.normal(0, 1, (3, 3))
```

---

# Array Attributes

---

## Key Attributes

```python
arr = np.array([[1, 2, 3],
                [4, 5, 6]])

print(arr.shape)   # (2, 3) - dimensions
print(arr.ndim)    # 2 - number of dimensions
print(arr.size)    # 6 - total elements
print(arr.dtype)   # int64 - data type
```

---

## Data Types

| dtype                | Description     |
| -------------------- | --------------- |
| `int32`, `int64`     | Integers        |
| `float32`, `float64` | Floating point  |
| `bool`               | Boolean         |
| `complex64`          | Complex numbers |
| `object`             | Python objects  |

```python
arr = np.array([1, 2, 3], dtype=np.float64)
```

---

# Indexing & Slicing

---

## 1D Array Indexing

```python
arr = np.array([10, 20, 30, 40, 50])

print(arr[0])     # 10 (first element)
print(arr[-1])    # 50 (last element)
print(arr[1:4])   # [20 30 40] (slice)
print(arr[::2])   # [10 30 50] (every 2nd)
```

---

## 2D Array Indexing

```python
arr = np.array([[1, 2, 3],
                [4, 5, 6],
                [7, 8, 9]])

print(arr[0, 0])    # 1 (row 0, col 0)
print(arr[1, :])    # [4 5 6] (row 1, all cols)
print(arr[:, 2])    # [3 6 9] (all rows, col 2)
print(arr[0:2, 1:3]) # [[2 3] [5 6]]
```

---

## Boolean Indexing

```python
arr = np.array([1, 2, 3, 4, 5, 6, 7, 8, 9, 10])

# Elements greater than 5
print(arr[arr > 5])
# [6 7 8 9 10]

# Elements divisible by 3
print(arr[arr % 3 == 0])
# [3 6 9]
```

---

## Combined Conditions

```python
arr = np.arange(5, 101, 5)

# Divisible by both 3 AND 5
result = arr[(arr % 3 == 0) & (arr % 5 == 0)]
# [15 30 45 60 75 90]

# Greater than 50 OR less than 20
result = arr[(arr > 50) | (arr < 20)]
```

---

# Array Operations

---

## Element-wise Operations

```python
a = np.array([1, 2, 3])
b = np.array([4, 5, 6])

print(a + b)   # [5 7 9]
print(a - b)   # [-3 -3 -3]
print(a * b)   # [4 10 18]
print(a / b)   # [0.25 0.4 0.5]
print(a ** 2)  # [1 4 9]
```

---

## Mathematical Functions

```python
arr = np.array([1, 4, 9, 16, 25])

print(np.sqrt(arr))   # [1. 2. 3. 4. 5.]
print(np.exp(arr))    # exponential
print(np.log(arr))    # natural log
print(np.sin(arr))    # sine
print(np.abs(arr))    # absolute value
```

---

## Aggregate Functions

```python
arr = np.array([1, 2, 3, 4, 5])

print(np.sum(arr))    # 15
print(np.mean(arr))   # 3.0
print(np.std(arr))    # 1.414...
print(np.min(arr))    # 1
print(np.max(arr))    # 5
print(np.argmax(arr)) # 4 (index of max)
```

---

# Reshaping Arrays

---

## Reshape

```python
arr = np.arange(1, 13)
# [1 2 3 4 5 6 7 8 9 10 11 12]

reshaped = arr.reshape(3, 4)
# [[ 1  2  3  4]
#  [ 5  6  7  8]
#  [ 9 10 11 12]]

reshaped = arr.reshape(2, 2, 3)  # 3D
```

---

## Flatten & Ravel

```python
arr2d = np.array([[1, 2, 3],
                  [4, 5, 6]])

# Flatten (returns copy)
flat = arr2d.flatten()
# [1 2 3 4 5 6]

# Ravel (returns view)
raveled = arr2d.ravel()
```

---

## Transpose

```python
arr = np.array([[1, 2, 3],
                [4, 5, 6]])

transposed = arr.T
# [[1 4]
#  [2 5]
#  [3 6]]
```

---

# Matrix Operations

---

## Creating Matrices

```python
# 5x5 matrix of integers 1-25
matrix = np.arange(1, 26).reshape(5, 5)

# Replace diagonal with -1
np.fill_diagonal(matrix, -1)
```

---

## Matrix Multiplication

```python
a = np.array([[1, 2], [3, 4]])
b = np.array([[5, 6], [7, 8]])

# Element-wise
print(a * b)

# Matrix multiplication
print(np.dot(a, b))
# or
print(a @ b)
```

---

# Practical Example

---

## Sales Data Analysis

```python
# Monthly sales for 5 products over 6 months
sales = np.array([
    [1200, 1500, 1700, 1600, 1800, 1900],  # Product 1
    [1000, 1100, 1050, 1300, 1250, 1400],  # Product 2
    [2000, 1950, 2100, 2200, 2300, 2250],  # Product 3
    [900, 950, 980, 1000, 1100, 1200],     # Product 4
    [1700, 1800, 1850, 1900, 1950, 2000]   # Product 5
])
```

---

## Finding Best Product

```python
# Total sales per product
total_sales = np.sum(sales, axis=1)
print(total_sales)
# [9700 7100 12800 6130 11200]

# Best performing product
best_product = np.argmax(total_sales) + 1
print(f"Best Product: {best_product}")
# Best Product: 3
```

---

## Filtering Data

```python
months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"]
product_3 = sales[2]  # [2000 1950 2100 2200 2300 2250]

# Months where Product 3 > 2100
mask = product_3 > 2100
high_months = np.array(months)[mask]
print(high_months)
# ['Apr' 'May' 'Jun']
```

---

# Summary

---

## Key Takeaways

✅ **ndarray** - Fast, efficient n-dimensional arrays

✅ **Vectorization** - No loops needed for math

✅ **Broadcasting** - Operations on different shapes

✅ **Indexing** - Powerful slicing and boolean masks

✅ **Foundation** - Essential for Pandas & ML

---

## What's Next?

📊 **Pandas** - DataFrames for tabular data

📈 **Matplotlib** - Data visualization

🤖 **Scikit-learn** - Machine learning

🧠 **TensorFlow/PyTorch** - Deep learning

---

# Questions?

### 🔢 Happy Computing!

---

## Resources

- [NumPy Documentation](https://numpy.org/devdocs/user/)
- [NumPy Quickstart](https://numpy.org/doc/stable/user/quickstart.html)
- [NumPy for MATLAB Users](https://numpy.org/doc/stable/user/numpy-for-matlab-users.html)
