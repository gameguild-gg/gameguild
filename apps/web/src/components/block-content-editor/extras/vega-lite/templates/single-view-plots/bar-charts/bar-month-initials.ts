import { BarChart3 } from "lucide-react"

export default {
  id: "bar-month-initials",
  type: "bar",
  title: "Bar Chart showing Initials of Month Names",
  description: "Bar chart with abbreviated month labels",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Bar chart with month initials.",
    "data": {
      "values": [
        {"month": "January", "initial": "J", "value": 28},
        {"month": "February", "initial": "F", "value": 55},
        {"month": "March", "initial": "M", "value": 43},
        {"month": "April", "initial": "A", "value": 91},
        {"month": "May", "initial": "M", "value": 81},
        {"month": "June", "initial": "J", "value": 53}
      ]
    },
    "mark": "bar",
    "encoding": {
      "x": {"field": "initial", "type": "nominal"},
      "y": {"field": "value", "type": "quantitative"}
    }
  }
}
