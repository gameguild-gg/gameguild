'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Button } from '@game-guild/ui/components/button';
import { Badge } from '@game-guild/ui/components/badge';
import { Progress } from '@game-guild/ui/components/progress';
import { 
  CheckCircle, 
  Trophy, 
  Star, 
  Award, 
  Clock, 
  Target,
  AlertCircle,
  Sparkles
} from 'lucide-react';
import { useToast } from '@/lib/old/hooks/use-toast';

interface CompletionRequirement {
  readonly id: string;
  readonly title: string;
  readonly description: string;
  readonly type: 'activity' | 'quiz' | 'assignment' | 'peer-review' | 'attendance' | 'grade';
  readonly required: boolean;
  readonly completed: boolean;
  readonly completedAt?: string;
  readonly score?: number;
  readonly minScore?: number;
}

interface CompletionData {
  readonly courseId: string;
  readonly courseTitle: string;
  readonly requirements: CompletionRequirement[];
  readonly overallProgress: number;
  readonly isComplete: boolean;
  readonly certificateEligible: boolean;
  readonly finalGrade?: number;
  readonly completionDate?: string;
  readonly estimatedCompletionDate?: string;
}

interface CourseCompletionProps {
  readonly courseId: string;
  readonly onCertificateRequest?: () => void;
  readonly onContinueLearning?: () => void;
}

