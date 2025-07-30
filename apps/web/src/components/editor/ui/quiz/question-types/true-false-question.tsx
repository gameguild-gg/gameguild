'use client';

import { Button } from "@/components/ui/button"
import { Check, X } from "lucide-react"

interface TrueFalseQuestionProps {
  question: string;
  correctAnswer: boolean;
  selectedAnswer: boolean | null;
  onAnswerSelect: (answer: boolean) => void;
  showFeedback: boolean;
  isCorrect: boolean;
  disabled?: boolean;
}

export function TrueFalseQuestion({ question, selectedAnswer, onAnswerSelect, showFeedback, isCorrect, disabled = false }: TrueFalseQuestionProps) {
  return (
    <div className="space-y-4">
      <div className="font-semibold">{question}</div>
      <div className="flex gap-4">
        <Button
          variant={selectedAnswer === true ? 'default' : 'outline'}
          onClick={() => onAnswerSelect(true)}
          disabled={disabled}
          className="flex items-center gap-2"
        >
          <Check className="h-4 w-4" />
          True
        </Button>
        <Button
          variant={selectedAnswer === false ? 'default' : 'outline'}
          onClick={() => onAnswerSelect(false)}
          disabled={disabled}
          className="flex items-center gap-2"
        >
          <X className="h-4 w-4" />
          False
        </Button>
      </div>
    </div>
  );
}
