// Enhanced module-based permission and role system

export enum ModuleType {
  // Core Modules
  Users = 'Users',
  Projects = 'Projects',
  Content = 'Content',
  Analytics = 'Analytics',

  // Feature Modules
  TestingLab = 'TestingLab',
  Programs = 'Programs',
  Products = 'Products',
  Payments = 'Payments',
  Forums = 'Forums',
  Marketplace = 'Marketplace',
  Learning = 'Learning',
  Social = 'Social',

  // Administrative Modules
  UserManagement = 'UserManagement',
  TenantManagement = 'TenantManagement',
  Security = 'Security',
  Reporting = 'Reporting',
  Configuration = 'Configuration',
}

export enum RoleLevel {
  Global = 'Global', // System-wide permissions
  Tenant = 'Tenant', // Tenant-wide permissions
  Module = 'Module', // Module-specific permissions
  Resource = 'Resource', // Individual resource permissions
}

export enum ModuleAction {
  // Basic CRUD
  View = 'View',
  Create = 'Create',
  Edit = 'Edit',
  Delete = 'Delete',

  // Advanced Management
  Manage = 'Manage',
  Administer = 'Administer',
  Configure = 'Configure',

  // Collaboration
  Share = 'Share',
  Invite = 'Invite',
  Assign = 'Assign',
  Review = 'Review',
  Approve = 'Approve',

  // Publishing & Lifecycle
  Publish = 'Publish',
  Archive = 'Archive',
  Restore = 'Restore',

  // Analytics & Reporting
  ViewAnalytics = 'ViewAnalytics',
  ExportData = 'ExportData',

  // Moderation
  Moderate = 'Moderate',
  Hide = 'Hide',
  Ban = 'Ban',
}

// Define what resources exist within each module
export interface ModuleResourceType {
  module: ModuleType;
  resourceTypes: string[];
}

export const MODULE_RESOURCES: ModuleResourceType[] = [
  {
    module: ModuleType.TestingLab,
    resourceTypes: ['TestingSession', 'TestingRequest', 'TestingFeedback', 'TestingLocation', 'TestingProject', 'TestingSubmission'],
  },
  {
    module: ModuleType.Projects,
    resourceTypes: ['Project', 'ProjectCollaborator', 'ProjectFeedback', 'ProjectSubmission'],
  },
  {
    module: ModuleType.Programs,
    resourceTypes: ['Program', 'Track', 'Course', 'Lesson', 'Assignment'],
  },
  {
    module: ModuleType.Products,
    resourceTypes: ['Product', 'ProductReview', 'ProductOrder', 'ProductCategory'],
  },
  {
    module: ModuleType.Content,
    resourceTypes: ['Post', 'Comment', 'Article', 'Media', 'Document'],
  },
];

// Predefined role templates for different modules
export interface ModuleRole {
  id: string;
  name: string;
  displayName: string;
  description: string;
  module: ModuleType;
  level: RoleLevel;
  permissions: ModulePermission[];
  isSystemRole: boolean;
  canBeAssigned: boolean;
}

export interface ModulePermission {
  module: ModuleType;
  resourceType?: string; // If null, applies to entire module
  action: ModuleAction;
  isGranted: boolean;
  constraints?: PermissionConstraint[];
}

export interface PermissionConstraint {
  type: 'ownership' | 'time' | 'location' | 'status' | 'custom';
  value: any;
  description: string;
}

