# ADR: Valores acadêmicos textuais e fórmula do gradebook

- Status: Aceito
- Data: 2026-09-03
- Escopo: Assessments, Grading, Courses e Gradebook

## Contexto

Scores e percentuais atravessam TypeScript, JSON, C#, PostgreSQL e integrações.
Representações em ponto flutuante ou regras locais de arredondamento geram
divergência. O gradebook também precisa de uma única fórmula, sem cada consumer
reinterpretar pesos e escalas.

## Decisão

Valores acadêmicos persistidos e serializados são strings canônicas:

- `ScoreValue`: `^\d{8}\.\d{4}$`, de `00000000.0000` a `99999999.9999`;
- `PercentValue`: `^\d{3}\.\d{4}$`, de `000.0000` a `100.0000`.

O domínio usa aritmética decimal exata. A quantização ocorre uma única vez,
com quatro casas e midpoint away from zero, antes da serialização. Banco e SQL
não somam, tiram média ou convertem esses campos para tipos numéricos. Colunas
ordenáveis usam largura fixa e collation binária/invariante.

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
peso positivo devem totalizar exatamente `100.0000`.

Até uma policy de múltiplas tentativas estar implementada de ponta a ponta,
`maxAttempts > 1` é rejeitado. O primeiro modo seleciona uma única tentativa;
média fica fora do corte inicial.

## Consequências

- nenhuma API pública de grading aceita score acadêmico como `number`,
  `decimal`, `double` ou `float`;
- projeções são calculadas pela API e persistidas em formato canônico;
- UI pode usar texto decimal durante edição, mas só envia forma canônica nos
  contratos publicados;
- qualquer fórmula alternativa por consumer é defeito arquitetural.

## Alternativas rejeitadas

- `decimal` no banco;
- `number` no JSON;
- média simples dos percentuais dos assessments;
- redistribuição silenciosa de pesos incompletos.
