import { Type } from "lucide-react"

export default {
  id: "bar-with-labels",
  type: "bar",
  title: "Bar Chart with Labels",
  description: "Bar chart with value labels on each bar",
  icon: Type,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Bar chart with labels.",
    "data": {
      "values": [
        {"category": "A", "value": 28},
        {"category": "B", "value": 55},
        {"category": "C", "value": 43},
        {"category": "D", "value": 91}
      ]
    },
    "layer": [
      {
        "mark": "bar",
        "encoding": {
          "x": {"field": "category", "type": "nominal"},
          "y": {"field": "value", "type": "quantitative"}
        }
      },
      {
        "mark": {"type": "text", "align": "center", "baseline": "bottom", "dy": -2},
        "encoding": {
          "x": {"field": "category", "type": "nominal"},
          "y": {"field": "value", "type": "quantitative"},
          "text": {"field": "value", "type": "quantitative"}
        }
      }
    ]
  }
}
