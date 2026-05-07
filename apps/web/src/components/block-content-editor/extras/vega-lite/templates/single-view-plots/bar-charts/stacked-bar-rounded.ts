import { BarChart3 } from "lucide-react"

export default {
  id: "stacked-bar-rounded",
  type: "bar",
  title: "Stacked Bar Chart with Rounded Corners",
  description: "Stacked bars with rounded corner styling",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Stacked bar chart with rounded corners.",
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
    "mark": {"type": "bar", "cornerRadius": 2},
    "encoding": {
      "x": {"field": "category", "type": "nominal"},
      "y": {"aggregate": "sum", "field": "value", "type": "quantitative"},
      "color": {"field": "group", "type": "nominal"}
    }
  }
}
