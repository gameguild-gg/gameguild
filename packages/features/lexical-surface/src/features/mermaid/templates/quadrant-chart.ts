import { Grid3x3 } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "quadrant-chart-basic",
  type: "quadrant",
  title: "Quadrant Chart",
  description: "Categorize items into four quadrants",
  icon: Grid3x3,
  category: "charts",
  preview: "Impact vs Effort matrix",
  code: `quadrantChart
    title Prioritization Matrix
    x-axis Low Impact --> High Impact
    y-axis Low Effort --> High Effort
    
    quadrant-1 Quick Wins
    quadrant-2 Major Projects
    quadrant-3 Low Priority
    quadrant-4 Time Sinks
    
    Task A: [0.3, 0.6]
    Task B: [0.45, 0.23]
    Task C: [0.57, 0.69]
    Task D: [0.78, 0.34]
    Task E: [0.40, 0.34]`,
} as MermaidTemplate;
