import { PieChart } from "lucide-react"

export default {
  id: "pie-chart",
  type: "arc",
  title: "Pie Chart",
  description: "Circular pie chart showing proportions",
  icon: PieChart,
  category: "single-view-plots",
  subcategory: "circular-plots",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "A simple pie chart showing category proportions.",
    "data": {
      "values": [
        {"category": "A", "value": 28},
        {"category": "B", "value": 55},
        {"category": "C", "value": 43},
        {"category": "D", "value": 91},
        {"category": "E", "value": 81}
      ]
    },
    "mark": "arc",
    "encoding": {
      "theta": {"field": "value", "type": "quantitative"},
      "color": {"field": "category", "type": "nominal"}
    },
    "view": {"stroke": null}
  }
}
