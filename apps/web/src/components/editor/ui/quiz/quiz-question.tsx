import type React from 'react';

interface QuizQuestionProps {
  children: React.ReactNode;
}

export function QuizQuestion({ children }: QuizQuestionProps) {
  return <div className="font-semibold">{children}</div>;
}
