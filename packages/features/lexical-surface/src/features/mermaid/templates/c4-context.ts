import { Layers } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "c4-context-basic",
  type: "c4context",
  title: "C4 Context Diagram",
  description: "Model system context with users and external systems",
  icon: Layers,
  category: "c4models",
  preview: "Person → System → External",
  code: `C4Context
    title Internet Banking System - Context Diagram
    
    Person(customer, "Banking Customer", "A customer wanting to manage their bank accounts")
    
    System(banking, "Internet Banking System", "Allows customers to view information about their bank accounts")
    System_Ext(mainframe, "Mainframe Banking System", "Stores core banking data")
    System_Ext(email, "Email System", "Sends notifications to customers")
    
    Rel(customer, banking, "Uses")
    Rel(banking, mainframe, "Reads from / Writes to")
    Rel(banking, email, "Sends emails using")`,
} as MermaidTemplate;
