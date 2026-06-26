import { FileText } from "lucide-react"
import type { MermaidTemplate } from "./template-loader"

export default {
  id: "class-basic",
  type: "class",
  title: "Basic Class Diagram",
  description: "Model object-oriented systems with classes",
  icon: FileText,
  category: "classes",
  preview: "Animal class with Dog and Cat subclasses",
  code: `classDiagram
    class Animal {
        -String name
        -int age
        +makeSound()
        +move()
    }
    
    class Dog {
        -String breed
        +bark()
    }
    
    class Cat {
        -String color
        +meow()
    }
    
    Animal <|-- Dog
    Animal <|-- Cat`,
} as MermaidTemplate
