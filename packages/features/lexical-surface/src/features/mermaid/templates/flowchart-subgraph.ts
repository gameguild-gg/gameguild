import { GitBranch } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "flowchart-subgraph",
  type: "flowchart",
  title: "Flowchart with Subgraphs",
  description: "Organize flowchart into functional subgraphs",
  icon: GitBranch,
  category: "flowcharts",
  preview: "Main Flow → Sub Processes",
  code: `flowchart TD
    Start([Start])
    Start --> CheckAuth{Authenticated?}
    
    subgraph Auth["Authentication Process"]
        Login[Enter Credentials]
        Validate[Validate]
        CreateSession[Create Session]
        Login --> Validate --> CreateSession
    end
    
    subgraph Business["Business Logic"]
        FetchData[Fetch Data]
        Process[Process Data]
        Cache[Cache Results]
        FetchData --> Process --> Cache
    end
    
    subgraph Response["Response Handler"]
        Format[Format Response]
        SendResponse[Send to Client]
        Format --> SendResponse
    end
    
    CheckAuth -->|No| Auth
    CheckAuth -->|Yes| Business
    CreateSession --> Business
    Business --> Response
    SendResponse --> End([End])`,
} as MermaidTemplate;
