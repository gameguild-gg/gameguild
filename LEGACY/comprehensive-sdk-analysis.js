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

// Count functions in an action file
function countFunctionsInFile(filePath) {
  try {
    const content = fs.readFileSync(filePath, 'utf8');
    const functionMatches = content.match(/export\s+async\s+function\s+\w+/g);
    return functionMatches ? functionMatches.length : 0;
  } catch (error) {
    return 0;
  }
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
function analyzeSDKCoverage() {
  console.log('=== COMPREHENSIVE SDK COVERAGE ANALYSIS ===\n');
  
  const sdkFunctions = getSDKFunctions();
  const actionFiles = getAllActionFiles();
  
  console.log(`📊 Total SDK Functions Available: ${sdkFunctions.length}`);
  console.log(`📁 Total Action Files Found: ${actionFiles.length}\n`);
  
  // Track which SDK functions are used
  const usedSDKFunctions = new Set();
  let totalActionFunctions = 0;
  
  // Analyze each action file
  const moduleAnalysis = {};
  
  for (const filePath of actionFiles) {
    const relativePath = path.relative(path.join(__dirname, 'apps', 'web', 'src', 'lib'), filePath);
    const moduleName = relativePath.split(/[/\\]/)[0];
    
    if (!moduleAnalysis[moduleName]) {
      moduleAnalysis[moduleName] = {
        files: 0,
        functions: 0,
        sdkFunctions: new Set()
      };
    }
    
    const functionCount = countFunctionsInFile(filePath);
    const usedFunctions = getSDKUsageInFile(filePath, sdkFunctions);
    
    moduleAnalysis[moduleName].files++;
    moduleAnalysis[moduleName].functions += functionCount;
    usedFunctions.forEach(func => {
      moduleAnalysis[moduleName].sdkFunctions.add(func);
      usedSDKFunctions.add(func);
    });
    
    totalActionFunctions += functionCount;
  }
  
  // Sort modules by implementation size
  const sortedModules = Object.entries(moduleAnalysis)
    .sort(([,a], [,b]) => b.functions - a.functions);
  
  console.log('📋 MODULE ANALYSIS (sorted by implementation size):\n');
  
  for (const [moduleName, data] of sortedModules) {
    const coverage = data.sdkFunctions.size;
    const status = coverage > 0 ? '✅ IMPLEMENTED' : '❌ MISSING';
    
    console.log(`## ${moduleName.toUpperCase()} MODULE`);
    console.log(`   Status: ${status}`);
    console.log(`   Action Files: ${data.files}`);
    console.log(`   Server Actions: ${data.functions}`);
    console.log(`   SDK Functions Used: ${coverage}`);
    
    if (coverage > 10) {
      console.log(`   📈 HIGH COVERAGE - Comprehensive implementation`);
    } else if (coverage > 5) {
      console.log(`   📊 MODERATE COVERAGE - Good implementation`);
    } else if (coverage > 0) {
      console.log(`   📉 LOW COVERAGE - Basic implementation`);
    } else {
      console.log(`   🚫 NO SDK INTEGRATION - Local implementation only`);
    }
    console.log('');
  }
  
  // Overall statistics
  const coveragePercentage = ((usedSDKFunctions.size / sdkFunctions.length) * 100).toFixed(1);
  
  console.log('=== SUMMARY STATISTICS ===');
  console.log(`🎯 SDK Coverage: ${usedSDKFunctions.size}/${sdkFunctions.length} functions (${coveragePercentage}%)`);
  console.log(`⚡ Total Server Actions: ${totalActionFunctions}`);
  console.log(`📂 Implemented Modules: ${Object.keys(moduleAnalysis).length}`);
  
  // Coverage assessment
  if (coveragePercentage >= 90) {
    console.log(`🏆 EXCELLENT COVERAGE - Nearly complete SDK implementation!`);
  } else if (coveragePercentage >= 70) {
    console.log(`🥈 VERY GOOD COVERAGE - Most features implemented`);
  } else if (coveragePercentage >= 50) {
    console.log(`🥉 GOOD COVERAGE - Core features implemented`);
  } else {
    console.log(`📝 MODERATE COVERAGE - Significant work remaining`);
  }
  
  // Find truly missing functions
  const unusedFunctions = sdkFunctions.filter(func => !usedSDKFunctions.has(func));
  
  if (unusedFunctions.length > 0 && unusedFunctions.length < 50) {
    console.log(`\n🔍 UNUSED SDK FUNCTIONS (${unusedFunctions.length}):`);
    for (let i = 0; i < Math.min(20, unusedFunctions.length); i++) {
      console.log(`   - ${unusedFunctions[i]}`);
    }
    if (unusedFunctions.length > 20) {
      console.log(`   ... and ${unusedFunctions.length - 20} more`);
    }
  }
}

// Run the analysis
analyzeSDKCoverage();
