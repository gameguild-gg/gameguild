"use client"

import type { GameProject, Course } from "./types"

const KEY = "v0:games"
const COURSES_KEY = "v0:courses"

function read(): GameProject[] {
  if (typeof window === "undefined") return []
  try {
    const raw = localStorage.getItem(KEY)
    return raw ? (JSON.parse(raw) as GameProject[]) : []
  } catch {
    return []
  }
}

function write(data: GameProject[]) {
  if (typeof window === "undefined") return
  localStorage.setItem(KEY, JSON.stringify(data))
}

function readCourses(): Course[] {
  if (typeof window === "undefined") return []
  try {
    const raw = localStorage.getItem(COURSES_KEY)
    return raw ? (JSON.parse(raw) as Course[]) : []
  } catch {
    return []
  }
}

function writeCourses(data: Course[]) {
  if (typeof window === "undefined") return
  localStorage.setItem(COURSES_KEY, JSON.stringify(data))
}

export function listProjects(): GameProject[] {
  return read().sort((a, b) => b.updatedAt - a.updatedAt)
}

export function getProject(id: string): GameProject | undefined {
  return read().find((p) => p.id === id)
}

export function getProjectBySlug(slug: string): GameProject | undefined {
  return read().find((p) => p.slug === slug)
}

export function addProject(p: GameProject) {
  const data = read()
  data.push(p)
  write(data)
}

export function updateProject(id: string, patch: Partial<GameProject>) {
  const data = read()
  const idx = data.findIndex((p) => p.id === id)
  if (idx !== -1) {
    data[idx] = { ...data[idx], ...patch, updatedAt: Date.now() } as GameProject
    write(data)
    return data[idx]
  }
  return undefined
}

export function listCourses(): Course[] {
  return readCourses().sort((a, b) => b.updatedAt - a.updatedAt)
}

export function getCourse(id: string): Course | undefined {
  return readCourses().find((c) => c.id === id)
}

export function getCourseBySlug(slug: string): Course | undefined {
  return readCourses().find((c) => c.slug === slug)
}

export function addCourse(course: Course) {
  const data = readCourses()
  data.push(course)
  writeCourses(data)
}

export function updateCourse(id: string, patch: Partial<Course>) {
  const data = readCourses()
  const idx = data.findIndex((c) => c.id === id)
  if (idx !== -1) {
    data[idx] = { ...data[idx], ...patch, updatedAt: Date.now() } as Course
    writeCourses(data)
    return data[idx]
  }
  return undefined
}

// Demo data functions
export function upsertDemoIfEmpty() {
  const current = read()
  if (current.length > 0) return
  
  const now = Date.now()
  const demo: GameProject[] = [
    {
      id: "demo-celestia",
      title: "Celestia Run",
      slug: "celestia-run",
      tagline: "Endless runner among the stars",
      kind: "web",
      releaseStatus: "beta",
      pricing: "free",
      visibility: "public",
      description: "Dash through nebulae, dodge comets, and collect stardust in this thrilling endless runner.",
      team: [],
      teamInvites: [],
      createdAt: now - 1000 * 60 * 60 * 24 * 10,
      updatedAt: now - 1000 * 60 * 60 * 18,
    },
  ]
  write(demo)
}

export function upsertCourseDemoIfEmpty() {
  const current = readCourses()
  if (current.length > 0) return
  
  const now = Date.now()
  const demoCourses: Course[] = [
    {
      id: "course-game-dev-101",
      title: "Game Development Fundamentals",
      slug: "game-development-fundamentals",
      description: "Learn the core concepts of game development from scratch.",
      shortDescription: "Master the fundamentals of game development with hands-on projects",
      level: "beginner",
      status: "published",
      category: "Game Development",
      tags: ["Unity", "C#", "Beginner", "Programming"],
      deliveryMethod: "self-paced",
      duration: 40,
      pricing: {
        type: "paid",
        price: 99.99,
        currency: "USD",
      },
      certificateType: "completion",
      modules: [],
      learningObjectives: [
        "Understand core game development concepts",
        "Create your first playable game",
        "Learn industry-standard tools and workflows",
      ],
      enrollments: [],
      totalStudents: 1247,
      averageRating: 4.8,
      totalReviews: 156,
      instructor: "Sarah Johnson",
      team: [],
      teamInvites: [],
      createdAt: now - 1000 * 60 * 60 * 24 * 30,
      updatedAt: now - 1000 * 60 * 60 * 24 * 2,
    },
    {
      id: "course-business-marketing",
      title: "Business Track: Game Marketing & Monetization",
      slug: "business-track-game-marketing-monetization",
      description: "Master the business side of game development with comprehensive marketing and monetization strategies.",
      shortDescription: "Learn proven strategies for marketing your games and building sustainable revenue streams",
      level: "intermediate",
      status: "published",
      category: "Business",
      tags: ["Marketing", "Monetization", "Business", "Analytics", "Strategy"],
      deliveryMethod: "self-paced",
      duration: 32,
      pricing: {
        type: "paid",
        price: 149.99,
        currency: "USD",
      },
      certificateType: "completion",
      modules: [],
      learningObjectives: [
        "Develop effective game marketing strategies",
        "Implement proven monetization models",
        "Analyze market trends and competitor strategies",
        "Build sustainable revenue streams",
        "Create compelling marketing campaigns",
      ],
      enrollments: [],
      totalStudents: 892,
      averageRating: 4.6,
      totalReviews: 127,
      instructor: "Marcus Rodriguez",
      instructorBio: "Former marketing director at major game studios with 10+ years of experience in game marketing and monetization strategies.",
      team: [
        {
          id: "team-marcus",
          name: "Marcus Rodriguez",
          email: "marcus@gameguild.gg",
          role: "owner",
          joinedAt: now - 1000 * 60 * 60 * 24 * 60,
          permissions: {
            canEdit: true,
            canPublish: true,
            canManageTeam: true,
            canViewAnalytics: true,
            canManageSettings: true,
          }
        }
      ],
      teamInvites: [],
      createdAt: now - 1000 * 60 * 60 * 24 * 45,
      updatedAt: now - 1000 * 60 * 60 * 24 * 1,
    },
  ]
  writeCourses(demoCourses)
}
