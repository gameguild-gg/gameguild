import { LineChart } from "lucide-react"

export default {
  id: "line-chart",
  type: "line",
  title: "Line Chart",
  description: "Time series line chart",
  icon: LineChart,
  category: "single-view-plots",
  subcategory: "line-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "A simple line chart with embedded data.",
    "data": {
      "values": [
        {"year": "2018", "value": 28}, {"year": "2019", "value": 55}, 
        {"year": "2020", "value": 43}, {"year": "2021", "value": 91}, 
        {"year": "2022", "value": 81}, {"year": "2023", "value": 53}
      ]
    },
    "mark": "line",
    "encoding": {
      "x": {"field": "year", "type": "temporal"},
      "y": {"field": "value", "type": "quantitative"}
    }
  }
}
