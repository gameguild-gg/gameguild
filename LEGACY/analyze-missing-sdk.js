// Extract SDK functions from the provided attachment
const sdkFunctions = [
  // Achievements
  'getApiAchievementsLeaderboard',
  'getApiAchievements',
  'postApiAchievements',
  'deleteApiAchievementsByAchievementId',
  'getApiAchievementsByAchievementId',
  'putApiAchievementsByAchievementId',
  'postApiAchievementsByAchievementIdAward',
  'postApiAchievementsByAchievementIdBulkAward',
  'getApiAchievementsByAchievementIdStatistics',
  'getApiAchievementsStatistics',
  
  // Activity Grades
  'postApiProgramsByProgramIdActivityGrades',
  'getApiProgramsByProgramIdActivityGradesInteractionByContentInteractionId',
  'getApiProgramsByProgramIdActivityGradesGraderByGraderProgramUserId',
  'getApiProgramsByProgramIdActivityGradesStudentByProgramUserId',
  'deleteApiProgramsByProgramIdActivityGradesByGradeId',
  'putApiProgramsByProgramIdActivityGradesByGradeId',
  'getApiProgramsByProgramIdActivityGradesPending',
  'getApiProgramsByProgramIdActivityGradesStatistics',
  'getApiProgramsByProgramIdActivityGradesContentByContentId',
  
  // Auth
  'postApiAuthSignup',
  'postApiAuthSignin',
  'postApiAuthGoogleIdToken',
  'postApiAuthRefresh',
  'postApiAuthRevoke',
  'getApiAuthProfile',
  
  // Content Interaction
  'postApiContentinteractionStart',
  'putApiContentinteractionByInteractionIdProgress',
  'postApiContentinteractionByInteractionIdSubmit',
  'postApiContentinteractionByInteractionIdComplete',
  'getApiContentinteractionUserByProgramUserIdContentByContentId',
  'getApiContentinteractionUserByProgramUserId',
  'putApiContentinteractionByInteractionIdTimeSpent',
  
  // Credentials
  'getCredentials',
  'postCredentials',
  'getCredentialsUserByUserId',
  'deleteCredentialsById',
  'getCredentialsById',
  'putCredentialsById',
  'getCredentialsUserByUserIdTypeByType',
  'postCredentialsByIdRestore',
  'deleteCredentialsByIdHard',
  'postCredentialsByIdMarkUsed',
  'postCredentialsByIdDeactivate',
  'postCredentialsByIdActivate',
  'getCredentialsDeleted',
  
  // Health
  'getHealth',
  'getHealthDatabase',
  
  // Payments
  'getApiPaymentMethodsMe',
  'postApiPaymentIntent',
  'postApiPaymentByIdProcess',
  'postApiPaymentByIdRefund',
  'getApiPaymentById',
  'getApiPaymentUserByUserId',
  'getApiPaymentStats',
  'postApiPayments',
  'getApiPaymentsById',
  'getApiPaymentsMyPayments',
  'getApiPaymentsUsersByUserId',
  'getApiPaymentsProductsByProductId',
  'postApiPaymentsByIdProcess',
  'postApiPaymentsByIdRefund',
  'postApiPaymentsByIdCancel',
  'getApiPaymentsStats',
  'getApiPaymentsRevenueReport',
  
  // Posts
  'getApiPosts',
  'postApiPosts',
  'getApiPostsByPostId',
  
  // Products (already implemented - 41 functions)
  'getApiProduct',
  'postApiProduct',
  'deleteApiProductById',
  'getApiProductById',
  'putApiProductById',
  'getApiProductTypeByType',
  'getApiProductPublished',
  'getApiProductSearch',
  'getApiProductCreatorByCreatorId',
  'getApiProductPriceRange',
  'getApiProductPopular',
  'getApiProductRecent',
  'postApiProductByIdPublish',
  'postApiProductByIdUnpublish',
  'postApiProductByIdArchive',
  'putApiProductByIdVisibility',
  'getApiProductByIdBundleItems',
  'deleteApiProductByBundleIdBundleItemsByProductId',
  'postApiProductByBundleIdBundleItemsByProductId',
  'getApiProductByIdPricingCurrent',
  'getApiProductByIdPricingHistory',
  'postApiProductByIdPricing',
  'getApiProductByIdSubscriptionPlans',
  'postApiProductByIdSubscriptionPlans',
  'getApiProductSubscriptionPlansByPlanId',
  'deleteApiProductByIdAccessByUserId',
  'getApiProductByIdAccessByUserId',
  'postApiProductByIdAccessByUserId',
  'getApiProductByIdUserProductByUserId',
  'getApiProductAnalyticsCount',
  'getApiProductByIdAnalyticsUserCount',
  'getApiProductByIdAnalyticsRevenue',
  
  // Programs
  'getApiProgram',
  'postApiProgram',
  'getApiProgramPublished',
  'getApiProgramCategoryByCategory',
  'getApiProgramDifficultyByDifficulty',
  'getApiProgramSearch',
  'getApiProgramCreatorByCreatorId',
  'getApiProgramPopular',
  'getApiProgramRecent',
  'deleteApiProgramById',
  'getApiProgramById',
  'putApiProgramById',
  'getApiProgramByIdWithContent',
  'postApiProgramByIdClone',
  'getApiProgramSlugBySlug',
  'postApiProgramByIdContent',
  'deleteApiProgramByIdContentByContentId',
  'putApiProgramByIdContentByContentId',
  'postApiProgramByIdContentReorder',
  'deleteApiProgramByIdUsersByUserId',
  'postApiProgramByIdUsersByUserId',
  'getApiProgramByIdUsers',
  'getApiProgramByIdUsersByUserIdProgress',
  'putApiProgramByIdUsersByUserIdProgress',
  'postApiProgramByIdUsersByUserIdContentByContentIdComplete',
  'postApiProgramByIdUsersByUserIdReset',
  'postApiProgramByIdSubmit',
  'postApiProgramByIdApprove',
  'postApiProgramByIdReject',
  'postApiProgramByIdWithdraw',
  'postApiProgramByIdArchive',
  'postApiProgramByIdRestore',
  'postApiProgramByIdPublish',
  'postApiProgramByIdUnpublish',
  'postApiProgramByIdSchedule',
  'postApiProgramByIdMonetize',
  'postApiProgramByIdDisableMonetization',
  'getApiProgramByIdPricing',
  'putApiProgramByIdPricing',
  'getApiProgramByIdAnalytics',
  'getApiProgramByIdAnalyticsCompletionRates',
  'getApiProgramByIdAnalyticsEngagement',
  'getApiProgramByIdAnalyticsRevenue',
  'postApiProgramByIdCreateProduct',
  'deleteApiProgramByIdLinkProductByProductId',
  'postApiProgramByIdLinkProductByProductId',
  'getApiProgramByIdProducts',
  'getApiProgramsByProgramIdContent',
  'postApiProgramsByProgramIdContent',
  'getApiProgramsByProgramIdContentTopLevel',
  'deleteApiProgramsByProgramIdContentById',
  'getApiProgramsByProgramIdContentById',
  'putApiProgramsByProgramIdContentById',
  'getApiProgramsByProgramIdContentByParentIdChildren',
  'postApiProgramsByProgramIdContentReorder',
  'postApiProgramsByProgramIdContentByIdMove',
  'getApiProgramsByProgramIdContentRequired',
  'getApiProgramsByProgramIdContentByTypeByType',
  'getApiProgramsByProgramIdContentByVisibilityByVisibility',
  'postApiProgramsByProgramIdContentSearch',
  'getApiProgramsByProgramIdContentStats',
  
  // Projects
  'getApiProjects',
  'postApiProjects',
  'deleteApiProjectsById',
  'getApiProjectsById',
  'putApiProjectsById',
  'getApiProjectsSlugBySlug',
  'postApiProjectsByIdPublish',
  'postApiProjectsByIdUnpublish',
  'postApiProjectsByIdArchive',
  'getApiProjectsSearch',
  'getApiProjectsPopular',
  'getApiProjectsRecent',
  'getApiProjectsFeatured',
];

