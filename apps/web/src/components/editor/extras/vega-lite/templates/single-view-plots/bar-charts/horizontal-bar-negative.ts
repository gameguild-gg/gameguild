import { BarChart3 } from "lucide-react"

export default {
  id: "horizontal-bar-negative",
  type: "bar",
  title: "Horizontal Bar Chart with Negative Values and Labels",
  description: "Horizontal bars with negative values and text labels",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Horizontal bar chart with negative values and labels.",
    "data": {
      "values": [
        {"category": "A", "value": 28},
        {"category": "B", "value": -15},
        {"category": "C", "value": 43},
        {"category": "D", "value": -25}
      ]
    },
    "mark": "bar",
    "encoding": {
      "y": {"field": "category", "type": "nominal"},
      "x": {"field": "value", "type": "quantitative"},
      "color": {
        "condition": {"test": "datum.value > 0", "value": "steelblue"},
        "value": "coral"
      }
    }
  }
}
