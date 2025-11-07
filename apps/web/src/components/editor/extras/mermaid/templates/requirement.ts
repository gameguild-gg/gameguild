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

    requirement test_req {
    id: 1
    text: the test text.
    risk: high
    verifymethod: test
    }

    element test_entity {
    type: simulation
    }

    test_entity - satisfies -> test_req`,
} as MermaidTemplate
