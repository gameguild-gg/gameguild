'use client';

import React, { useState, useMemo } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Progress } from '@/components/ui/progress';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  BookOpen,
  Users,
  Trophy,
  Zap,
  Star,
  CheckCircle,
  ArrowRight,
  PlayCircle,
  Search,
  Clock,
  User,
  Target,
  Award,
  TrendingUp,
  Map,
  Code,
  Palette,
  Gamepad2,
} from 'lucide-react';

// Enhanced track interface
interface Track {
  id: string;
  title: string;
  description: string;
  longDescription: string;
  category: string;
  level: 'Beginner' | 'Intermediate' | 'Advanced' | 'All Levels';
  duration: string;
  totalCourses: number;
  completedByStudents: number;
  rating: number;
  image: string;
  icon: React.ComponentType<{ className?: string }>;
  skills: string[];
  outcomes: string[];
  prerequisites?: string[];
  price: number;
  isPopular?: boolean;
  isFeatured?: boolean;
  certification?: boolean;
  courses: {
    id: string;
    title: string;
    duration: string;
    level: string;
    isCompleted?: boolean;
  }[];
  roadmap: {
    phase: string;
    title: string;
    description: string;
    courses: string[];
  }[];
}

// Mock track data
const mockTracks: Track[] = [
  {
    id: 'unity-game-dev',
    title: 'Unity Game Development Master Track',
    description: 'Complete journey from beginner to professional Unity developer',
    longDescription:
      'Master Unity game development through hands-on projects, building 2D and 3D games, understanding C# programming, and learning industry best practices.',
    category: 'Programming',
    level: 'All Levels',
    duration: '6 months',
    totalCourses: 8,
    completedByStudents: 2450,
    rating: 4.9,
    image: '/tracks/unity-track.jpg',
    icon: Gamepad2,
    skills: ['Unity 3D', 'C# Programming', '2D/3D Game Development', 'Physics Systems', 'UI/UX for Games'],
    outcomes: ['Build complete games', 'Master Unity Editor', 'Understand game architecture', 'Publish to multiple platforms'],
    prerequisites: ['Basic programming knowledge helpful but not required'],
    price: 299.99,
    isPopular: true,
    isFeatured: true,
    certification: true,
    courses: [
      { id: '1', title: 'Unity Fundamentals', duration: '3 weeks', level: 'Beginner' },
      { id: '2', title: 'C# for Game Development', duration: '4 weeks', level: 'Beginner' },
      { id: '3', title: '2D Game Development', duration: '5 weeks', level: 'Intermediate' },
      { id: '4', title: '3D Game Development', duration: '6 weeks', level: 'Intermediate' },
      { id: '5', title: 'Game Physics & Animation', duration: '4 weeks', level: 'Intermediate' },
      { id: '6', title: 'UI/UX for Games', duration: '3 weeks', level: 'Intermediate' },
      { id: '7', title: 'Game Publishing & Monetization', duration: '2 weeks', level: 'Advanced' },
      { id: '8', title: 'Capstone Project', duration: '4 weeks', level: 'Advanced' },
    ],
    roadmap: [
      {
        phase: 'Foundation',
        title: 'Learn the Basics',
        description: 'Get familiar with Unity interface and basic programming concepts',
        courses: ['Unity Fundamentals', 'C# for Game Development'],
      },
      {
        phase: 'Development',
        title: 'Build Your First Games',
        description: 'Create 2D and 3D games while learning core development skills',
        courses: ['2D Game Development', '3D Game Development', 'Game Physics & Animation'],
      },
      {
        phase: 'Polish',
        title: 'Professional Skills',
        description: 'Learn UI/UX design and how to publish your games',
        courses: ['UI/UX for Games', 'Game Publishing & Monetization'],
      },
      {
        phase: 'Master',
        title: 'Showcase Your Skills',
        description: 'Build a portfolio-worthy game with mentor guidance',
        courses: ['Capstone Project'],
      },
    ],
  },
  {
    id: 'game-art-design',
    title: 'Game Art & Design Professional Track',
    description: 'Become a skilled game artist with expertise in 2D/3D art and animation',
    longDescription: 'Learn comprehensive game art creation from concept art to 3D modeling, texturing, animation, and environment design.',
    category: 'Art & Design',
    level: 'Beginner',
    duration: '8 months',
    totalCourses: 10,
    completedByStudents: 1890,
    rating: 4.8,
    image: '/tracks/art-track.jpg',
    icon: Palette,
    skills: ['Digital Art', '3D Modeling', 'Texturing', 'Animation', 'Concept Art', 'Environment Design'],
    outcomes: ['Create professional game art', 'Build a portfolio', 'Master industry tools', 'Understand art pipelines'],
    prerequisites: ['No prior art experience required'],
    price: 399.99,
    isFeatured: true,
    certification: true,
    courses: [
      { id: '1', title: 'Digital Art Fundamentals', duration: '3 weeks', level: 'Beginner' },
      { id: '2', title: 'Concept Art & Design', duration: '4 weeks', level: 'Beginner' },
      { id: '3', title: '3D Modeling Basics', duration: '5 weeks', level: 'Beginner' },
      { id: '4', title: 'Advanced 3D Modeling', duration: '5 weeks', level: 'Intermediate' },
      { id: '5', title: 'Texturing & Materials', duration: '4 weeks', level: 'Intermediate' },
      { id: '6', title: 'Character Animation', duration: '6 weeks', level: 'Intermediate' },
      { id: '7', title: 'Environment Design', duration: '5 weeks', level: 'Intermediate' },
      { id: '8', title: 'Lighting & Rendering', duration: '3 weeks', level: 'Advanced' },
      { id: '9', title: 'Portfolio Development', duration: '3 weeks', level: 'Advanced' },
      { id: '10', title: 'Industry Workflows', duration: '2 weeks', level: 'Advanced' },
    ],
    roadmap: [
      {
        phase: 'Foundation',
        title: 'Art Fundamentals',
        description: 'Learn basic digital art skills and design principles',
        courses: ['Digital Art Fundamentals', 'Concept Art & Design'],
      },
      {
        phase: 'Modeling',
        title: '3D Creation Skills',
        description: 'Master 3D modeling and texturing techniques',
        courses: ['3D Modeling Basics', 'Advanced 3D Modeling', 'Texturing & Materials'],
      },
      {
        phase: 'Animation',
        title: 'Bring Art to Life',
        description: 'Learn character animation and environment design',
        courses: ['Character Animation', 'Environment Design', 'Lighting & Rendering'],
      },
      {
        phase: 'Professional',
        title: 'Industry Ready',
        description: 'Build your portfolio and learn professional workflows',
        courses: ['Portfolio Development', 'Industry Workflows'],
      },
    ],
  },
  {
    id: 'indie-game-business',
    title: 'Indie Game Development & Business Track',
    description: 'Learn to build and market successful indie games',
    longDescription: 'Complete guide to indie game development including game design, development, marketing, and business strategies.',
    category: 'Business',
    level: 'Intermediate',
    duration: '4 months',
    totalCourses: 6,
    completedByStudents: 980,
    rating: 4.7,
    image: '/tracks/business-track.jpg',
    icon: TrendingUp,
    skills: ['Game Design', 'Project Management', 'Marketing', 'Publishing', 'Analytics', 'Monetization'],
    outcomes: ['Launch your own game', 'Build a sustainable business', 'Master marketing strategies', 'Understand game analytics'],
    prerequisites: ['Basic game development knowledge', 'Some programming experience'],
    price: 199.99,
    certification: true,
    courses: [
      { id: '1', title: 'Game Design Principles', duration: '4 weeks', level: 'Intermediate' },
      { id: '2', title: 'Rapid Prototyping', duration: '3 weeks', level: 'Intermediate' },
      { id: '3', title: 'Game Marketing Strategy', duration: '4 weeks', level: 'Intermediate' },
      { id: '4', title: 'Publishing & Distribution', duration: '3 weeks', level: 'Intermediate' },
      { id: '5', title: 'Game Analytics & Metrics', duration: '3 weeks', level: 'Advanced' },
      { id: '6', title: 'Business Models & Monetization', duration: '3 weeks', level: 'Advanced' },
    ],
    roadmap: [
      {
        phase: 'Design',
        title: 'Game Concept',
        description: 'Learn to design engaging games and create prototypes',
        courses: ['Game Design Principles', 'Rapid Prototyping'],
      },
      {
        phase: 'Marketing',
        title: 'Reach Your Audience',
        description: 'Master marketing and publishing strategies',
        courses: ['Game Marketing Strategy', 'Publishing & Distribution'],
      },
      {
        phase: 'Business',
        title: 'Sustainable Success',
        description: 'Understand analytics and monetization for long-term success',
        courses: ['Game Analytics & Metrics', 'Business Models & Monetization'],
      },
    ],
  },
];

