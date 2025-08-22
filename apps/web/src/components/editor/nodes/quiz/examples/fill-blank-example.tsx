"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import type { FillBlankField } from "../quiz-node"

// Exemplo de dados de quiz fill-blank
const exampleQuizData = {
  question: "A ___ é a capital do Brasil e fica no estado de ___. O país foi descoberto em ___.",
  fillBlankFields: [
    {
      id: "1",
      position: 0,
      expectedWords: ["Brasília", "brasilia", "BRASILIA"],
      alternatives: [
        { id: "alt1", words: ["Brasilia", "brasília"], isCorrect: true }
      ]
    },
    {
      id: "2", 
      position: 1,
      expectedWords: ["Distrito Federal", "distrito federal", "DISTRITO FEDERAL"],
      alternatives: [
        { id: "alt2", words: ["DF", "Distrito Federal"], isCorrect: true }
      ]
    },
    {
      id: "3",
      position: 2,
      expectedWords: ["1500", "1500 d.C.", "1500 DC"],
      alternatives: [
        { id: "alt3", words: ["1500 DC", "1500 d.C."], isCorrect: true }
      ]
    }
  ]
}

export function FillBlankExample() {
  const [userAnswers, setUserAnswers] = useState<string[]>(["", "", ""])
  const [showFeedback, setShowFeedback] = useState(false)
  const [isCorrect, setIsCorrect] = useState(false)

  const checkAnswers = () => {
    const isAllCorrect = exampleQuizData.fillBlankFields.every((field, index) => {
      const userAnswer = userAnswers[index] || ""
      if (!userAnswer.trim()) return false
      
      const allAcceptableWords = [
        ...field.expectedWords,
        ...field.alternatives.flatMap(alt => alt.words)
      ]
      
      return allAcceptableWords.some(word => 
        word.toLowerCase().trim() === userAnswer.toLowerCase().trim()
      )
    })

    setIsCorrect(isAllCorrect)
    setShowFeedback(true)
  }

  const resetQuiz = () => {
    setUserAnswers(["", "", ""])
    setShowFeedback(false)
    setIsCorrect(false)
  }

  const updateAnswer = (index: number, value: string) => {
    const newAnswers = [...userAnswers]
    newAnswers[index] = value
    setUserAnswers(newAnswers)
  }

  const renderQuestion = () => {
    const questionParts = exampleQuizData.question.split("___")
    return (
      <div className="text-lg leading-relaxed">
        {questionParts.map((part, index) => (
          <span key={index}>
            {part}
            {index < questionParts.length - 1 && (
              <input
                className="inline-block w-32 mx-2 px-3 py-2 border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none transition-colors"
                placeholder="..."
                value={userAnswers[index] || ""}
                onChange={(e) => updateAnswer(index, e.target.value)}
                disabled={showFeedback}
              />
            )}
          </span>
        ))}
      </div>
    )
  }

  return (
    <div className="max-w-4xl mx-auto p-6 space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Exemplo de Quiz Fill-in-the-Blank</CardTitle>
          <CardDescription>
            Este é um exemplo de como o novo sistema de quiz fill-in-the-blank funciona.
            Digite suas respostas nos campos e clique em "Verificar Respostas".
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* Pergunta */}
          <div className="space-y-4">
            <h3 className="text-lg font-medium">Pergunta:</h3>
            {renderQuestion()}
          </div>

          {/* Botões */}
          <div className="flex gap-4">
            {!showFeedback ? (
              <Button onClick={checkAnswers} className="bg-blue-600 hover:bg-blue-700">
                Verificar Respostas
              </Button>
            ) : (
              <Button onClick={resetQuiz} variant="outline">
                Tentar Novamente
              </Button>
            )}
          </div>

          {/* Feedback */}
          {showFeedback && (
            <div className={`p-4 rounded-lg border-2 ${
              isCorrect 
                ? "border-green-500 bg-green-50 text-green-800" 
                : "border-red-500 bg-red-50 text-red-800"
            }`}>
              <h4 className="font-medium mb-2">
                {isCorrect ? "✅ Parabéns! Todas as respostas estão corretas!" : "❌ Algumas respostas estão incorretas."}
              </h4>
              {!isCorrect && (
                <div className="text-sm space-y-2">
                  <p>Respostas corretas:</p>
                  <ul className="list-disc list-inside space-y-1">
                    {exampleQuizData.fillBlankFields.map((field, index) => (
                      <li key={index}>
                        <strong>Campo {index + 1}:</strong> {field.expectedWords.join(", ")}
                        {field.alternatives.length > 0 && (
                          <span className="text-gray-600">
                            {" "}ou {field.alternatives.map(alt => alt.words.join(", ")).join(", ")}
                          </span>
                        )}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          )}

          {/* Configuração dos Campos */}
          <div className="space-y-4">
            <h3 className="text-lg font-medium">Configuração dos Campos:</h3>
            <div className="grid gap-4">
              {exampleQuizData.fillBlankFields.map((field, index) => (
                <Card key={field.id} className="border-gray-200">
                  <CardHeader className="pb-3">
                    <CardTitle className="text-base">Campo #{index + 1}</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <div>
                      <label className="text-sm font-medium text-gray-700">Palavras Esperadas:</label>
                      <p className="text-sm text-gray-600 bg-gray-50 p-2 rounded">
                        {field.expectedWords.join(", ")}
                      </p>
                    </div>
                    {field.alternatives.length > 0 && (
                      <div>
                        <label className="text-sm font-medium text-gray-700">Alternativas:</label>
                        {field.alternatives.map(alt => (
                          <p key={alt.id} className="text-sm text-gray-600 bg-gray-50 p-2 rounded mt-1">
                            {alt.words.join(", ")}
                          </p>
                        ))}
                      </div>
                    )}
                  </CardContent>
                </Card>
              ))}
            </div>
          </div>

          {/* Instruções */}
          <Card className="bg-blue-50 border-blue-200">
            <CardHeader>
              <CardTitle className="text-blue-800">Como Usar</CardTitle>
            </CardHeader>
            <CardContent className="text-blue-700">
              <ul className="list-disc list-inside space-y-1 text-sm">
                <li>Use <code className="bg-blue-100 px-1 rounded">___</code> na pergunta para criar espaços em branco</li>
                <li>Configure as palavras esperadas para cada campo</li>
                <li>Adicione alternativas para variações aceitáveis</li>
                <li>O sistema valida automaticamente as respostas</li>
                <li>As respostas são case-insensitive (não diferenciam maiúsculas/minúsculas)</li>
              </ul>
            </CardContent>
          </Card>
        </CardContent>
      </Card>
    </div>
  )
}
