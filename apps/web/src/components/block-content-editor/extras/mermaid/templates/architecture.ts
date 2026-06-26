import { Building } from "lucide-react"
import type { MermaidTemplate } from "./template-loader"

export default {
  id: "architecture-basic",
  type: "architecture",
  title: "Architecture Diagram",
  description: "Design system architecture and components",
  icon: Building,
  category: "architecture",
  preview: "Frontend ↔ API ↔ Database",
  code: `architecture-beta
    group api(cloud)[API Layer]
        service db(database)[Database] in api
        service cache(disk)[Cache] in api
        service auth(server)[Auth Service] in api
    
    group frontend(cloud)[Frontend]
        service web(browser)[Web App] in frontend
        service mobile(phone)[Mobile App] in frontend
    
    group external(cloud)[External Services]
        service payment(server)[Payment Gateway] in external
        service email(server)[Email Service] in external
    
    web:R -- L:auth
    mobile:R -- L:auth
    auth:R -- L:db
    auth:R -- L:cache
    auth:R -- L:payment
    auth:R -- L:email`,
} as MermaidTemplate