const categories = ['All', 'Programming', 'Art & Design', 'Audio', 'Business'];
const levels = ['All', 'Beginner', 'Intermediate', 'Advanced', 'All Levels'];

export default function EnhancedTracksPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('All');
  const [selectedLevel, setSelectedLevel] = useState('All');
  const [sortBy, setSortBy] = useState('popular');

  const filteredTracks = useMemo(() => {
    let filtered = mockTracks.filter((track) => {
      const matchesSearch =
        track.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        track.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
        track.skills.some((skill) => skill.toLowerCase().includes(searchQuery.toLowerCase()));

      const matchesCategory = selectedCategory === 'All' || track.category === selectedCategory;
      const matchesLevel = selectedLevel === 'All' || track.level === selectedLevel;

      return matchesSearch && matchesCategory && matchesLevel;
    });

    // Sort tracks
    switch (sortBy) {
      case 'rating':
        return filtered.sort((a, b) => b.rating - a.rating);
      case 'price':
        return filtered.sort((a, b) => a.price - b.price);
      case 'students':
        return filtered.sort((a, b) => b.completedByStudents - a.completedByStudents);
      case 'duration':
        return filtered.sort((a, b) => a.totalCourses - b.totalCourses);
      default:
        return filtered.sort((a, b) => (b.isPopular ? 1 : 0) - (a.isPopular ? 1 : 0));
    }
  }, [searchQuery, selectedCategory, selectedLevel, sortBy]);

  return (
    <div className="min-h-screen bg-background">
      {/* Enhanced Hero Section */}
      <section className="relative bg-gradient-to-br from-purple-500/10 via-blue-500/5 to-background py-20 lg:py-32">
        <div className="container mx-auto px-4">
          <div className="max-w-4xl mx-auto text-center">
            <Badge variant="outline" className="mb-6 text-sm font-medium">
              🚀 Complete Learning Paths
            </Badge>
            <h1 className="text-4xl lg:text-6xl font-bold mb-6 bg-gradient-to-r from-purple-600 to-blue-600 bg-clip-text text-transparent">
              Master Game Development Tracks
            </h1>
            <p className="text-xl lg:text-2xl text-muted-foreground mb-8 max-w-3xl mx-auto">
              Follow structured learning paths designed by industry experts. From beginner to professional, achieve your game development goals.
            </p>

            {/* Enhanced Search Bar */}
            <div className="max-w-2xl mx-auto mb-12">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-muted-foreground" />
                <Input
                  type="text"
                  placeholder="Search learning tracks, skills, or outcomes..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-10 pr-4 py-3 text-lg"
                />
              </div>
            </div>

            {/* Key Features */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-6 max-w-4xl mx-auto">
              <div className="text-center">
                <div className="bg-purple-500/10 rounded-full p-3 w-16 h-16 mx-auto mb-3 flex items-center justify-center">
                  <Map className="h-6 w-6 text-purple-600" />
                </div>
                <p className="text-2xl font-bold">15+</p>
                <p className="text-sm text-muted-foreground">Learning Tracks</p>
              </div>
              <div className="text-center">
                <div className="bg-blue-500/10 rounded-full p-3 w-16 h-16 mx-auto mb-3 flex items-center justify-center">
                  <Target className="h-6 w-6 text-blue-600" />
                </div>
                <p className="text-2xl font-bold">100%</p>
                <p className="text-sm text-muted-foreground">Goal Oriented</p>
              </div>
              <div className="text-center">
                <div className="bg-green-500/10 rounded-full p-3 w-16 h-16 mx-auto mb-3 flex items-center justify-center">
                  <Award className="h-6 w-6 text-green-600" />
                </div>
                <p className="text-2xl font-bold">85%</p>
                <p className="text-sm text-muted-foreground">Completion Rate</p>
              </div>
              <div className="text-center">
                <div className="bg-orange-500/10 rounded-full p-3 w-16 h-16 mx-auto mb-3 flex items-center justify-center">
                  <Trophy className="h-6 w-6 text-orange-600" />
                </div>
                <p className="text-2xl font-bold">5K+</p>
                <p className="text-sm text-muted-foreground">Certificates Earned</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Filters and Track Grid */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          {/* Filter Controls */}
          <div className="mb-8">
            <Tabs defaultValue="all" className="w-full">
              <TabsList className="grid w-full grid-cols-2 lg:w-auto lg:grid-cols-4">
                <TabsTrigger value="all">All Tracks</TabsTrigger>
                <TabsTrigger value="featured">Featured</TabsTrigger>
                <TabsTrigger value="beginner">Beginner Friendly</TabsTrigger>
                <TabsTrigger value="certification">With Certification</TabsTrigger>
              </TabsList>

              <div className="flex flex-wrap gap-4 mt-6">
                {/* Category Filter */}
                <div className="flex gap-2">
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
                  <option value="duration">Shortest First</option>
                </select>
              </div>

              <TabsContent value="all" className="mt-8">
                <TrackGrid tracks={filteredTracks} />
              </TabsContent>
            </Tabs>
          </div>
        </div>
      </section>
    </div>
  );
}

