'use client';
import { QuizFeedback } from './quiz-feedback';
import type { QuizAnswer, QuestionType, FillBlankAlternative } from '../../nodes/quiz-node';

interface QuizDisplayProps {
  question: string;
  questionType: QuestionType;
  answers: QuizAnswer[];
  selectedAnswers: string[];
  setSelectedAnswers: (answers: string[]) => void;
  showFeedback: boolean;
  isCorrect: boolean;
  correctFeedback: string;
  incorrectFeedback: string;
  allowRetry: boolean;
  checkAnswers: () => void;
  toggleAnswer: (answerId: string) => void;
  blanks?: string[];
  fillBlankMode?: 'text' | 'multiple-choice';
  fillBlankOptions?: string[];
  ratingScale?: { min: number; max: number; step: number };
  correctRating?: number;
  fillBlankAlternatives?: FillBlankAlternative[];
}

export function QuizDisplay({
  question,
  questionType,
  answers,
  selectedAnswers,
  setSelectedAnswers,
  showFeedback,
  isCorrect,
  correctFeedback,
  incorrectFeedback,
  allowRetry,
  checkAnswers,
  toggleAnswer,
  blanks = [],
  fillBlankMode = 'text',
  fillBlankOptions = [],
  ratingScale = { min: 1, max: 5, step: 1 },
  correctRating = 3,
  fillBlankAlternatives = [],
}: QuizDisplayProps) {
  const renderQuestionContent = () => {
    switch (questionType) {
      case 'multiple-choice':
        return (
          <div className="space-y-3">
            {answers.map((answer) => (
              <div
                key={answer.id}
                className={`
                relative flex items-center p-4 rounded-lg border-2 cursor-pointer transition-all duration-200
                ${selectedAnswers.includes(answer.id) ? 'border-blue-500 bg-blue-50 shadow-sm' : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'}
                ${showFeedback && !allowRetry ? 'cursor-not-allowed opacity-75' : 'hover:shadow-sm'}
              `}
                onClick={() => (!showFeedback || allowRetry ? toggleAnswer(answer.id) : undefined)}
              >
                <div
                  className={`
                flex items-center justify-center w-5 h-5 rounded border-2 mr-3 transition-colors
                ${selectedAnswers.includes(answer.id) ? 'border-blue-500 bg-blue-500' : 'border-gray-300'}
              `}
                >
                  {selectedAnswers.includes(answer.id) && (
                    <svg className="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20">
                      <path
                        fillRule="evenodd"
                        d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                        clipRule="evenodd"
                      />
                    </svg>
                  )}
                </div>
                <span className="text-base font-medium text-gray-800">{answer.text}</span>
              </div>
            ))}
          </div>
        );

      case 'true-false':
        return (
          <div className="flex gap-4">
            <button
              className={`
              flex items-center justify-center px-6 py-3 rounded-lg border-2 font-medium transition-all duration-200 min-w-[120px]
              ${
                selectedAnswers.includes('true')
                  ? 'border-green-500 bg-green-50 text-green-700 shadow-sm'
                  : 'border-gray-200 text-gray-700 hover:border-green-300 hover:bg-green-50'
              }
              ${showFeedback && !allowRetry ? 'cursor-not-allowed opacity-75' : 'hover:shadow-sm cursor-pointer'}
            `}
              onClick={() => (!(showFeedback && !allowRetry) ? setSelectedAnswers(['true']) : undefined)}
              disabled={showFeedback && !allowRetry}
            >
              <svg className="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path
                  fillRule="evenodd"
                  d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                  clipRule="evenodd"
                />
              </svg>
              True
            </button>
            <button
              className={`
              flex items-center justify-center px-6 py-3 rounded-lg border-2 font-medium transition-all duration-200 min-w-[120px]
              ${
                selectedAnswers.includes('false')
                  ? 'border-red-500 bg-red-50 text-red-700 shadow-sm'
                  : 'border-gray-200 text-gray-700 hover:border-red-300 hover:bg-red-50'
              }
              ${showFeedback && !allowRetry ? 'cursor-not-allowed opacity-75' : 'hover:shadow-sm cursor-pointer'}
            `}
              onClick={() => (!(showFeedback && !allowRetry) ? setSelectedAnswers(['false']) : undefined)}
              disabled={showFeedback && !allowRetry}
            >
              <svg className="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 20 20">
                <path
                  fillRule="evenodd"
                  d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                  clipRule="evenodd"
                />
              </svg>
              False
            </button>
          </div>
        );

      case 'fill-blank':
        if (fillBlankMode === 'text') {
          const questionParts = question.split('___');
          return (
            <div className="space-y-4">
              <div className="text-lg leading-relaxed">
                {questionParts.map((part, index) => (
                  <span key={index}>
                    {part}
                    {index < questionParts.length - 1 && (
                      <input
                        className="inline-block w-32 mx-2 px-3 py-2 border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none transition-colors"
                        placeholder="..."
                        value={selectedAnswers[index] || ''}
                        onChange={(e) => {
                          const newAnswers = [...selectedAnswers];
                          newAnswers[index] = e.target.value;
                          setSelectedAnswers(newAnswers);
                        }}
                        disabled={showFeedback && !allowRetry}
                      />
                    )}
                  </span>
                ))}
              </div>
            </div>
          );
        } else {
          const questionParts = question.split('___');
          return (
            <div className="space-y-4">
              <div className="text-lg font-medium mb-6 p-4 bg-gray-50 rounded-lg">
                {questionParts.map((part, index) => (
                  <span key={index}>
                    {part}
                    {index < questionParts.length - 1 && <span className="mx-1 px-3 py-1 bg-blue-100 text-blue-800 rounded-md font-mono">___</span>}
                  </span>
                ))}
              </div>
              <div className="space-y-3">
                {fillBlankAlternatives?.map((alternative) => (
                  <div
                    key={alternative.id}
                    className={`
                    flex items-center p-4 rounded-lg border-2 cursor-pointer transition-all duration-200
                    ${selectedAnswers[0] === alternative.id ? 'border-blue-500 bg-blue-50 shadow-sm' : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'}
                    ${showFeedback && !allowRetry ? 'cursor-not-allowed opacity-75' : 'hover:shadow-sm'}
                  `}
                    onClick={() => (!(showFeedback && !allowRetry) ? setSelectedAnswers([alternative.id]) : undefined)}
                  >
                    <div
                      className={`
                    flex items-center justify-center w-5 h-5 rounded-full border-2 mr-4 transition-colors
                    ${selectedAnswers[0] === alternative.id ? 'border-blue-500 bg-blue-500' : 'border-gray-300'}
                  `}
                    >
                      {selectedAnswers[0] === alternative.id && <div className="w-2 h-2 bg-white rounded-full"></div>}
                    </div>
                    <span className="text-base font-medium text-gray-800">{alternative.words.join(', ')}</span>
                  </div>
                ))}
              </div>
            </div>
          );
        }

      case 'short-answer':
        return (
          <div className="space-y-2">
            <input
              className="w-full px-4 py-3 border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none transition-colors text-base"
              placeholder="Enter your answer..."
              value={selectedAnswers[0] || ''}
              onChange={(e) => setSelectedAnswers([e.target.value])}
              disabled={showFeedback && !allowRetry}
            />
          </div>
        );

      case 'essay':
        return (
          <div className="space-y-2">
            <textarea
              className="w-full px-4 py-3 border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none transition-colors text-base resize-vertical"
              placeholder="Write your essay here..."
              value={selectedAnswers[0] || ''}
              onChange={(e) => setSelectedAnswers([e.target.value])}
              disabled={showFeedback && !allowRetry}
              rows={6}
            />
          </div>
        );

      case 'rating':
        return (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-600 font-medium">{ratingScale.min} (Lowest)</span>
              <span className="text-sm text-gray-600 font-medium">{ratingScale.max} (Highest)</span>
            </div>
            <div className="flex items-center justify-center space-x-3">
              {Array.from({ length: ratingScale.max - ratingScale.min + 1 }, (_, i) => ratingScale.min + i).map((value) => (
                <button
                  key={value}
                  className={`
                    w-12 h-12 rounded-lg border-2 font-bold text-lg transition-all duration-200
                    ${
                      selectedAnswers.includes(value.toString())
                        ? 'border-blue-500 bg-blue-500 text-white shadow-lg scale-110'
                        : 'border-gray-300 text-gray-700 hover:border-blue-300 hover:bg-blue-50'
                    }
                    ${showFeedback && !allowRetry ? 'cursor-not-allowed opacity-75' : 'hover:shadow-md cursor-pointer'}
                  `}
                  onClick={() => (!(showFeedback && !allowRetry) ? setSelectedAnswers([value.toString()]) : undefined)}
                  disabled={showFeedback && !allowRetry}
                >
                  {value}
                </button>
              ))}
            </div>
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div className="space-y-4">
      {questionType !== 'fill-blank' || fillBlankMode !== 'multiple-choice' ? <div className="text-lg font-medium">{question}</div> : null}

      {renderQuestionContent()}

      {!showFeedback && (
        <button
          onClick={checkAnswers}
          className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-3 px-6 rounded-lg transition-colors duration-200 shadow-sm hover:shadow-md"
        >
          Submit Answer
        </button>
      )}

      {showFeedback && <QuizFeedback isCorrect={isCorrect} correctFeedback={correctFeedback} incorrectFeedback={incorrectFeedback} allowRetry={allowRetry} />}
    </div>
  );
}
