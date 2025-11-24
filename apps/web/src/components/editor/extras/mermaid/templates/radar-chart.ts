import { Radar } from "lucide-react"
import type { MermaidTemplate } from "./template-loader"

export default {
  id: "radar-chart-basic",
  type: "radar",
  title: "Radar Chart",
  description: "Display multivariate data in circular format",
  icon: Radar,
  category: "charts",
  preview: "Multi-dimensional assessment",
  code: `radar-beta
    axis m["Math"], s["Science"], e["English"]
    axis h["History"], g["Geography"], a["Art"]
    
    curve alice["Alice"] {85, 90, 80, 70, 75, 90}
    curve bob["Bob"] {70, 75, 85, 80, 90, 85}
    curve charlie["Charlie"] {95, 88, 82, 75, 80, 92}
    
    max 100
    min 0`,
} as MermaidTemplate
