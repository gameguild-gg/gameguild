'use client';

import { createContext, useContext, useEffect, useState } from 'react';
import { $getNodeByKey, DecoratorNode, type SerializedLexicalNode } from 'lexical';
import { Pencil, Plus, RotateCcw, Trash2, X } from 'lucide-react';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import type { JSX } from 'react/jsx-runtime';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { QuizDisplay } from '@/components/editor/ui/quiz/quiz-display';
import { QuizWrapper } from '@/components/editor/ui/quiz/quiz-wrapper';
import { QuizAnswerItem } from '@/components/editor/ui/quiz/quiz-answer-item';
import { useQuizLogic } from '@/hooks/editor/use-quiz-logic';
import { ContentEditMenu } from '@/components/ui/content-edit-menu';

// Adicionar no topo do arquivo, após os imports
const EditorLoadingContext = createContext<boolean>(false);

export const EditorLoadingProvider = EditorLoadingContext.Provider;
export const useEditorLoading = () => useContext(EditorLoadingContext);

export type QuestionType = 'multiple-choice' | 'true-false' | 'fill-blank' | 'short-answer' | 'essay' | 'matching' | 'ordering' | 'rating';

export interface QuizAnswer {
  id: string;
  text: string;
  isCorrect: boolean;
}

export interface MatchingPair {
  id: string;
  left: string;
  right: string;
}

export interface OrderingItem {
  id: string;
  text: string;
  order: number;
}

export interface FillBlankAlternative {
  id: string;
  words: string[]; // Array of words for each blank position
  isCorrect: boolean;
}

export interface QuizData {
  question: string;
  questionType: QuestionType;
  answers: QuizAnswer[];
  correctFeedback?: string;
  incorrectFeedback?: string;
  allowRetry: boolean;
  backgroundColor?: string;
  // For fill-in-the-blank
  blanks?: string[];
  fillBlankMode?: 'text' | 'multiple-choice';
  fillBlankAlternatives?: FillBlankAlternative[]; // New structure for alternatives
  // For matching questions
  matchingPairs?: MatchingPair[];
  // For ordering questions
  orderingItems?: OrderingItem[];
  // For rating questions
  ratingScale?: { min: number; max: number; step: number };
  correctRating?: number;
}

export interface SerializedQuizNode extends SerializedLexicalNode {
  type: 'quiz';
  data: QuizData;
  version: 1;
}

export class QuizNode extends DecoratorNode<JSX.Element> {
  __data: QuizData;

  constructor(data: QuizData, key?: string) {
    super(key);
    this.__data = {
      ...data,
      correctFeedback: data.correctFeedback || '',
      incorrectFeedback: data.incorrectFeedback || '',
      allowRetry: data.allowRetry !== undefined ? data.allowRetry : true,
    };
  }

  static getType(): string {
    return 'quiz';
  }

  static clone(node: QuizNode): QuizNode {
    return new QuizNode(node.__data, node.__key);
  }

  static importJSON(serializedNode: SerializedQuizNode): QuizNode {
    return new QuizNode(serializedNode.data);
  }

  createDOM(): HTMLElement {
    return document.createElement('div');
  }

  updateDOM(): false {
    return false;
  }

  setData(data: QuizData): void {
    const writable = this.getWritable();
    writable.__data = data;
  }

  exportJSON(): SerializedQuizNode {
    return {
      type: 'quiz',
      data: this.__data,
      version: 1,
    };
  }

  decorate(): JSX.Element {
    return <QuizComponent data={this.__data} nodeKey={this.__key} />;
  }
}

interface QuizComponentProps {
  data: QuizData;
  nodeKey: string;
}

