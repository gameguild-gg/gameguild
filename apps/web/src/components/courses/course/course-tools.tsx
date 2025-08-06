import { Program } from '@/lib/api/generated';
import { BookOpen } from 'lucide-react';

interface CourseToolsProps {
  readonly course: Program;
}

export function CourseTools({ course }: CourseToolsProps) {
  // For now, we'll use a default set of tools based on the category
  // In the future, this could be extended to include tools from the program content
  const getDefaultTools = (category: number): string[] => {
    const toolsByCategory: Record<number, string[]> = {
      0: ['Python', 'VS Code', 'Git', 'Jupyter Notebook'], // Programming
      1: ['Blender', 'Photoshop', 'GIMP', 'Inkscape'], // Art & Design
      2: ['Unity', 'Unreal Engine', 'Godot', 'GameMaker'], // Game Design
      3: ['Audacity', 'Reaper', 'Pro Tools', 'FL Studio'], // Audio
      4: ['Excel', 'PowerPoint', 'Word', 'Project'], // Business
      5: ['Google Analytics', 'Facebook Ads', 'Mailchimp', 'Canva'], // Marketing
      6: ['Trello', 'Asana', 'Jira', 'Notion'], // Production
      7: ['TestRail', 'Jira', 'Bugzilla', 'Postman'], // Quality Assurance
      8: ['Word', 'Google Docs', 'Grammarly', 'Scrivener'], // Writing
      9: ['Blender', 'Maya', '3ds Max', 'Cinema 4D'], // Animation
      10: ['Nuke', 'After Effects', 'Houdini', 'Blender'], // VFX
      11: ['Figma', 'Sketch', 'Adobe XD', 'InVision'], // UI/UX
      12: ['Android Studio', 'Xcode', 'React Native', 'Flutter'], // Mobile Development
      13: ['VS Code', 'Git', 'Docker', 'Node.js'], // Web Development
      14: ['Python', 'R', 'Jupyter', 'Tableau'], // Data Science
      15: ['TensorFlow', 'PyTorch', 'Scikit-learn', 'OpenAI'], // AI/ML
      16: ['Docker', 'Kubernetes', 'Jenkins', 'Terraform'], // DevOps
      17: ['VS Code', 'Git', 'Docker', 'Postman'], // Other
    };

    return toolsByCategory[category] || toolsByCategory[17]!;
  };

  const tools = getDefaultTools(course.category || 0);

  if (!tools || tools.length === 0) {
    return null;
  }

  return (
    <section>
      <h2 className="text-2xl font-bold mb-6 flex items-center">
        <BookOpen className="mr-3 h-6 w-6 text-purple-400" />
        Tools & Technologies
      </h2>
      <div className="bg-gray-800/50 rounded-xl p-6 border border-gray-700">
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {tools.map((tool, index) => (
            <div key={`${tool}-${index}`} className="flex items-center p-3 bg-gray-700/50 rounded-lg border border-gray-600">
              <div className="w-8 h-8 bg-gray-600 rounded mr-3 flex items-center justify-center">
                <BookOpen className="h-4 w-4 text-gray-300" />
              </div>
              <span className="text-sm font-medium text-gray-300">{tool}</span>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
