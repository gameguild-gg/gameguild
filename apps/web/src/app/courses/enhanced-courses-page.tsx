'use client';

import React, { useState, useMemo } from 'react';
import { Suspense } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  BookOpen,
  Users,
  Trophy,
  Zap,
  Code,
  Palette,
  Gamepad2,
  Star,
  CheckCircle,
  ArrowRight,
  PlayCircle,
  Filter,
  Search,
  Clock,
  User,
  DollarSign,
} from 'lucide-react';

// Enhanced course interface
interface Course {
  id: string;
  title: string;
  description: string;
  instructor: {
    name: string;
    avatar: string;
    title: string;
  };
  category: string;
  level: 'Beginner' | 'Intermediate' | 'Advanced';
  duration: string;
  price: number;
  rating: number;
  studentsCount: number;
  image: string;
  tags: string[];
  isPopular?: boolean;
  isFree?: boolean;
  certification?: boolean;
}

// Mock course data
const mockCourses: Course[] = [
  {
    id: '1',
    title: 'Game Development with Unity',
    description: 'Learn to create stunning 3D games using Unity engine from scratch.',
    instructor: {
      name: 'Alex Johnson',
      avatar: '/avatars/alex.jpg',
      title: 'Senior Game Developer',
    },
    category: 'Programming',
    level: 'Beginner',
    duration: '12 weeks',
    price: 99.99,
    rating: 4.8,
    studentsCount: 1250,
    image: '/courses/unity-course.jpg',
    tags: ['Unity', 'C#', '3D', 'Game Engine'],
    isPopular: true,
    certification: true,
  },
  {
    id: '2',
    title: 'Advanced Game Art & Animation',
    description: 'Master character design, environment art, and animation techniques.',
    instructor: {
      name: 'Sarah Chen',
      avatar: '/avatars/sarah.jpg',
      title: 'Lead Game Artist',
    },
    category: 'Art & Design',
    level: 'Advanced',
    duration: '16 weeks',
    price: 149.99,
    rating: 4.9,
    studentsCount: 890,
    image: '/courses/game-art.jpg',
    tags: ['Blender', '3D Modeling', 'Animation', 'Character Design'],
    certification: true,
  },
  {
    id: '3',
    title: 'Mobile Game Development',
    description: 'Build engaging mobile games for iOS and Android platforms.',
    instructor: {
      name: 'Mike Rodriguez',
      avatar: '/avatars/mike.jpg',
      title: 'Mobile Game Specialist',
    },
    category: 'Programming',
    level: 'Intermediate',
    duration: '10 weeks',
    price: 79.99,
    rating: 4.7,
    studentsCount: 2100,
    image: '/courses/mobile-game.jpg',
    tags: ['React Native', 'Flutter', 'Mobile', 'Cross-platform'],
  },
  {
    id: '4',
    title: 'Game Music & Sound Design',
    description: 'Create immersive audio experiences for your games.',
    instructor: {
      name: 'Emma Wilson',
      avatar: '/avatars/emma.jpg',
      title: 'Audio Director',
    },
    category: 'Audio',
    level: 'Beginner',
    duration: '8 weeks',
    price: 0,
    rating: 4.6,
    studentsCount: 1580,
    image: '/courses/audio-design.jpg',
    tags: ['FMOD', 'Wwise', 'Music Production', 'Sound Effects'],
    isFree: true,
  },
];

const categories = ['All', 'Programming', 'Art & Design', 'Audio', 'Business'];
const levels = ['All', 'Beginner', 'Intermediate', 'Advanced'];

