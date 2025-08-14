"use client";

import Image from "next/image";
import Link from "next/link";
import { Clock, Users, Star, BookOpen } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import type { Program } from "@/lib/api/generated/types.gen";
import { ProgramDifficulty, ContentStatus } from "@/lib/api/generated/types.gen";
import { cn } from "@/lib/utils";

export function CourseCard({ course, viewMode = "grid" }: { course: Program; viewMode?: "grid" | "list" }) {
  const updated = course.updatedAt ? new Date(course.updatedAt).toLocaleDateString() : 'N/A';

  const statusColors = {
    published: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
    draft: "bg-amber-500/10 text-amber-400 border-amber-500/20",
    archived: "bg-slate-500/10 text-slate-400 border-slate-500/20",
    under_review: "bg-blue-500/10 text-blue-400 border-blue-500/20",
  };

  const levelColors = {
    beginner: "bg-green-500/10 text-green-400",
    intermediate: "bg-blue-500/10 text-blue-400",
    advanced: "bg-purple-500/10 text-purple-400",
    expert: "bg-red-500/10 text-red-400",
  };

  // Map our Program type to display values
  const getStatusDisplay = (status?: ContentStatus): keyof typeof statusColors => {
    switch (status) {
      case ContentStatus.PUBLISHED: return 'published';
      case ContentStatus.ARCHIVED: return 'archived';
      case ContentStatus.UNDER_REVIEW: return 'under_review';
      case ContentStatus.DRAFT:
      default: return 'draft';
    }
  };
  
  const getDifficultyLevel = (difficulty?: ProgramDifficulty): keyof typeof levelColors => {
    switch (difficulty) {
      case ProgramDifficulty.BEGINNER: return 'beginner';
      case ProgramDifficulty.INTERMEDIATE: return 'intermediate';
      case ProgramDifficulty.ADVANCED: return 'advanced';
      case ProgramDifficulty.EXPERT: return 'expert';
      default: return 'beginner';
    }
  };

  const status = getStatusDisplay(course.status);
  const level = getDifficultyLevel(course.difficulty);

  if (viewMode === "list") {
    return (
      <Link href={`/dashboard/courses/${course.slug}`} className="block">
        <Card className="hover:shadow-xl transition-all duration-200 cursor-pointer">
          <CardContent className="p-6">
            <div className="flex items-center gap-6">
              <div className="relative w-32 h-20 bg-muted/20 rounded-lg overflow-hidden flex-shrink-0">
                <Image
                  src={course.thumbnail || "/placeholder.svg?height=80&width=128&query=course%20cover"}
                  alt={`${course.title} cover`}
                  fill
                  className="object-cover"
                  sizes="128px"
                />
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-3 mb-2">
                  <h3 className="font-semibold text-foreground truncate">{course.title}</h3>
                  <Badge variant="outline" className={cn("capitalize", statusColors[status])}>
                    {status}
                  </Badge>
                  <Badge variant="secondary" className={cn("capitalize", levelColors[level])}>
                    {level}
                  </Badge>
                </div>
                <p className="text-sm text-muted-foreground line-clamp-1 mb-3">
                  {course.description}
                </p>
                <div className="flex items-center gap-6 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1">
                    <Clock className="w-3 h-3" />
                    {course.estimatedHours || 0}h
                  </span>
                  <span className="flex items-center gap-1">
                    <Users className="w-3 h-3" />
                    0 students
                  </span>
                  <span className="flex items-center gap-1">
                    <Star className="w-3 h-3 fill-amber-400 text-amber-400" />
                    N/A
                  </span>
                  <span className="flex items-center gap-1">
                    <BookOpen className="w-3 h-3" />
                    0 modules
                  </span>
                  <span className="text-muted-foreground">Updated {updated}</span>
                </div>
              </div>
              <div className="text-right">
                <span className="text-sm font-semibold text-emerald-400">Free</span>
              </div>
            </div>
          </CardContent>
        </Card>
      </Link>
    );
  }

  return (
    <Link href={`/dashboard/courses/${course.slug}`} className="block">
      <Card className="group hover:shadow-xl transition-all duration-200 cursor-pointer">
        <CardHeader className="p-0">
          <div className="relative w-full h-48 bg-muted/20 rounded-t-lg overflow-hidden">
            <Image
              src={course.thumbnail || "/placeholder.svg?height=192&width=384&query=course%20cover"}
              alt={`${course.title} cover`}
              fill
              className="object-cover transition-transform group-hover:scale-105"
              sizes="(max-width:768px) 100vw, (max-width:1200px) 50vw, 33vw"
            />
            <div className="absolute top-3 right-3 flex gap-2">
              <Badge variant="outline" className={cn("capitalize", statusColors[status])}>
                {status}
              </Badge>
            </div>
            <div className="absolute top-3 left-3">
              <Badge variant="secondary" className={cn("capitalize", levelColors[level])}>
                {level}
              </Badge>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-6">
          <div className="mb-3">
            <CardTitle className="text-lg font-semibold text-foreground line-clamp-2 mb-2">{course.title}</CardTitle>
            <CardDescription className="text-muted-foreground line-clamp-2">
              {course.description}
            </CardDescription>
          </div>

          <div className="flex items-center justify-between text-xs text-muted-foreground mb-4">
            <div className="flex items-center gap-4">
              <span className="flex items-center gap-1">
                <Clock className="w-3 h-3" />
                {course.estimatedHours || 0}h
              </span>
              <span className="flex items-center gap-1">
                <Users className="w-3 h-3" />
                0
              </span>
              <span className="flex items-center gap-1">
                <Star className="w-3 h-3 fill-amber-400 text-amber-400" />
                N/A
              </span>
            </div>
          </div>

          <div className="flex items-center justify-between mb-2">
            <span className="text-sm text-muted-foreground">Category: {course.category || 'General'}</span>
            <div className="text-right">
              <span className="text-sm font-semibold text-emerald-400">Free</span>
            </div>
          </div>

          <div className="text-xs text-muted-foreground">Updated {updated}</div>
        </CardContent>
      </Card>
    </Link>
  );
}
