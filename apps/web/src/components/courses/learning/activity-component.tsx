'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { Clock, Code, FileText, MessageSquare, Play, Save, Send, Upload } from 'lucide-react';
import { submitActivity } from '@/lib/courses/server-actions';

interface ContentItem {
  id: string;
  title: string;
  type: 'lesson' | 'activity' | 'quiz' | 'assignment' | 'peer-review';
  status: 'locked' | 'available' | 'in-progress' | 'completed';
  duration?: number;
  description?: string;
  order: number;
  isRequired: boolean;
  activityType?: 'text' | 'code' | 'file' | 'quiz' | 'discussion';
  content?: unknown;
  progress?: number;
}

interface ActivityComponentProps {
  item: ContentItem;
  onComplete: (score?: number) => void;
}

interface QuizQuestion {
  id: string;
  question: string;
  type: 'multiple-choice' | 'multiple-select' | 'true-false' | 'short-answer';
  options?: string[];
  correctAnswer?: string | string[];
  points: number;
}

export function ActivityComponent({ item, onComplete }: ActivityComponentProps) {
  const [hasStarted, setHasStarted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submission, setSubmission] = useState<Record<string, unknown>>({});
  const [currentStep, setCurrentStep] = useState(0);

  const handleStart = () => {
    setHasStarted(true);
  };

  const handleSubmit = async () => {
    setIsSubmitting(true);

    try {
      const submissionData = {
        activityId: item.id,
        activityType: item.activityType || 'text',
        content: submission,
        isGraded: item.type === 'quiz' || item.type === 'assignment',
        attempt: 1,
      };

      const result = await submitActivity(submissionData);

      if (result.success) {
        // Calculate score for quiz
        let score;
        if (item.type === 'quiz') {
          score = calculateQuizScore();
        }

        onComplete(score);
      } else {
        console.error('Submission failed:', result.message);
      }
    } catch (error) {
      console.error('Error submitting activity:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const calculateQuizScore = (): number => {
    const quiz = getQuizContent();
    if (!quiz) return 0;

    let totalPoints = 0;
    let earnedPoints = 0;

    quiz.questions.forEach((question) => {
      totalPoints += question.points;
      const userAnswer = submission[question.id];

      if (question.type === 'multiple-choice' || question.type === 'true-false') {
        if (userAnswer === question.correctAnswer) {
          earnedPoints += question.points;
        }
      } else if (question.type === 'multiple-select') {
        const correct = question.correctAnswer as string[];
        const user = userAnswer as string[];
        if (user && correct && user.length === correct.length && user.every((answer) => correct.includes(answer))) {
          earnedPoints += question.points;
        }
      }
      // Short answer would need manual grading
    });

    return Math.round((earnedPoints / totalPoints) * 100);
  };

  const getQuizContent = () => {
    const quizzes = {
      'quiz-1': {
        title: 'Programming Concepts Quiz',
        description: 'Test your understanding of basic programming concepts',
        timeLimit: 15,
        questions: [
          {
            id: 'q1',
            question: 'What is a game loop?',
            type: 'multiple-choice' as const,
            options: [
              'A loop that only runs once',
              'A continuous cycle that updates game state and renders graphics',
              'A loop used for loading game assets',
              'A debugging tool for games',
            ],
            correctAnswer: 'A continuous cycle that updates game state and renders graphics',
            points: 10,
          },
          {
            id: 'q2',
            question: 'Which programming pattern is commonly used for game events?',
            type: 'multiple-choice' as const,
            options: ['Singleton Pattern', 'Observer Pattern', 'Factory Pattern', 'Decorator Pattern'],
            correctAnswer: 'Observer Pattern',
            points: 10,
          },
          {
            id: 'q3',
            question: 'Select all that are components of a typical game loop:',
            type: 'multiple-select' as const,
            options: ['Process Input', 'Update Game State', 'Render Graphics', 'Save Game Data', 'Load Assets'],
            correctAnswer: ['Process Input', 'Update Game State', 'Render Graphics'],
            points: 15,
          },
          {
            id: 'q4',
            question: 'Object pooling is used to optimize memory usage.',
            type: 'true-false' as const,
            options: ['True', 'False'],
            correctAnswer: 'True',
            points: 5,
          },
        ],
      },
    };

    return quizzes[item.id as keyof typeof quizzes];
  };

  const getActivityIcon = () => {
    switch (item.activityType || item.type) {
      case 'text':
        return <FileText className="h-16 w-16 text-blue-400" />;
      case 'code':
        return <Code className="h-16 w-16 text-purple-400" />;
      case 'file':
        return <Upload className="h-16 w-16 text-green-400" />;
      case 'quiz':
        return <MessageSquare className="h-16 w-16 text-yellow-400" />;
      default:
        return <Play className="h-16 w-16 text-slate-400" />;
    }
  };

  const renderQuizActivity = () => {
    const quiz = getQuizContent();
    if (!quiz) return <div>Quiz content not found</div>;

    const currentQuestion = quiz.questions[currentStep];
    const isLastQuestion = currentStep === quiz.questions.length - 1;

    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-semibold">
            Question {currentStep + 1} of {quiz.questions.length}
          </h3>
          <Badge variant="outline">{currentQuestion.points} points</Badge>
        </div>

        <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 shadow-lg">
          <CardContent className="p-6">
            <h4 className="text-lg mb-4 text-white">{currentQuestion.question}</h4>

            {currentQuestion.type === 'multiple-choice' || currentQuestion.type === 'true-false' ? (
              <RadioGroup
                value={(submission[currentQuestion.id] as string) || ''}
                onValueChange={(value) => setSubmission((prev) => ({ ...prev, [currentQuestion.id]: value }))}
              >
                {currentQuestion.options?.map((option, index) => (
                  <div key={index} className="flex items-center space-x-2">
                    <RadioGroupItem value={option} id={`option-${index}`} />
                    <Label htmlFor={`option-${index}`} className="text-slate-300">
                      {option}
                    </Label>
                  </div>
                ))}
              </RadioGroup>
            ) : currentQuestion.type === 'multiple-select' ? (
              <div className="space-y-2">
                {currentQuestion.options?.map((option, index) => (
                  <div key={index} className="flex items-center space-x-2">
                    <Checkbox
                      id={`option-${index}`}
                      checked={((submission[currentQuestion.id] as string[]) || []).includes(option)}
                      onCheckedChange={(checked) => {
                        const current = (submission[currentQuestion.id] as string[]) || [];
                        const updated = checked ? [...current, option] : current.filter((item) => item !== option);
                        setSubmission((prev) => ({ ...prev, [currentQuestion.id]: updated }));
                      }}
                    />
                    <Label htmlFor={`option-${index}`} className="text-gray-300">
                      {option}
                    </Label>
                  </div>
                ))}
              </div>
            ) : (
              <Textarea
                placeholder="Enter your answer..."
                value={(submission[currentQuestion.id] as string) || ''}
                onChange={(e) => setSubmission((prev) => ({ ...prev, [currentQuestion.id]: e.target.value }))}
                className="bg-gray-800 border-gray-600 text-white"
              />
            )}
          </CardContent>
        </Card>

        <div className="flex justify-between">
          <Button variant="outline" onClick={() => setCurrentStep(Math.max(0, currentStep - 1))} disabled={currentStep === 0} className="border-gray-600">
            Previous
          </Button>

          {isLastQuestion ? (
            <Button onClick={handleSubmit} disabled={isSubmitting} className="bg-green-600 hover:bg-green-700">
              {isSubmitting ? 'Submitting...' : 'Submit Quiz'}
            </Button>
          ) : (
            <Button onClick={() => setCurrentStep(Math.min(quiz.questions.length - 1, currentStep + 1))} className="bg-blue-600 hover:bg-blue-700">
              Next
            </Button>
          )}
        </div>
      </div>
    );
  };

  const renderTextActivity = () => (
    <div className="space-y-6">
      <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 shadow-lg">
        <CardHeader>
          <CardTitle className="text-white">Activity Instructions</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-slate-400 mb-4">{item.description || 'Complete this text-based activity by providing your response below.'}</p>

          {item.id === 'activity-1' && (
            <div className="bg-gradient-to-br from-slate-800/50 to-slate-700/50 p-4 rounded border border-slate-600 backdrop-blur-sm">
              <h4 className="font-semibold mb-2 text-white">Setup Instructions:</h4>
              <ol className="list-decimal list-inside space-y-2 text-slate-400">
                <li>Download and install your preferred game engine (Unity, Unreal Engine, or Godot)</li>
                <li>Set up a code editor (Visual Studio Code, Visual Studio, or similar)</li>
                <li>Create a new project in your chosen engine</li>
                <li>Familiarize yourself with the interface and basic navigation</li>
                <li>Document any challenges you encountered during setup</li>
              </ol>
            </div>
          )}
        </CardContent>
      </Card>

      <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 shadow-lg">
        <CardHeader>
          <CardTitle className="text-white">Your Response</CardTitle>
        </CardHeader>
        <CardContent>
          <Textarea
            placeholder="Describe your setup process, any challenges encountered, and what you learned..."
            value={(submission.response as string) || ''}
            onChange={(e) => setSubmission((prev) => ({ ...prev, response: e.target.value }))}
            className="bg-slate-800/50 border-slate-600 text-white min-h-[200px] backdrop-blur-sm"
          />
          <p className="text-sm text-slate-400 mt-2">Minimum 100 words required for completion.</p>
        </CardContent>
      </Card>

      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={() => setSubmission((prev) => ({ ...prev, saved: true }))} className="bg-slate-800/50 border-slate-600 text-slate-200 hover:bg-slate-700/50 hover:border-slate-500">
          <Save className="h-4 w-4 mr-2" />
          Save Draft
        </Button>
        <Button
          onClick={handleSubmit}
          disabled={isSubmitting || ((submission.response as string) || '').length < 100}
          className="bg-gradient-to-r from-green-600 to-teal-600 hover:from-green-700 hover:to-teal-700 border-0 shadow-lg hover:shadow-xl hover:shadow-green-500/25 transition-all"
        >
          <Send className="h-4 w-4 mr-2" />
          {isSubmitting ? 'Submitting...' : 'Submit Activity'}
        </Button>
      </div>
    </div>
  );

  const renderCodeActivity = () => (
    <div className="space-y-6">
      <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 shadow-lg">
        <CardHeader>
          <CardTitle className="text-white">Coding Challenge</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-slate-400 mb-4">Create a simple player movement script based on the lesson content.</p>

          <div className="bg-gradient-to-br from-slate-800/50 to-slate-700/50 p-4 rounded border border-slate-600 mb-4 backdrop-blur-sm">
            <h4 className="font-semibold mb-2 text-white">Requirements:</h4>
            <ul className="list-disc list-inside space-y-1 text-slate-400">
              <li>Create a script that handles player input (WASD or arrow keys)</li>
              <li>Implement basic movement in 2D or 3D space</li>
              <li>Include comments explaining your code</li>
              <li>Test your script and document any issues</li>
            </ul>
          </div>
        </CardContent>
      </Card>

      <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 shadow-lg">
        <CardHeader>
          <CardTitle className="text-white">Code Submission</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div>
              <Label htmlFor="language">Programming Language</Label>
              <Input
                id="language"
                placeholder="e.g., C#, JavaScript, GDScript"
                value={(submission.language as string) || ''}
                onChange={(e) => setSubmission((prev) => ({ ...prev, language: e.target.value }))}
                className="bg-gray-800 border-gray-600 text-white"
              />
            </div>

            <div>
              <Label htmlFor="code">Your Code</Label>
              <Textarea
                id="code"
                placeholder="Paste your player movement script here..."
                value={(submission.code as string) || ''}
                onChange={(e) => setSubmission((prev) => ({ ...prev, code: e.target.value }))}
                className="bg-gray-800 border-gray-600 text-white font-mono min-h-[300px]"
              />
            </div>

            <div>
              <Label htmlFor="explanation">Code Explanation</Label>
              <Textarea
                id="explanation"
                placeholder="Explain how your code works and any challenges you faced..."
                value={(submission.explanation as string) || ''}
                onChange={(e) => setSubmission((prev) => ({ ...prev, explanation: e.target.value }))}
                className="bg-gray-800 border-gray-600 text-white min-h-[150px]"
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={() => setSubmission((prev) => ({ ...prev, saved: true }))} className="border-gray-600">
          <Save className="h-4 w-4 mr-2" />
          Save Draft
        </Button>
        <Button onClick={handleSubmit} disabled={isSubmitting || !((submission.code as string) || '').trim()} className="bg-green-600 hover:bg-green-700">
          <Send className="h-4 w-4 mr-2" />
          {isSubmitting ? 'Submitting...' : 'Submit Code'}
        </Button>
      </div>
    </div>
  );

  if (!hasStarted) {
    return (
      <div className="text-center py-12">
        <div className="mb-6">{getActivityIcon()}</div>
        <h3 className="text-xl font-semibold mb-2 mt-4 text-white">{item.title}</h3>
        <p className="text-slate-400 mb-6 max-w-md mx-auto">{item.description || 'Ready to start this activity?'}</p>
        <div className="flex items-center justify-center gap-4 mb-6">
          {item.duration && (
            <div className="flex items-center gap-1 text-sm text-slate-400">
              <Clock className="h-4 w-4" />
              {item.duration} minutes
            </div>
          )}
          {item.isRequired && <Badge variant="secondary" className="bg-gradient-to-r from-purple-500/20 to-blue-500/20 border-purple-500/30 text-purple-300">Required</Badge>}
        </div>
        <Button 
          onClick={handleStart} 
          className="bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 border-0 shadow-lg hover:shadow-xl hover:shadow-blue-500/25 transition-all"
        >
          Start Activity
        </Button>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto">
      {item.type === 'quiz' && renderQuizActivity()}
      {item.activityType === 'text' && renderTextActivity()}
      {item.activityType === 'code' && renderCodeActivity()}
      {item.activityType === 'file' && (
        <div className="text-center py-12">
          <Upload className="h-16 w-16 text-gray-400 mx-auto mb-4" />
          <p className="text-gray-400">File upload activity - Implementation in progress</p>
        </div>
      )}
    </div>
  );
}
