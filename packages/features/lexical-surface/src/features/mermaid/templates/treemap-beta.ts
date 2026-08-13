import { Grid3x3 } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "treemap-beta",
  type: "treemap-beta",
  title: "Treemap Chart",
  description: "Visualize hierarchical data with nested rectangles",
  icon: Grid3x3,
  category: "charts",
  preview: "Hierarchical data visualization",
  previewImage: "previews/treemap-beta.svg",
  code: `treemap-beta
"Section 1"
    "Leaf 1.1": 12
    "Section 1.2"
      "Leaf 1.2.1": 12
"Section 2"
    "Leaf 2.1": 20
    "Leaf 2.2": 25`,
} as MermaidTemplate;
