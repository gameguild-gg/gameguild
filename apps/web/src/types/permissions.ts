// Types for the enhanced DAC permission system

export enum PermissionType {
  // Interaction Permissions
  Read = 1,
  Comment = 2,
  Reply = 3,
  Vote = 4,
  Share = 5,
  Report = 6,
  Follow = 7,
  Bookmark = 8,
  React = 9,
  Subscribe = 10,
  Mention = 11,
  Tag = 12,

  // Curation Permissions
  Categorize = 13,
  Collection = 14,
  Series = 15,
  CrossReference = 16,
  Translate = 17,
  Version = 18,
  Template = 19,

  // Lifecycle Permissions
  Create = 20,
  Draft = 21,
  Submit = 22,
  Withdraw = 23,
  Archive = 24,
  Restore = 25,
  Delete = 26,
  SoftDelete = 27,
  HardDelete = 28,
  Backup = 29,
  Migrate = 30,
  Clone = 31,

  // Editorial Permissions
  Edit = 32,
  Proofread = 33,
  FactCheck = 34,
  StyleGuide = 35,
  Plagiarism = 36,
  Seo = 37,
  Accessibility = 38,
  Legal = 39,
  Brand = 40,
  Guidelines = 41,

  // Moderation Permissions
  Review = 42,
  Approve = 43,
  Reject = 44,
  Hide = 45,
  Quarantine = 46,
  Flag = 47,
  Warning = 48,
  Suspend = 49,
  Ban = 50,
  Escalate = 51,

  // Monetization Permissions
  Monetize = 52,
  Paywall = 53,
  Subscription = 54,
  Advertisement = 55,
  Sponsorship = 56,
  Affiliate = 57,
  Commission = 58,
  License = 59,
  Pricing = 60,
  Revenue = 61,

  // Promotion Permissions
  Feature = 62,
  Pin = 63,
  Trending = 64,
  Recommend = 65,
  Spotlight = 66,
  Banner = 67,
  Carousel = 68,
  Widget = 69,
  Email = 70,
  Push = 71,
  Sms = 72,

  // Publishing Permissions
  Publish = 73,
  Unpublish = 74,
  Schedule = 75,
  Reschedule = 76,
  Distribute = 77,
  Syndicate = 78,
  Rss = 79,
  Newsletter = 80,
  SocialMedia = 81,
  Api = 82,

  // Quality Control Permissions
  Score = 83,
  Rate = 84,
  Benchmark = 85,
  Metrics = 86,
  Analytics = 87,
  Performance = 88,
  Feedback = 89,
  Audit = 90,
  Standards = 91,
  Improvement = 92,
}

export enum PermissionSource {
  None = 0,
  GlobalDefault = 1,
  TenantDefault = 2,
  ContentTypeDefault = 3,
  TenantUser = 4,
  ContentTypeUser = 5,
  ResourceDefault = 6,
  ResourceUser = 7,
  SystemOverride = 8,
}

export enum InvitationStatus {
  Pending = 0,
  Accepted = 1,
  Declined = 2,
  Expired = 3,
  Cancelled = 4,
}

export interface PermissionResult {
  isGranted: boolean;
  isExplicitlyDenied: boolean;
  source: PermissionSource;
  grantedBy?: string;
  grantedAt?: Date;
  expiresAt?: Date;
  reason?: string;
  priority: number;
  isInherited: boolean;
}

export interface EffectivePermission {
  permission: PermissionType;
  isGranted: boolean;
  source: PermissionSource;
  sourceDescription: string;
  grantedBy?: string;
  grantedAt?: Date;
  expiresAt?: Date;
  isInherited: boolean;
  isExplicit: boolean;
  priority: number;
}

export interface PermissionLayer {
  source: PermissionSource;
  isGranted?: boolean;
  isDefault: boolean;
  grantedBy?: string;
  grantedAt?: Date;
  expiresAt?: Date;
  priority: number;
  description: string;
}

export interface PermissionHierarchy {
  permission: PermissionType;
  userId: string;
  tenantId?: string;
  resourceId?: string;
  contentTypeName?: string;
  layers: PermissionLayer[];
  finalResult: PermissionResult;
}

export interface ResourceUserPermission {
  userId: string;
  userName: string;
  email: string;
  profilePictureUrl?: string;
  permissions: PermissionType[];
  grantedAt: Date;
  grantedByUserId: string;
  grantedByUserName: string;
  expiresAt?: Date;
  isOwner: boolean;
  source: PermissionSource;
}

export interface ShareResourceRequest {
  userEmails: string[];
  userIds: string[];
  permissions: PermissionType[];
  expiresAt?: Date;
  message?: string;
  requireAcceptance: boolean;
  notifyUsers: boolean;
}

export interface InviteUserRequest {
  email: string;
  permissions: PermissionType[];
  expiresAt?: Date;
  message?: string;
  requireAcceptance: boolean;
}

export interface ShareResult {
  success: boolean;
  errorMessage?: string;
  userResults: UserShareResult[];
  shareId?: string;
}

export interface UserShareResult {
  email?: string;
  userId?: string;
  success: boolean;
  errorMessage?: string;
  invitationSent: boolean;
  invitationId?: string;
}

