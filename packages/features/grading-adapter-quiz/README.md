# @game-guild/grading-adapter-quiz

Adapter explícito entre os contratos genéricos de `@game-guild/grading` e o
domínio de questões de `@game-guild/quiz`.

O package recebe somente `QuizGradingItemInputV1[]`. Ele não conhece blocos,
documentos persistidos, UI, workflow acadêmico ou infraestrutura de banco.
