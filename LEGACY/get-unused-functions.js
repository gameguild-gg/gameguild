const fs = require('fs');
const path = require('path');

// Read the SDK to get all available functions
function getSDKFunctions() {
  try {
    const sdkPath = path.join(__dirname, 'apps', 'web', 'src', 'lib', 'api', 'generated', 'sdk.gen.ts');
    const sdkContent = fs.readFileSync(sdkPath, 'utf8');
    
    // Extract all export function declarations
    const functionRegex = /export\s+(?:const|function)\s+([a-zA-Z][a-zA-Z0-9]*)\s*[:=]/g;
    const functions = [];
    let match;
    
    while ((match = functionRegex.exec(sdkContent)) !== null) {
      functions.push(match[1]);
    }
    
    return functions.sort();
  } catch (error) {
    console.error('Error reading SDK file:', error.message);
    return [];
  }
}

// Get all action files in the lib directory
function getAllActionFiles() {
  const libPath = path.join(__dirname, 'apps', 'web', 'src', 'lib');
  const actionFiles = [];

  function findActionFiles(dir) {
    const files = fs.readdirSync(dir);
    
    for (const file of files) {
      const fullPath = path.join(dir, file);
      const stat = fs.statSync(fullPath);
      
      if (stat.isDirectory()) {
        findActionFiles(fullPath);
      } else if (file.endsWith('.actions.ts')) {
        actionFiles.push(fullPath);
      }
    }
  }
  
  findActionFiles(libPath);
  return actionFiles;
}

// Get SDK function usage in action files
function getSDKUsageInFile(filePath, sdkFunctions) {
  try {
    const content = fs.readFileSync(filePath, 'utf8');
    const usedFunctions = [];
    
    for (const func of sdkFunctions) {
      if (content.includes(func)) {
        usedFunctions.push(func);
      }
    }
    
    return usedFunctions;
  } catch (error) {
    return [];
  }
}

// Main analysis
function getUnusedFunctions() {
  const sdkFunctions = getSDKFunctions();
  const actionFiles = getAllActionFiles();
  
  // Track which SDK functions are used
  const usedSDKFunctions = new Set();
  
  for (const filePath of actionFiles) {
    const usedFunctions = getSDKUsageInFile(filePath, sdkFunctions);
    usedFunctions.forEach(func => usedSDKFunctions.add(func));
  }
  
  // Find truly missing functions
  const unusedFunctions = sdkFunctions.filter(func => !usedSDKFunctions.has(func));
  
  console.log(`Total SDK Functions: ${sdkFunctions.length}`);
  console.log(`Used Functions: ${usedSDKFunctions.size}`);
  console.log(`Unused Functions: ${unusedFunctions.length}`);
  console.log('\n=== UNUSED FUNCTIONS ===');
  
  unusedFunctions.forEach((func, index) => {
    console.log(`${index + 1}. ${func}`);
  });
}

getUnusedFunctions();
