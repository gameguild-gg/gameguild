import { BarChart3 } from "lucide-react"

export default {
  id: "aggregate-bar",
  type: "bar",
  title: "Aggregate Bar Chart",
  description: "Bar chart aggregating data values",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Aggregated bar chart.",
    "data": {
      "values": [
        {"category": "A", "group": "x", "value": 28},
        {"category": "A", "group": "y", "value": 12},
        {"category": "B", "group": "x", "value": 55},
        {"category": "B", "group": "y", "value": 20},
        {"category": "C", "group": "x", "value": 43},
        {"category": "C", "group": "y", "value": 18}
      ]
    },
    "mark": "bar",
    "encoding": {
      "x": {"field": "category", "type": "nominal"},
      "y": {"aggregate": "sum", "field": "value", "type": "quantitative"}
    }
  }
}
