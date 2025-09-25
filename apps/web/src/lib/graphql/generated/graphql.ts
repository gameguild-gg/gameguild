/* eslint-disable */
import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
export type Maybe<T> = T | null;
export type InputMaybe<T> = Maybe<T>;
export type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]?: Maybe<T[SubKey]> };
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]: Maybe<T[SubKey]> };
export type MakeEmpty<T extends { [key: string]: unknown }, K extends keyof T> = { [_ in K]?: never };
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
/** All built-in and custom scalars, mapped to their actual values */
export type Scalars = {
  ID: { input: string; output: string; }
  String: { input: string; output: string; }
  Boolean: { input: boolean; output: boolean; }
  Int: { input: number; output: number; }
  Float: { input: number; output: number; }
  /** The `DateTime` scalar represents an ISO-8601 compliant date time type. */
  DateTime: { input: any; output: any; }
  /** The `Decimal` scalar type represents a decimal floating-point number. */
  Decimal: { input: any; output: any; }
  /** The `Long` scalar type represents non-fractional signed whole 64-bit numeric values. Long can represent values between -(2^63) and 2^63 - 1. */
  Long: { input: any; output: any; }
  UUID: { input: any; output: any; }
};

export enum AccessLevel {
  Private = 'PRIVATE',
  Protected = 'PROTECTED',
  Public = 'PUBLIC',
  Restricted = 'RESTRICTED',
  Unlisted = 'UNLISTED'
}

/** Represents a gamification achievement that users can earn */
export type Achievement = {
  __typename?: 'Achievement';
  /** The category this achievement belongs to */
  category: Scalars['String']['output'];
  /** Color associated with the achievement */
  color?: Maybe<Scalars['String']['output']>;
  /** Conditions required to earn this achievement */
  conditions?: Maybe<Scalars['String']['output']>;
  /** When the achievement was created */
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  /** The description of what the achievement represents */
  description?: Maybe<Scalars['String']['output']>;
  /** Display order for sorting achievements */
  displayOrder: Scalars['Int']['output'];
  domainEvents: Array<IDomainEvent>;
  /** Total number of times this achievement has been earned */
  earnCount?: Maybe<Scalars['Int']['output']>;
  /** URL to the achievement icon/image */
  iconUrl?: Maybe<Scalars['String']['output']>;
  /** The unique identifier of the achievement */
  id: Scalars['UUID']['output'];
  /** Whether the achievement is active and can be earned */
  isActive: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  /** Whether this achievement can be earned multiple times */
  isRepeatable: Scalars['Boolean']['output'];
  /** Whether this is a secret achievement */
  isSecret: Scalars['Boolean']['output'];
  /** Achievement levels if this is a multi-level achievement */
  levels: Array<AchievementLevel>;
  /** The name of the achievement */
  name: Scalars['String']['output'];
  /** Points awarded for earning this achievement */
  points: Scalars['Int']['output'];
  /** Prerequisites required before this achievement can be earned */
  prerequisites: Array<AchievementPrerequisite>;
  /** Statistics about this achievement */
  statistics?: Maybe<AchievementStatistics>;
  tenant?: Maybe<Tenant>;
  tenantId?: Maybe<Scalars['UUID']['output']>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  /** The type of achievement (badge, trophy, milestone, etc.) */
  type: Scalars['String']['output'];
  /** When the achievement was last updated */
  updatedAt: Scalars['DateTime']['output'];
  /** Users who have earned this achievement */
  userAchievements?: Maybe<Array<Maybe<UserAchievement>>>;
  version: Scalars['Int']['output'];
};

export type AchievementDto = {
  __typename?: 'AchievementDto';
  category: Scalars['String']['output'];
  color?: Maybe<Scalars['String']['output']>;
  conditions?: Maybe<Scalars['String']['output']>;
  createdAt: Scalars['DateTime']['output'];
  description?: Maybe<Scalars['String']['output']>;
  displayOrder: Scalars['Int']['output'];
  iconUrl?: Maybe<Scalars['String']['output']>;
  id: Scalars['UUID']['output'];
  isActive: Scalars['Boolean']['output'];
  isRepeatable: Scalars['Boolean']['output'];
  isSecret: Scalars['Boolean']['output'];
  levels?: Maybe<Array<AchievementLevelDto>>;
  name: Scalars['String']['output'];
  points: Scalars['Int']['output'];
  prerequisites?: Maybe<Array<AchievementDto>>;
  type: Scalars['String']['output'];
  updatedAt: Scalars['DateTime']['output'];
};

