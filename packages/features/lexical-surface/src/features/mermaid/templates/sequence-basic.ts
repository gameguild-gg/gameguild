import { Users } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "sequence-basic",
  type: "sequence",
  title: "Basic Sequence Diagram",
  description: "Show interactions between different actors",
  icon: Users,
  category: "sequences",
  preview: "Actor 1 → Actor 2 → Actor 3",
  code: `sequenceDiagram
    participant User
    participant Client
    participant Server
    participant Database
    
    User->>Client: Click Submit
    Client->>Server: POST /api/data
    activate Server
    Server->>Database: Query data
    activate Database
    Database-->>Server: Return results
    deactivate Database
    Server-->>Client: JSON response
    deactivate Server
    Client-->>User: Display results`,
} as MermaidTemplate;