export function CourseCompletion({ 
  courseId, 
  onCertificateRequest, 
  onContinueLearning 
}: CourseCompletionProps) {
  const [completionData, setCompletionData] = useState<CompletionData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isGeneratingCertificate, setIsGeneratingCertificate] = useState(false);
  const { toast } = useToast();

  useEffect(() => {
    fetchCompletionData();
  }, [courseId]);

  const fetchCompletionData = async () => {
    try {
      const response = await fetch(`/api/courses/${courseId}/completion`);
      if (response.ok) {
        const data = await response.json();
        setCompletionData(data);
      }
    } catch (error) {
      console.error('Error fetching completion data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleGenerateCertificate = async () => {
    if (!completionData?.certificateEligible) return;

    setIsGeneratingCertificate(true);
    try {
      await onCertificateRequest?.();
      toast({
        title: 'Certificate generated!',
        description: 'Your certificate has been generated and is ready for download.',
      });
    } catch (error) {
      console.error('Error generating certificate:', error);
      toast({
        title: 'Error',
        description: 'Failed to generate certificate. Please try again.',
        variant: 'destructive',
      });
    } finally {
      setIsGeneratingCertificate(false);
    }
  };

  const getRequirementIcon = (requirement: CompletionRequirement) => {
    if (requirement.completed) {
      return <CheckCircle className="h-5 w-5 text-green-600" />;
    }
    
    switch (requirement.type) {
      case 'quiz':
        return <Target className="h-5 w-5 text-blue-600" />;
      case 'assignment':
        return <Trophy className="h-5 w-5 text-purple-600" />;
      case 'peer-review':
        return <Star className="h-5 w-5 text-orange-600" />;
      case 'grade':
        return <Award className="h-5 w-5 text-yellow-600" />;
      case 'attendance':
        return <Clock className="h-5 w-5 text-gray-600" />;
      default:
        return <AlertCircle className="h-5 w-5 text-gray-400" />;
    }
  };

  const getGradeColor = (score: number, minScore: number) => {
    if (score >= minScore + 20) return 'text-green-600';
    if (score >= minScore + 10) return 'text-blue-600';
    if (score >= minScore) return 'text-orange-600';
    return 'text-red-600';
  };

  if (isLoading) {
    return (
      <Card>
        <CardContent className="p-6">
          <div className="animate-pulse space-y-4">
            <div className="h-6 bg-gray-200 rounded w-1/2"></div>
            <div className="h-4 bg-gray-200 rounded"></div>
            <div className="space-y-3">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-16 bg-gray-200 rounded"></div>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!completionData) {
    return (
      <Card>
        <CardContent className="p-6 text-center text-gray-500">
          Failed to load completion data
        </CardContent>
      </Card>
    );
  }

  const completedRequirements = completionData.requirements.filter(req => req.completed);
  const requiredCount = completionData.requirements.filter(req => req.required).length;
  const completedRequiredCount = completionData.requirements.filter(req => req.required && req.completed).length;

  return (
    <div className="space-y-6">
      {/* Completion Status Header */}
      <Card className={`${
        completionData.isComplete 
          ? 'border-green-300 bg-gradient-to-r from-green-50 to-emerald-50'
          : 'border-blue-300 bg-gradient-to-r from-blue-50 to-indigo-50'
      }`}>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-3">
              {completionData.isComplete ? (
                <div className="p-2 bg-green-100 rounded-full">
                  <Trophy className="h-6 w-6 text-green-600" />
                </div>
              ) : (
                <div className="p-2 bg-blue-100 rounded-full">
                  <Target className="h-6 w-6 text-blue-600" />
                </div>
              )}
              <div>
                <h2 className={`text-xl font-bold ${
                  completionData.isComplete ? 'text-green-800' : 'text-blue-800'
                }`}>
                  {completionData.isComplete ? 'Course Completed!' : 'Course Progress'}
                </h2>
                <p className={`text-sm ${
                  completionData.isComplete ? 'text-green-600' : 'text-blue-600'
                }`}>
                  {completionData.courseTitle}
                </p>
              </div>
            </CardTitle>
            
            <div className="text-right">
              <Badge 
                className={`${
                  completionData.isComplete 
                    ? 'bg-green-500 text-white' 
                    : 'bg-blue-500 text-white'
                } mb-2`}
              >
                {completionData.overallProgress}% Complete
              </Badge>
              {completionData.finalGrade && (
                <div className="text-sm font-medium">
                  Final Grade: <span className="text-lg">{completionData.finalGrade}%</span>
                </div>
              )}
            </div>
          </div>
        </CardHeader>
        
        <CardContent className="space-y-4">
          <Progress value={completionData.overallProgress} className="h-3" />
          
          <div className="grid md:grid-cols-3 gap-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-green-600">
                {completedRequirements.length}
              </div>
              <div className="text-sm text-gray-600">Items Completed</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-blue-600">
                {completedRequiredCount}/{requiredCount}
              </div>
              <div className="text-sm text-gray-600">Required Items</div>
            </div>
            <div className="text-center">
              <div className={`text-2xl font-bold ${
                completionData.certificateEligible ? 'text-green-600' : 'text-gray-400'
              }`}>
                {completionData.certificateEligible ? '✓' : '○'}
              </div>
              <div className="text-sm text-gray-600">Certificate Ready</div>
            </div>
          </div>

          {completionData.isComplete && completionData.completionDate && (
            <div className="text-center p-3 bg-white/50 rounded-lg">
              <div className="flex items-center justify-center gap-2 text-green-700">
                <Sparkles className="h-4 w-4" />
                <span className="font-medium">
                  Completed on {new Date(completionData.completionDate).toLocaleDateString()}
                </span>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Requirements Breakdown */}
      <Card>
        <CardHeader>
          <CardTitle>Completion Requirements</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {completionData.requirements.map((requirement) => (
              <div
                key={requirement.id}
                className={`flex items-center gap-4 p-4 rounded-lg border ${
                  requirement.completed
                    ? 'bg-green-50 border-green-200'
                    : requirement.required
                    ? 'bg-orange-50 border-orange-200'
                    : 'bg-gray-50 border-gray-200'
                }`}
              >
                <div className="flex-shrink-0">
                  {getRequirementIcon(requirement)}
                </div>
                
                <div className="flex-grow">
                  <div className="flex items-center gap-2 mb-1">
                    <h4 className="font-medium">{requirement.title}</h4>
                    {requirement.required && (
                      <Badge variant="outline" className="text-xs">
                        Required
                      </Badge>
                    )}
                  </div>
                  <p className="text-sm text-gray-600 mb-2">{requirement.description}</p>
                  
                  {requirement.score !== undefined && requirement.minScore !== undefined && (
                    <div className="flex items-center gap-2 text-sm">
                      <span>Score:</span>
                      <span className={`font-medium ${getGradeColor(requirement.score, requirement.minScore)}`}>
                        {requirement.score}%
                      </span>
                      <span className="text-gray-500">
                        (min: {requirement.minScore}%)
                      </span>
                    </div>
                  )}
                  
                  {requirement.completedAt && (
                    <div className="text-xs text-gray-500">
                      Completed: {new Date(requirement.completedAt).toLocaleString()}
                    </div>
                  )}
                </div>

                <div className="flex-shrink-0">
                  <Badge 
                    variant={requirement.completed ? 'default' : 'outline'}
                    className={requirement.completed ? 'bg-green-500 text-white' : ''}
                  >
                    {requirement.completed ? 'Complete' : 'Pending'}
                  </Badge>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Action Buttons */}
      <Card>
        <CardContent className="p-6">
          <div className="flex flex-col sm:flex-row gap-4">
            {completionData.certificateEligible && (
              <Button
                onClick={handleGenerateCertificate}
                disabled={isGeneratingCertificate}
                className="bg-green-600 hover:bg-green-700 flex-1"
              >
                <Award className="h-4 w-4 mr-2" />
                {isGeneratingCertificate ? 'Generating...' : 'Get Certificate'}
              </Button>
            )}
            
            {!completionData.isComplete && (
              <Button
                onClick={onContinueLearning}
                variant="outline"
                className="flex-1"
              >
                Continue Learning
              </Button>
            )}
            
            {completionData.isComplete && (
              <Button
                onClick={onContinueLearning}
                variant="outline"
                className="flex-1"
              >
                Explore More Courses
              </Button>
            )}
          </div>
          
          {!completionData.certificateEligible && completionData.estimatedCompletionDate && (
            <div className="mt-4 text-center text-sm text-gray-600">
              <Clock className="h-4 w-4 inline mr-1" />
              Estimated completion: {new Date(completionData.estimatedCompletionDate).toLocaleDateString()}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

/* Completion badge for course cards */
export function CompletionBadge({ 
  isComplete, 
  progress, 
  certificateAvailable 
}: {
  readonly isComplete: boolean;
  readonly progress: number;
  readonly certificateAvailable?: boolean;
}) {
  if (isComplete) {
    return (
      <Badge className="bg-green-500 text-white">
        <Trophy className="h-3 w-3 mr-1" />
        {certificateAvailable ? 'Certified' : 'Completed'}
      </Badge>
    );
  }
  
  if (progress > 0) {
    return (
      <Badge variant="secondary">
        {progress}% Complete
      </Badge>
    );
  }
  
  return (
    <Badge variant="outline">
      Not Started
    </Badge>
  );
}
