const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// Function to convert PascalCase/camelCase to kebab-case
function toKebabCase(str) {
  return str
    .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
    .toLowerCase();
}

// Files to rename (relative to apps/web/src/components)
const filesToRename = [
  'auth/EmailPasswordSignInForm.tsx',
  'auth/TenantManagementComponent.tsx',
  'auth/TenantSelector.tsx',
  'courses/course/CourseAccessCard.tsx',
  'courses/course/CourseAccessCard-enhanced.tsx',
  'courses/course/CourseAccessCard-legacy.tsx',
  'courses/course/CourseFeatures.tsx',
  'courses/course/CourseHeader.tsx',
  'courses/course/CourseOverview.tsx',
  'courses/course/CoursesBanner.tsx',
  'courses/course/CourseSidebar.tsx',
  'courses/course/CourseTools.tsx',
  'courses/CourseCard.tsx',
  'courses/CourseFilter.tsx',
  'courses/CourseList.tsx',
  'courses/CourseListWrapper.tsx',
  'courses/sections/ExploreMoreSection.tsx',
  'legacy/contributors/ContributorCard.tsx',
  'legacy/examples/IntegrationExample.tsx',
  'legacy/learning/ActivitySubmission.tsx',
  'legacy/learning/CertificateGeneration.tsx',
  'legacy/learning/CourseCatalog.tsx',
  'legacy/learning/CourseCompletion.tsx',
  'legacy/learning/CourseViewer.tsx',
  'legacy/learning/PeerReview.tsx',
  'legacy/learning/ProgressTracker.tsx',
  'legacy/markdown-renderer/Admonition.tsx',
  'legacy/markdown-renderer/MarkdownCodeActivity.tsx',
  'legacy/markdown-renderer/MarkdownQuizActivity.tsx',
  'legacy/markdown-renderer/Mermaid.tsx',
  'legacy/markdown-renderer/RevealJS.tsx',
  'legacy/others/common/NotificationContext.tsx',
  'legacy/profile/UserProfileDropdown.tsx',
  'legacy/ReportButton.tsx',
  'legacy/UserProfile.tsx'
];

const componentsDir = 'w:/repositories/game-guild/game-guild/apps/web/src/components';

console.log('Files to be renamed:');
filesToRename.forEach(file => {
  const oldPath = path.join(componentsDir, file);
  const dir = path.dirname(file);
  const ext = path.extname(file);
  const basename = path.basename(file, ext);
  const kebabName = toKebabCase(basename);
  const newFile = path.join(dir, kebabName + ext);
  const newPath = path.join(componentsDir, newFile);
  
  console.log(`${file} -> ${newFile}`);
});
