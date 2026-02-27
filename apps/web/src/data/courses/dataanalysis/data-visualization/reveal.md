# Data Visualization with Plotly Express

### Interactive Charts in Python

---

## Why Data Visualization?

- Humans process visual information **60,000x faster** than text
- Reveals **patterns, trends, and outliers** hidden in raw data
- Communicates findings to **non-technical stakeholders**
- Essential step in **Exploratory Data Analysis (EDA)**

---

## Why Plotly Express?

- **One-liner** chart creation with sensible defaults
- **Interactive** — zoom, pan, hover tooltips out of the box
- Built on top of **Plotly.js** (D3 + WebGL)
- Consistent API across **30+ chart types**
- Built-in **sample datasets** for learning

---

## Installation & Import

```python
pip install plotly
```

```python
import plotly.express as px
```

Plotly Express ships with several datasets:

```python
df = px.data.gapminder()
df = px.data.iris()
df = px.data.tips()
df = px.data.stocks()
```

---

# Scatter Plots

---

## Basic Scatter Plot

```python
import plotly.express as px

df = px.data.iris()
fig = px.scatter(df, x="sepal_width", y="sepal_length")
fig.show()
```

- Each point = one observation
- Reveals **correlation** between two numeric variables

---

## Adding Color & Size

```python
fig = px.scatter(
    df,
    x="sepal_width",
    y="sepal_length",
    color="species",
    size="petal_length",
    hover_data=["petal_width"],
)
fig.show()
```

- `color` → categorical or continuous encoding
- `size` → bubble chart (third dimension)
- `hover_data` → extra info on mouseover

---

# Line Charts

---

## Time-Series Line Chart

```python
df = px.data.gapminder().query("country == 'Canada'")
fig = px.line(df, x="year", y="lifeExp",
              title="Life Expectancy in Canada")
fig.show()
```

Best for **continuous data over time**.

---

## Multi-Line Comparison

```python
df = px.data.gapminder().query(
    "country in ['Canada', 'Brazil', 'Japan']"
)
fig = px.line(df, x="year", y="gdpPercap",
              color="country",
              title="GDP per Capita Over Time")
fig.show()
```

Use `color` to split into **one line per group**.

---

# Bar Charts

---

## Vertical Bar Chart

```python
df = px.data.tips()
fig = px.bar(df, x="day", y="total_bill",
             color="sex", barmode="group",
             title="Total Bill by Day & Gender")
fig.show()
```

- `barmode="group"` → side-by-side
- `barmode="stack"` → stacked
- `barmode="relative"` → stacked with negatives below

---

## Horizontal Bar Chart

```python
df = px.data.gapminder().query(
    "year == 2007 and continent == 'Americas'"
)
fig = px.bar(df, y="country", x="gdpPercap",
             orientation="h",
             title="GDP per Capita in the Americas (2007)")
fig.update_layout(yaxis={"categoryorder": "total ascending"})
fig.show()
```

---

# Histograms & Distributions

---

## Histogram

```python
df = px.data.tips()
fig = px.histogram(df, x="total_bill", nbins=30,
                   color="sex", barmode="overlay",
                   opacity=0.7,
                   title="Distribution of Total Bill")
fig.show()
```

- `nbins` controls bin count
- `barmode="overlay"` with `opacity` for comparison

---

## Box Plot

```python
fig = px.box(df, x="day", y="total_bill",
             color="smoker",
             title="Bill Distribution by Day")
fig.show()
```

Shows **median, quartiles, and outliers**.

---

## Violin Plot

```python
fig = px.violin(df, x="day", y="total_bill",
                color="sex", box=True,
                title="Bill Distribution (Violin)")
fig.show()
```

Combines box plot with **kernel density estimation**.

---

# Categorical & Part-to-Whole

---

## Pie Chart

```python
df = px.data.gapminder().query("year == 2007 and continent == 'Europe'")
top10 = df.nlargest(10, "pop")
fig = px.pie(top10, values="pop", names="country",
             title="Top 10 European Countries by Population (2007)")
fig.show()
```

