'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { Input } from '@game-guild/ui/components/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@game-guild/ui/components/tabs';
import {
  Search,
  Filter,
  Clock,
  Users,
  Star,
  Trophy,
  BookOpen,
  PlayCircle,
  Code,
  Brain,
  Palette,
  Zap,
  ShoppingCart,
  Eye,
} from 'lucide-react';

interface Course {
  id: string;
  title: string;
  description: string;
  instructor: string;
  instructorAvatar: string;
  duration: number; // in hours
  level: 'beginner' | 'intermediate' | 'advanced';
  category: string;
  tags: string[];
  rating: number;
  enrollmentCount: number;
  price: number;
  currency: string;
  thumbnail: string;
  isEnrolled: boolean;
  progress?: number;
  hasCertificate: boolean;
  lastUpdated: string;
  contentCount: number;
  isPremium: boolean;
}

interface CourseCatalogProps {
  initialCourses: Course[];
}

const CategoryIcon = ({ category }: { category: string }) => {
  const iconProps = { className: 'h-5 w-5' };
  
  switch (category.toLowerCase()) {
    case 'programming':
      return <Code {...iconProps} />;
    case 'design':
      return <Palette {...iconProps} />;
    case 'business':
      return <Users {...iconProps} />;
    case 'data science':
      return <Brain {...iconProps} />;
    case 'marketing':
      return <Zap {...iconProps} />;
    default:
      return <BookOpen {...iconProps} />;
  }
};

const LevelBadge = ({ level }: { level: Course['level'] }) => {
  const colors = {
    beginner: 'bg-green-900/20 text-green-400 border-green-800',
    intermediate: 'bg-yellow-900/20 text-yellow-400 border-yellow-800',
    advanced: 'bg-red-900/20 text-red-400 border-red-800',
  };

  return (
    <Badge variant="outline" className={colors[level]}>
      {level.charAt(0).toUpperCase() + level.slice(1)}
    </Badge>
  );
};

