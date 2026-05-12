import { Users } from "lucide-react"
import type { MermaidTemplate } from "./template-loader"

export default {
  id: "sequence-async",
  type: "sequence",
  title: "Async Operations Sequence",
  description: "Show asynchronous interactions and callbacks",
  icon: Users,
  category: "sequences",
  preview: "Request → Async → Callback",
  code: `sequenceDiagram
    actor User
    participant Frontend
    participant API
    participant Queue
    participant Worker
    
    User->>Frontend: Request long operation
    Frontend->>API: POST /api/process
    activate API
    API->>Queue: Enqueue job
    API-->>Frontend: Job ID (202)
    deactivate API
    
    activate Queue
    Queue->>Worker: Start processing
    deactivate Queue
    
    Frontend->>Frontend: Poll for status
    
    activate Worker
    Note over Worker: Processing...
    Worker-->>API: Result ready
    deactivate Worker
    
    Frontend->>API: GET /api/result/{jobId}
    activate API
    API-->>Frontend: Result
    deactivate API
    
    Frontend-->>User: Show results`,
} as MermaidTemplate
