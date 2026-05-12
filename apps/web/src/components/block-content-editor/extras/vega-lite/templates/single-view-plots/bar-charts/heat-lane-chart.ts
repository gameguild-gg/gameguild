import { BarChart3 } from "lucide-react"

export default {
  id: "heat-lane-chart",
  type: "bar",
  title: "Heat Lane Chart",
  description: "Time-based heat lanes showing intensity patterns",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Heat lane chart.",
    "data": {
      "values": [
        {"day": "Monday", "hour": 8, "value": 28},
        {"day": "Monday", "hour": 12, "value": 45},
        {"day": "Monday", "hour": 18, "value": 35},
        {"day": "Tuesday", "hour": 8, "value": 38},
        {"day": "Tuesday", "hour": 12, "value": 55},
        {"day": "Tuesday", "hour": 18, "value": 42},
        {"day": "Wednesday", "hour": 8, "value": 32},
        {"day": "Wednesday", "hour": 12, "value": 50},
        {"day": "Wednesday", "hour": 18, "value": 39}
      ]
    },
    "mark": "rect",
    "encoding": {
      "y": {"field": "day", "type": "nominal"},
      "x": {"field": "hour", "type": "ordinal"},
      "color": {"field": "value", "type": "quantitative", "scale": {"scheme": "viridis"}}
    }
  }
}