---

## Sunburst Chart

```python
df = px.data.gapminder().query("year == 2007")
fig = px.sunburst(df, path=["continent", "country"],
                  values="pop",
                  title="World Population Hierarchy (2007)")
fig.show()
```

Hierarchical part-to-whole — click to drill down.

---

## Treemap

```python
df = px.data.gapminder().query("year == 2007")
fig = px.treemap(df, path=["continent", "country"],
                 values="pop", color="lifeExp",
                 color_continuous_scale="RdYlGn",
                 title="Population & Life Expectancy (2007)")
fig.show()
```

---

# Maps & Geospatial

---

## Choropleth Map

```python
df = px.data.gapminder().query("year == 2007")
fig = px.choropleth(df, locations="iso_alpha",
                    color="gdpPercap",
                    hover_name="country",
                    color_continuous_scale="Viridis",
                    title="GDP per Capita (2007)")
fig.show()
```

---

## Animated Scatter on Map

```python
df = px.data.gapminder()
fig = px.scatter_geo(df, locations="iso_alpha",
                     size="pop", color="continent",
                     hover_name="country",
                     animation_frame="year",
                     projection="natural earth",
                     title="World Population Over Time")
fig.show()
```

Press **Play** to animate through years.

---

# Animation

---

## Animated Scatter Plot

```python
df = px.data.gapminder()
fig = px.scatter(df, x="gdpPercap", y="lifeExp",
                 size="pop", color="continent",
                 hover_name="country",
                 animation_frame="year",
                 animation_group="country",
                 log_x=True, size_max=60,
                 range_x=[100, 100000],
                 range_y=[25, 90],
                 title="Gapminder: GDP vs Life Expectancy")
fig.show()
```

The famous **Hans Rosling** bubble chart!

---

# Customization

---

## Updating Layout

```python
fig.update_layout(
    template="plotly_dark",
    title_font_size=24,
    xaxis_title="GDP per Capita (log scale)",
    yaxis_title="Life Expectancy",
    legend_title="Continent",
)
```

Built-in templates: `plotly`, `plotly_white`, `plotly_dark`, `ggplot2`, `seaborn`, `simple_white`

---

## Faceting (Small Multiples)

```python
df = px.data.tips()
fig = px.scatter(df, x="total_bill", y="tip",
                 color="smoker",
                 facet_col="sex", facet_row="time",
                 title="Tips: Faceted by Sex & Time")
fig.show()
```

- `facet_col` → columns of subplots
- `facet_row` → rows of subplots

---

## Trendlines

```python
fig = px.scatter(df, x="total_bill", y="tip",
                 trendline="ols",
                 title="Tip vs Total Bill with OLS Trendline")
fig.show()
```

- `"ols"` → Ordinary Least Squares (linear)
- `"lowess"` → Locally Weighted Scatterplot Smoothing

---

# Summary

---

## Plotly Express Cheat Sheet

| Chart Type  | Function         | Best For                        |
| ----------- | ---------------- | ------------------------------- |
| Scatter     | `px.scatter`     | Correlation between 2 variables |
| Line        | `px.line`        | Trends over time                |
| Bar         | `px.bar`         | Comparing categories            |
| Histogram   | `px.histogram`   | Distribution of one variable    |
| Box         | `px.box`         | Distribution + outliers         |
| Violin      | `px.violin`      | Distribution shape              |
| Pie         | `px.pie`         | Part-to-whole (few categories)  |
| Sunburst    | `px.sunburst`    | Hierarchical part-to-whole      |
| Treemap     | `px.treemap`     | Hierarchical with color         |
| Choropleth  | `px.choropleth`  | Geographic data                 |
| Scatter Geo | `px.scatter_geo` | Points on a map                 |

---

## Key Takeaways

1. **One function call** creates a complete interactive chart
2. Use `color`, `size`, `facet_col`, `facet_row` to encode **extra dimensions**
3. `animation_frame` brings data to life over **time**
4. `update_layout()` and `update_traces()` for **fine-tuning**
5. Always pick the **right chart type** for your data and question
