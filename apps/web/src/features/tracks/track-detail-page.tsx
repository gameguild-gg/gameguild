'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Separator } from '@/components/ui/separator';
import {
  BookOpen,
  Users,
  Trophy,
  Star,
  CheckCircle,
  ArrowRight,
  PlayCircle,
  Clock,
  Target,
  Award,
  Code,
  Palette,
  Gamepad2,
  TrendingUp,
  Calendar,
  Download,
  Share2,
} from 'lucide-react';

// Track detail page component
export default function TrackDetailPage({ params }: { params: { id: string } }) {
  const [enrollmentStep, setEnrollmentStep] = useState<'overview' | 'payment' | 'enrolled'>('overview');

  // Mock track data (in real app, fetch by params.id)
  const track = {
    id: 'unity-game-dev',
    title: 'Unity Game Development Master Track',
    description: 'Complete journey from beginner to professional Unity developer',
    longDescription:
      'Master Unity game development through hands-on projects, building 2D and 3D games, understanding C# programming, and learning industry best practices. This comprehensive track is designed to take you from complete beginner to job-ready Unity developer.',
    category: 'Programming',
    level: 'All Levels',
    duration: '6 months',
    totalCourses: 8,
    completedByStudents: 2450,
    rating: 4.9,
    reviewCount: 890,
    image: '/tracks/unity-track.jpg',
    icon: Gamepad2,
    skills: [
      'Unity 3D',
      'C# Programming',
      '2D/3D Game Development',
      'Physics Systems',
      'UI/UX for Games',
      'Game Architecture',
      'Version Control',
      'Publishing',
    ],
    outcomes: [
      'Build complete games',
      'Master Unity Editor',
      'Understand game architecture',
      'Publish to multiple platforms',
      'Work with teams',
      'Build portfolio',
    ],
    prerequisites: ['Basic programming knowledge helpful but not required'],
    price: 299.99,
    isPopular: true,
    isFeatured: true,
    certification: true,
    instructor: {
      name: 'Sarah Chen',
      title: 'Senior Game Developer at Unity',
      image: '/instructors/sarah.jpg',
      experience: '8+ years',
      students: 15000,
    },
    courses: [
      {
        id: '1',
        title: 'Unity Fundamentals',
        duration: '3 weeks',
        level: 'Beginner',
        description: 'Learn Unity interface, basic concepts, and create your first project',
        isCompleted: false,
        isLocked: false,
      },
      {
        id: '2',
        title: 'C# for Game Development',
        duration: '4 weeks',
        level: 'Beginner',
        description: 'Master C# programming specifically for game development',
        isCompleted: false,
        isLocked: true,
      },
      {
        id: '3',
        title: '2D Game Development',
        duration: '5 weeks',
        level: 'Intermediate',
        description: 'Build complete 2D games with sprites, animations, and physics',
        isCompleted: false,
        isLocked: true,
      },
      {
        id: '4',
        title: '3D Game Development',
        duration: '6 weeks',
        level: 'Intermediate',
        description: 'Create immersive 3D games with advanced mechanics',
        isCompleted: false,
        isLocked: true,
      },
      {
        id: '5',
        title: 'Game Physics & Animation',
        duration: '4 weeks',
        level: 'Intermediate',
        description: 'Implement realistic physics and smooth animations',
        isCompleted: false,
        isLocked: true,
      },
      {
        id: '6',
        title: 'UI/UX for Games',
        duration: '3 weeks',
        level: 'Intermediate',
        description: 'Design intuitive and engaging game interfaces',
        isCompleted: false,
        isLocked: true,
      },
      {
        id: '7',
        title: 'Game Publishing & Monetization',
        duration: '2 weeks',
        level: 'Advanced',
        description: 'Learn to publish and monetize your games effectively',
        isCompleted: false,
        isLocked: true,
      },
      {
        id: '8',
        title: 'Capstone Project',
        duration: '4 weeks',
        level: 'Advanced',
        description: 'Build a portfolio-worthy game with mentor guidance',
        isCompleted: false,
        isLocked: true,
      },
    ],
    roadmap: [
      {
        phase: 'Foundation',
        title: 'Learn the Basics',
        description: 'Get familiar with Unity interface and basic programming concepts',
        duration: '7 weeks',
        courses: ['Unity Fundamentals', 'C# for Game Development'],
      },
      {
        phase: 'Development',
        title: 'Build Your First Games',
        description: 'Create 2D and 3D games while learning core development skills',
        duration: '15 weeks',
        courses: ['2D Game Development', '3D Game Development', 'Game Physics & Animation'],
      },
      {
        phase: 'Polish',
        title: 'Professional Skills',
        description: 'Learn UI/UX design and how to publish your games',
        duration: '5 weeks',
        courses: ['UI/UX for Games', 'Game Publishing & Monetization'],
      },
      {
        phase: 'Master',
        title: 'Showcase Your Skills',
        description: 'Build a portfolio-worthy game with mentor guidance',
        duration: '4 weeks',
        courses: ['Capstone Project'],
      },
    ],
  };

  const userProgress = 15; // Mock user progress

  return (
    <div className="min-h-screen bg-background">
      {/* Hero Section with Track Info */}
      <section className="relative bg-gradient-to-br from-purple-500/10 via-blue-500/5 to-background py-16">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Main Track Info */}
            <div className="lg:col-span-2">
              <div className="flex items-center gap-3 mb-4">
                <div className="bg-primary/10 rounded-full p-3">
                  <track.icon className="h-8 w-8 text-primary" />
                </div>
                <div className="flex gap-2">
                  {track.isPopular && <Badge className="bg-orange-500">🔥 Popular</Badge>}
                  {track.isFeatured && <Badge className="bg-purple-500">⭐ Featured</Badge>}
                  <Badge variant="secondary">{track.category}</Badge>
                </div>
              </div>

              <h1 className="text-4xl lg:text-5xl font-bold mb-4">{track.title}</h1>

              <p className="text-xl text-muted-foreground mb-6">{track.longDescription}</p>

              {/* Track Stats */}
              <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
                <div className="text-center p-4 bg-card rounded-lg">
                  <BookOpen className="h-6 w-6 mx-auto mb-2 text-primary" />
                  <p className="text-2xl font-bold">{track.totalCourses}</p>
                  <p className="text-sm text-muted-foreground">Courses</p>
                </div>
                <div className="text-center p-4 bg-card rounded-lg">
                  <Clock className="h-6 w-6 mx-auto mb-2 text-primary" />
                  <p className="text-2xl font-bold">{track.duration}</p>
                  <p className="text-sm text-muted-foreground">Duration</p>
                </div>
                <div className="text-center p-4 bg-card rounded-lg">
                  <Users className="h-6 w-6 mx-auto mb-2 text-primary" />
                  <p className="text-2xl font-bold">{track.completedByStudents.toLocaleString()}</p>
                  <p className="text-sm text-muted-foreground">Students</p>
                </div>
                <div className="text-center p-4 bg-card rounded-lg">
                  <Star className="h-6 w-6 mx-auto mb-2 text-primary" />
                  <p className="text-2xl font-bold">{track.rating}</p>
                  <p className="text-sm text-muted-foreground">Rating</p>
                </div>
              </div>

              {/* Progress Section (if enrolled) */}
              {userProgress > 0 && (
                <Card className="mb-8">
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Target className="h-5 w-5" />
                      Your Progress
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="flex justify-between text-sm mb-2">
                      <span>Track Completion</span>
                      <span>{userProgress}% complete</span>
                    </div>
                    <Progress value={userProgress} className="h-3 mb-4" />
                    <p className="text-sm text-muted-foreground">Keep going! You're making great progress.</p>
                  </CardContent>
                </Card>
              )}
            </div>

            {/* Enrollment Card */}
            <div className="lg:col-span-1">
              <Card className="sticky top-8">
                <CardHeader>
                  <div className="aspect-video bg-gradient-to-br from-purple-500 to-blue-600 rounded-lg mb-4 flex items-center justify-center">
                    <PlayCircle className="h-16 w-16 text-white" />
                  </div>
                  <div className="text-center">
                    <div className="text-3xl font-bold mb-2">${track.price}</div>
                    <p className="text-sm text-muted-foreground">{track.certification && 'Includes Professional Certificate'}</p>
                  </div>
                </CardHeader>
                <CardContent className="space-y-4">
                  <Button size="lg" className="w-full" onClick={() => setEnrollmentStep('payment')}>
                    {userProgress > 0 ? 'Continue Learning' : 'Enroll Now'}
                    <ArrowRight className="h-4 w-4 ml-2" />
                  </Button>

                  <Button variant="outline" size="lg" className="w-full">
                    <PlayCircle className="h-4 w-4 mr-2" />
                    Preview Track
                  </Button>

                  <div className="flex justify-center gap-4 pt-4">
                    <Button variant="ghost" size="sm">
                      <Share2 className="h-4 w-4 mr-1" />
                      Share
                    </Button>
                    <Button variant="ghost" size="sm">
                      <Download className="h-4 w-4 mr-1" />
                      Syllabus
                    </Button>
                  </div>

                  {/* Instructor Info */}
                  <Separator />
                  <div className="space-y-3">
                    <h4 className="font-semibold">Your Instructor</h4>
                    <div className="flex items-center gap-3">
                      <img
                        src={track.instructor.image}
                        alt={track.instructor.name}
                        className="h-12 w-12 rounded-full object-cover"
                        onError={(e) => {
                          e.currentTarget.src = '/placeholder-avatar.jpg';
                        }}
                      />
                      <div>
                        <p className="font-medium">{track.instructor.name}</p>
                        <p className="text-sm text-muted-foreground">{track.instructor.title}</p>
                        <p className="text-xs text-muted-foreground">
                          {track.instructor.experience} • {track.instructor.students.toLocaleString()} students
                        </p>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </section>

      {/* Track Content */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          <Tabs defaultValue="curriculum" className="w-full">
            <TabsList className="grid w-full grid-cols-4 lg:w-auto">
              <TabsTrigger value="curriculum">Curriculum</TabsTrigger>
              <TabsTrigger value="roadmap">Learning Path</TabsTrigger>
              <TabsTrigger value="outcomes">Outcomes</TabsTrigger>
              <TabsTrigger value="reviews">Reviews</TabsTrigger>
            </TabsList>

            <TabsContent value="curriculum" className="mt-8">
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                <div className="lg:col-span-2 space-y-4">
                  {track.courses.map((course, index) => (
                    <Card key={course.id} className={`transition-all duration-200 ${course.isLocked ? 'opacity-60' : 'hover:shadow-md'}`}>
                      <CardContent className="p-6">
                        <div className="flex items-center justify-between mb-3">
                          <div className="flex items-center gap-3">
                            <div
                              className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold ${
                                course.isCompleted
                                  ? 'bg-green-500 text-white'
                                  : course.isLocked
                                    ? 'bg-muted text-muted-foreground'
                                    : 'bg-primary text-primary-foreground'
                              }`}
                            >
                              {course.isCompleted ? <CheckCircle className="h-4 w-4" /> : index + 1}
                            </div>
                            <div>
                              <h3 className="font-semibold">{course.title}</h3>
                              <p className="text-sm text-muted-foreground">{course.description}</p>
                            </div>
                          </div>
                          <div className="text-right">
                            <p className="text-sm font-medium">{course.duration}</p>
                            <Badge variant="outline" className="text-xs">
                              {course.level}
                            </Badge>
                          </div>
                        </div>
                        {!course.isLocked && (
                          <Button size="sm" variant="outline" className="mt-2">
                            <PlayCircle className="h-3 w-3 mr-1" />
                            {course.isCompleted ? 'Review' : 'Start Course'}
                          </Button>
                        )}
                      </CardContent>
                    </Card>
                  ))}
                </div>

                {/* Track Skills */}
                <div className="lg:col-span-1">
                  <Card>
                    <CardHeader>
                      <CardTitle>Skills You'll Gain</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-2">
                        {track.skills.map((skill) => (
                          <Badge key={skill} variant="secondary" className="mr-2 mb-2">
                            {skill}
                          </Badge>
                        ))}
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </div>
            </TabsContent>

            <TabsContent value="roadmap" className="mt-8">
              <div className="space-y-8">
                {track.roadmap.map((phase, index) => (
                  <Card key={phase.phase}>
                    <CardContent className="p-6">
                      <div className="flex items-center gap-4 mb-4">
                        <div className="bg-primary text-primary-foreground rounded-full w-10 h-10 flex items-center justify-center font-bold">{index + 1}</div>
                        <div>
                          <h3 className="text-xl font-semibold">{phase.title}</h3>
                          <p className="text-muted-foreground">{phase.description}</p>
                          <p className="text-sm text-muted-foreground mt-1">
                            <Calendar className="h-3 w-3 inline mr-1" />
                            {phase.duration}
                          </p>
                        </div>
                      </div>
                      <div className="ml-14">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                          {phase.courses.map((courseName) => (
                            <div key={courseName} className="flex items-center gap-2 p-2 bg-muted rounded">
                              <BookOpen className="h-4 w-4 text-primary" />
                              <span className="text-sm">{courseName}</span>
                            </div>
                          ))}
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </TabsContent>

            <TabsContent value="outcomes" className="mt-8">
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Trophy className="h-5 w-5" />
                      Learning Outcomes
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ul className="space-y-3">
                      {track.outcomes.map((outcome) => (
                        <li key={outcome} className="flex items-start gap-2">
                          <CheckCircle className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                          <span className="text-sm">{outcome}</span>
                        </li>
                      ))}
                    </ul>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Award className="h-5 w-5" />
                      Certification
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-center py-8">
                      <div className="bg-primary/10 rounded-full p-6 w-24 h-24 mx-auto mb-4 flex items-center justify-center">
                        <Award className="h-12 w-12 text-primary" />
                      </div>
                      <h3 className="font-semibold mb-2">Professional Certificate</h3>
                      <p className="text-sm text-muted-foreground mb-4">
                        Earn a professional certificate upon completion to showcase your skills to employers.
                      </p>
                      <Button variant="outline" size="sm">
                        View Sample Certificate
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              </div>
            </TabsContent>

            <TabsContent value="reviews" className="mt-8">
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                <div className="lg:col-span-2 space-y-6">
                  {/* Sample reviews */}
                  {[1, 2, 3].map((review) => (
                    <Card key={review}>
                      <CardContent className="p-6">
                        <div className="flex items-center gap-3 mb-3">
                          <img
                            src={`/avatars/user-${review}.jpg`}
                            alt="User"
                            className="h-10 w-10 rounded-full object-cover"
                            onError={(e) => {
                              e.currentTarget.src = '/placeholder-avatar.jpg';
                            }}
                          />
                          <div>
                            <p className="font-medium">Student {review}</p>
                            <div className="flex items-center gap-1">
                              {[1, 2, 3, 4, 5].map((star) => (
                                <Star key={star} className="h-3 w-3 fill-yellow-400 text-yellow-400" />
                              ))}
                            </div>
                          </div>
                        </div>
                        <p className="text-sm text-muted-foreground">
                          "This track completely transformed my game development skills. The hands-on projects were incredibly valuable and the instructor
                          support was excellent."
                        </p>
                      </CardContent>
                    </Card>
                  ))}
                </div>

                <div className="lg:col-span-1">
                  <Card>
                    <CardHeader>
                      <CardTitle>Reviews Summary</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="text-center mb-6">
                        <div className="text-4xl font-bold">{track.rating}</div>
                        <div className="flex justify-center gap-1 mt-1">
                          {[1, 2, 3, 4, 5].map((star) => (
                            <Star key={star} className="h-4 w-4 fill-yellow-400 text-yellow-400" />
                          ))}
                        </div>
                        <p className="text-sm text-muted-foreground mt-1">Based on {track.reviewCount} reviews</p>
                      </div>

                      {/* Rating breakdown */}
                      <div className="space-y-2">
                        {[5, 4, 3, 2, 1].map((rating) => (
                          <div key={rating} className="flex items-center gap-2 text-sm">
                            <span>{rating}★</span>
                            <Progress value={rating === 5 ? 75 : rating === 4 ? 20 : 5} className="flex-1 h-2" />
                            <span className="text-muted-foreground">{rating === 5 ? '75%' : rating === 4 ? '20%' : '5%'}</span>
                          </div>
                        ))}
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </div>
            </TabsContent>
          </Tabs>
        </div>
      </section>
    </div>
  );
}
