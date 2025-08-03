const fs = require('fs');
const path = require('path');

// Read the SDK file and extract all function names
const sdkContent = fs.readFileSync('apps/web/src/lib/api/generated/sdk.gen.ts', 'utf8');
const sdkFunctions = [];

// Extract all export const function names
const matches = sdkContent.matchAll(/export const ([a-zA-Z0-9]+) = /g);
for (const match of matches) {
  sdkFunctions.push(match[1]);
}

console.log(`Total SDK functions: ${sdkFunctions.length}`);

// Group SDK functions by API module
const moduleGroups = {};

sdkFunctions.forEach(func => {
  let module = 'unknown';
  
  if (func.includes('Achievement') || func.includes('achievements')) {
    module = 'achievements';
  } else if (func.includes('Auth') || func.includes('auth')) {
    module = 'auth';
  } else if (func.includes('Contentinteraction') || func.includes('contentinteraction')) {
    module = 'content-interaction';
  } else if (func.includes('Credential') || func.includes('credentials')) {
    module = 'credentials';
  } else if (func.includes('Health') || func.includes('health')) {
    module = 'health';
  } else if (func.includes('Payment') || func.includes('payments')) {
    module = 'payments';
  } else if (func.includes('Posts') || func.includes('posts')) {
    module = 'posts';
  } else if (func.includes('Product') || func.includes('product')) {
    module = 'products';
  } else if (func.includes('Program') || func.includes('program')) {
    module = 'programs';
  } else if (func.includes('Project') || func.includes('projects')) {
    module = 'projects';
  } else if (func.includes('Subscription') || func.includes('subscription')) {
    module = 'subscriptions';
  } else if (func.includes('Tenant') || func.includes('tenant')) {
    module = 'tenants';
  } else if (func.includes('User') || func.includes('users')) {
    module = 'users';
  } else if (func.includes('Testing') || func.includes('testing')) {
    module = 'testing-feedback';
  } else if (func.includes('ActivityGrades') || func.includes('activitygrades')) {
    module = 'activity-grades';
  }
  
  if (!moduleGroups[module]) {
    moduleGroups[module] = [];
  }
  moduleGroups[module].push(func);
});

// Check which action files exist
const actionFiles = [];
const libDir = 'apps/web/src/lib';

function findActionFiles(dir) {
  const items = fs.readdirSync(dir);
  for (const item of items) {
    const itemPath = path.join(dir, item);
    const stat = fs.statSync(itemPath);
    if (stat.isDirectory()) {
      findActionFiles(itemPath);
    } else if (item.endsWith('.actions.ts')) {
      actionFiles.push(itemPath);
    }
  }
}

findActionFiles(libDir);

console.log('\n=== SDK COVERAGE ANALYSIS ===\n');

// Analyze each module
for (const [module, functions] of Object.entries(moduleGroups)) {
  console.log(`## ${module.toUpperCase()} MODULE`);
  console.log(`SDK Functions: ${functions.length}`);
  
  // Check if action file exists
  const actionFile = actionFiles.find(file => 
    file.includes(`/${module}/`) && file.endsWith('.actions.ts')
  );
  
  if (actionFile) {
    console.log(`✅ Action file exists: ${actionFile}`);
    
    // Check if file covers all functions
    const actionContent = fs.readFileSync(actionFile, 'utf8');
    const missingFunctions = functions.filter(func => !actionContent.includes(func));
    
    if (missingFunctions.length === 0) {
      console.log(`✅ All ${functions.length} functions implemented`);
    } else {
      console.log(`⚠️  Missing ${missingFunctions.length}/${functions.length} functions:`);
      missingFunctions.forEach(func => console.log(`   - ${func}`));
    }
  } else {
    console.log(`❌ No action file found`);
    console.log(`❌ Missing all ${functions.length} functions:`);
    functions.slice(0, 10).forEach(func => console.log(`   - ${func}`));
    if (functions.length > 10) {
      console.log(`   ... and ${functions.length - 10} more`);
    }
  }
  
  console.log('');
}

// Summary
const totalImplemented = Object.entries(moduleGroups).reduce((total, [module, functions]) => {
  const actionFile = actionFiles.find(file => 
    file.includes(`/${module}/`) && file.endsWith('.actions.ts')
  );
  
  if (actionFile) {
    const actionContent = fs.readFileSync(actionFile, 'utf8');
    const implementedCount = functions.filter(func => actionContent.includes(func)).length;
    return total + implementedCount;
  }
  return total;
}, 0);

console.log('=== SUMMARY ===');
console.log(`Total SDK functions: ${sdkFunctions.length}`);
console.log(`Implemented functions: ${totalImplemented}`);
console.log(`Missing functions: ${sdkFunctions.length - totalImplemented}`);
console.log(`Coverage: ${((totalImplemented / sdkFunctions.length) * 100).toFixed(1)}%`);
