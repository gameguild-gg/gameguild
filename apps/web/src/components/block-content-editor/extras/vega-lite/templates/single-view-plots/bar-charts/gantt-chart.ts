import { BarChart3 } from "lucide-react"

export default {
  id: "gantt-chart",
  type: "bar",
  title: "Gantt Chart (Ranged Bar Marks)",
  description: "Timeline visualization with bar ranges",
  icon: BarChart3,
  category: "single-view-plots",
  subcategory: "bar-charts",
  spec: {
    "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
    "description": "Gantt chart showing time ranges.",
    "data": {
      "values": [
        {"task": "Task A", "start": 0, "end": 10},
        {"task": "Task B", "start": 5, "end": 20},
        {"task": "Task C", "start": 15, "end": 30}
      ]
    },
    "mark": "bar",
    "encoding": {
      "y": {"field": "task", "type": "nominal"},
      "x": {"field": "start", "type": "quantitative"},
      "x2": {"field": "end"}
    }
  }
}
