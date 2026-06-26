import { Layers } from "lucide-react"

export default {
  id: "layered-bar",
  type: "bar",
  title: "Layered Bar Chart",
  description: "Multiple layers of bars with different encodings",
  icon: Layers,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Layered bar chart.",
    "data": {
      "values": [
        {"category": "A", "value": 28, "baseline": 20},
        {"category": "B", "value": 55, "baseline": 30},
        {"category": "C", "value": 43, "baseline": 25},
        {"category": "D", "value": 91, "baseline": 60}
      ]
    },
    "layer": [
      {
        "mark": {"type": "bar", "opacity": 0.3},
        "encoding": {
          "x": {"field": "category", "type": "nominal"},
          "y": {"field": "baseline", "type": "quantitative"}
        }
      },
      {
        "mark": "bar",
        "encoding": {
          "x": {"field": "category", "type": "nominal"},
          "y": {"field": "value", "type": "quantitative"}
        }
      }
    ]
  }
}
