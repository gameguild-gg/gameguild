import { TrendingUp } from "lucide-react"

export default {
  id: "error-bars",
  type: "error-bars",
  title: "Error Bars Chart",
  description: "Chart with error bars showing uncertainty ranges",
  icon: TrendingUp,
  category: "composite-marks",
  subcategory: "error-bars-bands",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "A bar chart with error bars.",
    "data": {
      "values": [
        {"category": "A", "mean": 28, "error": 5},
        {"category": "B", "mean": 55, "error": 8},
        {"category": "C", "mean": 43, "error": 6},
        {"category": "D", "mean": 91, "error": 10},
        {"category": "E", "mean": 81, "error": 7}
      ]
    },
    "layer": [
      {
        "mark": "bar",
        "encoding": {
          "x": {"field": "category", "type": "nominal"},
          "y": {"field": "mean", "type": "quantitative", "scale": {"zero": true}}
        }
      },
      {
        "mark": {"type": "errorbar", "ticks": true},
        "encoding": {
          "x": {"field": "category", "type": "nominal"},
          "y": {
            "field": "mean",
            "type": "quantitative"
          },
          "yError": {"field": "error"}
        }
      }
    ]
  }
}
