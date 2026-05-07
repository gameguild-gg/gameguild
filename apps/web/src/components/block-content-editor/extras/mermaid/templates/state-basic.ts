import { Activity } from "lucide-react"
import type { MermaidTemplate } from "./template-loader"

export default {
  id: "state-basic",
  type: "state",
  title: "Basic State Diagram",
  description: "Model system states and transitions",
  icon: Activity,
  category: "states",
  preview: "Idle → Active → Complete",
  code: `stateDiagram-v2
    [*] --> Idle
    
    Idle --> Running : start
    Running --> Paused : pause
    Running --> Idle : stop
    Paused --> Running : resume
    Paused --> Idle : stop
    
    Running --> Complete : finish
    Complete --> [*]`,
} as MermaidTemplate