const CourseCard = ({ course, onEnroll, onView }: { 
  course: Course; 
  onEnroll: (courseId: string) => void; 
  onView: (courseId: string) => void;
}) => {
  return (
    <Card className="bg-gray-900 border-gray-800 hover:border-gray-700 transition-colors group">
      <div className="relative">
        <div className="aspect-video bg-gradient-to-br from-blue-600 to-purple-600 rounded-t-lg flex items-center justify-center">
          <PlayCircle className="h-12 w-12 text-white/80" />
        </div>
        {course.isPremium && (
          <Badge className="absolute top-2 right-2 bg-yellow-600">
            <Trophy className="h-3 w-3 mr-1" />
            Premium
          </Badge>
        )}
        {course.isEnrolled && (
          <Badge className="absolute top-2 left-2 bg-green-600">
            Enrolled
          </Badge>
        )}
      </div>
      
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between gap-2">
          <CardTitle className="text-lg leading-tight group-hover:text-blue-400 transition-colors">
            {course.title}
          </CardTitle>
          <div className="flex items-center gap-1 text-sm text-yellow-400">
            <Star className="h-4 w-4 fill-current" />
            {course.rating.toFixed(1)}
          </div>
        </div>
        
        <div className="flex items-center gap-2 text-sm text-gray-400">
          <CategoryIcon category={course.category} />
          <span>{course.category}</span>
          <LevelBadge level={course.level} />
        </div>
      </CardHeader>
      
      <CardContent className="space-y-4">
        <p className="text-gray-300 text-sm line-clamp-2">{course.description}</p>
        
        <div className="flex items-center gap-4 text-sm text-gray-400">
          <span className="flex items-center gap-1">
            <Clock className="h-4 w-4" />
            {course.duration}h
          </span>
          <span className="flex items-center gap-1">
            <Users className="h-4 w-4" />
            {course.enrollmentCount.toLocaleString()}
          </span>
          <span className="flex items-center gap-1">
            <BookOpen className="h-4 w-4" />
            {course.contentCount} items
          </span>
        </div>
        
        <div className="flex items-center gap-2 text-sm">
          <div className="w-8 h-8 bg-gray-700 rounded-full flex items-center justify-center">
            {course.instructor.charAt(0)}
          </div>
          <span className="text-gray-300">{course.instructor}</span>
        </div>
        
        {course.isEnrolled && course.progress !== undefined && (
          <div className="space-y-1">
            <div className="flex justify-between text-sm">
              <span className="text-gray-400">Progress</span>
              <span className="text-gray-300">{course.progress}%</span>
            </div>
            <div className="w-full bg-gray-800 rounded-full h-2">
              <div 
                className="bg-blue-600 h-2 rounded-full transition-all"
                style={{ width: `${course.progress}%` }}
              />
            </div>
          </div>
        )}
        
        <div className="flex items-center justify-between pt-2">
          <div className="flex items-center gap-2">
            <span className="text-xl font-bold text-white">
              {course.price === 0 ? 'Free' : `$${course.price}`}
            </span>
            {course.hasCertificate && (
              <Badge variant="outline" className="text-xs">
                <Trophy className="h-3 w-3 mr-1" />
                Certificate
              </Badge>
            )}
          </div>
          
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onView(course.id)}
            >
              <Eye className="h-4 w-4 mr-1" />
              View
            </Button>
            {!course.isEnrolled && (
              <Button
                size="sm"
                onClick={() => onEnroll(course.id)}
                className="flex items-center gap-1"
              >
                {course.price > 0 ? (
                  <>
                    <ShoppingCart className="h-4 w-4" />
                    Buy
                  </>
                ) : (
                  'Enroll'
                )}
              </Button>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
};

export function CourseCatalog({ initialCourses }: CourseCatalogProps) {
  const router = useRouter();
  const [courses, setCourses] = useState<Course[]>(initialCourses);
  const [filteredCourses, setFilteredCourses] = useState<Course[]>(initialCourses);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [selectedLevel, setSelectedLevel] = useState<string>('all');
  const [sortBy, setSortBy] = useState<string>('popular');
  const [activeTab, setActiveTab] = useState('all');

  // Extract unique categories from courses
  const categories = Array.from(new Set(courses.map(course => course.category)));

  // Filter and sort courses
  useEffect(() => {
    let filtered = courses;

    // Filter by tab
    if (activeTab === 'enrolled') {
      filtered = filtered.filter(course => course.isEnrolled);
    } else if (activeTab === 'free') {
      filtered = filtered.filter(course => course.price === 0);
    } else if (activeTab === 'premium') {
      filtered = filtered.filter(course => course.isPremium);
    }

    // Filter by search term
    if (searchTerm) {
      filtered = filtered.filter(course =>
        course.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
        course.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
        course.instructor.toLowerCase().includes(searchTerm.toLowerCase()) ||
        course.tags.some(tag => tag.toLowerCase().includes(searchTerm.toLowerCase()))
      );
    }

    // Filter by category
    if (selectedCategory !== 'all') {
      filtered = filtered.filter(course => course.category === selectedCategory);
    }

    // Filter by level
    if (selectedLevel !== 'all') {
      filtered = filtered.filter(course => course.level === selectedLevel);
    }

    // Sort courses
    switch (sortBy) {
      case 'popular':
        filtered.sort((a, b) => b.enrollmentCount - a.enrollmentCount);
        break;
      case 'rating':
        filtered.sort((a, b) => b.rating - a.rating);
        break;
      case 'newest':
        filtered.sort((a, b) => new Date(b.lastUpdated).getTime() - new Date(a.lastUpdated).getTime());
        break;
      case 'price-low':
        filtered.sort((a, b) => a.price - b.price);
        break;
      case 'price-high':
        filtered.sort((a, b) => b.price - a.price);
        break;
      case 'duration':
        filtered.sort((a, b) => a.duration - b.duration);
        break;
    }

    setFilteredCourses(filtered);
  }, [courses, searchTerm, selectedCategory, selectedLevel, sortBy, activeTab]);

  const handleEnroll = async (courseId: string) => {
    try {
      // TODO: Implement enrollment logic (GraphQL mutation)
      console.log('Enrolling in course:', courseId);
      
      const course = courses.find(c => c.id === courseId);
      if (course && course.price > 0) {
        // Redirect to payment page
        router.push(`/payment?courseId=${courseId}`);
      } else {
        // Free enrollment
        setCourses(prev => 
          prev.map(c => c.id === courseId ? { ...c, isEnrolled: true, progress: 0 } : c)
        );
      }
    } catch (error) {
      console.error('Failed to enroll:', error);
    }
  };

  const handleViewCourse = (courseId: string) => {
    router.push(`/course/${courseId}`);
  };

  return (
    <div className="min-h-screen bg-gray-950 text-white">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold mb-2">Course Catalog</h1>
          <p className="text-gray-400">Discover and learn new skills with our comprehensive courses</p>
        </div>

        {/* Tabs */}
        <Tabs value={activeTab} onValueChange={setActiveTab} className="mb-6">
          <TabsList className="grid w-full grid-cols-4 bg-gray-900">
            <TabsTrigger value="all">All Courses</TabsTrigger>
            <TabsTrigger value="enrolled">My Courses</TabsTrigger>
            <TabsTrigger value="free">Free</TabsTrigger>
            <TabsTrigger value="premium">Premium</TabsTrigger>
          </TabsList>
        </Tabs>

        {/* Search and Filters */}
        <div className="mb-8 space-y-4">
          <div className="flex flex-col lg:flex-row gap-4">
            {/* Search */}
            <div className="relative flex-1">
              <Search className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
              <Input
                placeholder="Search courses, instructors, or topics..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10 bg-gray-900 border-gray-800"
              />
            </div>

            {/* Filters */}
            <div className="flex gap-4">
              <Select value={selectedCategory} onValueChange={setSelectedCategory}>
                <SelectTrigger className="w-48 bg-gray-900 border-gray-800">
                  <SelectValue placeholder="Category" />
                </SelectTrigger>
                <SelectContent className="bg-gray-900 border-gray-800">
                  <SelectItem value="all">All Categories</SelectItem>
                  {categories.map(category => (
                    <SelectItem key={category} value={category}>{category}</SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Select value={selectedLevel} onValueChange={setSelectedLevel}>
                <SelectTrigger className="w-40 bg-gray-900 border-gray-800">
                  <SelectValue placeholder="Level" />
                </SelectTrigger>
                <SelectContent className="bg-gray-900 border-gray-800">
                  <SelectItem value="all">All Levels</SelectItem>
                  <SelectItem value="beginner">Beginner</SelectItem>
                  <SelectItem value="intermediate">Intermediate</SelectItem>
                  <SelectItem value="advanced">Advanced</SelectItem>
                </SelectContent>
              </Select>

              <Select value={sortBy} onValueChange={setSortBy}>
                <SelectTrigger className="w-40 bg-gray-900 border-gray-800">
                  <SelectValue placeholder="Sort by" />
                </SelectTrigger>
                <SelectContent className="bg-gray-900 border-gray-800">
                  <SelectItem value="popular">Most Popular</SelectItem>
                  <SelectItem value="rating">Highest Rated</SelectItem>
                  <SelectItem value="newest">Newest</SelectItem>
                  <SelectItem value="price-low">Price: Low to High</SelectItem>
                  <SelectItem value="price-high">Price: High to Low</SelectItem>
                  <SelectItem value="duration">Duration</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </div>

        {/* Results Count */}
        <div className="mb-6 text-gray-400">
          Showing {filteredCourses.length} of {courses.length} courses
        </div>

        {/* Course Grid */}
        {filteredCourses.length > 0 ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            {filteredCourses.map(course => (
              <CourseCard
                key={course.id}
                course={course}
                onEnroll={handleEnroll}
                onView={handleViewCourse}
              />
            ))}
          </div>
        ) : (
          <div className="text-center py-12">
            <BookOpen className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <h3 className="text-xl font-semibold text-gray-300 mb-2">No courses found</h3>
            <p className="text-gray-400">Try adjusting your search terms or filters</p>
          </div>
        )}
      </div>
    </div>
  );
}
