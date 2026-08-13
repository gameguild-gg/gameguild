import { GitBranch } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "flowchart-decision",
  type: "flowchart",
  title: "Decision Flowchart",
  description: "Create complex decision trees with multiple branches",
  icon: GitBranch,
  category: "flowcharts",
  preview: "Decision → Multiple Outcomes",
  code: `flowchart TD
    A[User Request] --> B{Is Authenticated?}
    B -->|No| C[Redirect to Login]
    C --> D[Enter Credentials]
    D --> E{Valid?}
    E -->|No| F[Show Error]
    F --> D
    E -->|Yes| G[Create Session]
    B -->|Yes| H{Has Permission?}
    H -->|No| I[Show Access Denied]
    H -->|Yes| J[Load Resource]
    G --> K[Display Data]
    J --> K
    I --> L[End]
    K --> L`,
} as MermaidTemplate;
