'use client';

import { Textarea } from '@/components/ui/textarea';

interface ShortAnswerQuestionProps {
  question: string;
  userAnswer: string;
  onAnswerChange: (answer: string) => void;
  showFeedback: boolean;
  isCorrect: boolean;
  disabled?: boolean;
}

export function ShortAnswerQuestion({ question, userAnswer, onAnswerChange, disabled = false }: ShortAnswerQuestionProps) {
  return (
    <div className="space-y-4">
      <div className="font-semibold">{question}</div>
      <Textarea value={userAnswer} onChange={(e) => onAnswerChange(e.target.value)} disabled={disabled} placeholder="Enter your answer" rows={3} />
    </div>
  );
}
