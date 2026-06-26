import { PieChart } from "lucide-react"
import type { MermaidTemplate } from "./template-loader"

export default {
  id: "pie-chart-basic",
  type: "pie",
  title: "Pie Chart",
  description: "Display data proportions and percentages",
  icon: PieChart,
  category: "charts",
  preview: "Frontend: 40% | Backend: 35%",
  code: `pie title Development Time Distribution
    "Frontend Development" : 40
    "Backend Development" : 35
    "DevOps & Infrastructure" : 15
    "Testing & QA" : 10`,
} as MermaidTemplate
