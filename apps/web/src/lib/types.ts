export type PricingModel = "free" | "donation" | "paid" | "no-payments"
export type ReleaseStatus = "released" | "beta" | "wip"
export type Visibility = "draft" | "public" | "private" | "unlisted"
export type Kind = "downloadable" | "web"
type Platform = "windows" | "mac" | "linux"

// Team-related types
export type TeamRole = "owner" | "admin" | "editor" | "viewer"

export interface TeamMember {
  id: string
  name: string
  email: string
  role: TeamRole
  avatar?: string
  joinedAt: number
  lastActive?: number
  permissions: {
    canEdit: boolean
    canPublish: boolean
    canManageTeam: boolean
    canViewAnalytics: boolean
    canManageSettings: boolean
  }
}

export interface TeamInvite {
  id: string
  email: string
  role: TeamRole
  invitedBy: string
  invitedAt: number
  expiresAt: number
  status: "pending" | "accepted" | "expired" | "cancelled"
}

export interface ProjectVersion {
  id: string
  version: string
  notes: string
  fileUrl?: string
  createdAt: number
}

export interface ProjectFeedback {
  id: string
  author: string
  comment: string
  rating: number
  createdAt: number
}

export interface ProjectDevlog {
  id: string
  title: string
  content: string
  createdAt: number
}

export interface Achievement {
  id: string
  name: string
  description: string
  iconUrl?: string
  isHidden: boolean
  rarity: "common" | "uncommon" | "rare" | "epic" | "legendary"
  unlockedBy: number // percentage of players who unlocked it
  totalUnlocks: number
  createdAt: number
}

export interface TestingSession {
  id: string
  name: string
  description: string
  status: "planned" | "active" | "completed" | "cancelled"
  startDate: number
  endDate?: number
  participantCount: number
  targetParticipants: number
  version: string
  testType: "alpha" | "beta" | "focus-group" | "stress-test"
  platforms: Platform[]
  feedback: {
    id: string
    participant: string
    rating: number
    comment: string
    bugs: string[]
    suggestions: string[]
    createdAt: number
  }[]
  metrics: {
    avgPlayTime: number // in minutes
    completionRate: number // percentage
    crashRate: number // percentage
    avgRating: number
  }
  createdAt: number
}

export interface GameProject {
  id: string
  title: string
  slug: string
  tagline?: string
  coverUrl?: string
  screenshots?: string[]
  trailerUrl?: string
  kind: Kind
  releaseStatus: ReleaseStatus
  pricing: PricingModel
  price?: number
  suggestedDonation?: number
  genre?: string
  tags?: string[]
  visibility: Visibility
  description?: string
  platforms?: Platform[]
  versions?: ProjectVersion[]
  feedback?: ProjectFeedback[]
  devlogs?: ProjectDevlog[]
  gameJams?: { name: string; url: string }[]
  achievements?: Achievement[]
  testingSessions?: TestingSession[]

  // Team functionality
  team: TeamMember[]
  teamInvites: TeamInvite[]

  createdAt: number
  updatedAt: number
}

// Course-related types
export type CourseLevel = "beginner" | "intermediate" | "advanced"
export type CourseStatus = "draft" | "published" | "archived"
export type DeliveryMethod = "self-paced" | "instructor-led" | "hybrid"
export type CertificateType = "completion" | "achievement" | "none"

export interface CourseLesson {
  id: string
  title: string
  description?: string
  duration: number // in minutes
  videoUrl?: string
  materials?: string[]
  order: number
}

export interface CourseModule {
  id: string
  title: string
  description?: string
  lessons: CourseLesson[]
  order: number
}

export interface CoursePricing {
  type: "free" | "paid" | "subscription"
  price?: number
  currency: string
  discountPrice?: number
  subscriptionPeriod?: "monthly" | "yearly"
}

export interface CourseEnrollment {
  id: string
  studentName: string
  studentEmail: string
  enrolledAt: number
  progress: number // percentage
  completedAt?: number
  certificateIssued?: boolean
}

export interface Course {
  id: string
  title: string
  slug: string
  description: string
  shortDescription?: string
  coverUrl?: string
  thumbnailUrl?: string
  trailerUrl?: string
  level: CourseLevel
  status: CourseStatus
  category: string
  tags: string[]

  // Delivery
  deliveryMethod: DeliveryMethod
  duration: number // total duration in hours
  startDate?: number
  endDate?: number
  maxStudents?: number

  // Pricing
  pricing: CoursePricing

  // Certificates
  certificateType: CertificateType
  certificateTemplate?: string

  // Content
  modules: CourseModule[]
  prerequisites?: string[]
  learningObjectives: string[]

  // Media
  screenshots?: string[]
  gallery?: string[]

  // Analytics
  enrollments: CourseEnrollment[]
  totalStudents: number
  averageRating: number
  totalReviews: number

  // Team functionality
  team: TeamMember[]
  teamInvites: TeamInvite[]

  // Metadata
  instructor: string
  instructorBio?: string
  createdAt: number
  updatedAt: number
}
