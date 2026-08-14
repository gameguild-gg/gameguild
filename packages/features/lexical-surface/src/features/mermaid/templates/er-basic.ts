import { Database } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "er-basic",
  type: "er",
  title: "Entity Relationship Diagram",
  description: "Design database schemas with entities and relationships",
  icon: Database,
  category: "databases",
  preview: "User ← Order → Product",
  code: `erDiagram
    USER {
        int id PK
        string name
        string email
        datetime created_at
    }
    
    ORDER {
        int id PK
        int user_id FK
        decimal total
        datetime order_date
    }
    
    PRODUCT {
        int id PK
        string name
        decimal price
        int stock
    }
    
    ORDER_ITEM {
        int order_id FK
        int product_id FK
        int quantity
        decimal unit_price
    }
    
    USER ||--o{ ORDER : places
    ORDER ||--o{ ORDER_ITEM : contains
    PRODUCT ||--o{ ORDER_ITEM : included_in`,
} as MermaidTemplate;
