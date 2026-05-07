import { TrendingUp } from "lucide-react"

export default {
  id: "box-plot",
  type: "box",
  title: "Box Plot",
  description: "Statistical box plot showing quartiles",
  icon: TrendingUp,
  category: "composite-marks",
  subcategory: "box-plots",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "A box plot showing distribution quartiles.",
    "data": {
      "values": [
        {"category": "A", "value": 10}, {"category": "A", "value": 20}, {"category": "A", "value": 15},
        {"category": "A", "value": 25}, {"category": "A", "value": 18}, {"category": "A", "value": 22},
        {"category": "B", "value": 30}, {"category": "B", "value": 35}, {"category": "B", "value": 32},
        {"category": "B", "value": 40}, {"category": "B", "value": 38}, {"category": "B", "value": 45}
      ]
    },
    "mark": {"type": "boxplot", "extent": "min-max"},
    "encoding": {
      "x": {"field": "category", "type": "nominal"},
      "y": {"field": "value", "type": "quantitative"}
    }
  }
}
