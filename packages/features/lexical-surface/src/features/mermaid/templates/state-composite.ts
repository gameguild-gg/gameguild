import { Activity } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "state-composite",
  type: "state",
  title: "Composite State Diagram",
  description: "Use nested states for complex state machines",
  icon: Activity,
  category: "states",
  preview: "Composite states with nested logic",
  code: `stateDiagram-v2
    [*] --> Running
    
    state Running {
        [*] --> Initializing
        Initializing --> Processing : setup_complete
        Processing --> Finalizing : all_done
        Finalizing --> [*]
    }
    
    state Error {
        [*] --> LogError
        LogError --> RetryDecision : log_done
        RetryDecision --> [*]
    }
    
    Running --> Error : error
    Error --> Running : retry
    Running --> [*] : exit`,
} as MermaidTemplate;
