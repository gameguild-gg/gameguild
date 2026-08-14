import { FilePlus2 } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "blank-diagram",
  type: "flowchart",
  title: "Blank Diagram",
  description: "Start with an empty Mermaid document",
  icon: FilePlus2,
  category: "starter",
  preview: "Empty Mermaid document",
  code: "flowchart",
} as MermaidTemplate;
