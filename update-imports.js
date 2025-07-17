const fs = require('fs');
const path = require('path');

// Mapping of old names to new names
const fileMapping = {
  'EmailPasswordSignInForm': 'email-password-sign-in-form',
  'TenantManagementComponent': 'tenant-management-component',
  'TenantSelector': 'tenant-selector',
  'CourseAccessCard': 'course-access-card',
  'CourseAccessCard-enhanced': 'course-access-card-enhanced',
  'CourseAccessCard-legacy': 'course-access-card-legacy',
  'CourseFeatures': 'course-features',
  'CourseHeader': 'course-header',
  'CourseOverview': 'course-overview',
  'CoursesBanner': 'courses-banner',
  'CourseSidebar': 'course-sidebar',
  'CourseTools': 'course-tools',
  'CourseCard': 'course-card-duplicate',
  'CourseFilter': 'course-filter',
  'CourseList': 'course-list',
  'CourseListWrapper': 'course-list-wrapper',
  'ExploreMoreSection': 'explore-more-section',
  'ContributorCard': 'contributor-card',
  'IntegrationExample': 'integration-example',
  'ActivitySubmission': 'activity-submission',
  'CertificateGeneration': 'certificate-generation',
  'CourseCatalog': 'course-catalog',
  'CourseCompletion': 'course-completion',
  'CourseViewer': 'course-viewer',
  'PeerReview': 'peer-review',
  'ProgressTracker': 'progress-tracker',
  'Admonition': 'admonition',
  'MarkdownCodeActivity': 'markdown-code-activity',
  'MarkdownQuizActivity': 'markdown-quiz-activity',
  'Mermaid': 'mermaid',
  'RevealJS': 'reveal-js',
  'NotificationContext': 'notification-context',
  'UserProfileDropdown': 'user-profile-dropdown',
  'ReportButton': 'report-button',
  'UserProfile': 'user-profile'
};

// Function to process a file and update imports
function updateImportsInFile(filePath) {
  try {
    const content = fs.readFileSync(filePath, 'utf8');
    let updatedContent = content;
    
    // Update import statements
    for (const [oldName, newName] of Object.entries(fileMapping)) {
      // Pattern for various import formats
      const patterns = [
        new RegExp(`from\\s+['"]([^'"]*/)${oldName}['"]`, 'g'),
        new RegExp(`import\\s+.*\\s+from\\s+['"]([^'"]*/)${oldName}['"]`, 'g'),
        new RegExp(`import\\s*\\{[^}]*\\}\\s*from\\s+['"]([^'"]*/)${oldName}['"]`, 'g'),
        new RegExp(`import\\s+\\w+\\s+from\\s+['"]([^'"]*/)${oldName}['"]`, 'g')
      ];
      
      patterns.forEach(pattern => {
        updatedContent = updatedContent.replace(pattern, (match, path) => {
          return match.replace(`/${oldName}`, `/${newName}`);
        });
      });
    }
    
    if (content !== updatedContent) {
      fs.writeFileSync(filePath, updatedContent);
      console.log(`Updated: ${filePath}`);
    }
  } catch (error) {
    console.error(`Error processing ${filePath}: ${error.message}`);
  }
}

// Find all TypeScript/JavaScript files in src
function findFiles(dir) {
  const files = [];
  const items = fs.readdirSync(dir);
  
  for (const item of items) {
    const fullPath = path.join(dir, item);
    const stat = fs.statSync(fullPath);
    
    if (stat.isDirectory() && !item.startsWith('.') && item !== 'node_modules') {
      files.push(...findFiles(fullPath));
    } else if (item.match(/\.(ts|tsx|js|jsx)$/)) {
      files.push(fullPath);
    }
  }
  
  return files;
}

// Process all files
const srcDir = 'w:/repositories/game-guild/game-guild/apps/web/src';
const files = findFiles(srcDir);

console.log(`Processing ${files.length} files...`);
files.forEach(updateImportsInFile);
console.log('Done!');
