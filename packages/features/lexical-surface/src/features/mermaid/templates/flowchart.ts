import { GitBranch } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "flowchart-simple",
  type: "flowchart",
  title: "Simple Flowchart",
  description: "Create process flows and basic workflows",
  icon: GitBranch,
  category: "flowcharts",
  preview: "A → B → C",
  previewImage: "previews/flowchart.svg",
  code: `flowchart TD
    A[Start] --> B[Process]
    B --> C{Decision}
    C -->|Yes| D[Action 1]
    C -->|No| E[Action 2]
    D --> F[End]
    E --> F`,
} as MermaidTemplate;
