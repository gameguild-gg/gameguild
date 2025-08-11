'use client';

import { Textarea } from '@/components/ui/textarea';

interface EssayQuestionProps {
  question: string;
  userAnswer: string;
  onAnswerChange: (answer: string) => void;
  showFeedback: boolean;
  isCorrect: boolean;
  disabled?: boolean;
}

export function EssayQuestion({ question, userAnswer, onAnswerChange, disabled = false }: EssayQuestionProps) {
  return (
    <div className="space-y-4">
      <div className="font-semibold">{question}</div>
      <Textarea value={userAnswer} onChange={(e) => onAnswerChange(e.target.value)} disabled={disabled} placeholder="Write your essay answer here..." rows={8} className="min-h-[200px]" />
    </div>
  );
}
