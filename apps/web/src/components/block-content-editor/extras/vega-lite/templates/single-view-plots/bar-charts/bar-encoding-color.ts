import { BarChart3 } from "lucide-react"

export default {
  id: "bar-encoding-color",
  type: "bar",
  title: "Bar Chart Encoding Color Names in the Data",
  description: "Bars colored by data-driven color field",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Bar chart with data-driven colors.",
    "data": {
      "values": [
        {"category": "A", "value": 28, "color": "steelblue"},
        {"category": "B", "value": 55, "color": "orange"},
        {"category": "C", "value": 43, "color": "green"},
        {"category": "D", "value": 91, "color": "red"}
      ]
    },
    "mark": "bar",
    "encoding": {
      "x": {"field": "category", "type": "nominal"},
      "y": {"field": "value", "type": "quantitative"},
      "color": {"field": "color", "type": "nominal", "scale": null}
    }
  }
}
