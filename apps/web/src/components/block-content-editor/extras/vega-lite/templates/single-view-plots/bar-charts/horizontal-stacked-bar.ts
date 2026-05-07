import { BarChart3 } from "lucide-react"

export default {
  id: "horizontal-stacked-bar",
  type: "bar",
  title: "Horizontal Stacked Bar Chart",
  description: "Stacked bars displayed horizontally",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Horizontal stacked bar chart.",
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
      "y": {"field": "category", "type": "nominal"},
      "x": {"aggregate": "sum", "field": "value", "type": "quantitative"},
      "color": {"field": "group", "type": "nominal"}
    }
  }
}