export default function EnhancedCoursesPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('All');
  const [selectedLevel, setSelectedLevel] = useState('All');
  const [sortBy, setSortBy] = useState('popular');

  const filteredCourses = useMemo(() => {
    let filtered = mockCourses.filter((course) => {
      const matchesSearch =
        course.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        course.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
        course.tags.some((tag) => tag.toLowerCase().includes(searchQuery.toLowerCase()));

      const matchesCategory = selectedCategory === 'All' || course.category === selectedCategory;
      const matchesLevel = selectedLevel === 'All' || course.level === selectedLevel;

      return matchesSearch && matchesCategory && matchesLevel;
    });

    // Sort courses
    switch (sortBy) {
      case 'rating':
        return filtered.sort((a, b) => b.rating - a.rating);
      case 'price':
        return filtered.sort((a, b) => a.price - b.price);
      case 'students':
        return filtered.sort((a, b) => b.studentsCount - a.studentsCount);
      default:
        return filtered.sort((a, b) => (b.isPopular ? 1 : 0) - (a.isPopular ? 1 : 0));
    }
  }, [searchQuery, selectedCategory, selectedLevel, sortBy]);

  return (
    <div className="min-h-screen bg-background">
      {/* Enhanced Hero Section */}
      <section className="relative bg-gradient-to-br from-primary/10 via-primary/5 to-background py-20 lg:py-32">
        <div className="container mx-auto px-4">
          <div className="max-w-4xl mx-auto text-center">
            <Badge variant="outline" className="mb-6 text-sm font-medium">
              🎮 Master Game Development
            </Badge>
            <h1 className="text-4xl lg:text-6xl font-bold mb-6 bg-gradient-to-r from-primary to-primary/60 bg-clip-text text-transparent">
              Learn from Industry Experts
            </h1>
            <p className="text-xl lg:text-2xl text-muted-foreground mb-8 max-w-3xl mx-auto">
              Join thousands of students learning game development from industry professionals. Build real projects and advance your career.
            </p>

            {/* Enhanced Search Bar */}
            <div className="max-w-2xl mx-auto mb-12">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-muted-foreground" />
                <Input
                  type="text"
                  placeholder="Search courses, skills, or instructors..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-10 pr-4 py-3 text-lg"
                />
              </div>
            </div>

            {/* Key Statistics */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-6 max-w-4xl mx-auto">
              <div className="text-center">
                <div className="bg-primary/10 rounded-full p-3 w-16 h-16 mx-auto mb-3 flex items-center justify-center">
                  <Users className="h-6 w-6 text-primary" />
                </div>
                <p className="text-2xl font-bold">50K+</p>
                <p className="text-sm text-muted-foreground">Active Students</p>
              </div>
              <div className="text-center">
                <div className="bg-primary/10 rounded-full p-3 w-16 h-16 mx-auto mb-3 flex items-center justify-center">
                  <BookOpen className="h-6 w-6 text-primary" />
                </div>
                <p className="text-2xl font-bold">200+</p>
                <p className="text-sm text-muted-foreground">Expert Courses</p>
              </div>
              <div className="text-center">
                <div className="bg-primary/10 rounded-full p-3 w-16 h-16 mx-auto mb-3 flex items-center justify-center">
                  <Trophy className="h-6 w-6 text-primary" />
                </div>
                <p className="text-2xl font-bold">95%</p>
                <p className="text-sm text-muted-foreground">Success Rate</p>
              </div>
              <div className="text-center">
                <div className="bg-primary/10 rounded-full p-3 w-16 h-16 mx-auto mb-3 flex items-center justify-center">
                  <Zap className="h-6 w-6 text-primary" />
                </div>
                <p className="text-2xl font-bold">24/7</p>
                <p className="text-sm text-muted-foreground">Support</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Filters and Course Grid */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          {/* Filter Controls */}
          <div className="mb-8">
            <Tabs defaultValue="courses" className="w-full">
              <TabsList className="grid w-full grid-cols-2 lg:w-auto lg:grid-cols-4">
                <TabsTrigger value="courses">All Courses</TabsTrigger>
                <TabsTrigger value="free">Free Courses</TabsTrigger>
                <TabsTrigger value="certification">With Certification</TabsTrigger>
                <TabsTrigger value="new">New Releases</TabsTrigger>
              </TabsList>

              <div className="flex flex-wrap gap-4 mt-6">
                {/* Category Filter */}
                <div className="flex gap-2">
                  <Button variant="outline" size="sm">
                    <Filter className="h-4 w-4 mr-2" />
                    Category
                  </Button>
                  {categories.map((category) => (
                    <Button
                      key={category}
                      variant={selectedCategory === category ? 'default' : 'outline'}
                      size="sm"
                      onClick={() => setSelectedCategory(category)}
                    >
                      {category}
                    </Button>
                  ))}
                </div>

                {/* Level Filter */}
                <div className="flex gap-2">
                  {levels.map((level) => (
                    <Button key={level} variant={selectedLevel === level ? 'default' : 'outline'} size="sm" onClick={() => setSelectedLevel(level)}>
                      {level}
                    </Button>
                  ))}
                </div>

                {/* Sort Options */}
                <select value={sortBy} onChange={(e) => setSortBy(e.target.value)} className="px-3 py-1 border rounded-md text-sm">
                  <option value="popular">Most Popular</option>
                  <option value="rating">Highest Rated</option>
                  <option value="price">Price: Low to High</option>
                  <option value="students">Most Students</option>
                </select>
              </div>

              <TabsContent value="courses" className="mt-8">
                <CourseGrid courses={filteredCourses} />
              </TabsContent>
            </Tabs>
          </div>
        </div>
      </section>
    </div>
  );
}

