'use client';
import { Button } from '@/components/ui/button';
import { ChevronDown, ChevronUp } from 'lucide-react';

interface OrderingQuestionProps {
  question: string;
  items: string[];
  userOrder: string[];
  onOrderChange: (newOrder: string[]) => void;
  showFeedback: boolean;
  isCorrect: boolean;
  disabled?: boolean;
}

export function OrderingQuestion({ question, userOrder, onOrderChange, disabled = false }: OrderingQuestionProps) {
  const moveItem = (index: number, direction: 'up' | 'down') => {
    if (disabled) return;

    const newOrder = [...userOrder];
    const targetIndex = direction === 'up' ? index - 1 : index + 1;

    if (targetIndex >= 0 && targetIndex < newOrder.length) {
      [newOrder[index], newOrder[targetIndex]] = [newOrder[targetIndex], newOrder[index]];
      onOrderChange(newOrder);
    }
  };

  return (
    <div className="space-y-4">
      <div className="font-semibold">{question}</div>
      <div className="space-y-2">
        {userOrder.map((item, index) => (
          <div key={item} className="flex items-center gap-2 p-3 border rounded-md">
            <span className="flex-1">{item}</span>
            <div className="flex gap-1">
              <Button variant="ghost" size="sm" onClick={() => moveItem(index, 'up')} disabled={disabled || index === 0}>
                <ChevronUp className="h-4 w-4" />
              </Button>
              <Button variant="ghost" size="sm" onClick={() => moveItem(index, 'down')} disabled={disabled || index === userOrder.length - 1}>
                <ChevronDown className="h-4 w-4" />
              </Button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
