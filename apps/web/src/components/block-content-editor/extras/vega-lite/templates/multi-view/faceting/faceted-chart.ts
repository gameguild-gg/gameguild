import { Grid3x3 } from "lucide-react"

export default {
  id: "faceted-chart",
  type: "facet",
  title: "Faceted Chart",
  description: "Small multiples showing data across categories",
  icon: Grid3x3,
  category: "multi-view",
  subcategory: "faceting",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "A faceted chart showing multiple categories.",
    "data": {
      "values": [
        {"category": "A", "x": 1, "y": 28},
        {"category": "A", "x": 2, "y": 55},
        {"category": "A", "x": 3, "y": 43},
        {"category": "B", "x": 1, "y": 91},
        {"category": "B", "x": 2, "y": 81},
        {"category": "B", "x": 3, "y": 53},
        {"category": "C", "x": 1, "y": 19},
        {"category": "C", "x": 2, "y": 87},
        {"category": "C", "x": 3, "y": 52}
      ]
    },
    "facet": {
      "field": "category",
      "type": "nominal",
      "columns": 2
    },
    "spec": {
      "mark": "line",
      "encoding": {
        "x": {"field": "x", "type": "quantitative"},
        "y": {"field": "y", "type": "quantitative"}
      }
    }
  }
}
