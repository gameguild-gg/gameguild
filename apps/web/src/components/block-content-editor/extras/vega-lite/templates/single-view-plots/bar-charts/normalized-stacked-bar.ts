import { BarChart3 } from "lucide-react"

export default {
  id: "normalized-stacked-bar",
  type: "bar",
  title: "Normalized (Percentage) Stacked Bar Chart",
  description: "Stacked bars normalized to 100%",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Normalized stacked bar chart.",
    "data": {
      "values": [
        {"category": "A", "group": "x", "value": 28},
        {"category": "A", "group": "y", "value": 55},
        {"category": "B", "group": "x", "value": 43},
        {"category": "B", "group": "y", "value": 61},
        {"category": "C", "group": "x", "value": 81},
        {"category": "C", "group": "y", "value": 73}
      ]
    },
    "mark": "bar",
    "encoding": {
      "x": {"field": "category", "type": "nominal"},
      "y": {"aggregate": "sum", "field": "value", "type": "quantitative", "stack": "normalize"},
      "color": {"field": "group", "type": "nominal"}
    }
  }
}
