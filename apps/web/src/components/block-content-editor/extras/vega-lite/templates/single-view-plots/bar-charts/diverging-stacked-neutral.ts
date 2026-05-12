import { BarChart3 } from "lucide-react"

export default {
  id: "diverging-stacked-neutral",
  type: "bar",
  title: "Diverging Stacked Bar Chart (with Neutral Parts)",
  description: "Diverging bars with neutral center sections",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Diverging stacked bar with neutral parts.",
    "data": {
      "values": [
        {"category": "A", "negative": -30, "neutral": 40, "positive": 30},
        {"category": "B", "negative": -25, "neutral": 50, "positive": 25},
        {"category": "C", "negative": -35, "neutral": 30, "positive": 35}
      ]
    },
    "mark": "bar",
    "encoding": {
      "x": {"field": "category", "type": "nominal"},
      "y": {"field": "value", "type": "quantitative"},
      "color": {"field": "type", "type": "nominal"}
    }
  }
}