// Testing Lab specific roles
export const TESTING_LAB_ROLES: ModuleRole[] = [
  {
    id: 'testing-lab-admin',
    name: 'TestingLabAdmin',
    displayName: 'Testing Lab Administrator',
    description: 'Full administrative control over all Testing Lab features and resources',
    module: ModuleType.TestingLab,
    level: RoleLevel.Module,
    isSystemRole: true,
    canBeAssigned: true,
    permissions: [
      // Full module permissions
      { module: ModuleType.TestingLab, action: ModuleAction.Administer, isGranted: true },
      { module: ModuleType.TestingLab, action: ModuleAction.Configure, isGranted: true },
      { module: ModuleType.TestingLab, action: ModuleAction.ViewAnalytics, isGranted: true },
      { module: ModuleType.TestingLab, action: ModuleAction.ExportData, isGranted: true },

      // All resource types
      { module: ModuleType.TestingLab, resourceType: 'TestingSession', action: ModuleAction.Create, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingSession', action: ModuleAction.Edit, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingSession', action: ModuleAction.Delete, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingSession', action: ModuleAction.Manage, isGranted: true },

      { module: ModuleType.TestingLab, resourceType: 'TestingRequest', action: ModuleAction.Create, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingRequest', action: ModuleAction.Edit, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingRequest', action: ModuleAction.Delete, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingRequest', action: ModuleAction.Approve, isGranted: true },

      { module: ModuleType.TestingLab, resourceType: 'TestingLocation', action: ModuleAction.Create, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingLocation', action: ModuleAction.Edit, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingLocation', action: ModuleAction.Delete, isGranted: true },
    ],
  },

  {
    id: 'testing-lab-manager',
    name: 'TestingLabManager',
    displayName: 'Testing Lab Manager',
    description: 'Can create, edit and manage testing sessions but cannot delete or configure system settings',
    module: ModuleType.TestingLab,
    level: RoleLevel.Module,
    isSystemRole: true,
    canBeAssigned: true,
    permissions: [
      // Module permissions (limited)
      { module: ModuleType.TestingLab, action: ModuleAction.Manage, isGranted: true },
      { module: ModuleType.TestingLab, action: ModuleAction.ViewAnalytics, isGranted: true },

      // Testing Sessions - create and edit but not delete
      { module: ModuleType.TestingLab, resourceType: 'TestingSession', action: ModuleAction.View, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingSession', action: ModuleAction.Create, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingSession', action: ModuleAction.Edit, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingSession', action: ModuleAction.Archive, isGranted: true },
      // Note: Delete is not granted

      // Testing Requests - full control
      { module: ModuleType.TestingLab, resourceType: 'TestingRequest', action: ModuleAction.View, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingRequest', action: ModuleAction.Create, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingRequest', action: ModuleAction.Edit, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingRequest', action: ModuleAction.Approve, isGranted: true },

      // Testing Feedback - manage
      { module: ModuleType.TestingLab, resourceType: 'TestingFeedback', action: ModuleAction.View, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingFeedback', action: ModuleAction.Edit, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingFeedback', action: ModuleAction.Review, isGranted: true },

      // Locations - view only
      { module: ModuleType.TestingLab, resourceType: 'TestingLocation', action: ModuleAction.View, isGranted: true },
    ],
  },

  {
    id: 'testing-lab-coordinator',
    name: 'TestingLabCoordinator',
    displayName: 'Testing Lab Coordinator',
    description: 'Can coordinate testing sessions and handle user submissions but limited administrative access',
    module: ModuleType.TestingLab,
    level: RoleLevel.Module,
    isSystemRole: true,
    canBeAssigned: true,
    permissions: [
      // Basic module access
      { module: ModuleType.TestingLab, action: ModuleAction.View, isGranted: true },

      // Testing Sessions - limited access
      { module: ModuleType.TestingLab, resourceType: 'TestingSession', action: ModuleAction.View, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingSession', action: ModuleAction.Edit, isGranted: true, constraints: [{ type: 'ownership', value: true, description: 'Can only edit sessions they created' }] },

      // Testing Requests - can handle
      { module: ModuleType.TestingLab, resourceType: 'TestingRequest', action: ModuleAction.View, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingRequest', action: ModuleAction.Edit, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingRequest', action: ModuleAction.Assign, isGranted: true },

      // Testing Submissions - coordinate
      { module: ModuleType.TestingLab, resourceType: 'TestingSubmission', action: ModuleAction.View, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingSubmission', action: ModuleAction.Review, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingSubmission', action: ModuleAction.Edit, isGranted: true },

      // Feedback - limited
      { module: ModuleType.TestingLab, resourceType: 'TestingFeedback', action: ModuleAction.View, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingFeedback', action: ModuleAction.Create, isGranted: true },
    ],
  },

  {
    id: 'testing-lab-tester',
    name: 'TestingLabTester',
    displayName: 'Testing Lab Tester',
    description: 'Can participate in testing sessions and provide feedback',
    module: ModuleType.TestingLab,
    level: RoleLevel.Module,
    isSystemRole: true,
    canBeAssigned: true,
    permissions: [
      // Basic access
      { module: ModuleType.TestingLab, action: ModuleAction.View, isGranted: true },

      // Sessions - participate only
      { module: ModuleType.TestingLab, resourceType: 'TestingSession', action: ModuleAction.View, isGranted: true },

      // Submissions - create and edit own
      { module: ModuleType.TestingLab, resourceType: 'TestingSubmission', action: ModuleAction.Create, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingSubmission', action: ModuleAction.Edit, isGranted: true, constraints: [{ type: 'ownership', value: true, description: 'Can only edit own submissions' }] },
      { module: ModuleType.TestingLab, resourceType: 'TestingSubmission', action: ModuleAction.View, isGranted: true, constraints: [{ type: 'ownership', value: true, description: 'Can only view own submissions' }] },

      // Feedback - create
      { module: ModuleType.TestingLab, resourceType: 'TestingFeedback', action: ModuleAction.Create, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingFeedback', action: ModuleAction.Edit, isGranted: true, constraints: [{ type: 'ownership', value: true, description: 'Can only edit own feedback' }] },
    ],
  },
];

// Global system roles
export const GLOBAL_ROLES: ModuleRole[] = [
  {
    id: 'super-admin',
    name: 'SuperAdmin',
    displayName: 'Super Administrator',
    description: 'Complete system-wide access to all modules and features',
    module: ModuleType.Users, // Belongs to core user management
    level: RoleLevel.Global,
    isSystemRole: true,
    canBeAssigned: true,
    permissions: [
      // Grant all permissions across all modules - this would be expanded programmatically
      { module: ModuleType.TestingLab, action: ModuleAction.Administer, isGranted: true },
      { module: ModuleType.Projects, action: ModuleAction.Administer, isGranted: true },
      { module: ModuleType.Programs, action: ModuleAction.Administer, isGranted: true },
      { module: ModuleType.Products, action: ModuleAction.Administer, isGranted: true },
      { module: ModuleType.UserManagement, action: ModuleAction.Administer, isGranted: true },
      { module: ModuleType.TenantManagement, action: ModuleAction.Administer, isGranted: true },
      { module: ModuleType.Security, action: ModuleAction.Administer, isGranted: true },
    ],
  },

  {
    id: 'tenant-admin',
    name: 'TenantAdmin',
    displayName: 'Tenant Administrator',
    description: 'Administrative access within a specific tenant',
    module: ModuleType.TenantManagement,
    level: RoleLevel.Tenant,
    isSystemRole: true,
    canBeAssigned: true,
    permissions: [
      // Tenant-level module administration
      { module: ModuleType.TestingLab, action: ModuleAction.Administer, isGranted: true },
      { module: ModuleType.Projects, action: ModuleAction.Administer, isGranted: true },
      { module: ModuleType.Programs, action: ModuleAction.Administer, isGranted: true },
      { module: ModuleType.Products, action: ModuleAction.Administer, isGranted: true },
      { module: ModuleType.UserManagement, action: ModuleAction.Manage, isGranted: true },
      { module: ModuleType.Analytics, action: ModuleAction.ViewAnalytics, isGranted: true },
      { module: ModuleType.Reporting, action: ModuleAction.ViewAnalytics, isGranted: true },
    ],
  },

  {
    id: 'content-moderator',
    name: 'ContentModerator',
    displayName: 'Content Moderator',
    description: 'Can moderate content across multiple modules',
    module: ModuleType.Content,
    level: RoleLevel.Global,
    isSystemRole: true,
    canBeAssigned: true,
    permissions: [
      { module: ModuleType.Content, action: ModuleAction.Moderate, isGranted: true },
      { module: ModuleType.Content, action: ModuleAction.Hide, isGranted: true },
      { module: ModuleType.Projects, action: ModuleAction.Moderate, isGranted: true },
      { module: ModuleType.TestingLab, resourceType: 'TestingFeedback', action: ModuleAction.Moderate, isGranted: true },
      { module: ModuleType.Forums, action: ModuleAction.Moderate, isGranted: true },
    ],
  },
];

// Combine all predefined roles
export const ALL_PREDEFINED_ROLES: ModuleRole[] = [
  ...GLOBAL_ROLES,
  ...TESTING_LAB_ROLES,
  // Add other module roles here as they're defined
];

// User role assignment interface
export interface UserRoleAssignment {
  id: string;
  userId: string;
  roleId: string;
  tenantId?: string; // null for global roles
  assignedBy: string;
  assignedAt: Date;
  expiresAt?: Date;
  isActive: boolean;
  scope?: {
    module?: ModuleType;
    resourceType?: string;
    resourceIds?: string[];
  };
}

// Permission check result with detailed context
export interface ModulePermissionResult {
  isGranted: boolean;
  source: 'role' | 'direct' | 'inherited';
  roleId?: string;
  roleName?: string;
  constraints?: PermissionConstraint[];
  reason?: string;
}

export default {
  ModuleType,
  RoleLevel,
  ModuleAction,
  TESTING_LAB_ROLES,
  GLOBAL_ROLES,
  ALL_PREDEFINED_ROLES,
  MODULE_RESOURCES,
};
