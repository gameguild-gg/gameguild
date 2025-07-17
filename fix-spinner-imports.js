const fs = require('fs');
const path = require('path');

// Files with LoadingSpinner imports
const filesToFix = [
  'apps/web/src/components/dashboard/users/user-management-content.tsx',
  'apps/web/src/components/dashboard/courses/course-management-content.tsx'
];

const projectRoot = path.resolve(__dirname);

filesToFix.forEach(filePath => {
  const fullPath = path.join(projectRoot, filePath);
  
  if (fs.existsSync(fullPath)) {
    let content = fs.readFileSync(fullPath, 'utf8');
    
    // Replace LoadingSpinner import with Loader2 from lucide-react
    content = content.replace(
      /import\s*{\s*LoadingSpinner\s*}\s*from\s*['"]@game-guild\/ui\/components\/spinner['"];?\s*/g,
      'import { Loader2 } from "lucide-react";\n'
    );
    
    // Replace LoadingSpinner component usage with Loader2
    content = content.replace(
      /<LoadingSpinner\s+size="sm"\s+className="([^"]*)"?\s*\/>/g,
      '<Loader2 className="h-4 w-4 animate-spin $1" />'
    );
    
    content = content.replace(
      /<LoadingSpinner\s+\/>/g,
      '<Loader2 className="h-4 w-4 animate-spin" />'
    );
    
    content = content.replace(
      /<LoadingSpinner\s+size="sm"\s*\/>/g,
      '<Loader2 className="h-4 w-4 animate-spin" />'
    );
    
    fs.writeFileSync(fullPath, content);
    console.log(`Fixed LoadingSpinner imports in ${filePath}`);
  } else {
    console.log(`File not found: ${filePath}`);
  }
});

console.log('Completed fixing LoadingSpinner imports');
