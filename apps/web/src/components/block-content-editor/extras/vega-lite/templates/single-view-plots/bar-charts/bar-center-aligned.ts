import { BarChart3 } from "lucide-react"

export default {
  id: "bar-center-aligned",
  type: "bar",
  title: "Bar Chart with bars center-aligned with time unit ticks",
  description: "Bars center-aligned with temporal axis ticks",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Bar chart center-aligned with time ticks.",
    "data": {
      "values": [
        {"date": "2023-01-01", "value": 28},
        {"date": "2023-02-01", "value": 55},
        {"date": "2023-03-01", "value": 43},
        {"date": "2023-04-01", "value": 91}
      ]
    },
    "mark": {"type": "bar", "binSpacing": 0.5},
    "encoding": {
      "x": {"field": "date", "type": "temporal"},
      "y": {"field": "value", "type": "quantitative"}
    }
  }
}
