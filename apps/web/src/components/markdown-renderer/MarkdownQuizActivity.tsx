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
    <div className="border rounded-lg p-4 my-4 bg-gray-50">
      <h3 className="text-lg font-semibold mb-2">{title}</h3>
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
                ? 'border-blue-500 bg-blue-50 hover:bg-blue-100'
                : 'border-gray-200 hover:bg-gray-50'
            }`}
            onClick={() => handleOptionChange(option)}
          >
            <div className={`w-5 h-5 border-2 rounded flex items-center justify-center ${
              selectedOptions.includes(option)
                ? 'border-blue-600 bg-blue-600'
                : 'border-gray-400 bg-white'
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
            <span className="text-sm font-medium flex-1">
              {option}
            </span>
          </div>
        ))}
      </div>
      {(!submitted || !isCorrect) && (
        <Button 
          onClick={handleSubmit} 
          className="mt-4 w-full bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded-lg"
        >
          Submit
        </Button>
      )}
      {submitted && (
        <div
          className={`mt-4 p-2 rounded ${isCorrect ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}
        >
          {isCorrect ? 'Correct!' : 'Incorrect. Try again!'}
        </div>
      )}
    </div>
  );
} 