export interface PermissionUpdateResult {
  success: boolean;
  errorMessage?: string;
  grantedPermissions: PermissionType[];
  revokedPermissions: PermissionType[];
}

export interface InvitationResult {
  success: boolean;
  errorMessage?: string;
  invitationId?: string;
  userExists: boolean;
  emailSent: boolean;
}

export interface ResourceInvitation {
  id: string;
  email: string;
  permissions: PermissionType[];
  invitedAt: Date;
  invitedByUserId: string;
  invitedByUserName: string;
  expiresAt?: Date;
  message?: string;
  status: InvitationStatus;
}

export interface ProjectCollaborator {
  userId: string;
  userName: string;
  email: string;
  profilePictureUrl?: string;
  role: string;
  permissions: PermissionType[];
  joinedAt: Date;
  invitedBy: string;
  isOwner: boolean;
  expiresAt?: Date;
}

export interface ProjectRoleTemplate {
  name: string;
  description: string;
  permissions: PermissionType[];
}

export interface AddCollaboratorRequest {
  email: string;
  permissions: PermissionType[];
  expiresAt?: Date;
  message?: string;
  requireAcceptance: boolean;
}

export interface UpdateCollaboratorRequest {
  permissions: PermissionType[];
  expiresAt?: Date;
}

export interface ShareProjectWithRoleRequest {
  roleName: string;
  userEmails: string[];
  userIds: string[];
  expiresAt?: Date;
  message?: string;
  requireAcceptance: boolean;
  notifyUsers: boolean;
}

// Predefined role templates
export const PROJECT_ROLE_TEMPLATES: ProjectRoleTemplate[] = [
  {
    name: 'Viewer',
    description: 'Can view project content',
    permissions: [PermissionType.Read, PermissionType.Comment],
  },
  {
    name: 'Collaborator',
    description: 'Can edit project content and collaborate',
    permissions: [PermissionType.Read, PermissionType.Edit, PermissionType.Comment, PermissionType.Reply, PermissionType.Share, PermissionType.Create],
  },
  {
    name: 'Editor',
    description: 'Can edit, review, and publish project content',
    permissions: [PermissionType.Read, PermissionType.Edit, PermissionType.Create, PermissionType.Comment, PermissionType.Reply, PermissionType.Share, PermissionType.Review, PermissionType.Approve, PermissionType.Publish],
  },
  {
    name: 'Admin',
    description: 'Full access to project including user management',
    permissions: [
      PermissionType.Read,
      PermissionType.Edit,
      PermissionType.Create,
      PermissionType.Delete,
      PermissionType.Comment,
      PermissionType.Reply,
      PermissionType.Share,
      PermissionType.Review,
      PermissionType.Approve,
      PermissionType.Publish,
      PermissionType.Archive,
      PermissionType.Restore,
    ],
  },
];

// Helper functions for permission management
export const PermissionUtils = {
  getPermissionName: (permission: PermissionType): string => {
    return PermissionType[permission] || `Unknown (${permission})`;
  },

  getSourceDescription: (source: PermissionSource): string => {
    switch (source) {
      case PermissionSource.GlobalDefault:
        return 'Global default permissions';
      case PermissionSource.TenantDefault:
        return 'Tenant default permissions';
      case PermissionSource.ContentTypeDefault:
        return 'Content type default permissions';
      case PermissionSource.TenantUser:
        return 'User tenant permissions';
      case PermissionSource.ContentTypeUser:
        return 'User content type permissions';
      case PermissionSource.ResourceDefault:
        return 'Resource default permissions';
      case PermissionSource.ResourceUser:
        return 'User resource permissions';
      case PermissionSource.SystemOverride:
        return 'System override';
      default:
        return 'Unknown source';
    }
  },

  hasPermission: (permissions: EffectivePermission[], permission: PermissionType): boolean => {
    return permissions.some((p) => p.permission === permission && p.isGranted);
  },

  hasAnyPermission: (permissions: EffectivePermission[], requiredPermissions: PermissionType[]): boolean => {
    return requiredPermissions.some((required) => permissions.some((p) => p.permission === required && p.isGranted));
  },

  hasAllPermissions: (permissions: EffectivePermission[], requiredPermissions: PermissionType[]): boolean => {
    return requiredPermissions.every((required) => permissions.some((p) => p.permission === required && p.isGranted));
  },

  getRoleFromPermissions: (permissions: PermissionType[]): string => {
    const permissionSet = new Set(permissions);

    if (permissionSet.has(PermissionType.Delete) && permissionSet.has(PermissionType.Archive)) {
      return 'Admin';
    }

    if (permissionSet.has(PermissionType.Publish) && permissionSet.has(PermissionType.Review)) {
      return 'Editor';
    }

    if (permissionSet.has(PermissionType.Edit) && permissionSet.has(PermissionType.Create)) {
      return 'Collaborator';
    }

    if (permissionSet.has(PermissionType.Read)) {
      return 'Viewer';
    }

    return 'Custom';
  },

  getRoleTemplate: (roleName: string): ProjectRoleTemplate | undefined => {
    return PROJECT_ROLE_TEMPLATES.find((template) => template.name.toLowerCase() === roleName.toLowerCase());
  },
};
