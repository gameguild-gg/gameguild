import { Kanban } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "kanban-board",
  type: "kanban",
  title: "Kanban Board",
  description: "Visualize workflow with task cards in columns",
  icon: Kanban,
  category: "project-management",
  preview: "TODO → IN PROGRESS → DONE",
  code: `kanban
  Todo
    [Create user interface mockups]
    [Review project requirements]
    [Setup development environment]
    [Write technical documentation]
  In Progress
    docs[Update API documentation]
    auth[Implement OAuth login]
    db[Design database schema]
  Review
    [Code review for feature-x]
    bug-fix[Fix authentication timeout issue]
  Testing
    [Test payment integration]
    [Perform security audit]
  Done
    [Deploy staging environment]
    [Create project roadmap]
    [Setup CI/CD pipeline]`,
} as MermaidTemplate;
