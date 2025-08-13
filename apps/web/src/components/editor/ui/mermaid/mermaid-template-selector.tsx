"use client"

import type React from "react"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { GitBranch, FileText, Users, ArrowRight, Activity, Database, PieChart } from "lucide-react"
import type { MermaidData } from "@/components/editor/nodes/mermaid-node"

interface Template {
  type: MermaidData["type"]
  title: string
  description: string
  icon: React.ComponentType<{ className?: string }>
  code: string
  preview: string
}

const templates: Template[] = [
  {
    type: "flowchart",
    title: "Flowchart",
    description: "Create process flows, decision trees, and workflow diagrams",
    icon: GitBranch,
    preview: "A → B → C",
    code: `flowchart TD
    A[Start] --> B{Decision}
    B -->|Yes| C[Process 1]
    B -->|No| D[Process 2]
    C --> E[End]
    D --> E`,
  },
  {
    type: "class",
    title: "Class Diagram",
    description: "Model object-oriented systems with classes and relationships",
    icon: FileText,
    preview: "Class ← Interface → Implementation",
    code: `classDiagram
    class Animal {
        +String name
        +int age
        +makeSound()
    }
    class Dog {
        +String breed
        +bark()
    }
    class Cat {
        +String color
        +meow()
    }
    Animal <|-- Dog
    Animal <|-- Cat`,
  },
  {
    type: "sequence",
    title: "Sequence Diagram",
    description: "Show interactions between different actors over time",
    icon: Users,
    preview: "User → System → Database",
    code: `sequenceDiagram
    participant U as User
    participant S as System
    participant D as Database
    
    U->>S: Login Request
    S->>D: Validate User
    D-->>S: User Valid
    S-->>U: Login Success`,
  },
  {
    type: "state",
    title: "State Diagram",
    description: "Model system states and transitions between them",
    icon: Activity,
    preview: "Idle → Active → Complete",
    code: `stateDiagram-v2
    [*] --> Idle
    Idle --> Active : start
    Active --> Processing : process
    Processing --> Complete : finish
    Processing --> Error : fail
    Error --> Idle : retry
    Complete --> [*]`,
  },
  {
    type: "er",
    title: "Entity Relationship",
    description: "Design database schemas with entities and relationships",
    icon: Database,
    preview: "User ||--o{ Order : places",
    code: `erDiagram
    USER {
        int id PK
        string name
        string email
        date created_at
    }
    ORDER {
        int id PK
        int user_id FK
        decimal total
        date order_date
    }
    PRODUCT {
        int id PK
        string name
        decimal price
        int stock
    }
    ORDER_ITEM {
        int order_id FK
        int product_id FK
        int quantity
        decimal price
    }
    
    USER ||--o{ ORDER : places
    ORDER ||--o{ ORDER_ITEM : contains
    PRODUCT ||--o{ ORDER_ITEM : included_in`,
  },
  {
    type: "pie",
    title: "Pie Chart",
    description: "Display data proportions and percentages visually",
    icon: PieChart,
    preview: "Frontend: 40% | Backend: 35% | DevOps: 25%",
    code: `pie title Development Time Distribution
    "Frontend Development" : 40
    "Backend Development" : 35
    "DevOps & Infrastructure" : 15
    "Testing & QA" : 10`,
  },
]

interface MermaidTemplateSelectorProps {
  onSelect: (template: { type: MermaidData["type"]; code: string }) => void
  onCancel: () => void
}

export function MermaidTemplateSelector({ onSelect, onCancel }: MermaidTemplateSelectorProps) {
  return (
    <div className="p-6 border-b bg-gray-50">
      <div className="text-center mb-6">
        <h3 className="text-lg font-semibold mb-2">Choose a Diagram Template</h3>
        <p className="text-gray-600">Select a template to get started with your Mermaid diagram</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 max-w-6xl mx-auto">
        {templates.map((template) => {
          const IconComponent = template.icon
          return (
            <Card
              key={template.type}
              className="cursor-pointer hover:shadow-md transition-shadow border-2 hover:border-blue-300"
              onClick={() => onSelect({ type: template.type, code: template.code })}
            >
              <CardHeader className="text-center pb-3">
                <div className="mx-auto mb-2 p-3 bg-blue-100 rounded-full w-fit">
                  <IconComponent className="h-6 w-6 text-blue-600" />
                </div>
                <CardTitle className="text-lg">{template.title}</CardTitle>
                <CardDescription className="text-sm">{template.description}</CardDescription>
              </CardHeader>
              <CardContent className="pt-0">
                <div className="bg-gray-100 rounded p-3 mb-3 text-center">
                  <code className="text-sm text-gray-700 font-mono">{template.preview}</code>
                </div>
                <Button className="w-full bg-transparent" variant="outline">
                  <span>Select Template</span>
                  <ArrowRight className="h-4 w-4 ml-2" />
                </Button>
              </CardContent>
            </Card>
          )
        })}
      </div>

      <div className="text-center mt-6">
        <Button variant="ghost" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </div>
  )
}