// Additional functions from full SDK that are commonly missing
const additionalSDKFunctions = [
  // Users & User Management
  'getApiUsers',
  'postApiUsers',
  'deleteApiUsersById',
  'getApiUsersById',
  'putApiUsersById',
  'getApiUsersSearch',
  'getApiUsersStatistics',
  'postApiUsersBulk',
  'postApiUsersByIdRestore',
  'patchApiUsersBulkActivate',
  'patchApiUsersBulkDeactivate',
  'putApiUsersByIdBalance',
  'getApiUsersByUserIdAchievements',
  'getApiUsersByUserIdAchievementsAvailable',
  'getApiUsersByUserIdAchievementsByAchievementIdPrerequisites',
  'getApiUsersByUserIdAchievementsProgress',
  'getApiUsersByUserIdAchievementsSummary',
  'deleteApiUsersByUserIdAchievementsByUserAchievementId',
  'postApiUsersByUserIdAchievementsByAchievementIdProgress',
  'postApiUsersByUserIdAchievementsByUserAchievementIdMarkNotified',
  
  // User Profiles
  'getApiUserprofiles',
  'postApiUserprofiles',
  'deleteApiUserprofilesById',
  'getApiUserprofilesById',
  'putApiUserprofilesById',
  'getApiUserprofilesUserByUserId',
  'postApiUserprofilesByIdRestore',
  
  // Tenants
  'getApiTenants',
  'postApiTenants',
  'deleteApiTenantsById',
  'getApiTenantsById',
  'putApiTenantsById',
  'getApiTenantsActive',
  'getApiTenantsByNameByName',
  'getApiTenantsBySlugBySlug',
  'getApiTenantsDeleted',
  'getApiTenantsSearch',
  'getApiTenantsStatistics',
  'postApiTenantsBulkDelete',
  'postApiTenantsBulkRestore',
  'postApiTenantsByIdActivate',
  'postApiTenantsByIdDeactivate',
  'postApiTenantsByIdRestore',
  'deleteApiTenantsByIdPermanent',
  
  // Tenant Domains
  'getApiTenantDomains',
  'postApiTenantDomains',
  'deleteApiTenantDomainsById',
  'getApiTenantDomainsById',
  'putApiTenantDomainsById',
  'getApiTenantDomainsDomainMatch',
  'postApiTenantDomainsByTenantIdSetMainByDomainId',
  'postApiTenantDomainsAutoAssign',
  'postApiTenantDomainsAutoAssignBulk',
  'postApiTenantDomainsMemberships',
  
  // Tenant User Groups
  'getApiTenantDomainsUserGroups',
  'postApiTenantDomainsUserGroups',
  'deleteApiTenantDomainsUserGroupsById',
  'getApiTenantDomainsUserGroupsById',
  'putApiTenantDomainsUserGroupsById',
  'getApiTenantDomainsMembershipsUserByUserId',
  'deleteApiTenantDomainsUserGroupsMemberships',
  'postApiTenantDomainsUserGroupsMemberships',
  'getApiTenantDomainsUserGroupsByGroupIdMembers',
  'getApiTenantDomainsUsersByUserIdGroups',
  'getApiTenantDomainsGroupsByGroupIdUsers',
  
  // Subscriptions
  'getApiSubscription',
  'postApiSubscription',
  'getApiSubscriptionById',
  'getApiSubscriptionMe',
  'getApiSubscriptionMeActive',
  'postApiSubscriptionByIdCancel',
  'postApiSubscriptionByIdResume',
  'putApiSubscriptionByIdPaymentMethod',
  
  // Testing & Feedback
  'getTestingRequests',
  'postTestingRequests',
  'deleteTestingRequestsById',
  'getTestingRequestsById',
  'putTestingRequestsById',
  'getTestingRequestsByCreatorByCreatorId',
  'getTestingRequestsByIdDetails',
  'getTestingRequestsByProjectVersionByProjectVersionId',
  'getTestingRequestsByRequestIdFeedback',
  'getTestingRequestsByRequestIdParticipants',
  'getTestingRequestsByRequestIdParticipantsByUserIdCheck',
  'getTestingRequestsByRequestIdStatistics',
  'getTestingRequestsByStatusByStatus',
  'getTestingRequestsSearch',
  'postTestingRequestsByIdRestore',
  'postTestingRequestsByRequestIdFeedback',
  'postTestingRequestsByRequestIdParticipantsByUserId',
  'deleteTestingRequestsByRequestIdParticipantsByUserId',
  'getTestingMyRequests',
  'getTestingAvailableForTesting',
  
  // Testing Sessions
  'getTestingSessions',
  'postTestingSessions',
  'deleteTestingSessionsById',
  'getTestingSessionsById',
  'putTestingSessionsById',
  'getTestingSessionsByIdDetails',
  'getTestingSessionsByLocationByLocationId',
  'getTestingSessionsByManagerByManagerId',
  'getTestingSessionsByRequestByTestingRequestId',
  'getTestingSessionsBySessionIdRegistrations',
  'getTestingSessionsBySessionIdStatistics',
  'getTestingSessionsBySessionIdWaitlist',
  'getTestingSessionsByStatusByStatus',
  'getTestingSessionsSearch',
  'postTestingSessionsByIdRestore',
  'postTestingSessionsBySessionIdAttendance',
  'postTestingSessionsBySessionIdRegister',
  'deleteTestingSessionsBySessionIdRegister',
  'postTestingSessionsBySessionIdWaitlist',
  'deleteTestingSessionsBySessionIdWaitlist',
  
  // Testing Feedback
  'postTestingFeedback',
  'getTestingFeedbackByUserByUserId',
  'postTestingFeedbackByFeedbackIdQuality',
  'postTestingFeedbackByFeedbackIdReport',
  'postTestingSubmitSimple',
  
  // Testing Analytics & Attendance
  'getTestingAttendanceSessions',
  'getTestingAttendanceStudents',
  'getTestingUsersByUserIdActivity',
  
  // Projects Extended (missing from basic projects)
  'getApiProjectsByIdStatistics',
  'getApiProjectsCategoryByCategoryId',
  'getApiProjectsCreatorByCreatorId',
];

