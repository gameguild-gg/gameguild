import { FileText } from "lucide-react"
import type { MermaidTemplate } from "./template-loader"

export default {
  id: "class-relationships",
  type: "class",
  title: "Class Relationships",
  description: "Show complex class relationships and dependencies",
  icon: FileText,
  category: "classes",
  preview: "Multiple relationship types between classes",
  code: `classDiagram
    class Vehicle {
        -String make
        -String model
        +drive()
    }
    
    class Car {
        -int doors
        +honk()
    }
    
    class Truck {
        -float capacity
        +load()
    }
    
    class Engine {
        -int horsepower
        +start()
    }
    
    class Wheel {
        -String brand
        +rotate()
    }
    
    Vehicle <|-- Car
    Vehicle <|-- Truck
    Vehicle *-- Engine : has
    Vehicle *-- Wheel : has 4`,
} as MermaidTemplate
