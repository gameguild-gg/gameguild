'use client';

import React from 'react';
import Image from 'next/image';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { Book, Code, Music, Paintbrush } from 'lucide-react';
import { Course, COURSE_LEVEL_NAMES } from '@/components/legacy/types/courses';

interface CourseCardProps {
  course: Course;
  onClick?: () => void;
}

const areaIcons = {
  programming: Code,
  art: Paintbrush,
  design: Book,
  audio: Music,
};

const levelColors = {
  1: 'bg-green-500',
  2: 'bg-yellow-500',
  3: 'bg-red-500',
  4: 'bg-purple-500',
};

export default function CourseCard({ course, onClick }: CourseCardProps) {
  const AreaIcon = areaIcons[course.area];
  const levelName = COURSE_LEVEL_NAMES[course.level - 1];
  const levelColor = levelColors[course.level];

  return (
    <Card className="cursor-pointer hover:shadow-lg bg-white dark:bg-gray-950 transition-all duration-300 ease-in-out hover:scale-105" onClick={onClick}>
      <CardHeader className="pb-2">
        <div className="flex justify-between items-center mb-2">
          <Badge className={`${levelColor} text-white px-2 py-1 text-xs font-bold`}>{levelName}</Badge>
          <div className="p-2 rounded-full transition-all duration-300 hover:bg-gray-800">
            <AreaIcon className="w-6 h-6 transition-all duration-300 hover:scale-110" />
          </div>
        </div>

        <div className="relative w-full h-48 mb-4">
          <Image src={course.image || '/placeholder.svg'} alt={course.title} fill className="object-cover rounded-md" sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw" />
        </div>

        <CardTitle className="text-lg font-semibold line-clamp-2">{course.title}</CardTitle>
      </CardHeader>

      <CardContent>
        <p className="text-sm text-muted-foreground line-clamp-3 mb-4">{course.description}</p>

        {course.progress !== undefined && (
          <div className="mb-4">
            <div className="flex justify-between text-sm mb-1">
              <span>Progress</span>
              <span>{course.progress}%</span>
            </div>
            <Progress value={course.progress} className="w-full" />
          </div>
        )}

        <div className="flex flex-wrap gap-1 mb-4">
          {course.tools && course.tools.length > 0 && (
            <>
              {course.tools.slice(0, 3).map((tool) => (
                <Badge key={tool} variant="outline" className="text-xs">
                  {tool}
                </Badge>
              ))}
              {course.tools.length > 3 && (
                <Badge variant="outline" className="text-xs">
                  +{course.tools.length - 3} more
                </Badge>
              )}
            </>
          )}
        </div>

        <div className="text-sm text-muted-foreground capitalize">{course.area}</div>
      </CardContent>
    </Card>
  );
}
