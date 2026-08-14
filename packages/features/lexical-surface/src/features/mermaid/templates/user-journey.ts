import { Users } from "lucide-react";
import type { MermaidTemplate } from "./template-loader";

export default {
  id: "user-journey-basic",
  type: "journey",
  title: "User Journey",
  description: "Map user interactions and touchpoints across a service",
  icon: Users,
  category: "user-interactions",
  preview: "User → Task 1 → Task 2 → Task 3",
  previewImage: "previews/user-journey.svg",
  code: `journey
    title My working day
    section Go to work
      Make tea: 5: Me
      Go upstairs: 3: Me
    section Work
      Do work: 3: Me, Cat
      Have lunch: 2: Me
    section Go home
      Leave office: 5: Me
      Go downstairs: 5: Me`,
} as MermaidTemplate;
