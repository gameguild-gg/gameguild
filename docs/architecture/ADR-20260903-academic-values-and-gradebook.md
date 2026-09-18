# ADR: Valores acadêmicos inteiros e fórmula do gradebook

- Status: Aceito
- Data: 2026-09-03
- Escopo: Assessments, Grading, Courses e Gradebook

## Contexto

Scores e percentuais atravessam TypeScript, JSON, C#, PostgreSQL e integrações.
Representações em ponto flutuante ou regras locais de arredondamento geram
divergência. O gradebook também precisa de uma única fórmula, sem cada consumer
reinterpretar pesos e escalas.

## Decisão

Valores acadêmicos persistidos e serializados são inteiros de ponto fixo com
escala `100`:

- `ScoreValue`: `0..2147483647`; `100` unidades representam `1` ponto;
- `PercentValue`: `0..10000`; `100` unidades representam `1%` e `10000`
  representam `100%`.

O domínio usa aritmética inteira exata. Cálculos fracionários usam
intermediários largos (`long`, `BigInteger` ou `bigint`) e quantizam uma única
vez para unidades inteiras com arredondamento `half-up`. JSON transporta
números inteiros e o PostgreSQL persiste `integer`; strings numéricas,
`decimal`, `numeric`, `double` e `float` não representam valores acadêmicos.

`QuizEntry.points` é um `ScoreValue`; `Assessment.MaxScore` é derivado da soma
dos pontos das questões. `Assessment.PassingScore` é absoluto na escala do
assessment. `Program.PassingScore` e `AssessmentGroup.WeightPercent` são
`PercentValue`.

A policy de tentativas seleciona no máximo uma contribuição efetiva finalizada
por assessment. A fórmula única é:

```text
groupRatio = sum(effectiveScore) / sum(capturedMaxScore)
groupContribution = groupRatio * groupWeightPercent
coursePercent = sum(groupContribution)
```

Não há média de percentuais por assessment nem renormalização de pesos ausentes.
Grupo de peso zero e assessment sem grupo não entram no total. Denominador vazio
ou não positivo não produz contribuição nem resultado global oficial. Antes de
publicar resultado global por `Program.PassingScore`, os grupos publicados de
peso positivo devem totalizar exatamente `10000` unidades, equivalentes a
`100%`.

Até uma policy de múltiplas tentativas estar implementada de ponta a ponta,
`maxAttempts > 1` é rejeitado. O primeiro modo seleciona uma única tentativa;
média fica fora do corte inicial.

## Consequências

- APIs públicas aceitam scores e percentuais somente como unidades inteiras
  dentro dos limites dos respectivos value objects;
- projeções são calculadas pela API e persistidas como inteiros canônicos;
- a UI pode exibir e editar valores decimais humanos, mas converte na fronteira
  para unidades inteiras antes de enviar o contrato;
- qualquer fórmula alternativa por consumer é defeito arquitetural.

## Alternativas rejeitadas

- `decimal` no banco;
- strings numéricas no banco ou no JSON;
- ponto flutuante para persistência ou cálculo acadêmico autoritativo;
- média simples dos percentuais dos assessments;
- redistribuição silenciosa de pesos incompletos.
