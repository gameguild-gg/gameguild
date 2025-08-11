import { useState } from 'react';
import { Button } from '@/components/ui/button';
import MarkdownRenderer from '@/components/markdown-renderer/markdown-renderer';

interface QuizProps {
  title: string;
  question: string;
  options: string[];
  answers: string[];
}

export function MarkdownQuizActivity({
  title,
  question,
  options,
  answers,
}: QuizProps) {
  const [selectedOptions, setSelectedOptions] = useState<string[]>([]);
  const [submitted, setSubmitted] = useState(false);

  const handleOptionChange = (option: string) => {
    setSelectedOptions((prev) =>
      prev.includes(option)
        ? prev.filter((item) => item !== option)
        : [...prev, option],
    );
  };

  const handleSubmit = () => {
    setSubmitted(true);
  };

  const isCorrect =
    JSON.stringify(selectedOptions.sort()) === JSON.stringify(answers.sort());

  return (
    <div className="border border-border rounded-lg p-4 my-4 bg-card">
      <h3 className="text-lg font-semibold mb-2 text-foreground">{title}</h3>
      <MarkdownRenderer
        renderer="markdown"
        content={question}
      />
      <div className="space-y-3 mt-4">
        {options.map((option, index) => (
          <div 
            key={index} 
            className={`flex items-center space-x-3 p-3 rounded-lg border transition-colors cursor-pointer ${
              selectedOptions.includes(option)
                ? 'border-primary bg-primary/10 hover:bg-primary/20'
                : 'border-border hover:bg-muted/50'
            }`}
            onClick={() => handleOptionChange(option)}
          >
            <div className={`w-5 h-5 border-2 rounded flex items-center justify-center ${
              selectedOptions.includes(option)
                ? 'border-primary bg-primary'
                : 'border-muted-foreground bg-background'
            }`}>
              {selectedOptions.includes(option) && (
                <svg 
                  className="w-3 h-3 text-white" 
                  fill="currentColor" 
                  viewBox="0 0 20 20"
                >
                  <path 
                    fillRule="evenodd" 
                    d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" 
                    clipRule="evenodd" 
                  />
                </svg>
              )}
            </div>
            <span className="text-sm font-medium flex-1 text-foreground">
              {option}
            </span>
          </div>
        ))}
      </div>
      {(!submitted || !isCorrect) && (
        <Button 
          onClick={handleSubmit} 
          className="mt-4 w-full"
        >
          Submit
        </Button>
      )}
      {submitted && (
        <div
          className={`mt-4 p-3 rounded-lg border ${
            isCorrect 
              ? 'bg-green-50 border-green-200 text-green-800 dark:bg-green-950 dark:border-green-800 dark:text-green-200' 
              : 'bg-red-50 border-red-200 text-red-800 dark:bg-red-950 dark:border-red-800 dark:text-red-200'
          }`}
        >
          {isCorrect ? 'Correct!' : 'Incorrect. Try again!'}
        </div>
      )}
    </div>
  );
}