// Enhanced Course Grid Component
function CourseGrid({ courses }: { courses: Course[] }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      {courses.map((course) => (
        <CourseCard key={course.id} course={course} />
      ))}
    </div>
  );
}

// Enhanced Course Card Component
function CourseCard({ course }: { course: Course }) {
  return (
    <Card className="group hover:shadow-lg transition-all duration-300 hover:-translate-y-1">
      <CardHeader className="p-0">
        <div className="relative">
          <img
            src={course.image}
            alt={course.title}
            className="w-full h-48 object-cover rounded-t-lg"
            onError={(e) => {
              e.currentTarget.src = '/placeholder-course.jpg';
            }}
          />
          {course.isPopular && <Badge className="absolute top-2 left-2 bg-orange-500">🔥 Popular</Badge>}
          {course.isFree && <Badge className="absolute top-2 right-2 bg-green-500">Free</Badge>}
          <Button size="sm" className="absolute bottom-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity" asChild>
            <Link href={`/courses/${course.id}`}>
              <PlayCircle className="h-4 w-4 mr-1" />
              Preview
            </Link>
          </Button>
        </div>
      </CardHeader>

      <CardContent className="p-6">
        <div className="flex items-center gap-2 mb-2">
          <Badge variant="secondary" className="text-xs">
            {course.category}
          </Badge>
          <Badge variant="outline" className="text-xs">
            {course.level}
          </Badge>
        </div>

        <CardTitle className="text-lg mb-2 line-clamp-2">{course.title}</CardTitle>

        <p className="text-sm text-muted-foreground mb-4 line-clamp-2">{course.description}</p>

        {/* Instructor Info */}
        <div className="flex items-center gap-2 mb-4">
          <img
            src={course.instructor.avatar}
            alt={course.instructor.name}
            className="w-6 h-6 rounded-full"
            onError={(e) => {
              e.currentTarget.src = '/default-avatar.jpg';
            }}
          />
          <span className="text-xs text-muted-foreground">{course.instructor.name}</span>
        </div>

        {/* Course Meta */}
        <div className="flex items-center justify-between text-xs text-muted-foreground mb-4">
          <div className="flex items-center gap-1">
            <Star className="h-3 w-3 fill-yellow-400 text-yellow-400" />
            <span>{course.rating}</span>
          </div>
          <div className="flex items-center gap-1">
            <Users className="h-3 w-3" />
            <span>{course.studentsCount.toLocaleString()}</span>
          </div>
          <div className="flex items-center gap-1">
            <Clock className="h-3 w-3" />
            <span>{course.duration}</span>
          </div>
        </div>

        {/* Tags */}
        <div className="flex flex-wrap gap-1 mb-4">
          {course.tags.slice(0, 3).map((tag) => (
            <Badge key={tag} variant="outline" className="text-xs">
              {tag}
            </Badge>
          ))}
        </div>

        {/* Price and Enroll */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1">
            {course.price === 0 ? (
              <span className="text-lg font-bold text-green-600">Free</span>
            ) : (
              <>
                <DollarSign className="h-4 w-4" />
                <span className="text-lg font-bold">{course.price}</span>
              </>
            )}
          </div>
          <Button asChild size="sm">
            <Link href={`/courses/${course.id}`}>Enroll Now</Link>
          </Button>
        </div>

        {course.certification && (
          <div className="flex items-center gap-1 mt-2 text-xs text-primary">
            <CheckCircle className="h-3 w-3" />
            <span>Certificate included</span>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