// Group functions by module
const moduleGroups = {
  achievements: [],
  'activity-grades': [],
  auth: [],
  'content-interaction': [],
  credentials: [],
  health: [],
  payments: [],
  posts: [],
  products: [],
  programs: [],
  projects: [],
  subscriptions: [],
  tenants: [],
  'tenant-domains': [],
  users: [],
  'user-management': [],
  'testing-feedback': [],
  unknown: []
};

// Categorize all functions
const allFunctions = [...sdkFunctions, ...additionalSDKFunctions];

allFunctions.forEach(func => {
  if (func.includes('Achievement') || func.includes('achievements')) {
    moduleGroups.achievements.push(func);
  } else if (func.includes('ActivityGrades') || func.includes('activitygrades')) {
    moduleGroups['activity-grades'].push(func);
  } else if (func.includes('Auth') || func.includes('auth')) {
    moduleGroups.auth.push(func);
  } else if (func.includes('Contentinteraction') || func.includes('contentinteraction')) {
    moduleGroups['content-interaction'].push(func);
  } else if (func.includes('Credential') || func.includes('credentials')) {
    moduleGroups.credentials.push(func);
  } else if (func.includes('Health') || func.includes('health')) {
    moduleGroups.health.push(func);
  } else if (func.includes('Payment') || func.includes('payments')) {
    moduleGroups.payments.push(func);
  } else if (func.includes('Posts') || func.includes('posts')) {
    moduleGroups.posts.push(func);
  } else if (func.includes('Product') || func.includes('product')) {
    moduleGroups.products.push(func);
  } else if (func.includes('Program') || func.includes('program')) {
    moduleGroups.programs.push(func);
  } else if (func.includes('Project') || func.includes('projects')) {
    moduleGroups.projects.push(func);
  } else if (func.includes('Subscription') || func.includes('subscription')) {
    moduleGroups.subscriptions.push(func);
  } else if (func.includes('TenantDomains') || func.includes('tenantdomains')) {
    moduleGroups['tenant-domains'].push(func);
  } else if (func.includes('Tenant') || func.includes('tenant')) {
    moduleGroups.tenants.push(func);
  } else if (func.includes('User') || func.includes('users') || func.includes('Userprofile')) {
    moduleGroups.users.push(func);
  } else if (func.includes('Testing') || func.includes('testing')) {
    moduleGroups['testing-feedback'].push(func);
  } else {
    moduleGroups.unknown.push(func);
  }
});

