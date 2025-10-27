import { FileCheck } from "lucide-react"
import type { MermaidTemplate } from "./template-loader"

export default {
  id: "requirement-basic",
  type: "requirement",
  title: "Requirement Diagram",
  description: "Model system requirements and relationships",
  icon: FileCheck,
  category: "requirements",
  preview: "Requirement → Element → Verification",
  code: `requirementDiagram
    requirement FR_LOGIN {
        id: 1
        text: User must be able to login with email and password
        risk: high
        verifymethod: test
    }
    
    functionalRequirement FR_SESSION {
        id: 1.1
        text: Session must persist across browser refreshes
        risk: medium
        verifymethod: test
    }
    
    performanceRequirement PR_RESPONSE {
        id: 2.1
        text: Login response time must be under 500ms
        risk: medium
        verifymethod: demonstration
    }
    
    element Auth_System {
        type: system
    }
    
    element Database {
        type: system
    }
    
    FR_LOGIN - satisfies -> Auth_System
    FR_SESSION - derives -> FR_LOGIN
    PR_RESPONSE - refines -> FR_LOGIN
    Auth_System - interacts -> Database`,
} as MermaidTemplate
