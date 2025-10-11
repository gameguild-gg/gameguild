import type React from 'react';

interface QuizAnswerItemProps {
  children: React.ReactNode;
}

export function QuizAnswerItem({ children }: QuizAnswerItemProps) {
  return <div className="flex items-center gap-2">{children}</div>;
}
