import { Brain } from "lucide-react"
import type { MermaidTemplate } from "./template-loader"

export default {
  id: "mindmap-basic",
  type: "mindmap",
  title: "Mindmap",
  description: "Organize ideas and concepts hierarchically",
  icon: Brain,
  category: "ideation",
  preview: "Central Idea → Multiple Branches",
  code: `mindmap
  root((Project Goals))
    Planning
      Timeline
      Resources
      Budget
    Development
      Frontend
        React Components
        UI Design
      Backend
        API Design
        Database
    Testing
      Unit Tests
      Integration Tests
      E2E Tests
    Deployment
      CI/CD Pipeline
      Monitoring
      Documentation`,
} as MermaidTemplate
