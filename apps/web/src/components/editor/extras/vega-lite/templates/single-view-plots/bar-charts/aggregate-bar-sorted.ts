import { BarChart3 } from "lucide-react"

export default {
  id: "aggregate-bar-sorted",
  type: "bar",
  title: "Aggregate Bar Chart (Sorted)",
  description: "Aggregated bar chart sorted by value",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Sorted aggregated bar chart.",
    "data": {
      "values": [
        {"category": "A", "value": 28},
        {"category": "B", "value": 55},
        {"category": "C", "value": 43},
        {"category": "D", "value": 91},
        {"category": "E", "value": 81},
        {"category": "F", "value": 53}
      ]
    },
    "mark": "bar",
    "encoding": {
      "x": {"field": "category", "type": "nominal", "sort": "-y"},
      "y": {"field": "value", "type": "quantitative"}
    }
  }
}