function QuizComponent({ data, nodeKey }: QuizComponentProps) {
  const [editor] = useLexicalComposerContext();
  const isLoading = useEditorLoading();

  // Modificar esta linha para considerar o contexto de carregamento
  const [isEditing, setIsEditing] = useState(!data.question && !isLoading);
  const [question, setQuestion] = useState(data.question);
  const [questionType, setQuestionType] = useState<QuestionType>(data.questionType || 'multiple-choice');
  const [answers, setAnswers] = useState<QuizAnswer[]>(data.answers);
  const [correctFeedback, setCorrectFeedback] = useState(data.correctFeedback || '');
  const [incorrectFeedback, setIncorrectFeedback] = useState(data.incorrectFeedback || '');
  const [allowRetry, setAllowRetry] = useState(data.allowRetry !== undefined ? data.allowRetry : true);
  const [blanks, setBlanks] = useState<string[]>(data.blanks || []);
  const [matchingPairs, setMatchingPairs] = useState<MatchingPair[]>(data.matchingPairs || []);
  const [orderingItems, setOrderingItems] = useState<OrderingItem[]>(data.orderingItems || []);
  const [ratingScale, setRatingScale] = useState(data.ratingScale || { min: 1, max: 5, step: 1 });
  const [correctRating, setCorrectRating] = useState(data.correctRating || 3);
  const [fillBlankMode, setFillBlankMode] = useState<'text' | 'multiple-choice'>(data.fillBlankMode || 'text');
  const [fillBlankAlternatives, setFillBlankAlternatives] = useState<FillBlankAlternative[]>(data.fillBlankAlternatives || []);
  const [backgroundColor, setBackgroundColor] = useState(data.backgroundColor || 'white');

  const { selectedAnswers, setSelectedAnswers, showFeedback, setShowFeedback, isCorrect, setIsCorrect, resetQuiz, checkAnswers, toggleAnswer } = useQuizLogic({
    answers: answers,
    allowRetry: allowRetry,
    correctFeedback: correctFeedback,
    incorrectFeedback: incorrectFeedback,
  });

  useEffect(() => {
    if (!isEditing) {
      resetQuiz();
    }
  }, [isEditing, resetQuiz]);

  // Adicionar após os outros useEffects
  useEffect(() => {
    if (isLoading && isEditing) {
      setIsEditing(false);
    }
  }, [isLoading, isEditing]);

  const updateQuiz = (newData: Partial<QuizData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if (node instanceof QuizNode) {
        node.setData({ ...data, ...newData });
      }
    });
  };

  const handleQuestionTypeChange = (newType: QuestionType) => {
    setQuestionType(newType);

    // Reset type-specific data when changing question type
    const newData: Partial<QuizData> = { questionType: newType };

    switch (newType) {
      case 'true-false':
        newData.answers = [
          { id: 'true', text: 'True', isCorrect: false },
          { id: 'false', text: 'False', isCorrect: false },
        ];
        setAnswers(newData.answers);
        break;
      case 'fill-blank':
        newData.blanks = [''];
        setBlanks(['']);
        newData.fillBlankAlternatives = [];
        setFillBlankAlternatives([]);
        break;
      case 'matching':
        newData.matchingPairs = [{ id: '1', left: '', right: '' }];
        setMatchingPairs(newData.matchingPairs);
        break;
      case 'ordering':
        newData.orderingItems = [{ id: '1', text: '', order: 1 }];
        setOrderingItems(newData.orderingItems);
        break;
      case 'rating':
        newData.ratingScale = { min: 1, max: 5, step: 1 };
        newData.correctRating = 3;
        setRatingScale(newData.ratingScale);
        setCorrectRating(3);
        break;
      default:
        // Multiple choice and others
        if (answers.length === 0) {
          newData.answers = [
            { id: '1', text: '', isCorrect: false },
            { id: '2', text: '', isCorrect: false },
          ];
          setAnswers(newData.answers);
        }
    }

    updateQuiz(newData);
  };

  const addAnswer = () => {
    const newAnswer: QuizAnswer = {
      id: Math.random().toString(36).substring(7),
      text: '',
      isCorrect: false,
    };
    const newAnswers = [...answers, newAnswer];
    setAnswers(newAnswers);
    updateQuiz({ answers: newAnswers });
  };

  const updateAnswer = (id: string, text: string) => {
    const newAnswers = answers.map((a) => (a.id === id ? { ...a, text } : a));
    setAnswers(newAnswers);
    updateQuiz({ answers: newAnswers });
  };

  const toggleCorrect = (id: string) => {
    const newAnswers = answers.map((a) => (a.id === id ? { ...a, isCorrect: !a.isCorrect } : a));
    setAnswers(newAnswers);
    updateQuiz({ answers: newAnswers });
  };

  const removeAnswer = (id: string) => {
    const newAnswers = answers.filter((a) => a.id !== id);
    setAnswers(newAnswers);
    updateQuiz({ answers: newAnswers });
  };

  // Get number of blanks from question
  const getBlankCount = () => {
    return (question.match(/___/g) || []).length;
  };

  const addFillBlankAlternative = () => {
    const blankCount = getBlankCount();
    const newAlternative: FillBlankAlternative = {
      id: Math.random().toString(36).substring(7),
      words: new Array(blankCount).fill(''),
      isCorrect: false,
    };
    const newAlternatives = [...fillBlankAlternatives, newAlternative];
    setFillBlankAlternatives(newAlternatives);
    updateQuiz({ fillBlankAlternatives: newAlternatives });
  };

  const updateFillBlankAlternative = (id: string, wordIndex: number, word: string) => {
    const newAlternatives = fillBlankAlternatives.map((alt) => {
      if (alt.id === id) {
        const newWords = [...alt.words];
        newWords[wordIndex] = word;
        return { ...alt, words: newWords };
      }
      return alt;
    });
    setFillBlankAlternatives(newAlternatives);
    updateQuiz({ fillBlankAlternatives: newAlternatives });
  };

  const toggleFillBlankCorrect = (id: string) => {
    const newAlternatives = fillBlankAlternatives.map((alt) => ({
      ...alt,
      isCorrect: alt.id === id ? !alt.isCorrect : alt.isCorrect,
    }));
    setFillBlankAlternatives(newAlternatives);
    updateQuiz({ fillBlankAlternatives: newAlternatives });
  };

  const removeFillBlankAlternative = (id: string) => {
    const newAlternatives = fillBlankAlternatives.filter((alt) => alt.id !== id);
    setFillBlankAlternatives(newAlternatives);
    updateQuiz({ fillBlankAlternatives: newAlternatives });
  };

  const renderQuestionTypeEditor = () => {
    switch (questionType) {
      case 'true-false':
        return (
          <div className="space-y-2">
            <div className="font-semibold">Correct Answer:</div>
            <Select
              value={answers.find((a) => a.isCorrect)?.id || ''}
              onValueChange={(value) => {
                const newAnswers = answers.map((a) => ({ ...a, isCorrect: a.id === value }));
                setAnswers(newAnswers);
                updateQuiz({ answers: newAnswers });
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select correct answer" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="true">True</SelectItem>
                <SelectItem value="false">False</SelectItem>
              </SelectContent>
            </Select>
          </div>
        );

      case 'fill-blank':
        const blankCount = getBlankCount();
        return (
          <div className="space-y-4">
            <div className="font-semibold">Fill Mode:</div>
            <Select
              value={fillBlankMode}
              onValueChange={(value: 'text' | 'multiple-choice') => {
                setFillBlankMode(value);
                updateQuiz({ fillBlankMode: value });
              }}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="text">Free Text</SelectItem>
                <SelectItem value="multiple-choice">Multiple Choice</SelectItem>
              </SelectContent>
            </Select>

            {fillBlankMode === 'text' && (
              <div className="space-y-2">
                <div className="font-semibold">Correct Answers (use ___ in question for blanks):</div>
                {blanks.map((blank, index) => (
                  <div key={index} className="flex gap-2">
                    <Input
                      placeholder={`Answer for blank ${index + 1}`}
                      value={blank}
                      onChange={(e) => {
                        const newBlanks = [...blanks];
                        newBlanks[index] = e.target.value;
                        setBlanks(newBlanks);
                        updateQuiz({ blanks: newBlanks });
                      }}
                    />
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => {
                        const newBlanks = blanks.filter((_, i) => i !== index);
                        setBlanks(newBlanks);
                        updateQuiz({ blanks: newBlanks });
                      }}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                ))}
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    const newBlanks = [...blanks, ''];
                    setBlanks(newBlanks);
                    updateQuiz({ blanks: newBlanks });
                  }}
                >
                  <Plus className="mr-1 h-4 w-4" />
                  Add Blank
                </Button>
              </div>
            )}

            {fillBlankMode === 'multiple-choice' && (
              <div className="space-y-4">
                <div className="font-semibold">Answer Alternatives ({blankCount} blanks detected):</div>
                {fillBlankAlternatives.map((alternative) => (
                  <div key={alternative.id} className="border rounded-lg p-4 space-y-2">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-2">
                        <input type="checkbox" checked={alternative.isCorrect} onChange={() => toggleFillBlankCorrect(alternative.id)} className="rounded" />
                        <Label>Correct Alternative</Label>
                      </div>
                      <Button variant="ghost" size="icon" onClick={() => removeFillBlankAlternative(alternative.id)}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                    <div className="grid grid-cols-1 gap-2">
                      {alternative.words.map((word, wordIndex) => (
                        <div key={wordIndex} className="flex items-center space-x-2">
                          <Label className="w-20">Blank {wordIndex + 1}:</Label>
                          <Input
                            placeholder={`Word for blank ${wordIndex + 1}`}
                            value={word}
                            onChange={(e) => updateFillBlankAlternative(alternative.id, wordIndex, e.target.value)}
                            className="flex-1"
                          />
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
                <Button variant="outline" size="sm" onClick={addFillBlankAlternative} disabled={blankCount === 0}>
                  <Plus className="mr-1 h-4 w-4" />
                  Add Alternative
                </Button>
                {blankCount === 0 && <p className="text-sm text-muted-foreground">Add "___" to your question to create blanks first.</p>}
              </div>
            )}
          </div>
        );

      case 'rating':
        return (
          <div className="space-y-4">
            <div className="grid grid-cols-3 gap-4">
              <div>
                <Label>Min Value</Label>
                <Input
                  type="number"
                  value={ratingScale.min}
                  onChange={(e) => {
                    const newScale = { ...ratingScale, min: Number.parseInt(e.target.value) || 1 };
                    setRatingScale(newScale);
                    updateQuiz({ ratingScale: newScale });
                  }}
                />
              </div>
              <div>
                <Label>Max Value</Label>
                <Input
                  type="number"
                  value={ratingScale.max}
                  onChange={(e) => {
                    const newScale = { ...ratingScale, max: Number.parseInt(e.target.value) || 5 };
                    setRatingScale(newScale);
                    updateQuiz({ ratingScale: newScale });
                  }}
                />
              </div>
              <div>
                <Label>Correct Rating</Label>
                <Input
                  type="number"
                  value={correctRating}
                  min={ratingScale.min}
                  max={ratingScale.max}
                  onChange={(e) => {
                    const rating = Number.parseInt(e.target.value) || 3;
                    setCorrectRating(rating);
                    updateQuiz({ correctRating: rating });
                  }}
                />
              </div>
            </div>
          </div>
        );

      default:
        // Multiple choice, short answer, essay
        return (
          <div className="space-y-2">
            {questionType === 'multiple-choice' && (
              <>
                {answers.map((answer) => (
                  <QuizAnswerItem key={answer.id}>
                    <Input
                      type="checkbox"
                      id={answer.id}
                      checked={answer.isCorrect}
                      onChange={() => toggleCorrect(answer.id)}
                      className="h-5 w-5 rounded-full"
                    />
                    <Input placeholder="Enter answer" value={answer.text} onChange={(e) => updateAnswer(answer.id, e.target.value)} className="flex-1" />
                    <Button variant="ghost" size="icon" onClick={() => removeAnswer(answer.id)}>
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </QuizAnswerItem>
                ))}
                <Button variant="outline" size="sm" onClick={addAnswer}>
                  <Plus className="mr-1 h-4 w-4" />
                  Add Answer
                </Button>
              </>
            )}
            {(questionType === 'short-answer' || questionType === 'essay') && (
              <div>
                <Label>Sample Answer (optional):</Label>
                <Input
                  placeholder="Enter a sample correct answer"
                  value={answers[0]?.text || ''}
                  onChange={(e) => {
                    const newAnswers = [{ id: '1', text: e.target.value, isCorrect: true }];
                    setAnswers(newAnswers);
                    updateQuiz({ answers: newAnswers });
                  }}
                />
              </div>
            )}
          </div>
        );
    }
  };

  return (
    <>
      <div className="relative group">
        {!isEditing && (
          <Button
            variant="outline"
            size="sm"
            className="absolute -right-2 top-2 z-10 opacity-0 group-hover:opacity-100 transition-opacity bg-white shadow-sm"
            onClick={() => setIsEditing(true)}
          >
            <Pencil className="h-4 w-4" />
          </Button>
        )}
        <QuizWrapper backgroundColor={backgroundColor}>
          {!isEditing && (
            <ContentEditMenu
              options={[
                {
                  id: 'edit',
                  icon: <Pencil className="h-4 w-4" />,
                  label: 'Edit Quiz',
                  action: () => setIsEditing(true),
                },
              ]}
            />
          )}
          {!isEditing && showFeedback && !allowRetry && (
            <div className="flex justify-end mt-4 pt-3 border-t border-border/50">
              <Button
                variant="outline"
                size="sm"
                onClick={resetQuiz}
                className="flex items-center gap-2 text-muted-foreground hover:text-foreground transition-colors"
              >
                <RotateCcw className="h-4 w-4" />
                Try Again
              </Button>
            </div>
          )}
          <QuizDisplay
            question={question}
            questionType={questionType}
            answers={answers}
            selectedAnswers={selectedAnswers}
            setSelectedAnswers={setSelectedAnswers}
            showFeedback={showFeedback}
            isCorrect={isCorrect}
            correctFeedback={correctFeedback}
            incorrectFeedback={incorrectFeedback}
            allowRetry={allowRetry}
            checkAnswers={checkAnswers}
            toggleAnswer={toggleAnswer}
            blanks={blanks}
            fillBlankMode={fillBlankMode}
            fillBlankAlternatives={fillBlankAlternatives}
            ratingScale={ratingScale}
            correctRating={correctRating}
          />
        </QuizWrapper>
      </div>

      {/* Modal Editor - Rendered separately */}
      {isEditing && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" onClick={() => setIsEditing(false)}>
          <div className="bg-white rounded-xl shadow-xl max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto" onClick={(e) => e.stopPropagation()}>
            {/* Header */}
            <div className="sticky top-0 z-10 bg-white border-b px-6 py-4 rounded-t-xl">
              <div className="flex items-center justify-between">
                <h3 className="text-lg font-semibold">Edit Quiz</h3>
                <Button variant="ghost" size="icon" onClick={() => setIsEditing(false)}>
                  <X className="h-4 w-4" />
                </Button>
              </div>
            </div>

            {/* Content */}
            <div className="p-6 space-y-6">
              {/* Header with question type and background color */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="text-sm font-medium">Question Type</Label>
                  <Select value={questionType} onValueChange={handleQuestionTypeChange}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="multiple-choice">Multiple Choice</SelectItem>
                      <SelectItem value="true-false">True/False</SelectItem>
                      <SelectItem value="fill-blank">Fill in the Blank</SelectItem>
                      <SelectItem value="short-answer">Short Answer</SelectItem>
                      <SelectItem value="essay">Essay</SelectItem>
                      <SelectItem value="rating">Rating Scale</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div>
                  <Label className="text-sm font-medium">Background Color</Label>
                  <Select
                    value={backgroundColor}
                    onValueChange={(value) => {
                      setBackgroundColor(value);
                      updateQuiz({ backgroundColor: value });
                    }}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="white">White</SelectItem>
                      <SelectItem value="blue">Blue</SelectItem>
                      <SelectItem value="green">Green</SelectItem>
                      <SelectItem value="purple">Purple</SelectItem>
                      <SelectItem value="orange">Orange</SelectItem>
                      <SelectItem value="gray">Gray</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              {/* Question input */}
              <div>
                <Label className="text-sm font-medium">Question</Label>
                <Input
                  placeholder="Enter your question"
                  value={question}
                  onChange={(e) => {
                    setQuestion(e.target.value);
                    updateQuiz({ question: e.target.value });
                  }}
                  className="mt-1"
                />
              </div>

              {/* Question type specific content */}
              <div className="border rounded-lg p-4 bg-gray-50">
                <Label className="text-sm font-medium mb-3 block">Answer Options</Label>
                {renderQuestionTypeEditor()}
              </div>

              {/* Feedback and settings in a compact grid */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="text-sm font-medium">Correct Feedback</Label>
                  <Input
                    placeholder="Great job!"
                    value={correctFeedback}
                    onChange={(e) => {
                      setCorrectFeedback(e.target.value);
                      updateQuiz({ correctFeedback: e.target.value });
                    }}
                    className="mt-1"
                  />
                </div>
                <div>
                  <Label className="text-sm font-medium">Incorrect Feedback</Label>
                  <Input
                    placeholder="Try again!"
                    value={incorrectFeedback}
                    onChange={(e) => {
                      setIncorrectFeedback(e.target.value);
                      updateQuiz({ incorrectFeedback: e.target.value });
                    }}
                    className="mt-1"
                  />
                </div>
              </div>

              {/* Settings row */}
              <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                <div className="flex items-center space-x-2">
                  <Switch
                    id="allow-retry"
                    checked={allowRetry}
                    onCheckedChange={(checked) => {
                      setAllowRetry(checked);
                      updateQuiz({ allowRetry: checked });
                    }}
                  />
                  <Label htmlFor="allow-retry" className="text-sm">
                    Allow Retry
                  </Label>
                </div>
                <Button size="sm" onClick={() => setIsEditing(false)} className="ml-4">
                  Save Quiz
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

export function $createQuizNode(): QuizNode {
  return new QuizNode({
    question: '',
    questionType: 'multiple-choice',
    answers: [
      { id: '1', text: '', isCorrect: false },
      { id: '2', text: '', isCorrect: false },
    ],
    allowRetry: true,
    fillBlankMode: 'text',
    fillBlankAlternatives: [],
    backgroundColor: 'white',
  });
}
