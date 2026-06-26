import { Clock } from "lucide-react"
import type { MermaidTemplate } from "./template-loader"

export default {
  id: "timeline-basic",
  type: "timeline",
  title: "Timeline",
  description: "Create chronological sequences and project timelines",
  icon: Clock,
  category: "timelines",
  preview: "2020 → 2021 → 2022 → 2023",
  code: `timeline
    title Project Timeline
    
    2020 : Project Planning
         : Team Formation
    
    2021 : Design Phase
         : Frontend Development
         : Backend Development
    
    2022 : Testing & QA
         : Beta Release
         : Production Launch
    
    2023 : Feature Enhancements
         : Performance Optimization
         : User Feedback Integration`,
} as MermaidTemplate