// Enhanced Track Grid Component
function TrackGrid({ tracks }: { tracks: Track[] }) {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-8">
      {tracks.map((track) => (
        <TrackCard key={track.id} track={track} />
      ))}
    </div>
  );
}

// Enhanced Track Card Component
function TrackCard({ track }: { track: Track }) {
  const completionPercentage = Math.floor(Math.random() * 40) + 10; // Mock progress

  return (
    <Card className="group hover:shadow-xl transition-all duration-300 hover:-translate-y-1 overflow-hidden">
      <CardHeader className="p-0">
        <div className="relative">
          <img
            src={track.image}
            alt={track.title}
            className="w-full h-56 object-cover"
            onError={(e) => {
              e.currentTarget.src = '/placeholder-track.jpg';
            }}
          />
          <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent" />

          {/* Badges */}
          <div className="absolute top-4 left-4 flex gap-2">
            {track.isPopular && <Badge className="bg-orange-500">🔥 Popular</Badge>}
            {track.isFeatured && <Badge className="bg-purple-500">⭐ Featured</Badge>}
          </div>

          {/* Track Icon */}
          <div className="absolute bottom-4 left-4">
            <div className="bg-white/90 rounded-full p-3">
              <track.icon className="h-6 w-6 text-primary" />
            </div>
          </div>

          {/* Preview Button */}
          <Button size="sm" className="absolute bottom-4 right-4 opacity-0 group-hover:opacity-100 transition-opacity" asChild>
            <Link href={`/tracks/${track.id}`}>
              <PlayCircle className="h-4 w-4 mr-1" />
              Preview
            </Link>
          </Button>
        </div>
      </CardHeader>

      <CardContent className="p-6">
        <div className="flex items-center justify-between mb-3">
          <Badge variant="secondary" className="text-xs">
            {track.category}
          </Badge>
          <div className="flex items-center gap-1 text-sm">
            <Star className="h-3 w-3 fill-yellow-400 text-yellow-400" />
            <span>{track.rating}</span>
          </div>
        </div>

        <CardTitle className="text-xl mb-3 line-clamp-2">{track.title}</CardTitle>

        <p className="text-sm text-muted-foreground mb-4 line-clamp-3">{track.description}</p>

        {/* Track Stats */}
        <div className="grid grid-cols-2 gap-4 mb-4 text-xs">
          <div className="flex items-center gap-1">
            <BookOpen className="h-3 w-3" />
            <span>{track.totalCourses} courses</span>
          </div>
          <div className="flex items-center gap-1">
            <Clock className="h-3 w-3" />
            <span>{track.duration}</span>
          </div>
          <div className="flex items-center gap-1">
            <Users className="h-3 w-3" />
            <span>{track.completedByStudents.toLocaleString()} students</span>
          </div>
          <div className="flex items-center gap-1">
            <Target className="h-3 w-3" />
            <span>{track.level}</span>
          </div>
        </div>

        {/* Progress Bar */}
        <div className="mb-4">
          <div className="flex justify-between text-xs mb-1">
            <span>Track Progress</span>
            <span>{completionPercentage}%</span>
          </div>
          <Progress value={completionPercentage} className="h-2" />
        </div>

        {/* Skills Preview */}
        <div className="mb-4">
          <p className="text-xs font-medium mb-2">You'll learn:</p>
          <div className="flex flex-wrap gap-1">
            {track.skills.slice(0, 3).map((skill) => (
              <Badge key={skill} variant="outline" className="text-xs">
                {skill}
              </Badge>
            ))}
            {track.skills.length > 3 && (
              <Badge variant="outline" className="text-xs">
                +{track.skills.length - 3} more
              </Badge>
            )}
          </div>
        </div>

        {/* Price and Enroll */}
        <div className="flex items-center justify-between mb-4">
          <div className="text-2xl font-bold">${track.price}</div>
          <Button asChild>
            <Link href={`/tracks/${track.id}`}>
              Start Track
              <ArrowRight className="h-4 w-4 ml-1" />
            </Link>
          </Button>
        </div>

        {/* Certification */}
        {track.certification && (
          <div className="flex items-center gap-1 text-xs text-primary">
            <CheckCircle className="h-3 w-3" />
            <span>Professional Certificate included</span>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