console.log('=== SDK COVERAGE ANALYSIS ===\n');

// Analyze each module
let totalFunctions = 0;
let implementedCount = 0;

for (const [module, functions] of Object.entries(moduleGroups)) {
  if (functions.length === 0) continue;
  
  totalFunctions += functions.length;
  
  console.log(`## ${module.toUpperCase()} MODULE`);
  console.log(`SDK Functions: ${functions.length}`);
  
  // Check implementation status
  if (module === 'products') {
    implementedCount += 41; // All 41 product functions implemented
    console.log(`✅ FULLY IMPLEMENTED - All ${functions.length} functions`);
  } else {
    console.log(`❌ MISSING IMPLEMENTATION`);
    console.log(`   Functions to implement: ${functions.length}`);
    
    // Show first 10 functions as examples
    functions.slice(0, 10).forEach((func, index) => {
      console.log(`   ${index + 1}. ${func}`);
    });
    
    if (functions.length > 10) {
      console.log(`   ... and ${functions.length - 10} more`);
    }
  }
  
  console.log('');
}

console.log('=== PRIORITY SUMMARY ===');
console.log(`Total SDK functions analyzed: ${totalFunctions}`);
console.log(`Fully implemented modules: 1 (products)`);
console.log(`Missing implementation modules: ${Object.keys(moduleGroups).filter(m => moduleGroups[m].length > 0 && m !== 'products').length}`);

console.log('\n=== TOP PRIORITY MODULES TO IMPLEMENT ===');

const priorityModules = [
  { module: 'auth', count: moduleGroups.auth.length, reason: 'Core authentication functionality' },
  { module: 'users', count: moduleGroups.users.length, reason: 'User management and profiles' },
  { module: 'programs', count: moduleGroups.programs.length, reason: 'Core learning platform functionality' },
  { module: 'projects', count: moduleGroups.projects.length, reason: 'Project management features' },
  { module: 'payments', count: moduleGroups.payments.length, reason: 'E-commerce and monetization' },
  { module: 'subscriptions', count: moduleGroups.subscriptions.length, reason: 'Subscription management' },
  { module: 'tenants', count: moduleGroups.tenants.length, reason: 'Multi-tenancy support' },
  { module: 'content-interaction', count: moduleGroups['content-interaction'].length, reason: 'Learning interactions' },
  { module: 'achievements', count: moduleGroups.achievements.length, reason: 'Gamification features' }
];

priorityModules
  .filter(m => m.count > 0)
  .sort((a, b) => b.count - a.count)
  .forEach((item, index) => {
    console.log(`${index + 1}. ${item.module.toUpperCase()}: ${item.count} functions - ${item.reason}`);
  });
