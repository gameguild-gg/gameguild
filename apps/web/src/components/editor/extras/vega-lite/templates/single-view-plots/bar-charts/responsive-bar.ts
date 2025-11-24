import { BarChart3 } from "lucide-react"

export default {
  id: "responsive-bar",
  type: "bar",
  title: "Responsive Bar Chart",
  description: "Bar chart that adapts to container size",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "A responsive bar chart.",
    "data": {
      "values": [
        {"category": "A", "value": 28},
        {"category": "B", "value": 55},
        {"category": "C", "value": 43},
        {"category": "D", "value": 91},
        {"category": "E", "value": 81},
        {"category": "F", "value": 53},
        {"category": "G", "value": 19}
      ]
    },
    "mark": "bar",
    "encoding": {
      "x": {"field": "category", "type": "nominal"},
      "y": {"field": "value", "type": "quantitative"},
      "tooltip": [{"field": "category"}, {"field": "value"}]
    }
  }
}
