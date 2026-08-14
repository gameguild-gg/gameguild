import { GitCommit } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "gitgraph-basic",
  type: "gitgraph",
  title: "Git Graph",
  description: "Visualize git branching strategies",
  icon: GitCommit,
  category: "versioncontrol",
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
    commit id: "Release v1.0.1"
    
    checkout develop
    merge hotfix`,
} as MermaidTemplate;
