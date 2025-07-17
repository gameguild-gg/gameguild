import React from 'react';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader } from '@game-guild/ui/components/card';
import { Avatar, AvatarFallback, AvatarImage } from '@game-guild/ui/components/avatar';
import { Star, Clock, Users, BookOpen, ArrowRight } from 'lucide-react';
import Link from 'next/link';

interface Course {
  id: string;
  title: string;
  description: string;
  category: string;
  level: 'Beginner' | 'Intermediate' | 'Advanced';
  duration: string;
  enrolledStudents: number;
  rating: number;
  price: number;
  image: string;
  instructor: {
    name: string;
    avatar: string;
  };
  isEnrolled?: boolean;
  progress?: number;
  certification?: boolean;
}

interface CourseCardProps {
  course: Course;
  variant?: 'default' | 'compact' | 'featured';
}

export function CourseCard({ course, variant = 'default' }: CourseCardProps) {
  if (variant === 'compact') {
    return (
      <Card className="hover:shadow-md transition-shadow">
        <CardContent className="p-4">
          <div className="flex gap-3">
            <img
              src={course.image}
              alt={course.title}
              className="w-16 h-16 rounded object-cover"
              onError={(e) => {
                e.currentTarget.src = '/placeholder-course.jpg';
              }}
            />
            <div className="flex-1 min-w-0">
              <h3 className="font-semibold text-sm line-clamp-1">{course.title}</h3>
              <p className="text-xs text-muted-foreground">{course.instructor.name}</p>
              <div className="flex items-center gap-2 mt-1">
                <div className="flex items-center gap-1">
                  <Star className="h-3 w-3 fill-yellow-400 text-yellow-400" />
                  <span className="text-xs">{course.rating}</span>
                </div>
                <span className="text-xs font-medium">${course.price}</span>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card
      className={`group hover:shadow-xl transition-all duration-300 hover:-translate-y-1 overflow-hidden ${variant === 'featured' ? 'border-primary' : ''}`}
    >
      <CardHeader className="p-0">
        <div className="relative">
          <img
            src={course.image}
            alt={course.title}
            className="w-full h-48 object-cover"
            onError={(e) => {
              e.currentTarget.src = '/placeholder-course.jpg';
            }}
          />
          <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent" />

          <div className="absolute top-4 left-4 flex gap-2">
            <Badge variant="secondary">{course.category}</Badge>
            {course.certification && <Badge className="bg-green-500">Certified</Badge>}
          </div>

          <div className="absolute bottom-4 right-4">
            <Button size="sm" asChild>
              <Link href={`/courses/${course.id}`}>
                View Course
                <ArrowRight className="h-3 w-3 ml-1" />
              </Link>
            </Button>
          </div>
        </div>
      </CardHeader>

      <CardContent className="p-6">
        <div className="flex items-center justify-between mb-3">
          <Badge variant="outline" className="text-xs">
            {course.level}
          </Badge>
          <div className="flex items-center gap-1 text-sm">
            <Star className="h-3 w-3 fill-yellow-400 text-yellow-400" />
            <span>{course.rating}</span>
          </div>
        </div>

        <h3 className="text-xl font-semibold mb-3 line-clamp-2">{course.title}</h3>

        <p className="text-sm text-muted-foreground mb-4 line-clamp-3">{course.description}</p>

        <div className="flex items-center gap-3 mb-4">
          <Avatar className="h-8 w-8">
            <AvatarImage src={course.instructor.avatar} />
            <AvatarFallback>{course.instructor.name[0]}</AvatarFallback>
          </Avatar>
          <span className="text-sm font-medium">{course.instructor.name}</span>
        </div>

        <div className="grid grid-cols-2 gap-4 mb-4 text-xs">
          <div className="flex items-center gap-1">
            <Clock className="h-3 w-3" />
            <span>{course.duration}</span>
          </div>
          <div className="flex items-center gap-1">
            <Users className="h-3 w-3" />
            <span>{course.enrolledStudents.toLocaleString()}</span>
          </div>
        </div>

        {course.isEnrolled && course.progress !== undefined && (
          <div className="mb-4">
            <div className="flex justify-between text-xs mb-1">
              <span>Progress</span>
              <span>{course.progress}%</span>
            </div>
            <div className="w-full bg-secondary rounded-full h-2">
              <div className="bg-primary h-2 rounded-full transition-all duration-300" style={{ width: `${course.progress}%` }} />
            </div>
          </div>
        )}

        <div className="flex items-center justify-between">
          <div className="text-2xl font-bold">${course.price}</div>
          <Button asChild>
            <Link href={`/courses/${course.id}`}>{course.isEnrolled ? 'Continue' : 'Enroll Now'}</Link>
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
