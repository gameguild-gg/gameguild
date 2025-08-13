"use client"

import type React from "react"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import {
  GitBranch,
  FileText,
  Users,
  ArrowRight,
  Activity,
  Database,
  PieChart,
  GitCommit,
  FileCheck,
  Building,
  Layers,
  Clock,
  Brain,
  BarChart3,
  Radar,
  Grid3X3,
  Workflow,
} from "lucide-react"
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
  {
    type: "gitgraph",
    title: "Git Graph",
    description: "Visualize git branching strategies and commit history",
    icon: GitCommit,
    preview: "main ← feature → hotfix",
    code: `gitGraph
    commit id: "Initial commit"
    branch develop
    checkout develop
    commit id: "Add feature A"
    commit id: "Add feature B"
    checkout main
    merge develop
    commit id: "Release v1.0"
    branch hotfix
    checkout hotfix
    commit id: "Fix critical bug"
    checkout main
    merge hotfix
    commit id: "Release v1.0.1"`,
  },
  {
    type: "requirement",
    title: "Requirement Diagram",
    description: "Model system requirements and their relationships",
    icon: FileCheck,
    preview: "Requirement → Element → Verification",
    code: `requirementDiagram
    requirement test_req {
        id: 1
        text: the test text.
        risk: high
        verifymethod: test
    }
    
    functionalRequirement test_req2 {
        id: 1.1
        text: the second test text.
        risk: low
        verifymethod: inspection
    }
    
    performanceRequirement test_req3 {
        id: 1.2
        text: the third test text.
        risk: medium
        verifymethod: demonstration
    }
    
    element test_entity {
        type: simulation
    }
    
    test_req - satisfies -> test_entity
    test_req2 - derives -> test_req
    test_req3 - refines -> test_req2`,
  },
  {
    type: "architecture",
    title: "Architecture Diagram",
    description: "Design system architecture and component relationships",
    icon: Building,
    preview: "Frontend ↔ API ↔ Database",
    code: `architecture-beta
    group api(cloud)[API Layer]
    
    service db(database)[Database] in api
    service cache(disk)[Cache] in api
    service auth(server)[Auth Service] in api
    
    group frontend(cloud)[Frontend]
    service web(browser)[Web App] in frontend
    service mobile(phone)[Mobile App] in frontend
    
    group external(cloud)[External Services]
    service payment(server)[Payment Gateway] in external
    service email(server)[Email Service] in external
    
    web:R -- L:auth
    mobile:R -- L:auth
    auth:R -- L:db
    auth:R -- L:cache
    auth:R -- L:payment
    auth:R -- L:email`,
  },
  {
    type: "c4context",
    title: "C4 Context",
    description: "Model system context with users, systems and relationships",
    icon: Layers,
    preview: "Person → System → External System",
    code: `C4Context
    title System Context diagram for Internet Banking System
    
    Person(customerA, "Banking Customer A", "A customer of the bank, with personal bank accounts.")
    Person(customerB, "Banking Customer B")
    Person_Ext(customerC, "Banking Customer C", "desc")
    
    Person(customerD, "Banking Customer D", "A customer of the bank, <br/> with personal bank accounts.")
    
    System(SystemAA, "Internet Banking System", "Allows customers to view information about their bank accounts, and make payments.")
    
    System_Ext(SystemE, "Mainframe Banking System", "Stores all of the core banking information about customers, accounts, transactions, etc.")
    
    System_Ext(SystemC, "E-mail system", "The internal Microsoft Exchange e-mail system.")
    System_Ext(SystemD, "ATM", "Allows customers to withdraw cash.")
    
    Rel(customerA, SystemAA, "Uses")
    Rel(SystemAA, SystemE, "Uses")
    Rel(SystemAA, SystemC, "Sends e-mails", "SMTP")
    Rel(customerC, SystemD, "Withdraws cash", "ATM")`,
  },
  {
    type: "timeline",
    title: "Timeline",
    description: "Create chronological sequences and project timelines",
    icon: Clock,
    preview: "2021 → 2022 → 2023 → 2024",
    code: `timeline
    title History of Social Media Platform
    
    2002 : LinkedIn
    2003 : MySpace
    2004 : Facebook
         : Google
    2005 : Youtube
    2006 : Twitter
    2007 : FourSquare
    2008 : Github
    2010 : Instagram
    2011 : Snapchat
    2012 : Discord
    2013 : Slack
         : Telegram
    2016 : TikTok
    2017 : Mastodon`,
  },
  {
    type: "mindmap",
    title: "Mindmap",
    description: "Organize ideas and concepts in a hierarchical structure",
    icon: Brain,
    preview: "Central Idea → Branch 1 → Branch 2",
    code: `mindmap
  root((mindmap))
    Origins
      Long history
      ::icon(fa fa-book)
      Popularisation
        British popular psychology author Tony Buzan
    Research
      On effectiveness<br/>and features
      On Automatic creation
        Uses
            Creative techniques
            Strategic planning
            Argument mapping
    Tools
      Pen and paper
      Mermaid`,
  },
  {
    type: "xyChart",
    title: "XY Chart",
    description: "Create scatter plots and line charts with X-Y coordinates",
    icon: BarChart3,
    preview: "X-axis → Y-axis data points",
    code: `xychart-beta
    title "Sales Revenue"
    x-axis [Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec]
    y-axis "Revenue (in $)" 4000 --> 11000
    bar [5000, 6000, 7500, 8200, 9500, 10500, 11000, 10200, 9200, 8500, 7000, 6000]
    line [5000, 6000, 7500, 8200, 9500, 10500, 11000, 10200, 9200, 8500, 7000, 6000]`,
  },
  {
    type: "radar",
    title: "Radar Chart",
    description: "Display multivariate data in a circular format",
    icon: Radar,
    preview: "Skills assessment radar",
    code: `radar-beta
    axis m["Math"], s["Science"], e["English"]
    axis h["History"], g["Geography"], a["Art"]
    curve a["Alice"]{85, 90, 80, 70, 75, 90}
    curve b["Bob"]{70, 75, 85, 80, 90, 85}

    max 100
    min 0`,
  },
  {
    type: "quadrant",
    title: "Quadrant Chart",
    description: "Categorize items into four quadrants for analysis",
    icon: Grid3X3,
    preview: "High/Low Impact vs Effort matrix",
    code: `quadrantChart
    title Reach and influence
    x-axis Low Reach --> High Reach
    y-axis Low Influence --> High Influence
    quadrant-1 We should expand
    quadrant-2 Need to promote
    quadrant-3 Re-evaluate
    quadrant-4 May be improved
    Campaign A: [0.3, 0.6]
    Campaign B: [0.45, 0.23]
    Campaign C: [0.57, 0.69]
    Campaign D: [0.78, 0.34]
    Campaign E: [0.40, 0.34]`,
  },
  {
    type: "sankey",
    title: "Sankey Diagram",
    description: "Show flow quantities between different stages or categories",
    icon: Workflow,
    preview: "Source → Flow → Destination",
    code: `sankey-beta
    Agricultural 'waste',Bio-conversion,124.729
    Bio-conversion,Liquid,0.597
    Bio-conversion,Losses,26.862
    Bio-conversion,Solid,280.322
    Bio-conversion,Gas,81.144
    Biofuel imports,Liquid,35
    Biomass imports,Solid,35
    Coal imports,Coal,11.606
    Coal reserves,Coal,63.965
    Coal,Solid,75.571
    District heating,Industry,10.639
    District heating,Heating and cooling - commercial,22.505
    District heating,Heating and cooling - homes,46.184`,
  },
]

interface MermaidTemplateSelectorProps {
  onSelect: (template: { type: MermaidData["type"]; code: string }) => void
  onCancel: () => void
}

export function MermaidTemplateSelector({ onSelect, onCancel }: MermaidTemplateSelectorProps) {
  return (
    <div className="p-6 border-b bg-gray-50 max-h-[80vh] overflow-y-auto">
      <div className="text-center mb-6">
        <h3 className="text-lg font-semibold mb-2">Choose a Diagram Template</h3>
        <p className="text-gray-600">Select a template to get started with your Mermaid diagram</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-4 max-w-7xl mx-auto">
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
