const fs = require('fs');
const path = require('path');

// Function to find all TypeScript files
function findTsFiles(dir) {
  const files = [];
  const items = fs.readdirSync(dir);
  
  for (const item of items) {
    const fullPath = path.join(dir, item);
    const stat = fs.statSync(fullPath);
    
    if (stat.isDirectory() && !item.startsWith('.') && item !== 'node_modules') {
      files.push(...findTsFiles(fullPath));
    } else if (item.match(/\.(ts|tsx)$/)) {
      files.push(fullPath);
    }
  }
  
  return files;
}

// Function to fix imports in a file
function fixImportsInFile(filePath) {
  try {
    const content = fs.readFileSync(filePath, 'utf8');
    let updatedContent = content;
    
    // Fix @/types/ imports to @/components/legacy/types/
    updatedContent = updatedContent.replace(
      /from\s+['"]@\/types\/([^'"]+)['"]/g,
      "from '@/components/legacy/types/$1'"
    );
    
    updatedContent = updatedContent.replace(
      /import\s+type\s+\{([^}]+)\}\s+from\s+['"]@\/types\/([^'"]+)['"]/g,
      "import type {$1} from '@/components/legacy/types/$2'"
    );
    
    updatedContent = updatedContent.replace(
      /import\s+\{([^}]+)\}\s+from\s+['"]@\/types\/([^'"]+)['"]/g,
      "import {$1} from '@/components/legacy/types/$2'"
    );
    
    if (content !== updatedContent) {
      fs.writeFileSync(filePath, updatedContent);
      console.log(`Fixed imports in: ${filePath}`);
      return true;
    }
    return false;
  } catch (error) {
    console.error(`Error processing ${filePath}: ${error.message}`);
    return false;
  }
}

// Main execution
const srcDir = 'w:/repositories/game-guild/game-guild/apps/web/src';
const files = findTsFiles(srcDir);

console.log(`Processing ${files.length} TypeScript files...`);
let fixedCount = 0;

files.forEach(file => {
  if (fixImportsInFile(file)) {
    fixedCount++;
  }
});

console.log(`Fixed imports in ${fixedCount} files.`);