/** Represents a level within a multi-level achievement */
export type AchievementLevel = {
  __typename?: 'AchievementLevel';
  /** The achievement this level belongs to */
  achievement?: Maybe<Achievement>;
  achievementId: Scalars['UUID']['output'];
  /** Color specific to this level */
  color?: Maybe<Scalars['String']['output']>;
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  /** Description of what this level represents */
  description?: Maybe<Scalars['String']['output']>;
  domainEvents: Array<IDomainEvent>;
  /** Icon specific to this level */
  iconUrl?: Maybe<Scalars['String']['output']>;
  /** The unique identifier of the achievement level */
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  /** The level number */
  level: Scalars['Int']['output'];
  /** The name of this level */
  name: Scalars['String']['output'];
  /** Points awarded for reaching this level */
  points: Scalars['Int']['output'];
  /** Progress required to reach this level */
  requiredProgress: Scalars['Int']['output'];
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

export type AchievementLevelDto = {
  __typename?: 'AchievementLevelDto';
  color?: Maybe<Scalars['String']['output']>;
  description?: Maybe<Scalars['String']['output']>;
  iconUrl?: Maybe<Scalars['String']['output']>;
  id: Scalars['UUID']['output'];
  level: Scalars['Int']['output'];
  name: Scalars['String']['output'];
  points: Scalars['Int']['output'];
  requiredProgress: Scalars['Int']['output'];
};

export type AchievementPopularityDto = {
  __typename?: 'AchievementPopularityDto';
  achievementId: Scalars['UUID']['output'];
  earnRate: Scalars['Float']['output'];
  name: Scalars['String']['output'];
  timesEarned: Scalars['Int']['output'];
};

export type AchievementPrerequisite = {
  __typename?: 'AchievementPrerequisite';
  achievement?: Maybe<Achievement>;
  achievementId: Scalars['UUID']['output'];
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  minimumLevel?: Maybe<Scalars['Int']['output']>;
  prerequisiteAchievement?: Maybe<Achievement>;
  prerequisiteAchievementId: Scalars['UUID']['output'];
  requiresCompletion: Scalars['Boolean']['output'];
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

export type AchievementPrerequisiteCheckDto = {
  __typename?: 'AchievementPrerequisiteCheckDto';
  achievementId: Scalars['UUID']['output'];
  canEarn: Scalars['Boolean']['output'];
  prerequisites: Array<PrerequisiteStatusDto>;
};

/** Represents a user's progress towards an achievement */
export type AchievementProgress = {
  __typename?: 'AchievementProgress';
  /** The achievement being progressed towards */
  achievement?: Maybe<Achievement>;
  /** The ID of the achievement being progressed towards */
  achievementId: Scalars['UUID']['output'];
  /** Additional context data */
  context?: Maybe<Scalars['String']['output']>;
  createdAt: Scalars['DateTime']['output'];
  /** Current progress value */
  currentProgress: Scalars['Int']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  /** The unique identifier of the achievement progress */
  id: Scalars['UUID']['output'];
  /** Whether this achievement has been completed */
  isCompleted: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  /** When progress was last updated */
  lastUpdated: Scalars['DateTime']['output'];
  /** Progress as a percentage */
  progressPercentage?: Maybe<Scalars['Float']['output']>;
  /** Target progress required for completion */
  targetProgress: Scalars['Int']['output'];
  tenant?: Maybe<Tenant>;
  tenantId?: Maybe<Scalars['UUID']['output']>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  /** The user making progress */
  user?: Maybe<User>;
  /** The ID of the user making progress */
  userId?: Maybe<Scalars['UUID']['output']>;
  version: Scalars['Int']['output'];
};

export type AchievementProgressDto = {
  __typename?: 'AchievementProgressDto';
  achievement?: Maybe<AchievementDto>;
  achievementId: Scalars['UUID']['output'];
  context?: Maybe<Scalars['String']['output']>;
  currentProgress: Scalars['Int']['output'];
  id: Scalars['UUID']['output'];
  isCompleted: Scalars['Boolean']['output'];
  lastUpdated: Scalars['DateTime']['output'];
  progressPercentage: Scalars['Float']['output'];
  targetProgress: Scalars['Int']['output'];
  userId: Scalars['UUID']['output'];
};

/** Statistics about achievements */
export type AchievementStatistics = {
  __typename?: 'AchievementStatistics';
  achievementId: Scalars['UUID']['output'];
  /** Achievements grouped by category */
  achievementsByCategory: Array<KeyValuePairOfStringAndInt32>;
  /** Achievements grouped by type */
  achievementsByType: Array<KeyValuePairOfStringAndInt32>;
  /** Number of active achievements */
  activeAchievements: Scalars['Int']['output'];
  completionRate: Scalars['Float']['output'];
  firstEarned?: Maybe<Scalars['DateTime']['output']>;
  inProgress: Scalars['Int']['output'];
  lastEarned?: Maybe<Scalars['DateTime']['output']>;
  /** Most frequently earned achievements */
  mostEarnedAchievements: Array<AchievementPopularityDto>;
  /** Rarest achievements */
  rarestAchievements: Array<AchievementPopularityDto>;
  /** Number of repeatable achievements */
  repeatableAchievements: Scalars['Int']['output'];
  /** Number of secret achievements */
  secretAchievements: Scalars['Int']['output'];
  /** Total number of achievements */
  totalAchievements: Scalars['Int']['output'];
  /** Total number of achievements awarded */
  totalAchievementsAwarded: Scalars['Int']['output'];
  totalEarned: Scalars['Int']['output'];
  totalUsers: Scalars['Int']['output'];
  /** Number of users who have earned achievements */
  usersWithAchievements: Scalars['Int']['output'];
};

export type AchievementsPageDto = {
  __typename?: 'AchievementsPageDto';
  achievements: Array<AchievementDto>;
  hasNextPage: Scalars['Boolean']['output'];
  hasPreviousPage: Scalars['Boolean']['output'];
  pageNumber: Scalars['Int']['output'];
  pageSize: Scalars['Int']['output'];
  totalCount: Scalars['Int']['output'];
};

export type ActivityGrade = {
  __typename?: 'ActivityGrade';
  contentInteraction: ContentInteraction;
  contentInteractionId: Scalars['UUID']['output'];
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  feedback?: Maybe<Scalars['String']['output']>;
  grade: Scalars['Decimal']['output'];
  gradedAt: Scalars['DateTime']['output'];
  graderProgramUser: ProgramUser;
  graderProgramUserId: Scalars['UUID']['output'];
  gradingDetails?: Maybe<Scalars['String']['output']>;
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

/** Defines when a policy shall be executed. */
export enum ApplyPolicy {
  /** After the resolver was executed. */
  AfterResolver = 'AFTER_RESOLVER',
  /** Before the resolver was executed. */
  BeforeResolver = 'BEFORE_RESOLVER',
  /** The policy is applied in the validation step before the execution. */
  Validation = 'VALIDATION'
}

export type AwardAchievementInput = {
  achievementId: Scalars['UUID']['input'];
  context?: InputMaybe<Scalars['String']['input']>;
  level?: InputMaybe<Scalars['Int']['input']>;
  maxProgress: Scalars['Int']['input'];
  notifyUser: Scalars['Boolean']['input'];
  progress: Scalars['Int']['input'];
  userId: Scalars['UUID']['input'];
};

export enum BillingCycle {
  Annually = 'ANNUALLY',
  Biannually = 'BIANNUALLY',
  Monthly = 'MONTHLY',
  Quarterly = 'QUARTERLY',
  SemiAnnually = 'SEMI_ANNUALLY'
}

export type BulkAwardAchievementInput = {
  achievementId: Scalars['UUID']['input'];
  context?: InputMaybe<Scalars['String']['input']>;
  notifyUsers: Scalars['Boolean']['input'];
  userCriteria?: InputMaybe<Scalars['String']['input']>;
  userIds?: InputMaybe<Array<Scalars['UUID']['input']>>;
};

export type BundleManagementInput = {
  bundleId: Scalars['UUID']['input'];
  productId: Scalars['UUID']['input'];
};

export enum CancellationReason {
  AccountSuspension = 'ACCOUNT_SUSPENSION',
  Administrative = 'ADMINISTRATIVE',
  Fraud = 'FRAUD',
  Other = 'OTHER',
  PaymentFailure = 'PAYMENT_FAILURE',
  PlanDowngrade = 'PLAN_DOWNGRADE',
  PlanUpgrade = 'PLAN_UPGRADE',
  TermsViolation = 'TERMS_VIOLATION',
  TrialEnded = 'TRIAL_ENDED',
  UserRequested = 'USER_REQUESTED'
}

export type Certificate = {
  __typename?: 'Certificate';
  certificateTags: Array<CertificateTag>;
  certificateTemplate?: Maybe<Scalars['String']['output']>;
  completionPercentage: Scalars['Decimal']['output'];
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  description: Scalars['String']['output'];
  domainEvents: Array<IDomainEvent>;
  id: Scalars['UUID']['output'];
  isActive: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  minimumGrade?: Maybe<Scalars['Decimal']['output']>;
  minimumRating?: Maybe<Scalars['Decimal']['output']>;
  name: Scalars['String']['output'];
  product?: Maybe<Product>;
  productId?: Maybe<Scalars['UUID']['output']>;
  program?: Maybe<Program>;
  programId?: Maybe<Scalars['UUID']['output']>;
  requiresFeedback: Scalars['Boolean']['output'];
  requiresRating: Scalars['Boolean']['output'];
  tenant?: Maybe<Tenant>;
  tenantId?: Maybe<Scalars['UUID']['output']>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  type: CertificateType;
  updatedAt: Scalars['DateTime']['output'];
  userCertificates: Array<UserCertificate>;
  validityDays?: Maybe<Scalars['Int']['output']>;
  verificationMethod: VerificationMethod;
  version: Scalars['Int']['output'];
};

export type CertificateBlockchainAnchor = {
  __typename?: 'CertificateBlockchainAnchor';
  anchoredAt: Scalars['DateTime']['output'];
  blockHash?: Maybe<Scalars['String']['output']>;
  blockNumber?: Maybe<Scalars['Long']['output']>;
  blockchainNetwork: Scalars['String']['output'];
  certificate: UserCertificate;
  certificateId: Scalars['UUID']['output'];
  confirmedAt?: Maybe<Scalars['DateTime']['output']>;
  contractAddress?: Maybe<Scalars['String']['output']>;
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  status: Scalars['String']['output'];
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  tokenId?: Maybe<Scalars['String']['output']>;
  transactionHash: Scalars['String']['output'];
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

export enum CertificateStatus {
  Active = 'ACTIVE',
  Expired = 'EXPIRED',
  Pending = 'PENDING',
  Revoked = 'REVOKED'
}

export type CertificateTag = {
  __typename?: 'CertificateTag';
  certificate: Certificate;
  certificateId: Scalars['UUID']['output'];
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  relationshipType: CertificateTagRelationshipType;
  tag: TagProficiency;
  tagId: Scalars['UUID']['output'];
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

export enum CertificateTagRelationshipType {
  Demonstrates = 'DEMONSTRATES',
  Optional = 'OPTIONAL',
  Required = 'REQUIRED'
}

export enum CertificateType {
  Achievement = 'ACHIEVEMENT',
  AssessmentPassed = 'ASSESSMENT_PASSED',
  EventParticipation = 'EVENT_PARTICIPATION',
  Instructor = 'INSTRUCTOR',
  LearningPathway = 'LEARNING_PATHWAY',
  PeerRecognition = 'PEER_RECOGNITION',
  ProductBundleCompletion = 'PRODUCT_BUNDLE_COMPLETION',
  Professional = 'PROFESSIONAL',
  ProgramCompletion = 'PROGRAM_COMPLETION',
  ProjectCompletion = 'PROJECT_COMPLETION',
  SkillMastery = 'SKILL_MASTERY',
  Specialization = 'SPECIALIZATION',
  TimeInvestment = 'TIME_INVESTMENT'
}

export type CompleteContentInput = {
  interactionId: Scalars['UUID']['input'];
};

export type Content = {
  __typename?: 'Content';
  addLocalization: ResourceLocalization;
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  description?: Maybe<Scalars['String']['output']>;
  domainEvents: Array<IDomainEvent>;
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  licenses: Array<ContentLicense>;
  localizations: Array<ResourceLocalization>;
  metadata?: Maybe<ResourceMetadata>;
  slug: Scalars['String']['output'];
  status: ContentStatus;
  tenant?: Maybe<Tenant>;
  title: Scalars['String']['output'];
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
  visibility: AccessLevel;
};


export type ContentAddLocalizationArgs = {
  content: Scalars['String']['input'];
  fieldName: Scalars['String']['input'];
  language: LanguageInput;
  status?: LocalizationStatus;
};

/** Represents a user's interaction with program content */
export type ContentInteraction = {
  __typename?: 'ContentInteraction';
  /** Activity grades associated with this interaction */
  activityGrades: Array<ActivityGrade>;
  /** Whether this interaction can still be modified (not submitted) */
  canModify: Scalars['Boolean']['output'];
  /** Date when user completed this content */
  completedAt?: Maybe<Scalars['DateTime']['output']>;
  /** Completion percentage for this specific content (0-100) */
  completionPercentage: Scalars['Decimal']['output'];
  /** The program content being interacted with */
  content: ProgramContent;
  /** ID of the content being interacted with */
  contentId: Scalars['UUID']['output'];
  /** When the interaction was created */
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  /** Duration between first and last access in minutes */
  durationInMinutes?: Maybe<Scalars['Int']['output']>;
  /** Date when user first accessed this content */
  firstAccessedAt?: Maybe<Scalars['DateTime']['output']>;
  /** Unique identifier for the content interaction */
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  /** Whether this interaction has been submitted and is now immutable */
  isSubmitted: Scalars['Boolean']['output'];
  /** Date when user last accessed this content */
  lastAccessedAt?: Maybe<Scalars['DateTime']['output']>;
  /** The program user who is interacting with the content */
  programUser: ProgramUser;
  /** ID of the program user who is interacting with the content */
  programUserId: Scalars['UUID']['output'];
  /** Current progress status of the interaction */
  status: ProgressStatus;
  /** JSON data containing user's submission or response to the content */
  submissionData?: Maybe<Scalars['String']['output']>;
  /** Date when user submitted their work (for gradeable content). Once submitted, interaction becomes immutable */
  submittedAt?: Maybe<Scalars['DateTime']['output']>;
  tenant?: Maybe<Tenant>;
  /** Time spent on this content in minutes */
  timeSpentMinutes?: Maybe<Scalars['Int']['output']>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  /** When the interaction was last updated */
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

export type ContentInteractionResult = {
  __typename?: 'ContentInteractionResult';
  error?: Maybe<Scalars['String']['output']>;
  interaction?: Maybe<ContentInteraction>;
  success: Scalars['Boolean']['output'];
};

export type ContentInteractionStats = {
  __typename?: 'ContentInteractionStats';
  averageCompletionPercentage: Scalars['Decimal']['output'];
  averageTimeSpentMinutes: Scalars['Decimal']['output'];
  completedInteractions: Scalars['Int']['output'];
  inProgressInteractions: Scalars['Int']['output'];
  programId: Scalars['UUID']['output'];
  submittedInteractions: Scalars['Int']['output'];
  totalInteractions: Scalars['Int']['output'];
};

export type ContentLicense = {
  __typename?: 'ContentLicense';
  addLocalization: ResourceLocalization;
  contents: Array<Content>;
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  description?: Maybe<Scalars['String']['output']>;
  domainEvents: Array<IDomainEvent>;
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  localizations: Array<ResourceLocalization>;
  metadata?: Maybe<ResourceMetadata>;
  tenant?: Maybe<Tenant>;
  title: Scalars['String']['output'];
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  url?: Maybe<Scalars['String']['output']>;
  version: Scalars['Int']['output'];
  visibility: AccessLevel;
};


export type ContentLicenseAddLocalizationArgs = {
  content: Scalars['String']['input'];
  fieldName: Scalars['String']['input'];
  language: LanguageInput;
  status?: LocalizationStatus;
};

export enum ContentStatus {
  Archived = 'ARCHIVED',
  Draft = 'DRAFT',
  Published = 'PUBLISHED',
  UnderReview = 'UNDER_REVIEW'
}

export type CreateAchievementInput = {
  category: Scalars['String']['input'];
  color?: InputMaybe<Scalars['String']['input']>;
  conditions?: InputMaybe<Scalars['String']['input']>;
  description?: InputMaybe<Scalars['String']['input']>;
  displayOrder: Scalars['Int']['input'];
  iconUrl?: InputMaybe<Scalars['String']['input']>;
  isActive: Scalars['Boolean']['input'];
  isRepeatable: Scalars['Boolean']['input'];
  isSecret: Scalars['Boolean']['input'];
  levels?: InputMaybe<Array<CreateAchievementLevelInput>>;
  name: Scalars['String']['input'];
  points: Scalars['Int']['input'];
  prerequisiteAchievementIds?: InputMaybe<Array<Scalars['UUID']['input']>>;
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
  type: Scalars['String']['input'];
};

export type CreateAchievementLevelInput = {
  color?: InputMaybe<Scalars['String']['input']>;
  description?: InputMaybe<Scalars['String']['input']>;
  iconUrl?: InputMaybe<Scalars['String']['input']>;
  level: Scalars['Int']['input'];
  name: Scalars['String']['input'];
  points: Scalars['Int']['input'];
  requiredProgress: Scalars['Int']['input'];
};

export type CreateProductInput = {
  isBundle: Scalars['Boolean']['input'];
  name: Scalars['String']['input'];
  shortDescription?: InputMaybe<Scalars['String']['input']>;
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
  type: ProductType;
};

export type CreateProgramInput = {
  category?: InputMaybe<ProgramCategory>;
  description?: InputMaybe<Scalars['String']['input']>;
  difficulty?: InputMaybe<ProgramDifficulty>;
  estimatedHours?: InputMaybe<Scalars['Float']['input']>;
  thumbnail?: InputMaybe<Scalars['String']['input']>;
  title: Scalars['String']['input'];
  videoShowcaseUrl?: InputMaybe<Scalars['String']['input']>;
  visibility?: InputMaybe<AccessLevel>;
};

export type CreatePromoCodeInput = {
  code: Scalars['String']['input'];
  discountPercentage: Scalars['Decimal']['input'];
  discountType: PromoCodeType;
  discountValue: Scalars['Decimal']['input'];
  expiryDate?: InputMaybe<Scalars['DateTime']['input']>;
  maxUses?: InputMaybe<Scalars['Int']['input']>;
  productId: Scalars['UUID']['input'];
  validFrom?: InputMaybe<Scalars['DateTime']['input']>;
  validUntil?: InputMaybe<Scalars['DateTime']['input']>;
};

export type Credential = {
  __typename?: 'Credential';
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  expiresAt?: Maybe<Scalars['DateTime']['output']>;
  id: Scalars['UUID']['output'];
  isActive: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isExpired: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  isValid: Scalars['Boolean']['output'];
  lastUsedAt?: Maybe<Scalars['DateTime']['output']>;
  metadata?: Maybe<Scalars['String']['output']>;
  tenant?: Maybe<Tenant>;
  tenantId?: Maybe<Scalars['UUID']['output']>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  type: Scalars['String']['output'];
  updatedAt: Scalars['DateTime']['output'];
  user: User;
  userId: Scalars['UUID']['output'];
  value: Scalars['String']['output'];
  version: Scalars['Int']['output'];
};

export enum DacPermissionLevel {
  ContentType = 'CONTENT_TYPE',
  Resource = 'RESOURCE',
  Tenant = 'TENANT'
}

export type EmailAddress = {
  __typename?: 'EmailAddress';
  value: Scalars['String']['output'];
};

export enum EnrollmentStatus {
  Active = 'ACTIVE',
  Cancelled = 'CANCELLED',
  Closed = 'CLOSED',
  Completed = 'COMPLETED',
  Expired = 'EXPIRED',
  InviteOnly = 'INVITE_ONLY',
  Open = 'OPEN',
  Paused = 'PAUSED',
  Waitlist = 'WAITLIST'
}

export type FinancialTransaction = {
  __typename?: 'FinancialTransaction';
  amount: Scalars['Decimal']['output'];
  createdAt: Scalars['DateTime']['output'];
  currency: Scalars['String']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  description?: Maybe<Scalars['String']['output']>;
  domainEvents: Array<IDomainEvent>;
  error?: Maybe<Scalars['String']['output']>;
  externalTransactionId?: Maybe<Scalars['String']['output']>;
  failedAt?: Maybe<Scalars['DateTime']['output']>;
  failureReason?: Maybe<Scalars['String']['output']>;
  fromUser?: Maybe<User>;
  fromUserId?: Maybe<Scalars['UUID']['output']>;
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  metadata?: Maybe<Scalars['String']['output']>;
  netAmount?: Maybe<Scalars['Decimal']['output']>;
  paymentMethod?: Maybe<UserFinancialMethod>;
  paymentMethodId?: Maybe<Scalars['UUID']['output']>;
  platformFee?: Maybe<Scalars['Decimal']['output']>;
  processedAt?: Maybe<Scalars['DateTime']['output']>;
  processorFee?: Maybe<Scalars['Decimal']['output']>;
  promoCode?: Maybe<PromoCode>;
  promoCodeId?: Maybe<Scalars['UUID']['output']>;
  promoCodeUses: Array<PromoCodeUse>;
  status: TransactionStatus;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  toUser?: Maybe<User>;
  toUserId?: Maybe<Scalars['UUID']['output']>;
  type: TransactionType;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

export enum GradingMethod {
  Ai = 'AI',
  AutomatedTests = 'AUTOMATED_TESTS',
  Instructor = 'INSTRUCTOR',
  Peer = 'PEER'
}

export type GrantProductAccessInput = {
  acquisitionType: ProductAcquisitionType;
  currency?: InputMaybe<Scalars['String']['input']>;
  expiresAt?: InputMaybe<Scalars['DateTime']['input']>;
  productId: Scalars['UUID']['input'];
  purchasePrice: Scalars['Decimal']['input'];
  userId: Scalars['UUID']['input'];
};

export type IDomainEvent = {
  aggregateId: Scalars['UUID']['output'];
  aggregateType: Scalars['String']['output'];
  eventId: Scalars['UUID']['output'];
  occurredAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

export type KeyValuePairOfStringAndInt32 = {
  __typename?: 'KeyValuePairOfStringAndInt32';
  key: Scalars['String']['output'];
  value: Scalars['Int']['output'];
};

export type KeyValuePairOfStringAndObject = {
  __typename?: 'KeyValuePairOfStringAndObject';
  key: Scalars['String']['output'];
};

export type Language = {
  __typename?: 'Language';
  code: Scalars['String']['output'];
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  id: Scalars['UUID']['output'];
  isActive: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  name: Scalars['String']['output'];
  resourceLocalizations: Array<ResourceLocalization>;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

export type LanguageInput = {
  code: Scalars['String']['input'];
  createdAt: Scalars['DateTime']['input'];
  deletedAt?: InputMaybe<Scalars['DateTime']['input']>;
  id: Scalars['UUID']['input'];
  isActive: Scalars['Boolean']['input'];
  name: Scalars['String']['input'];
  resourceLocalizations: Array<ResourceLocalizationInput>;
  tenant?: InputMaybe<TenantInput>;
  updatedAt: Scalars['DateTime']['input'];
  version: Scalars['Int']['input'];
};

export enum LocalizationStatus {
  Archived = 'ARCHIVED',
  Draft = 'DRAFT',
  MachineTranslated = 'MACHINE_TRANSLATED',
  NeedsReview = 'NEEDS_REVIEW',
  Published = 'PUBLISHED'
}

export type Money = {
  __typename?: 'Money';
  amount: Scalars['Decimal']['output'];
  currency: Scalars['String']['output'];
  toDecimal: Scalars['Decimal']['output'];
};

export type Mutation = {
  __typename?: 'Mutation';
  addToBundle?: Maybe<Product>;
  archiveProduct?: Maybe<Product>;
  awardAchievement: UserAchievement;
  bulkAwardAchievement: Array<UserAchievement>;
  completeContentInteraction: ContentInteractionResult;
  createAchievement: Achievement;
  createContent: ProgramContent;
  createProduct: Product;
  createProgram: Program;
  createPromoCode: PromoCode;
  deleteAchievement: Scalars['Boolean']['output'];
  deleteContent: Scalars['Boolean']['output'];
  deleteProduct: Scalars['Boolean']['output'];
  deleteProgram: Scalars['Boolean']['output'];
  deletePromoCode: Scalars['Boolean']['output'];
  grantUserAccess: UserProduct;
  healthMutation: Scalars['String']['output'];
  markAchievementNotified: Scalars['Boolean']['output'];
  moveContent: Scalars['Boolean']['output'];
  publishProduct?: Maybe<Product>;
  publishProgram: Program;
  removeFromBundle?: Maybe<Product>;
  reorderContent: Scalars['Boolean']['output'];
  revokeAchievement: Scalars['Boolean']['output'];
  revokeUserAccess: Scalars['Boolean']['output'];
  setProductPricing: ProductPricing;
  setProductVisibility?: Maybe<Product>;
  startContentInteraction: ContentInteractionResult;
  submitContentInteraction: ContentInteractionResult;
  unpublishProduct?: Maybe<Product>;
  updateAchievement: Achievement;
  updateAchievementProgress: AchievementProgress;
  updateContent: ProgramContent;
  updateContentProgress: ContentInteractionResult;
  updateProduct?: Maybe<Product>;
  updateProductPricing?: Maybe<ProductPricing>;
  updateProgram: Program;
  updatePromoCode?: Maybe<PromoCode>;
  updateTimeSpent: ContentInteractionResult;
  usePromoCode: PromoCodeUse;
};


export type MutationAddToBundleArgs = {
  input: BundleManagementInput;
};


export type MutationArchiveProductArgs = {
  id: Scalars['UUID']['input'];
};


export type MutationAwardAchievementArgs = {
  input: AwardAchievementInput;
};


export type MutationBulkAwardAchievementArgs = {
  input: BulkAwardAchievementInput;
};


export type MutationCompleteContentInteractionArgs = {
  input: CompleteContentInput;
  programId: Scalars['UUID']['input'];
};


export type MutationCreateAchievementArgs = {
  input: CreateAchievementInput;
};


export type MutationCreateContentArgs = {
  body: Scalars['String']['input'];
  description: Scalars['String']['input'];
  estimatedMinutes: Scalars['Int']['input'];
  gradingMethod?: InputMaybe<GradingMethod>;
  isRequired?: Scalars['Boolean']['input'];
  maxPoints?: Scalars['Int']['input'];
  parentId?: InputMaybe<Scalars['UUID']['input']>;
  programId: Scalars['UUID']['input'];
  sortOrder?: InputMaybe<Scalars['Int']['input']>;
  title: Scalars['String']['input'];
  type: ProgramContentType;
  visibility?: Visibility;
};


export type MutationCreateProductArgs = {
  input: CreateProductInput;
};


export type MutationCreateProgramArgs = {
  input: CreateProgramInput;
};


export type MutationCreatePromoCodeArgs = {
  input: CreatePromoCodeInput;
};


export type MutationDeleteAchievementArgs = {
  achievementId: Scalars['UUID']['input'];
};


export type MutationDeleteContentArgs = {
  contentId: Scalars['UUID']['input'];
  programId: Scalars['UUID']['input'];
};


export type MutationDeleteProductArgs = {
  id: Scalars['UUID']['input'];
};


export type MutationDeleteProgramArgs = {
  id: Scalars['UUID']['input'];
};


export type MutationDeletePromoCodeArgs = {
  id: Scalars['UUID']['input'];
};


export type MutationGrantUserAccessArgs = {
  input: GrantProductAccessInput;
};


export type MutationMarkAchievementNotifiedArgs = {
  userAchievementId: Scalars['UUID']['input'];
};


export type MutationMoveContentArgs = {
  contentId: Scalars['UUID']['input'];
  newParentId?: InputMaybe<Scalars['UUID']['input']>;
  newSortOrder?: Scalars['Int']['input'];
  programId: Scalars['UUID']['input'];
};


export type MutationPublishProductArgs = {
  id: Scalars['UUID']['input'];
};


export type MutationPublishProgramArgs = {
  id: Scalars['UUID']['input'];
};


export type MutationRemoveFromBundleArgs = {
  input: BundleManagementInput;
};


export type MutationReorderContentArgs = {
  contentIds: Array<Scalars['UUID']['input']>;
  programId: Scalars['UUID']['input'];
  sortOrders: Array<Scalars['Int']['input']>;
};


export type MutationRevokeAchievementArgs = {
  input: RevokeAchievementInput;
};


export type MutationRevokeUserAccessArgs = {
  productId: Scalars['UUID']['input'];
  userId: Scalars['UUID']['input'];
};


export type MutationSetProductPricingArgs = {
  input: SetProductPricingInput;
};


export type MutationSetProductVisibilityArgs = {
  id: Scalars['UUID']['input'];
  visibility: AccessLevel;
};


export type MutationStartContentInteractionArgs = {
  input: StartContentInput;
  programId: Scalars['UUID']['input'];
};


export type MutationSubmitContentInteractionArgs = {
  input: SubmitContentInput;
  programId: Scalars['UUID']['input'];
};


export type MutationUnpublishProductArgs = {
  id: Scalars['UUID']['input'];
};


export type MutationUpdateAchievementArgs = {
  input: UpdateAchievementInput;
};


export type MutationUpdateAchievementProgressArgs = {
  input: UpdateAchievementProgressInput;
};


export type MutationUpdateContentArgs = {
  body?: InputMaybe<Scalars['String']['input']>;
  contentId: Scalars['UUID']['input'];
  description?: InputMaybe<Scalars['String']['input']>;
  estimatedMinutes?: InputMaybe<Scalars['Int']['input']>;
  gradingMethod?: InputMaybe<GradingMethod>;
  isRequired?: InputMaybe<Scalars['Boolean']['input']>;
  maxPoints?: InputMaybe<Scalars['Int']['input']>;
  programId: Scalars['UUID']['input'];
  sortOrder?: InputMaybe<Scalars['Int']['input']>;
  title?: InputMaybe<Scalars['String']['input']>;
  type?: InputMaybe<ProgramContentType>;
  visibility?: InputMaybe<Visibility>;
};


export type MutationUpdateContentProgressArgs = {
  input: UpdateProgressInput;
  programId: Scalars['UUID']['input'];
};


export type MutationUpdateProductArgs = {
  input: UpdateProductInput;
};


export type MutationUpdateProductPricingArgs = {
  input: UpdateProductPricingInput;
};


export type MutationUpdateProgramArgs = {
  id: Scalars['UUID']['input'];
  input: UpdateProgramInput;
};


export type MutationUpdatePromoCodeArgs = {
  input: UpdatePromoCodeInput;
};


export type MutationUpdateTimeSpentArgs = {
  input: UpdateTimeSpentInput;
  programId: Scalars['UUID']['input'];
};


export type MutationUsePromoCodeArgs = {
  code: Scalars['String']['input'];
  discountAmount: Scalars['Decimal']['input'];
  userId: Scalars['UUID']['input'];
};

export enum PaymentMethodStatus {
  Active = 'ACTIVE',
  Expired = 'EXPIRED',
  Inactive = 'INACTIVE',
  Removed = 'REMOVED'
}

export enum PaymentMethodType {
  BankTransfer = 'BANK_TRANSFER',
  CreditCard = 'CREDIT_CARD',
  CryptoWallet = 'CRYPTO_WALLET',
  DebitCard = 'DEBIT_CARD',
  WalletBalance = 'WALLET_BALANCE'
}

export enum PermissionType {
  Accessibility = 'ACCESSIBILITY',
  Admin = 'ADMIN',
  Advertisement = 'ADVERTISEMENT',
  Affiliate = 'AFFILIATE',
  Analytics = 'ANALYTICS',
  Api = 'API',
  Approve = 'APPROVE',
  Archive = 'ARCHIVE',
  Audit = 'AUDIT',
  Backup = 'BACKUP',
  Ban = 'BAN',
  Banner = 'BANNER',
  Benchmark = 'BENCHMARK',
  Bookmark = 'BOOKMARK',
  Brand = 'BRAND',
  Carousel = 'CAROUSEL',
  Categorize = 'CATEGORIZE',
  Clone = 'CLONE',
  Collection = 'COLLECTION',
  Comment = 'COMMENT',
  Commission = 'COMMISSION',
  Create = 'CREATE',
  CrossReference = 'CROSS_REFERENCE',
  Delete = 'DELETE',
  Distribute = 'DISTRIBUTE',
  Draft = 'DRAFT',
  Edit = 'EDIT',
  Email = 'EMAIL',
  Escalate = 'ESCALATE',
  Execute = 'EXECUTE',
  Export = 'EXPORT',
  FactCheck = 'FACT_CHECK',
  Feature = 'FEATURE',
  Feedback = 'FEEDBACK',
  Flag = 'FLAG',
  Follow = 'FOLLOW',
  Guidelines = 'GUIDELINES',
  HardDelete = 'HARD_DELETE',
  Hide = 'HIDE',
  Import = 'IMPORT',
  Improvement = 'IMPROVEMENT',
  Legal = 'LEGAL',
  License = 'LICENSE',
  Manage = 'MANAGE',
  Mention = 'MENTION',
  Metrics = 'METRICS',
  Migrate = 'MIGRATE',
  Monetize = 'MONETIZE',
  Newsletter = 'NEWSLETTER',
  Paywall = 'PAYWALL',
  Performance = 'PERFORMANCE',
  Pin = 'PIN',
  Plagiarism = 'PLAGIARISM',
  Pricing = 'PRICING',
  Proofread = 'PROOFREAD',
  Publish = 'PUBLISH',
  Push = 'PUSH',
  Quarantine = 'QUARANTINE',
  Rate = 'RATE',
  React = 'REACT',
  Read = 'READ',
  Recommend = 'RECOMMEND',
  Reject = 'REJECT',
  Reply = 'REPLY',
  Report = 'REPORT',
  Reschedule = 'RESCHEDULE',
  Restore = 'RESTORE',
  Revenue = 'REVENUE',
  Review = 'REVIEW',
  Rss = 'RSS',
  Schedule = 'SCHEDULE',
  Score = 'SCORE',
  Seo = 'SEO',
  Series = 'SERIES',
  Share = 'SHARE',
  Sms = 'SMS',
  SocialMedia = 'SOCIAL_MEDIA',
  Sponsorship = 'SPONSORSHIP',
  Spotlight = 'SPOTLIGHT',
  Standards = 'STANDARDS',
  StyleGuide = 'STYLE_GUIDE',
  Submit = 'SUBMIT',
  Subscribe = 'SUBSCRIBE',
  Subscription = 'SUBSCRIPTION',
  Suspend = 'SUSPEND',
  Syndicate = 'SYNDICATE',
  SystemAdmin = 'SYSTEM_ADMIN',
  Tag = 'TAG',
  Template = 'TEMPLATE',
  TenantAdmin = 'TENANT_ADMIN',
  Translate = 'TRANSLATE',
  Trending = 'TRENDING',
  Unpublish = 'UNPUBLISH',
  UserManagement = 'USER_MANAGEMENT',
  Version = 'VERSION',
  Vote = 'VOTE',
  Warning = 'WARNING',
  Widget = 'WIDGET',
  Withdraw = 'WITHDRAW'
}

export type PhoneNumber = {
  __typename?: 'PhoneNumber';
  countryCode: Scalars['String']['output'];
  displayFormat: Scalars['String']['output'];
  nationalNumber: Scalars['String']['output'];
  value: Scalars['String']['output'];
};

export type PrerequisiteStatusDto = {
  __typename?: 'PrerequisiteStatusDto';
  isMet: Scalars['Boolean']['output'];
  minimumLevel?: Maybe<Scalars['Int']['output']>;
  name: Scalars['String']['output'];
  prerequisiteAchievementId: Scalars['UUID']['output'];
  requiresCompletion: Scalars['Boolean']['output'];
  userLevel?: Maybe<Scalars['Int']['output']>;
};

/** Represents a product in the CMS system with full EntityBase support and DAC permissions. */
export type Product = {
  __typename?: 'Product';
  addLocalization: ResourceLocalization;
  affiliateCommissionPercentage: Scalars['Decimal']['output'];
  bundleItemIds: Array<Scalars['UUID']['output']>;
  /** Items included in this bundle (only applicable if IsBundle is true) */
  bundleItems?: Maybe<Array<Maybe<Product>>>;
  /** Indicates if the current user can delete this product */
  canDelete?: Maybe<Scalars['Boolean']['output']>;
  /** Indicates if the current user can edit this product */
  canEdit?: Maybe<Scalars['Boolean']['output']>;
  /** Indicates if the current user can publish this product */
  canPublish?: Maybe<Scalars['Boolean']['output']>;
  /** The date and time when the product was created. */
  createdAt: Scalars['DateTime']['output'];
  /** The user who created this product. */
  creator?: Maybe<User>;
  creatorId?: Maybe<Scalars['UUID']['output']>;
  /** The current active pricing for this product */
  currentPricing?: Maybe<ProductPricing>;
  /** The date and time when the product was soft deleted (null if not deleted). */
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  /** Detailed description of the product. */
  description?: Maybe<Scalars['String']['output']>;
  domainEvents: Array<IDomainEvent>;
  /** Indicates if the current user has access to this product */
  hasAccess?: Maybe<Scalars['Boolean']['output']>;
  /** The unique identifier for the product (UUID). */
  id: Scalars['UUID']['output'];
  imageUrl?: Maybe<Scalars['String']['output']>;
  /** Indicates whether this product is a bundle containing other products. */
  isBundle: Scalars['Boolean']['output'];
  /** Indicates whether the product has been soft deleted. */
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  licenses: Array<ContentLicense>;
  localizations: Array<ResourceLocalization>;
  maxAffiliateDiscount: Scalars['Decimal']['output'];
  metadata?: Maybe<ResourceMetadata>;
  /** The name of the product (product-specific field). */
  name: Scalars['String']['output'];
  /** Pricing information for this product. */
  productPricings?: Maybe<Array<Maybe<ProductPricing>>>;
  /** Programs included in this product. */
  productPrograms?: Maybe<Array<Maybe<ProductProgram>>>;
  /** Promotional codes associated with this product. */
  promoCodes?: Maybe<Array<Maybe<PromoCode>>>;
  referralCommissionPercentage: Scalars['Decimal']['output'];
  /** Short description of the product. */
  shortDescription?: Maybe<Scalars['String']['output']>;
  slug: Scalars['String']['output'];
  /** The publication status of the product. */
  status: ContentStatus;
  subscriptionPlans: Array<ProductSubscriptionPlan>;
  /** The tenant this product belongs to. */
  tenant?: Maybe<Tenant>;
  /** The title of the product. */
  title: Scalars['String']['output'];
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  /** The type of the product. */
  type: ProductType;
  /** The date and time when the product was last updated. */
  updatedAt?: Maybe<Scalars['DateTime']['output']>;
  /** User access records for this product. */
  userProducts?: Maybe<Array<Maybe<UserProduct>>>;
  /** Version control for optimistic concurrency. */
  version: Scalars['Int']['output'];
  /** The access level of the product. */
  visibility: AccessLevel;
};


/** Represents a product in the CMS system with full EntityBase support and DAC permissions. */
export type ProductAddLocalizationArgs = {
  content: Scalars['String']['input'];
  fieldName: Scalars['String']['input'];
  language: LanguageInput;
  status?: LocalizationStatus;
};

export enum ProductAccessStatus {
  Active = 'ACTIVE',
  Expired = 'EXPIRED',
  Revoked = 'REVOKED',
  Suspended = 'SUSPENDED'
}

export enum ProductAcquisitionType {
  Free = 'FREE',
  Gift = 'GIFT',
  Purchase = 'PURCHASE',
  Subscription = 'SUBSCRIPTION'
}

/** Represents pricing information for a product */
export type ProductPricing = {
  __typename?: 'ProductPricing';
  /** The base price for this product */
  basePrice: Scalars['Decimal']['output'];
  /** When this pricing was created */
  createdAt: Scalars['DateTime']['output'];
  /** The currency code (e.g., USD, EUR) */
  currency: Scalars['String']['output'];
  currentPrice: Scalars['Decimal']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  /** The unique identifier for the pricing */
  id: Scalars['UUID']['output'];
  /** Indicates if this is the default pricing */
  isDefault: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  isSaleActive: Scalars['Boolean']['output'];
  /** The name of this pricing tier */
  name: Scalars['String']['output'];
  product: Product;
  productId: Scalars['UUID']['output'];
  saleEndDate?: Maybe<Scalars['DateTime']['output']>;
  salePrice?: Maybe<Scalars['Decimal']['output']>;
  saleStartDate?: Maybe<Scalars['DateTime']['output']>;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

/** Represents a program included in a product */
export type ProductProgram = {
  __typename?: 'ProductProgram';
  /** When this program was added to the product */
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  /** The unique identifier for this product-program relationship */
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  /** The product that contains this program */
  product?: Maybe<Product>;
  productId: Scalars['UUID']['output'];
  /** The program included in the product */
  program?: Maybe<Program>;
  programId: Scalars['UUID']['output'];
  /** The display order of this program within the product */
  sortOrder: Scalars['Int']['output'];
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

export type ProductSubscriptionPlan = {
  __typename?: 'ProductSubscriptionPlan';
  billingInterval: SubscriptionBillingInterval;
  createdAt: Scalars['DateTime']['output'];
  currency: Scalars['String']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  description?: Maybe<Scalars['String']['output']>;
  domainEvents: Array<IDomainEvent>;
  id: Scalars['UUID']['output'];
  intervalCount: Scalars['Int']['output'];
  isActive: Scalars['Boolean']['output'];
  isDefault: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  name: Scalars['String']['output'];
  price: Scalars['Decimal']['output'];
  product: Product;
  productId: Scalars['UUID']['output'];
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  trialPeriodDays?: Maybe<Scalars['Int']['output']>;
  updatedAt: Scalars['DateTime']['output'];
  userSubscriptions: Array<UserSubscription>;
  version: Scalars['Int']['output'];
};

export enum ProductType {
  Bundle = 'BUNDLE',
  Certification = 'CERTIFICATION',
  Community = 'COMMUNITY',
  Ebook = 'EBOOK',
  LearningPathway = 'LEARNING_PATHWAY',
  Mentorship = 'MENTORSHIP',
  Other = 'OTHER',
  Program = 'PROGRAM',
  ResourcePack = 'RESOURCE_PACK',
  Subscription = 'SUBSCRIPTION',
  Workshop = 'WORKSHOP'
}

/** Represents a learning program with structured educational content */
export type Program = {
  __typename?: 'Program';
  addLocalization: ResourceLocalization;
  averageRating: Scalars['Decimal']['output'];
  calculateEstimatedWeeks?: Maybe<Scalars['Float']['output']>;
  /** The category/domain of the program */
  category?: Maybe<ProgramCategory>;
  certificates: Array<Certificate>;
  /** When the program was created */
  createdAt: Scalars['DateTime']['output'];
  currentEnrollments: Scalars['Int']['output'];
  /** When the program was soft deleted (null if not deleted) */
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  /** Detailed description of the program */
  description?: Maybe<Scalars['String']['output']>;
  /** The difficulty level of the program */
  difficulty?: Maybe<ProgramDifficulty>;
  domainEvents: Array<IDomainEvent>;
  enrollmentDeadline?: Maybe<Scalars['DateTime']['output']>;
  enrollmentStatus: EnrollmentStatus;
  /** Estimated time in hours required to complete the program */
  estimatedHours?: Maybe<Scalars['Float']['output']>;
  estimatedWeeks?: Maybe<Scalars['Float']['output']>;
  feedbackSubmissions: Array<ProgramFeedbackSubmission>;
  /** The unique identifier for the program */
  id: Scalars['UUID']['output'];
  /** Whether the program has been soft deleted */
  isDeleted: Scalars['Boolean']['output'];
  isEnrollmentOpen: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  licenses: Array<ContentLicense>;
  localizations: Array<ResourceLocalization>;
  maxEnrollments?: Maybe<Scalars['Int']['output']>;
  metadata?: Maybe<ResourceMetadata>;
  productPrograms: Array<ProductProgram>;
  programContents: Array<ProgramContent>;
  programRatings: Array<ProgramRating>;
  programUsers: Array<ProgramUser>;
  programWishlists: Array<ProgramWishlist>;
  providedSkills: Array<TagProficiency>;
  requiredSkills: Array<TagProficiency>;
  skillsProvided: Array<CertificateTag>;
  skillsRequired: Array<CertificateTag>;
  /** URL-friendly identifier for the program */
  slug: Scalars['String']['output'];
  /** The publication status of the program */
  status: ContentStatus;
  /** The tenant this program belongs to */
  tenant?: Maybe<Tenant>;
  /** Thumbnail image URL for program display */
  thumbnail?: Maybe<Scalars['String']['output']>;
  /** The title of the program */
  title: Scalars['String']['output'];
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  totalRatings: Scalars['Int']['output'];
  /** When the program was last updated */
  updatedAt?: Maybe<Scalars['DateTime']['output']>;
  /** Version control for optimistic concurrency */
  version: Scalars['Int']['output'];
  /** Video showcase URL for program preview */
  videoShowcaseUrl?: Maybe<Scalars['String']['output']>;
  /** The access level of the program */
  visibility: AccessLevel;
};


/** Represents a learning program with structured educational content */
export type ProgramAddLocalizationArgs = {
  content: Scalars['String']['input'];
  fieldName: Scalars['String']['input'];
  language: LanguageInput;
  status?: LocalizationStatus;
};


/** Represents a learning program with structured educational content */
export type ProgramCalculateEstimatedWeeksArgs = {
  hoursPerWeek: Scalars['Int']['input'];
};


/** Represents a learning program with structured educational content */
export type ProgramEstimatedWeeksArgs = {
  hoursPerWeek: Scalars['Int']['input'];
};

export enum ProgramCategory {
  Ai = 'AI',
  Business = 'BUSINESS',
  CreativeArts = 'CREATIVE_ARTS',
  Cybersecurity = 'CYBERSECURITY',
  Database = 'DATABASE',
  DataScience = 'DATA_SCIENCE',
  Design = 'DESIGN',
  DevOps = 'DEV_OPS',
  GameDevelopment = 'GAME_DEVELOPMENT',
  Language = 'LANGUAGE',
  Marketing = 'MARKETING',
  MobileDevelopment = 'MOBILE_DEVELOPMENT',
  Other = 'OTHER',
  PersonalDevelopment = 'PERSONAL_DEVELOPMENT',
  Programming = 'PROGRAMMING',
  ProjectManagement = 'PROJECT_MANAGEMENT',
  Science = 'SCIENCE',
  WebDevelopment = 'WEB_DEVELOPMENT'
}

export type ProgramContent = {
  __typename?: 'ProgramContent';
  body: Scalars['String']['output'];
  children: Array<ProgramContent>;
  content: Scalars['String']['output'];
  contentInteractions: Array<ContentInteraction>;
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  description: Scalars['String']['output'];
  domainEvents: Array<IDomainEvent>;
  estimatedMinutes?: Maybe<Scalars['Int']['output']>;
  gradingMethod?: Maybe<GradingMethod>;
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  isRequired: Scalars['Boolean']['output'];
  maxPoints?: Maybe<Scalars['Decimal']['output']>;
  parent?: Maybe<ProgramContent>;
  parentId?: Maybe<Scalars['UUID']['output']>;
  program: Program;
  programId: Scalars['UUID']['output'];
  slug?: Maybe<Scalars['String']['output']>;
  sortOrder: Scalars['Int']['output'];
  tenant?: Maybe<Tenant>;
  title: Scalars['String']['output'];
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  type: ProgramContentType;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
  visibility: Visibility;
};

export enum ProgramContentType {
  Assignment = 'ASSIGNMENT',
  Challenge = 'CHALLENGE',
  Code = 'CODE',
  Discussion = 'DISCUSSION',
  Page = 'PAGE',
  Questionnaire = 'QUESTIONNAIRE',
  Reflection = 'REFLECTION',
  Survey = 'SURVEY'
}

export enum ProgramDifficulty {
  Advanced = 'ADVANCED',
  Beginner = 'BEGINNER',
  Expert = 'EXPERT',
  Intermediate = 'INTERMEDIATE'
}

export type ProgramFeedbackSubmission = {
  __typename?: 'ProgramFeedbackSubmission';
  comments?: Maybe<Scalars['String']['output']>;
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  feedbackData: Scalars['String']['output'];
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  overallRating?: Maybe<Scalars['Decimal']['output']>;
  product?: Maybe<Product>;
  productId?: Maybe<Scalars['UUID']['output']>;
  program: Program;
  programId: Scalars['UUID']['output'];
  programUser: ProgramUser;
  programUserId: Scalars['UUID']['output'];
  submittedAt: Scalars['DateTime']['output'];
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  user: User;
  userId: Scalars['UUID']['output'];
  version: Scalars['Int']['output'];
  wouldRecommend?: Maybe<Scalars['Boolean']['output']>;
};

export type ProgramRating = {
  __typename?: 'ProgramRating';
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  helpfulVotes: Scalars['Int']['output'];
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isFeatured: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  isVerified: Scalars['Boolean']['output'];
  program: Program;
  programId: Scalars['UUID']['output'];
  rating: Scalars['Decimal']['output'];
  review?: Maybe<Scalars['String']['output']>;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  unhelpfulVotes: Scalars['Int']['output'];
  updatedAt: Scalars['DateTime']['output'];
  userId: Scalars['String']['output'];
  version: Scalars['Int']['output'];
};

export type ProgramUser = {
  __typename?: 'ProgramUser';
  completedAt?: Maybe<Scalars['DateTime']['output']>;
  completionPercentage: Scalars['Decimal']['output'];
  contentInteractions: Array<ContentInteraction>;
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  feedbackSubmissions: Array<ProgramFeedbackSubmission>;
  finalGrade?: Maybe<Scalars['Decimal']['output']>;
  givenGrades: Array<ActivityGrade>;
  id: Scalars['UUID']['output'];
  isActive: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  joinedAt: Scalars['DateTime']['output'];
  lastAccessedAt?: Maybe<Scalars['DateTime']['output']>;
  program: Program;
  programId: Scalars['UUID']['output'];
  programRatings: Array<ProgramRating>;
  receivedGrades: Array<ActivityGrade>;
  startedAt?: Maybe<Scalars['DateTime']['output']>;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  user: User;
  userCertificates: Array<UserCertificate>;
  userId: Scalars['UUID']['output'];
  version: Scalars['Int']['output'];
};

export type ProgramWishlist = {
  __typename?: 'ProgramWishlist';
  addedAt: Scalars['DateTime']['output'];
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  notes?: Maybe<Scalars['String']['output']>;
  program: Program;
  programId: Scalars['UUID']['output'];
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  user: User;
  userId: Scalars['UUID']['output'];
  version: Scalars['Int']['output'];
};

export enum ProgressStatus {
  Completed = 'COMPLETED',
  InProgress = 'IN_PROGRESS',
  NotStarted = 'NOT_STARTED',
  Skipped = 'SKIPPED'
}

/** Represents a promotional code for products */
export type PromoCode = {
  __typename?: 'PromoCode';
  calculateDiscount: Scalars['Decimal']['output'];
  /** The promotional code */
  code: Scalars['String']['output'];
  createdAt: Scalars['DateTime']['output'];
  createdBy: Scalars['UUID']['output'];
  createdByUser: User;
  currency: Scalars['String']['output'];
  /** Current number of times this code has been used */
  currentUsageCount: Scalars['Int']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  description?: Maybe<Scalars['String']['output']>;
  /** The discount amount (for fixed amount discounts) */
  discountAmount?: Maybe<Scalars['Decimal']['output']>;
  /** The discount percentage (for percentage-based discounts) */
  discountPercentage?: Maybe<Scalars['Decimal']['output']>;
  domainEvents: Array<IDomainEvent>;
  financialTransactions: Array<FinancialTransaction>;
  /** The unique identifier for the promo code */
  id: Scalars['UUID']['output'];
  isActive: Scalars['Boolean']['output'];
  isCurrentlyValid: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  /** Indicates if the promo code is currently valid */
  isValid: Scalars['Boolean']['output'];
  /** Maximum number of times this code can be used */
  maxUses?: Maybe<Scalars['Int']['output']>;
  maxUsesPerUser?: Maybe<Scalars['Int']['output']>;
  minimumOrderAmount?: Maybe<Scalars['Decimal']['output']>;
  name: Scalars['String']['output'];
  product?: Maybe<Product>;
  productId?: Maybe<Scalars['UUID']['output']>;
  promoCodeUses: Array<PromoCodeUse>;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  /** The type of discount (percentage or fixed amount) */
  type: PromoCodeType;
  updatedAt: Scalars['DateTime']['output'];
  /** When the promo code becomes valid */
  validFrom?: Maybe<Scalars['DateTime']['output']>;
  /** When the promo code expires */
  validUntil?: Maybe<Scalars['DateTime']['output']>;
  version: Scalars['Int']['output'];
};


/** Represents a promotional code for products */
export type PromoCodeCalculateDiscountArgs = {
  orderAmount: Scalars['Decimal']['input'];
};

export enum PromoCodeType {
  BuyOneGetOne = 'BUY_ONE_GET_ONE',
  FirstMonthFree = 'FIRST_MONTH_FREE',
  FixedAmountOff = 'FIXED_AMOUNT_OFF',
  PercentageOff = 'PERCENTAGE_OFF'
}

export type PromoCodeUse = {
  __typename?: 'PromoCodeUse';
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  discountApplied: Scalars['Decimal']['output'];
  domainEvents: Array<IDomainEvent>;
  financialTransaction: FinancialTransaction;
  financialTransactionId: Scalars['UUID']['output'];
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  promoCode: PromoCode;
  promoCodeId: Scalars['UUID']['output'];
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  user: User;
  userId: Scalars['UUID']['output'];
  version: Scalars['Int']['output'];
};

export type Query = {
  __typename?: 'Query';
  achievement: Achievement;
  achievementLeaderboard: Array<UserAchievementLeaderboardDto>;
  achievementStatistics: AchievementStatistics;
  achievements: AchievementsPageDto;
  availableAchievements: AchievementsPageDto;
  bundleItems: Array<Product>;
  checkAchievementPrerequisites: AchievementPrerequisiteCheckDto;
  contentByParent: Array<ProgramContent>;
  contentByType: Array<ProgramContent>;
  contentByVisibility: Array<ProgramContent>;
  contentCount: Scalars['Int']['output'];
  contentInteractionById?: Maybe<ContentInteraction>;
  contentInteractionStats: ContentInteractionStats;
  contentInteractionsByStatus: Array<ContentInteraction>;
  currentPricing?: Maybe<ProductPricing>;
  hasUserAccess: Scalars['Boolean']['output'];
  /** Health check query to ensure GraphQL is working */
  health: Scalars['String']['output'];
  isPromoCodeValid: Scalars['Boolean']['output'];
  myProducts: Array<Product>;
  myPrograms: Array<Program>;
  popularProducts: Array<Product>;
  pricingHistory: Array<ProductPricing>;
  productById?: Maybe<Product>;
  productCount: Scalars['Int']['output'];
  products: Array<Product>;
  productsByCreator: Array<Product>;
  productsByType: Array<Product>;
  productsInPriceRange: Array<Product>;
  programById?: Maybe<Program>;
  programBySlug?: Maybe<Program>;
  programContentById?: Maybe<ProgramContent>;
  programContents: Array<ProgramContent>;
  promoCode?: Maybe<PromoCode>;
  publishedProducts: Array<Product>;
  publishedPrograms: Array<Program>;
  recentProducts: Array<Product>;
  requiredContent: Array<ProgramContent>;
  requiredContentCount: Scalars['Int']['output'];
  rootContent: Array<ProgramContent>;
  searchProducts: Array<Product>;
  searchProgramContent: Array<ProgramContent>;
  testAuth: Scalars['String']['output'];
  totalRevenueForProduct: Scalars['Decimal']['output'];
  userAchievementProgress: Array<AchievementProgressDto>;
  userAchievementSummary: UserAchievementSummaryDto;
  userAchievements: UserAchievementsPageDto;
  userContentInteraction?: Maybe<ContentInteraction>;
  userContentInteractions: Array<ContentInteraction>;
  userCountForProduct: Scalars['Int']['output'];
  userProducts: Array<UserProduct>;
};


export type QueryAchievementArgs = {
  achievementId: Scalars['UUID']['input'];
  includeLevels?: Scalars['Boolean']['input'];
  includePrerequisites?: Scalars['Boolean']['input'];
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
};


export type QueryAchievementLeaderboardArgs = {
  category?: InputMaybe<Scalars['String']['input']>;
  limit?: Scalars['Int']['input'];
  orderBy?: Scalars['String']['input'];
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
  timeFrame?: InputMaybe<Scalars['DateTime']['input']>;
};


export type QueryAchievementStatisticsArgs = {
  achievementId: Scalars['UUID']['input'];
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
};


export type QueryAchievementsArgs = {
  category?: InputMaybe<Scalars['String']['input']>;
  descending?: Scalars['Boolean']['input'];
  includeSecrets?: Scalars['Boolean']['input'];
  isActive?: InputMaybe<Scalars['Boolean']['input']>;
  isSecret?: InputMaybe<Scalars['Boolean']['input']>;
  orderBy?: Scalars['String']['input'];
  pageNumber?: Scalars['Int']['input'];
  pageSize?: Scalars['Int']['input'];
  searchTerm?: InputMaybe<Scalars['String']['input']>;
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
  type?: InputMaybe<Scalars['String']['input']>;
};


export type QueryAvailableAchievementsArgs = {
  category?: InputMaybe<Scalars['String']['input']>;
  onlyEligible?: Scalars['Boolean']['input'];
  pageNumber?: Scalars['Int']['input'];
  pageSize?: Scalars['Int']['input'];
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
  userId: Scalars['UUID']['input'];
};


export type QueryBundleItemsArgs = {
  bundleId: Scalars['UUID']['input'];
};


export type QueryCheckAchievementPrerequisitesArgs = {
  achievementId: Scalars['UUID']['input'];
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
  userId: Scalars['UUID']['input'];
};


export type QueryContentByParentArgs = {
  parentContentId: Scalars['UUID']['input'];
  programId: Scalars['UUID']['input'];
};


export type QueryContentByTypeArgs = {
  contentType: ProgramContentType;
  programId: Scalars['UUID']['input'];
};


export type QueryContentByVisibilityArgs = {
  programId: Scalars['UUID']['input'];
  visibility: Visibility;
};


export type QueryContentCountArgs = {
  programId: Scalars['UUID']['input'];
};


export type QueryContentInteractionByIdArgs = {
  interactionId: Scalars['UUID']['input'];
  programId: Scalars['UUID']['input'];
};


export type QueryContentInteractionStatsArgs = {
  programId: Scalars['UUID']['input'];
};


export type QueryContentInteractionsByStatusArgs = {
  programId: Scalars['UUID']['input'];
  status: ProgressStatus;
};


export type QueryCurrentPricingArgs = {
  productId: Scalars['UUID']['input'];
};


export type QueryHasUserAccessArgs = {
  productId: Scalars['UUID']['input'];
  userId: Scalars['UUID']['input'];
};


export type QueryIsPromoCodeValidArgs = {
  code: Scalars['String']['input'];
  productId?: InputMaybe<Scalars['UUID']['input']>;
};


export type QueryMyProductsArgs = {
  skip?: Scalars['Int']['input'];
  take?: Scalars['Int']['input'];
};


export type QueryMyProgramsArgs = {
  skip?: Scalars['Int']['input'];
  take?: Scalars['Int']['input'];
};


export type QueryPopularProductsArgs = {
  count?: Scalars['Int']['input'];
};


export type QueryPricingHistoryArgs = {
  productId: Scalars['UUID']['input'];
};


export type QueryProductByIdArgs = {
  id: Scalars['UUID']['input'];
  includePricing?: Scalars['Boolean']['input'];
  includePrograms?: Scalars['Boolean']['input'];
};


export type QueryProductCountArgs = {
  type?: InputMaybe<ProductType>;
  visibility?: InputMaybe<AccessLevel>;
};


export type QueryProductsArgs = {
  isBundle?: InputMaybe<Scalars['Boolean']['input']>;
  searchTerm?: InputMaybe<Scalars['String']['input']>;
  skip?: Scalars['Int']['input'];
  status?: InputMaybe<ContentStatus>;
  take?: Scalars['Int']['input'];
  type?: InputMaybe<ProductType>;
  visibility?: InputMaybe<AccessLevel>;
};


export type QueryProductsByCreatorArgs = {
  creatorId: Scalars['UUID']['input'];
  skip?: Scalars['Int']['input'];
  take?: Scalars['Int']['input'];
};


export type QueryProductsByTypeArgs = {
  skip?: Scalars['Int']['input'];
  take?: Scalars['Int']['input'];
  type: ProductType;
};


export type QueryProductsInPriceRangeArgs = {
  currency?: Scalars['String']['input'];
  maxPrice: Scalars['Decimal']['input'];
  minPrice: Scalars['Decimal']['input'];
  skip?: Scalars['Int']['input'];
  take?: Scalars['Int']['input'];
};


export type QueryProgramByIdArgs = {
  id: Scalars['UUID']['input'];
};


export type QueryProgramBySlugArgs = {
  slug: Scalars['String']['input'];
};


export type QueryProgramContentByIdArgs = {
  id: Scalars['UUID']['input'];
  programId: Scalars['UUID']['input'];
};


export type QueryProgramContentsArgs = {
  programId: Scalars['UUID']['input'];
};


export type QueryPromoCodeArgs = {
  code: Scalars['String']['input'];
};


export type QueryPublishedProductsArgs = {
  skip?: Scalars['Int']['input'];
  take?: Scalars['Int']['input'];
};


export type QueryPublishedProgramsArgs = {
  skip?: Scalars['Int']['input'];
  take?: Scalars['Int']['input'];
};


export type QueryRecentProductsArgs = {
  count?: Scalars['Int']['input'];
};


export type QueryRequiredContentArgs = {
  programId: Scalars['UUID']['input'];
};


export type QueryRequiredContentCountArgs = {
  programId: Scalars['UUID']['input'];
};


export type QueryRootContentArgs = {
  programId: Scalars['UUID']['input'];
};


export type QuerySearchProductsArgs = {
  searchTerm: Scalars['String']['input'];
  skip?: Scalars['Int']['input'];
  take?: Scalars['Int']['input'];
};


export type QuerySearchProgramContentArgs = {
  programId: Scalars['UUID']['input'];
  searchTerm: Scalars['String']['input'];
};


export type QueryTotalRevenueForProductArgs = {
  productId: Scalars['UUID']['input'];
};


export type QueryUserAchievementProgressArgs = {
  category?: InputMaybe<Scalars['String']['input']>;
  onlyInProgress?: Scalars['Boolean']['input'];
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
  userId: Scalars['UUID']['input'];
};


export type QueryUserAchievementSummaryArgs = {
  nearCompletionThreshold?: Scalars['Int']['input'];
  recentLimit?: Scalars['Int']['input'];
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
  userId: Scalars['UUID']['input'];
};


export type QueryUserAchievementsArgs = {
  category?: InputMaybe<Scalars['String']['input']>;
  descending?: Scalars['Boolean']['input'];
  earnedAfter?: InputMaybe<Scalars['DateTime']['input']>;
  earnedBefore?: InputMaybe<Scalars['DateTime']['input']>;
  isCompleted?: InputMaybe<Scalars['Boolean']['input']>;
  orderBy?: Scalars['String']['input'];
  pageNumber?: Scalars['Int']['input'];
  pageSize?: Scalars['Int']['input'];
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
  type?: InputMaybe<Scalars['String']['input']>;
  userId: Scalars['UUID']['input'];
};


export type QueryUserContentInteractionArgs = {
  contentId: Scalars['UUID']['input'];
  programId: Scalars['UUID']['input'];
  programUserId: Scalars['UUID']['input'];
};


export type QueryUserContentInteractionsArgs = {
  programId: Scalars['UUID']['input'];
  programUserId: Scalars['UUID']['input'];
};


export type QueryUserCountForProductArgs = {
  productId: Scalars['UUID']['input'];
};


export type QueryUserProductsArgs = {
  userId: Scalars['UUID']['input'];
};

export type ResourceLocalization = {
  __typename?: 'ResourceLocalization';
  content: Scalars['String']['output'];
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  fieldName: Scalars['String']['output'];
  id: Scalars['UUID']['output'];
  isDefault: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  language: Language;
  resourceId: Scalars['UUID']['output'];
  resourceType: Scalars['String']['output'];
  status: LocalizationStatus;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

export type ResourceLocalizationInput = {
  content: Scalars['String']['input'];
  createdAt: Scalars['DateTime']['input'];
  deletedAt?: InputMaybe<Scalars['DateTime']['input']>;
  fieldName: Scalars['String']['input'];
  id: Scalars['UUID']['input'];
  isDefault: Scalars['Boolean']['input'];
  language: LanguageInput;
  resourceId: Scalars['UUID']['input'];
  resourceType: Scalars['String']['input'];
  status: LocalizationStatus;
  tenant?: InputMaybe<TenantInput>;
  updatedAt: Scalars['DateTime']['input'];
  version: Scalars['Int']['input'];
};

export type ResourceMetadata = {
  __typename?: 'ResourceMetadata';
  additionalData?: Maybe<Scalars['String']['output']>;
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  resourceType: Scalars['String']['output'];
  seoMetadata?: Maybe<Scalars['String']['output']>;
  tags?: Maybe<Scalars['String']['output']>;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

export type ResourceMetadataInput = {
  additionalData?: InputMaybe<Scalars['String']['input']>;
  createdAt: Scalars['DateTime']['input'];
  deletedAt?: InputMaybe<Scalars['DateTime']['input']>;
  id: Scalars['UUID']['input'];
  resourceType: Scalars['String']['input'];
  seoMetadata?: InputMaybe<Scalars['String']['input']>;
  tags?: InputMaybe<Scalars['String']['input']>;
  tenant?: InputMaybe<TenantInput>;
  updatedAt: Scalars['DateTime']['input'];
  version: Scalars['Int']['input'];
};

export type RevokeAchievementInput = {
  reason?: InputMaybe<Scalars['String']['input']>;
  userAchievementId: Scalars['UUID']['input'];
};

export type SetProductPricingInput = {
  basePrice: Scalars['Decimal']['input'];
  currency: Scalars['String']['input'];
  productId: Scalars['UUID']['input'];
};

export enum SkillProficiencyLevel {
  Advanced = 'ADVANCED',
  Awareness = 'AWARENESS',
  Beginner = 'BEGINNER',
  Expert = 'EXPERT',
  Intermediate = 'INTERMEDIATE',
  Master = 'MASTER',
  Novice = 'NOVICE'
}

export type StartContentInput = {
  contentId: Scalars['UUID']['input'];
  programUserId: Scalars['UUID']['input'];
};

export type SubmitContentInput = {
  interactionId: Scalars['UUID']['input'];
  submissionData: Scalars['String']['input'];
};

export enum SubscriptionBillingInterval {
  Day = 'DAY',
  Month = 'MONTH',
  Week = 'WEEK',
  Year = 'YEAR'
}

export enum SubscriptionStatus {
  Active = 'ACTIVE',
  Canceled = 'CANCELED',
  Incomplete = 'INCOMPLETE',
  IncompleteExpired = 'INCOMPLETE_EXPIRED',
  PastDue = 'PAST_DUE',
  PendingActivation = 'PENDING_ACTIVATION',
  Suspended = 'SUSPENDED',
  Trialing = 'TRIALING',
  Unpaid = 'UNPAID'
}

export type TagProficiency = {
  __typename?: 'TagProficiency';
  certificateTags: Array<CertificateTag>;
  color?: Maybe<Scalars['String']['output']>;
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  description?: Maybe<Scalars['String']['output']>;
  domainEvents: Array<IDomainEvent>;
  icon?: Maybe<Scalars['String']['output']>;
  id: Scalars['UUID']['output'];
  isActive: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  name: Scalars['String']['output'];
  proficiencyLevel: SkillProficiencyLevel;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  type: TagType;
  updatedAt: Scalars['DateTime']['output'];
  version: Scalars['Int']['output'];
};

export enum TagType {
  Category = 'CATEGORY',
  Certification = 'CERTIFICATION',
  Difficulty = 'DIFFICULTY',
  Industry = 'INDUSTRY',
  Skill = 'SKILL',
  Technology = 'TECHNOLOGY',
  Topic = 'TOPIC'
}

/** A tenant represents an organization or group within the system */
export type Tenant = {
  __typename?: 'Tenant';
  addLocalization: ResourceLocalization;
  adminEmail?: Maybe<Scalars['String']['output']>;
  /** The date and time when the tenant was created */
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  /** The description of the tenant */
  description?: Maybe<Scalars['String']['output']>;
  domainEvents: Array<IDomainEvent>;
  /** The unique identifier of the tenant */
  id: Scalars['UUID']['output'];
  /** Whether the tenant is active */
  isActive: Scalars['Boolean']['output'];
  isDefault: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  localizations: Array<ResourceLocalization>;
  metadata?: Maybe<ResourceMetadata>;
  /** The name of the tenant */
  name: Scalars['String']['output'];
  settings?: Maybe<TenantSettings>;
  slug: Scalars['String']['output'];
  tenant?: Maybe<Tenant>;
  /** The users and their permissions associated with this tenant */
  tenantPermissions?: Maybe<Array<Maybe<TenantPermission>>>;
  title: Scalars['String']['output'];
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  /** The date and time when the tenant was last updated */
  updatedAt?: Maybe<Scalars['DateTime']['output']>;
  /** The version number for optimistic concurrency control */
  version: Scalars['Int']['output'];
  visibility: AccessLevel;
};


/** A tenant represents an organization or group within the system */
export type TenantAddLocalizationArgs = {
  content: Scalars['String']['input'];
  fieldName: Scalars['String']['input'];
  language: LanguageInput;
  status?: LocalizationStatus;
};

export type TenantInput = {
  adminEmail?: InputMaybe<Scalars['String']['input']>;
  createdAt: Scalars['DateTime']['input'];
  deletedAt?: InputMaybe<Scalars['DateTime']['input']>;
  description?: InputMaybe<Scalars['String']['input']>;
  id: Scalars['UUID']['input'];
  isActive: Scalars['Boolean']['input'];
  isDefault: Scalars['Boolean']['input'];
  localizations: Array<ResourceLocalizationInput>;
  metadata?: InputMaybe<ResourceMetadataInput>;
  name: Scalars['String']['input'];
  settings?: InputMaybe<TenantSettingsInput>;
  slug: Scalars['String']['input'];
  tenant?: InputMaybe<TenantInput>;
  tenantPermissions: Array<TenantPermissionInput>;
  title: Scalars['String']['input'];
  updatedAt: Scalars['DateTime']['input'];
  version: Scalars['Int']['input'];
  visibility: AccessLevel;
};

/** Represents the permissions and relationship between a user and a tenant */
export type TenantPermission = {
  __typename?: 'TenantPermission';
  /** The date and time when the user joined the tenant */
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  /** The date and time when the permission expires */
  expiresAt?: Maybe<Scalars['DateTime']['output']>;
  hasAllPermissions: Scalars['Boolean']['output'];
  hasAnyPermission: Scalars['Boolean']['output'];
  hasPermission: Scalars['Boolean']['output'];
  /** The unique identifier of the tenant permission */
  id: Scalars['UUID']['output'];
  isActiveMembership: Scalars['Boolean']['output'];
  /** Whether this is a default permission for a specific tenant */
  isDefaultPermission: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  /** Whether this permission has expired */
  isExpired: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  /** Whether this is a global default permission */
  isGlobalDefaultPermission: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  /** Whether this is a user-specific permission */
  isUserPermission: Scalars['Boolean']['output'];
  /** Whether this permission is valid (not deleted and not expired) */
  isValid: Scalars['Boolean']['output'];
  /** Permission flags for bits 0-63 */
  permissionFlags1: Scalars['Long']['output'];
  /** Permission flags for bits 64-127 */
  permissionFlags2: Scalars['Long']['output'];
  /** The tenant in this relationship */
  tenant?: Maybe<Tenant>;
  /** The tenant identifier */
  tenantId: Scalars['UUID']['output'];
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  /** The user in this relationship */
  user?: Maybe<User>;
  /** The user identifier */
  userId: Scalars['UUID']['output'];
  version: Scalars['Int']['output'];
};


/** Represents the permissions and relationship between a user and a tenant */
export type TenantPermissionHasAllPermissionsArgs = {
  permissions: Array<PermissionType>;
};


/** Represents the permissions and relationship between a user and a tenant */
export type TenantPermissionHasAnyPermissionArgs = {
  permissions: Array<PermissionType>;
};


/** Represents the permissions and relationship between a user and a tenant */
export type TenantPermissionHasPermissionArgs = {
  permission: PermissionType;
};

export type TenantPermissionInput = {
  createdAt: Scalars['DateTime']['input'];
  deletedAt?: InputMaybe<Scalars['DateTime']['input']>;
  /** When this permission expires (null if it never expires) */
  expiresAt?: InputMaybe<Scalars['DateTime']['input']>;
  id: Scalars['UUID']['input'];
  /** Permission flags for bits 0-63 */
  permissionFlags1: Scalars['Long']['input'];
  /** Permission flags for bits 64-127 */
  permissionFlags2: Scalars['Long']['input'];
  /** The tenant ID this permission applies to (null for global defaults) */
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
  updatedAt: Scalars['DateTime']['input'];
  /** The user ID this permission applies to (null for default permissions) */
  userId?: InputMaybe<Scalars['UUID']['input']>;
  version: Scalars['Int']['input'];
};

export type TenantSettings = {
  __typename?: 'TenantSettings';
  addLocalization: ResourceLocalization;
  address?: Maybe<Scalars['String']['output']>;
  allowUserRegistration: Scalars['Boolean']['output'];
  createdAt: Scalars['DateTime']['output'];
  customCss?: Maybe<Scalars['String']['output']>;
  dateFormat: Scalars['String']['output'];
  defaultCurrency: Scalars['String']['output'];
  defaultLanguage: Scalars['String']['output'];
  defaultNotificationEmail?: Maybe<Scalars['String']['output']>;
  defaultTheme: Scalars['String']['output'];
  defaultTimezone: Scalars['String']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  description?: Maybe<Scalars['String']['output']>;
  domainEvents: Array<IDomainEvent>;
  enableEmailNotifications: Scalars['Boolean']['output'];
  enablePushNotifications: Scalars['Boolean']['output'];
  enableSmsNotifications: Scalars['Boolean']['output'];
  featureFlag: Scalars['Boolean']['output'];
  featureFlags?: Maybe<Scalars['String']['output']>;
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  localizations: Array<ResourceLocalization>;
  logoUrl?: Maybe<Scalars['String']['output']>;
  maxUsers?: Maybe<Scalars['Int']['output']>;
  metadata?: Maybe<ResourceMetadata>;
  minPasswordLength: Scalars['Int']['output'];
  moduleSettings?: Maybe<Scalars['String']['output']>;
  passwordComplexityRules?: Maybe<Scalars['String']['output']>;
  primaryColor?: Maybe<Scalars['String']['output']>;
  requireRegistrationApproval: Scalars['Boolean']['output'];
  requireTwoFactorAuth: Scalars['Boolean']['output'];
  secondaryColor?: Maybe<Scalars['String']['output']>;
  sessionTimeoutMinutes: Scalars['Int']['output'];
  storageQuotaMB?: Maybe<Scalars['Long']['output']>;
  subscriptionExpiresAt?: Maybe<Scalars['DateTime']['output']>;
  subscriptionPlan?: Maybe<Scalars['String']['output']>;
  supportEmail?: Maybe<Scalars['String']['output']>;
  supportPhone?: Maybe<Scalars['String']['output']>;
  tenant?: Maybe<Tenant>;
  tenantId?: Maybe<Scalars['UUID']['output']>;
  title: Scalars['String']['output'];
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  use24HourFormat: Scalars['Boolean']['output'];
  version: Scalars['Int']['output'];
  visibility: AccessLevel;
};


export type TenantSettingsAddLocalizationArgs = {
  content: Scalars['String']['input'];
  fieldName: Scalars['String']['input'];
  language: LanguageInput;
  status?: LocalizationStatus;
};


export type TenantSettingsFeatureFlagArgs = {
  defaultValue?: Scalars['Boolean']['input'];
  key: Scalars['String']['input'];
};

export type TenantSettingsInput = {
  address?: InputMaybe<Scalars['String']['input']>;
  allowUserRegistration: Scalars['Boolean']['input'];
  createdAt: Scalars['DateTime']['input'];
  customCss?: InputMaybe<Scalars['String']['input']>;
  dateFormat: Scalars['String']['input'];
  defaultCurrency: Scalars['String']['input'];
  defaultLanguage: Scalars['String']['input'];
  defaultNotificationEmail?: InputMaybe<Scalars['String']['input']>;
  defaultTheme: Scalars['String']['input'];
  defaultTimezone: Scalars['String']['input'];
  deletedAt?: InputMaybe<Scalars['DateTime']['input']>;
  description?: InputMaybe<Scalars['String']['input']>;
  enableEmailNotifications: Scalars['Boolean']['input'];
  enablePushNotifications: Scalars['Boolean']['input'];
  enableSmsNotifications: Scalars['Boolean']['input'];
  featureFlags?: InputMaybe<Scalars['String']['input']>;
  id: Scalars['UUID']['input'];
  localizations: Array<ResourceLocalizationInput>;
  logoUrl?: InputMaybe<Scalars['String']['input']>;
  maxUsers?: InputMaybe<Scalars['Int']['input']>;
  metadata?: InputMaybe<ResourceMetadataInput>;
  minPasswordLength: Scalars['Int']['input'];
  moduleSettings?: InputMaybe<Scalars['String']['input']>;
  passwordComplexityRules?: InputMaybe<Scalars['String']['input']>;
  primaryColor?: InputMaybe<Scalars['String']['input']>;
  requireRegistrationApproval: Scalars['Boolean']['input'];
  requireTwoFactorAuth: Scalars['Boolean']['input'];
  secondaryColor?: InputMaybe<Scalars['String']['input']>;
  sessionTimeoutMinutes: Scalars['Int']['input'];
  storageQuotaMB?: InputMaybe<Scalars['Long']['input']>;
  subscriptionExpiresAt?: InputMaybe<Scalars['DateTime']['input']>;
  subscriptionPlan?: InputMaybe<Scalars['String']['input']>;
  supportEmail?: InputMaybe<Scalars['String']['input']>;
  supportPhone?: InputMaybe<Scalars['String']['input']>;
  tenant?: InputMaybe<TenantInput>;
  tenantId?: InputMaybe<Scalars['UUID']['input']>;
  title: Scalars['String']['input'];
  updatedAt: Scalars['DateTime']['input'];
  use24HourFormat: Scalars['Boolean']['input'];
  version: Scalars['Int']['input'];
  visibility: AccessLevel;
};

export enum TransactionStatus {
  Cancelled = 'CANCELLED',
  Completed = 'COMPLETED',
  Failed = 'FAILED',
  Pending = 'PENDING',
  Processing = 'PROCESSING',
  Refunded = 'REFUNDED'
}

export enum TransactionType {
  Adjustment = 'ADJUSTMENT',
  Deposit = 'DEPOSIT',
  Fee = 'FEE',
  Purchase = 'PURCHASE',
  Refund = 'REFUND',
  Transfer = 'TRANSFER',
  Withdrawal = 'WITHDRAWAL'
}

export type UpdateAchievementInput = {
  achievementId: Scalars['UUID']['input'];
  category?: InputMaybe<Scalars['String']['input']>;
  color?: InputMaybe<Scalars['String']['input']>;
  conditions?: InputMaybe<Scalars['String']['input']>;
  description?: InputMaybe<Scalars['String']['input']>;
  displayOrder?: InputMaybe<Scalars['Int']['input']>;
  iconUrl?: InputMaybe<Scalars['String']['input']>;
  isActive?: InputMaybe<Scalars['Boolean']['input']>;
  isRepeatable?: InputMaybe<Scalars['Boolean']['input']>;
  isSecret?: InputMaybe<Scalars['Boolean']['input']>;
  name?: InputMaybe<Scalars['String']['input']>;
  points?: InputMaybe<Scalars['Int']['input']>;
  type?: InputMaybe<Scalars['String']['input']>;
};

export type UpdateAchievementProgressInput = {
  achievementId: Scalars['UUID']['input'];
  autoAward: Scalars['Boolean']['input'];
  context?: InputMaybe<Scalars['String']['input']>;
  progressIncrement: Scalars['Int']['input'];
  userId: Scalars['UUID']['input'];
};

export type UpdateProductInput = {
  description?: InputMaybe<Scalars['String']['input']>;
  id: Scalars['UUID']['input'];
  isBundle?: InputMaybe<Scalars['Boolean']['input']>;
  name?: InputMaybe<Scalars['String']['input']>;
  shortDescription?: InputMaybe<Scalars['String']['input']>;
  status?: InputMaybe<ContentStatus>;
  type?: InputMaybe<ProductType>;
  visibility?: InputMaybe<AccessLevel>;
};

export type UpdateProductPricingInput = {
  basePrice?: InputMaybe<Scalars['Decimal']['input']>;
  currency?: InputMaybe<Scalars['String']['input']>;
  pricingId: Scalars['UUID']['input'];
};

export type UpdateProgramInput = {
  category?: InputMaybe<ProgramCategory>;
  description?: InputMaybe<Scalars['String']['input']>;
  difficulty?: InputMaybe<ProgramDifficulty>;
  estimatedHours?: InputMaybe<Scalars['Float']['input']>;
  thumbnail?: InputMaybe<Scalars['String']['input']>;
  title?: InputMaybe<Scalars['String']['input']>;
  videoShowcaseUrl?: InputMaybe<Scalars['String']['input']>;
  visibility?: InputMaybe<AccessLevel>;
};

export type UpdateProgressInput = {
  completionPercentage: Scalars['Decimal']['input'];
  interactionId: Scalars['UUID']['input'];
};

export type UpdatePromoCodeInput = {
  code?: InputMaybe<Scalars['String']['input']>;
  discountPercentage?: InputMaybe<Scalars['Decimal']['input']>;
  discountType?: InputMaybe<PromoCodeType>;
  discountValue?: InputMaybe<Scalars['Decimal']['input']>;
  expiryDate?: InputMaybe<Scalars['DateTime']['input']>;
  id: Scalars['UUID']['input'];
  maxUses?: InputMaybe<Scalars['Int']['input']>;
  validFrom?: InputMaybe<Scalars['DateTime']['input']>;
  validUntil?: InputMaybe<Scalars['DateTime']['input']>;
};

export type UpdateTimeSpentInput = {
  additionalMinutes: Scalars['Int']['input'];
  interactionId: Scalars['UUID']['input'];
};

/** Represents a user in the CMS system with full EntityBase support. */
export type User = {
  __typename?: 'User';
  availableBalance: Money;
  balance: Money;
  /** The date and time when the user was created. */
  createdAt: Scalars['DateTime']['output'];
  credentials: Array<Credential>;
  /** The date and time when the user was soft deleted (null if not deleted). */
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  /** The email address of the user. */
  email: Scalars['String']['output'];
  emailAddress?: Maybe<EmailAddress>;
  /** The unique identifier for the user (UUID). */
  id?: Maybe<Scalars['ID']['output']>;
  /** Indicates whether the user is active. */
  isActive: Scalars['Boolean']['output'];
  /** Indicates whether the user has been soft deleted. */
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  lastSeenAt?: Maybe<Scalars['DateTime']['output']>;
  /** The name of the user. */
  name: Scalars['String']['output'];
  phoneNumber?: Maybe<PhoneNumber>;
  /** The user's profile information. */
  profile?: Maybe<UserProfile>;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  /** The date and time when the user was last updated. */
  updatedAt: Scalars['DateTime']['output'];
  /** The unique username/handle of the user. */
  username: Scalars['String']['output'];
  /** Version control for optimistic concurrency. */
  version: Scalars['Int']['output'];
};

/** Represents a user's earned achievement */
export type UserAchievement = {
  __typename?: 'UserAchievement';
  /** The achievement that was earned */
  achievement?: Maybe<Achievement>;
  /** The ID of the achievement that was earned */
  achievementId: Scalars['UUID']['output'];
  /** Additional context about how the achievement was earned */
  context?: Maybe<Scalars['String']['output']>;
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  /** Number of times this achievement has been earned (for repeatable achievements) */
  earnCount: Scalars['Int']['output'];
  /** When the achievement was earned */
  earnedAt: Scalars['DateTime']['output'];
  /** The unique identifier of the user achievement */
  id: Scalars['UUID']['output'];
  /** Whether the achievement has been completed */
  isCompleted: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  /** Whether the user has been notified about earning this achievement */
  isNotified: Scalars['Boolean']['output'];
  /** The level achieved if this is a multi-level achievement */
  level?: Maybe<Scalars['Int']['output']>;
  /** Maximum progress required for completion */
  maxProgress: Scalars['Int']['output'];
  /** Points earned from this achievement */
  pointsEarned: Scalars['Int']['output'];
  /** Current progress towards this achievement */
  progress: Scalars['Int']['output'];
  /** Progress as a percentage */
  progressPercentage?: Maybe<Scalars['Float']['output']>;
  tenant?: Maybe<Tenant>;
  tenantId?: Maybe<Scalars['UUID']['output']>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  /** The user who earned the achievement */
  user?: Maybe<User>;
  /** The ID of the user who earned the achievement */
  userId?: Maybe<Scalars['UUID']['output']>;
  version: Scalars['Int']['output'];
};

export type UserAchievementDto = {
  __typename?: 'UserAchievementDto';
  achievement?: Maybe<AchievementDto>;
  achievementId: Scalars['UUID']['output'];
  context?: Maybe<Scalars['String']['output']>;
  earnCount: Scalars['Int']['output'];
  earnedAt: Scalars['DateTime']['output'];
  id: Scalars['UUID']['output'];
  isCompleted: Scalars['Boolean']['output'];
  isNotified: Scalars['Boolean']['output'];
  level?: Maybe<Scalars['Int']['output']>;
  maxProgress: Scalars['Int']['output'];
  pointsEarned: Scalars['Int']['output'];
  progress: Scalars['Int']['output'];
  userId: Scalars['UUID']['output'];
};

export type UserAchievementLeaderboardDto = {
  __typename?: 'UserAchievementLeaderboardDto';
  rank: Scalars['Int']['output'];
  totalAchievements: Scalars['Int']['output'];
  totalPoints: Scalars['Int']['output'];
  userDisplayName: Scalars['String']['output'];
  userId: Scalars['UUID']['output'];
};

export type UserAchievementSummaryDto = {
  __typename?: 'UserAchievementSummaryDto';
  achievementsByCategory: Array<KeyValuePairOfStringAndInt32>;
  completedAchievements: Scalars['Int']['output'];
  inProgressAchievements: Scalars['Int']['output'];
  nearCompletion: Array<AchievementProgressDto>;
  recentAchievements: Array<UserAchievementDto>;
  totalAchievements: Scalars['Int']['output'];
  totalPoints: Scalars['Int']['output'];
  userId: Scalars['UUID']['output'];
};

export type UserAchievementsPageDto = {
  __typename?: 'UserAchievementsPageDto';
  hasNextPage: Scalars['Boolean']['output'];
  hasPreviousPage: Scalars['Boolean']['output'];
  pageNumber: Scalars['Int']['output'];
  pageSize: Scalars['Int']['output'];
  totalCount: Scalars['Int']['output'];
  userAchievements: Array<UserAchievementDto>;
};

export type UserCertificate = {
  __typename?: 'UserCertificate';
  blockchainAnchors: Array<CertificateBlockchainAnchor>;
  certificate: Certificate;
  certificateId: Scalars['UUID']['output'];
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  expiresAt?: Maybe<Scalars['DateTime']['output']>;
  finalGrade?: Maybe<Scalars['Decimal']['output']>;
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  issuedAt: Scalars['DateTime']['output'];
  metadata?: Maybe<Scalars['String']['output']>;
  product?: Maybe<Product>;
  productId?: Maybe<Scalars['UUID']['output']>;
  program?: Maybe<Program>;
  programId?: Maybe<Scalars['UUID']['output']>;
  programUser?: Maybe<ProgramUser>;
  programUserId?: Maybe<Scalars['UUID']['output']>;
  revocationReason?: Maybe<Scalars['String']['output']>;
  revokedAt?: Maybe<Scalars['DateTime']['output']>;
  status: CertificateStatus;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  user: User;
  userId: Scalars['UUID']['output'];
  verificationCode: Scalars['String']['output'];
  version: Scalars['Int']['output'];
};

export type UserFinancialMethod = {
  __typename?: 'UserFinancialMethod';
  brand?: Maybe<Scalars['String']['output']>;
  createdAt: Scalars['DateTime']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  displayName?: Maybe<Scalars['String']['output']>;
  domainEvents: Array<IDomainEvent>;
  expiryMonth?: Maybe<Scalars['String']['output']>;
  expiryYear?: Maybe<Scalars['String']['output']>;
  externalId?: Maybe<Scalars['String']['output']>;
  id: Scalars['UUID']['output'];
  isActive: Scalars['Boolean']['output'];
  isDefault: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  lastFour?: Maybe<Scalars['String']['output']>;
  lastFourDigits?: Maybe<Scalars['String']['output']>;
  name: Scalars['String']['output'];
  status: PaymentMethodStatus;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  type: PaymentMethodType;
  updatedAt: Scalars['DateTime']['output'];
  user: User;
  userId: Scalars['UUID']['output'];
  version: Scalars['Int']['output'];
};

/** Represents a user's access to a product */
export type UserProduct = {
  __typename?: 'UserProduct';
  /** When the access expires (null for permanent access) */
  accessEndDate?: Maybe<Scalars['DateTime']['output']>;
  accessStartDate?: Maybe<Scalars['DateTime']['output']>;
  /** The current access status */
  accessStatus: ProductAccessStatus;
  /** How the user acquired access to this product */
  acquisitionType: ProductAcquisitionType;
  createdAt: Scalars['DateTime']['output'];
  /** The currency used for payment */
  currency: Scalars['String']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  giftedByUser?: Maybe<User>;
  giftedByUserId?: Maybe<Scalars['UUID']['output']>;
  hasActiveAccess: Scalars['Boolean']['output'];
  /** The unique identifier for the user product record */
  id: Scalars['UUID']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  /** The price paid for this product */
  pricePaid: Scalars['Decimal']['output'];
  /** The product being accessed */
  product?: Maybe<Product>;
  productId: Scalars['UUID']['output'];
  subscription?: Maybe<UserSubscription>;
  subscriptionId?: Maybe<Scalars['UUID']['output']>;
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  updatedAt: Scalars['DateTime']['output'];
  /** The user who has access */
  user?: Maybe<User>;
  userId: Scalars['UUID']['output'];
  version: Scalars['Int']['output'];
};

/** Represents a user profile with personal information and settings */
export type UserProfile = {
  __typename?: 'UserProfile';
  addLocalization: ResourceLocalization;
  /** User's avatar URL */
  avatarUrl?: Maybe<Scalars['String']['output']>;
  /** User's biography (alias for description) */
  bio?: Maybe<Scalars['String']['output']>;
  /** The date and time when the user profile was created */
  createdAt: Scalars['DateTime']['output'];
  /** The date and time when the user profile was soft deleted */
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  /** A description of the user profile */
  description?: Maybe<Scalars['String']['output']>;
  /** The user's preferred display name */
  displayName?: Maybe<Scalars['String']['output']>;
  domainEvents: Array<IDomainEvent>;
  /** The user's family (last) name */
  familyName?: Maybe<Scalars['String']['output']>;
  /** The user's given (first) name */
  givenName?: Maybe<Scalars['String']['output']>;
  /** The unique identifier for the user profile */
  id: Scalars['UUID']['output'];
  /** Indicates whether the user profile has been soft deleted */
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  /** Localized versions of this user profile */
  localizations: Array<ResourceLocalization>;
  /** User's location */
  location?: Maybe<Scalars['String']['output']>;
  /** Metadata associated with this user profile resource */
  metadata?: Maybe<ResourceMetadata>;
  /** The tenant this profile belongs to (null for global profiles) */
  tenant?: Maybe<Tenant>;
  /** The title of the user profile */
  title: Scalars['String']['output'];
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  /** The date and time when the user profile was last updated */
  updatedAt: Scalars['DateTime']['output'];
  /** The version number for optimistic concurrency control */
  version: Scalars['Int']['output'];
  /** The visibility status of the user profile */
  visibility: AccessLevel;
};


/** Represents a user profile with personal information and settings */
export type UserProfileAddLocalizationArgs = {
  content: Scalars['String']['input'];
  fieldName: Scalars['String']['input'];
  language: LanguageInput;
  status?: LocalizationStatus;
};

export type UserSubscription = {
  __typename?: 'UserSubscription';
  amount: Money;
  autoRenew: Scalars['Boolean']['output'];
  billingCycle: BillingCycle;
  billingCycleCount: Scalars['Int']['output'];
  canceledAt?: Maybe<Scalars['DateTime']['output']>;
  cancellationNote?: Maybe<Scalars['String']['output']>;
  cancellationReason?: Maybe<CancellationReason>;
  createdAt: Scalars['DateTime']['output'];
  currentPeriodEnd: Scalars['DateTime']['output'];
  currentPeriodStart: Scalars['DateTime']['output'];
  daysUntilNextBilling: Scalars['Int']['output'];
  deletedAt?: Maybe<Scalars['DateTime']['output']>;
  domainEvents: Array<IDomainEvent>;
  endsAt?: Maybe<Scalars['DateTime']['output']>;
  externalCustomerId?: Maybe<Scalars['String']['output']>;
  externalSubscriptionId?: Maybe<Scalars['String']['output']>;
  id: Scalars['UUID']['output'];
  isActive: Scalars['Boolean']['output'];
  isCancelled: Scalars['Boolean']['output'];
  isDeleted: Scalars['Boolean']['output'];
  isGlobal: Scalars['Boolean']['output'];
  isNew: Scalars['Boolean']['output'];
  isTrialing: Scalars['Boolean']['output'];
  lastPaymentAt?: Maybe<Scalars['DateTime']['output']>;
  metadata?: Maybe<Scalars['String']['output']>;
  nextBillingAt?: Maybe<Scalars['DateTime']['output']>;
  remainingTrialDays?: Maybe<Scalars['Int']['output']>;
  status: SubscriptionStatus;
  subscriptionPlan: ProductSubscriptionPlan;
  subscriptionPlanId: Scalars['UUID']['output'];
  tenant?: Maybe<Tenant>;
  toDictionary: Array<KeyValuePairOfStringAndObject>;
  trialEndsAt?: Maybe<Scalars['DateTime']['output']>;
  updatedAt: Scalars['DateTime']['output'];
  user: User;
  userId: Scalars['UUID']['output'];
  userProducts: Array<UserProduct>;
  version: Scalars['Int']['output'];
};

export enum VerificationMethod {
  Blockchain = 'BLOCKCHAIN',
  Both = 'BOTH',
  Code = 'CODE'
}

export enum Visibility {
  Archived = 'ARCHIVED',
  Draft = 'DRAFT',
  Published = 'PUBLISHED'
}

export type CreateProductMutationVariables = Exact<{
  input: CreateProductInput;
}>;


export type CreateProductMutation = { __typename?: 'Mutation', createProduct: { __typename?: 'Product', id: any, title: string, name: string, description?: string | null, shortDescription?: string | null, imageUrl?: string | null, slug: string, status: ContentStatus, type: ProductType, isBundle: boolean, hasAccess?: boolean | null, createdAt: any, updatedAt?: any | null, currentPricing?: { __typename?: 'ProductPricing', id: any, basePrice: any, currency: string, isDefault: boolean } | null, creator?: { __typename?: 'User', id?: string | null, name: string, email: string } | null, productPrograms?: Array<{ __typename?: 'ProductProgram', id: any, sortOrder: number, program?: { __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null } | null } | null> | null } };

export type UpdateProductMutationVariables = Exact<{
  input: UpdateProductInput;
}>;


export type UpdateProductMutation = { __typename?: 'Mutation', updateProduct?: { __typename?: 'Product', id: any, title: string, name: string, description?: string | null, shortDescription?: string | null, imageUrl?: string | null, slug: string, status: ContentStatus, type: ProductType, isBundle: boolean, hasAccess?: boolean | null, createdAt: any, updatedAt?: any | null, currentPricing?: { __typename?: 'ProductPricing', id: any, basePrice: any, currency: string, isDefault: boolean } | null, creator?: { __typename?: 'User', id?: string | null, name: string, email: string } | null, productPrograms?: Array<{ __typename?: 'ProductProgram', id: any, sortOrder: number, program?: { __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null } | null } | null> | null } | null };

export type HealthQueryVariables = Exact<{ [key: string]: never; }>;


export type HealthQuery = { __typename?: 'Query', health: string };

export type GetPublishedProductsWithProgramsQueryVariables = Exact<{ [key: string]: never; }>;


export type GetPublishedProductsWithProgramsQuery = { __typename?: 'Query', publishedProducts: Array<{ __typename?: 'Product', id: any, title: string, name: string, description?: string | null, shortDescription?: string | null, imageUrl?: string | null, slug: string, status: ContentStatus, type: ProductType, isBundle: boolean, hasAccess?: boolean | null, createdAt: any, updatedAt?: any | null, currentPricing?: { __typename?: 'ProductPricing', id: any, basePrice: any, currency: string, isDefault: boolean } | null, creator?: { __typename?: 'User', id?: string | null, name: string, email: string } | null, productPrograms?: Array<{ __typename?: 'ProductProgram', id: any, sortOrder: number, program?: { __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null } | null } | null> | null }> };

export type GetAllProductsWithProgramsQueryVariables = Exact<{ [key: string]: never; }>;


export type GetAllProductsWithProgramsQuery = { __typename?: 'Query', products: Array<{ __typename?: 'Product', id: any, title: string, name: string, description?: string | null, shortDescription?: string | null, imageUrl?: string | null, slug: string, status: ContentStatus, type: ProductType, isBundle: boolean, hasAccess?: boolean | null, createdAt: any, updatedAt?: any | null, currentPricing?: { __typename?: 'ProductPricing', id: any, basePrice: any, currency: string, isDefault: boolean } | null, creator?: { __typename?: 'User', id?: string | null, name: string, email: string } | null, productPrograms?: Array<{ __typename?: 'ProductProgram', id: any, sortOrder: number, program?: { __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null } | null } | null> | null }> };

export type SearchProductsWithProgramsQueryVariables = Exact<{
  searchTerm: Scalars['String']['input'];
}>;


export type SearchProductsWithProgramsQuery = { __typename?: 'Query', searchProducts: Array<{ __typename?: 'Product', id: any, title: string, name: string, description?: string | null, shortDescription?: string | null, imageUrl?: string | null, slug: string, status: ContentStatus, type: ProductType, isBundle: boolean, hasAccess?: boolean | null, createdAt: any, updatedAt?: any | null, currentPricing?: { __typename?: 'ProductPricing', id: any, basePrice: any, currency: string, isDefault: boolean } | null, creator?: { __typename?: 'User', id?: string | null, name: string, email: string } | null, productPrograms?: Array<{ __typename?: 'ProductProgram', id: any, sortOrder: number, program?: { __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null } | null } | null> | null }> };

export type GetMyProductsWithProgramsQueryVariables = Exact<{
  skip?: InputMaybe<Scalars['Int']['input']>;
  take?: InputMaybe<Scalars['Int']['input']>;
}>;


export type GetMyProductsWithProgramsQuery = { __typename?: 'Query', myProducts: Array<{ __typename?: 'Product', id: any, title: string, name: string, description?: string | null, shortDescription?: string | null, imageUrl?: string | null, slug: string, status: ContentStatus, type: ProductType, isBundle: boolean, hasAccess?: boolean | null, createdAt: any, updatedAt?: any | null, currentPricing?: { __typename?: 'ProductPricing', id: any, basePrice: any, currency: string, isDefault: boolean } | null, creator?: { __typename?: 'User', id?: string | null, name: string, email: string } | null, productPrograms?: Array<{ __typename?: 'ProductProgram', id: any, sortOrder: number, program?: { __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null } | null } | null> | null }> };

export type GetMyProgramsQueryVariables = Exact<{
  skip?: InputMaybe<Scalars['Int']['input']>;
  take?: InputMaybe<Scalars['Int']['input']>;
}>;


export type GetMyProgramsQuery = { __typename?: 'Query', myPrograms: Array<{ __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null, visibility: AccessLevel, status: ContentStatus, createdAt: any, updatedAt?: any | null }> };

export type GetPublishedProgramsQueryVariables = Exact<{
  skip?: InputMaybe<Scalars['Int']['input']>;
  take?: InputMaybe<Scalars['Int']['input']>;
}>;


export type GetPublishedProgramsQuery = { __typename?: 'Query', publishedPrograms: Array<{ __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null, visibility: AccessLevel, status: ContentStatus, createdAt: any, updatedAt?: any | null }> };

export type GetProgramByIdQueryVariables = Exact<{
  id: Scalars['UUID']['input'];
}>;


export type GetProgramByIdQuery = { __typename?: 'Query', programById?: { __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null, visibility: AccessLevel, status: ContentStatus, createdAt: any, updatedAt?: any | null } | null };

export type GetProgramBySlugQueryVariables = Exact<{
  slug: Scalars['String']['input'];
}>;


export type GetProgramBySlugQuery = { __typename?: 'Query', programBySlug?: { __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null, visibility: AccessLevel, status: ContentStatus, createdAt: any, updatedAt?: any | null } | null };

export type TestAuthQueryVariables = Exact<{ [key: string]: never; }>;


export type TestAuthQuery = { __typename?: 'Query', testAuth: string };

export type CreateProgramMutationVariables = Exact<{
  input: CreateProgramInput;
}>;


export type CreateProgramMutation = { __typename?: 'Mutation', createProgram: { __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null, visibility: AccessLevel, status: ContentStatus, createdAt: any, updatedAt?: any | null } };

export type UpdateProgramMutationVariables = Exact<{
  id: Scalars['UUID']['input'];
  input: UpdateProgramInput;
}>;


export type UpdateProgramMutation = { __typename?: 'Mutation', updateProgram: { __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null, visibility: AccessLevel, status: ContentStatus, createdAt: any, updatedAt?: any | null } };

export type DeleteProgramMutationVariables = Exact<{
  id: Scalars['UUID']['input'];
}>;


export type DeleteProgramMutation = { __typename?: 'Mutation', deleteProgram: boolean };

export type PublishProgramMutationVariables = Exact<{
  id: Scalars['UUID']['input'];
}>;


export type PublishProgramMutation = { __typename?: 'Mutation', publishProgram: { __typename?: 'Program', id: any, title: string, description?: string | null, slug: string, thumbnail?: string | null, videoShowcaseUrl?: string | null, category?: ProgramCategory | null, difficulty?: ProgramDifficulty | null, estimatedHours?: number | null, visibility: AccessLevel, status: ContentStatus, createdAt: any, updatedAt?: any | null } };


export const CreateProductDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateProduct"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateProductInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createProduct"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"shortDescription"}},{"kind":"Field","name":{"kind":"Name","value":"imageUrl"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"isBundle"}},{"kind":"Field","name":{"kind":"Name","value":"hasAccess"}},{"kind":"Field","name":{"kind":"Name","value":"currentPricing"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"basePrice"}},{"kind":"Field","name":{"kind":"Name","value":"currency"}},{"kind":"Field","name":{"kind":"Name","value":"isDefault"}}]}},{"kind":"Field","name":{"kind":"Name","value":"creator"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"email"}}]}},{"kind":"Field","name":{"kind":"Name","value":"productPrograms"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"sortOrder"}},{"kind":"Field","name":{"kind":"Name","value":"program"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}}]}}]}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<CreateProductMutation, CreateProductMutationVariables>;
export const UpdateProductDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateProduct"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateProductInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateProduct"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"shortDescription"}},{"kind":"Field","name":{"kind":"Name","value":"imageUrl"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"isBundle"}},{"kind":"Field","name":{"kind":"Name","value":"hasAccess"}},{"kind":"Field","name":{"kind":"Name","value":"currentPricing"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"basePrice"}},{"kind":"Field","name":{"kind":"Name","value":"currency"}},{"kind":"Field","name":{"kind":"Name","value":"isDefault"}}]}},{"kind":"Field","name":{"kind":"Name","value":"creator"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"email"}}]}},{"kind":"Field","name":{"kind":"Name","value":"productPrograms"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"sortOrder"}},{"kind":"Field","name":{"kind":"Name","value":"program"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}}]}}]}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<UpdateProductMutation, UpdateProductMutationVariables>;
export const HealthDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"Health"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"health"}}]}}]} as unknown as DocumentNode<HealthQuery, HealthQueryVariables>;
export const GetPublishedProductsWithProgramsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetPublishedProductsWithPrograms"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"publishedProducts"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"shortDescription"}},{"kind":"Field","name":{"kind":"Name","value":"imageUrl"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"isBundle"}},{"kind":"Field","name":{"kind":"Name","value":"hasAccess"}},{"kind":"Field","name":{"kind":"Name","value":"currentPricing"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"basePrice"}},{"kind":"Field","name":{"kind":"Name","value":"currency"}},{"kind":"Field","name":{"kind":"Name","value":"isDefault"}}]}},{"kind":"Field","name":{"kind":"Name","value":"creator"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"email"}}]}},{"kind":"Field","name":{"kind":"Name","value":"productPrograms"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"sortOrder"}},{"kind":"Field","name":{"kind":"Name","value":"program"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}}]}}]}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetPublishedProductsWithProgramsQuery, GetPublishedProductsWithProgramsQueryVariables>;
export const GetAllProductsWithProgramsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetAllProductsWithPrograms"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"products"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"shortDescription"}},{"kind":"Field","name":{"kind":"Name","value":"imageUrl"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"isBundle"}},{"kind":"Field","name":{"kind":"Name","value":"hasAccess"}},{"kind":"Field","name":{"kind":"Name","value":"currentPricing"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"basePrice"}},{"kind":"Field","name":{"kind":"Name","value":"currency"}},{"kind":"Field","name":{"kind":"Name","value":"isDefault"}}]}},{"kind":"Field","name":{"kind":"Name","value":"creator"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"email"}}]}},{"kind":"Field","name":{"kind":"Name","value":"productPrograms"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"sortOrder"}},{"kind":"Field","name":{"kind":"Name","value":"program"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}}]}}]}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetAllProductsWithProgramsQuery, GetAllProductsWithProgramsQueryVariables>;
export const SearchProductsWithProgramsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"SearchProductsWithPrograms"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"searchTerm"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"String"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"searchProducts"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"searchTerm"},"value":{"kind":"Variable","name":{"kind":"Name","value":"searchTerm"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"shortDescription"}},{"kind":"Field","name":{"kind":"Name","value":"imageUrl"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"isBundle"}},{"kind":"Field","name":{"kind":"Name","value":"hasAccess"}},{"kind":"Field","name":{"kind":"Name","value":"currentPricing"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"basePrice"}},{"kind":"Field","name":{"kind":"Name","value":"currency"}},{"kind":"Field","name":{"kind":"Name","value":"isDefault"}}]}},{"kind":"Field","name":{"kind":"Name","value":"creator"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"email"}}]}},{"kind":"Field","name":{"kind":"Name","value":"productPrograms"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"sortOrder"}},{"kind":"Field","name":{"kind":"Name","value":"program"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}}]}}]}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<SearchProductsWithProgramsQuery, SearchProductsWithProgramsQueryVariables>;
export const GetMyProductsWithProgramsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetMyProductsWithPrograms"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"skip"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"Int"}},"defaultValue":{"kind":"IntValue","value":"0"}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"take"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"Int"}},"defaultValue":{"kind":"IntValue","value":"50"}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"myProducts"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"skip"},"value":{"kind":"Variable","name":{"kind":"Name","value":"skip"}}},{"kind":"Argument","name":{"kind":"Name","value":"take"},"value":{"kind":"Variable","name":{"kind":"Name","value":"take"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"shortDescription"}},{"kind":"Field","name":{"kind":"Name","value":"imageUrl"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"isBundle"}},{"kind":"Field","name":{"kind":"Name","value":"hasAccess"}},{"kind":"Field","name":{"kind":"Name","value":"currentPricing"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"basePrice"}},{"kind":"Field","name":{"kind":"Name","value":"currency"}},{"kind":"Field","name":{"kind":"Name","value":"isDefault"}}]}},{"kind":"Field","name":{"kind":"Name","value":"creator"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"email"}}]}},{"kind":"Field","name":{"kind":"Name","value":"productPrograms"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"sortOrder"}},{"kind":"Field","name":{"kind":"Name","value":"program"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}}]}}]}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetMyProductsWithProgramsQuery, GetMyProductsWithProgramsQueryVariables>;
export const GetMyProgramsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetMyPrograms"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"skip"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"Int"}},"defaultValue":{"kind":"IntValue","value":"0"}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"take"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"Int"}},"defaultValue":{"kind":"IntValue","value":"50"}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"myPrograms"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"skip"},"value":{"kind":"Variable","name":{"kind":"Name","value":"skip"}}},{"kind":"Argument","name":{"kind":"Name","value":"take"},"value":{"kind":"Variable","name":{"kind":"Name","value":"take"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}},{"kind":"Field","name":{"kind":"Name","value":"visibility"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetMyProgramsQuery, GetMyProgramsQueryVariables>;
export const GetPublishedProgramsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetPublishedPrograms"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"skip"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"Int"}},"defaultValue":{"kind":"IntValue","value":"0"}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"take"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"Int"}},"defaultValue":{"kind":"IntValue","value":"50"}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"publishedPrograms"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"skip"},"value":{"kind":"Variable","name":{"kind":"Name","value":"skip"}}},{"kind":"Argument","name":{"kind":"Name","value":"take"},"value":{"kind":"Variable","name":{"kind":"Name","value":"take"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}},{"kind":"Field","name":{"kind":"Name","value":"visibility"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetPublishedProgramsQuery, GetPublishedProgramsQueryVariables>;
export const GetProgramByIdDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetProgramById"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"programById"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}},{"kind":"Field","name":{"kind":"Name","value":"visibility"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetProgramByIdQuery, GetProgramByIdQueryVariables>;
export const GetProgramBySlugDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetProgramBySlug"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"slug"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"String"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"programBySlug"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"slug"},"value":{"kind":"Variable","name":{"kind":"Name","value":"slug"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}},{"kind":"Field","name":{"kind":"Name","value":"visibility"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetProgramBySlugQuery, GetProgramBySlugQueryVariables>;
export const TestAuthDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"TestAuth"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"testAuth"}}]}}]} as unknown as DocumentNode<TestAuthQuery, TestAuthQueryVariables>;
export const CreateProgramDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateProgram"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateProgramInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createProgram"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}},{"kind":"Field","name":{"kind":"Name","value":"visibility"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<CreateProgramMutation, CreateProgramMutationVariables>;
export const UpdateProgramDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateProgram"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateProgramInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateProgram"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}},{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}},{"kind":"Field","name":{"kind":"Name","value":"visibility"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<UpdateProgramMutation, UpdateProgramMutationVariables>;
export const DeleteProgramDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"DeleteProgram"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"deleteProgram"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}]}]}}]} as unknown as DocumentNode<DeleteProgramMutation, DeleteProgramMutationVariables>;
export const PublishProgramDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"PublishProgram"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"publishProgram"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"title"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"slug"}},{"kind":"Field","name":{"kind":"Name","value":"thumbnail"}},{"kind":"Field","name":{"kind":"Name","value":"videoShowcaseUrl"}},{"kind":"Field","name":{"kind":"Name","value":"category"}},{"kind":"Field","name":{"kind":"Name","value":"difficulty"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedHours"}},{"kind":"Field","name":{"kind":"Name","value":"visibility"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<PublishProgramMutation, PublishProgramMutationVariables>;