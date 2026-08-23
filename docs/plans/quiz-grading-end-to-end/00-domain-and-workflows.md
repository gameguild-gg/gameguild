# 00. Domínio e workflows

## Objetivo

Fechar o vocabulário e as invariantes antes de alterar API, banco ou UI. Esta
fase elimina a ambiguidade entre capacidade técnica de uma questão, ator que
avalia e etapa que publica o resultado.

## Estado atual

- `AssessmentGradingMethod` possui valores até `InstructorGraded = 8`;
- `SelfGraded` ainda não existe;
- `Assessments.GradingMethods` persiste uma bitmask;
- a constraint atual permite qualquer combinação dos quatro bits conhecidos;
- a web mostra flags técnicas como checkboxes independentes;
- o host de quiz cria `AutoGraded,InstructorGraded` fixo;
- salvo o caminho parcial de peer review, a API não executa as flags como um
  pipeline.

A API também possui `AssessmentType.SelfAssessment`. Esse enum descreve o tipo
da atividade e não substitui `SelfGraded`. Um quiz autoavaliado continua sendo
`AssessmentType.Quiz` com `GradingMethods = SelfGraded`.

Arquivos centrais:

```text
apps/api/Source/Modules/GameGuild.Learning.Assessments/Models/AssessmentGradingMethod.cs
apps/api/Source/Modules/GameGuild.Learning.Assessments/Entities/Assessment.cs
apps/api/Source/Modules/GameGuild.Learning.Assessments/Services/AssessmentService.cs
apps/web/src/lib/learning/assessment-grading-methods.ts
apps/web/src/components/learning/console/courses/[course]/assessments/[assessmentId]/assessment-editor.tsx
```

## Modelo-alvo

### Métodos primários

| Método | Ator | Natureza do resultado |
| --- | --- | --- |
| `PeerReview` | pares autorizados | consolidação da política de pares |
| `AIGraded` | serviço de IA | probabilístico, versionado e explicável |
| `AutoGraded` | serviço determinístico | reproduzível a partir da revisão e resposta |
| `SelfGraded` | aluno da tentativa | autoavaliação declarada e validada |
| `InstructorGraded` | instrutor autorizado | avaliação docente integral |

### Revisão docente

Quando `InstructorGraded` acompanha outro método, ele deixa de ser avaliador
primário e passa a ser a etapa final obrigatória:

```text
resultado primário -> aguardando instrutor -> aprovado ou alterado -> final
```

Uma bitmask não armazena ordem. A ordem deve ser inferida pela regra canônica,
independentemente da ordem textual recebida no JSON.

### Combinações válidas

| Decimal | Flags | Estágios |
| ---: | --- | --- |
| 1 | `PeerReview` | pares |
| 2 | `AIGraded` | IA |
| 4 | `AutoGraded` | determinístico |
| 8 | `InstructorGraded` | instrutor |
| 9 | `PeerReview,InstructorGraded` | pares -> instrutor |
| 10 | `AIGraded,InstructorGraded` | IA -> instrutor |
| 12 | `AutoGraded,InstructorGraded` | determinístico -> instrutor |
| 16 | `SelfGraded` | aluno |
| 24 | `SelfGraded,InstructorGraded` | aluno -> instrutor |

`None = 0` pode existir durante rascunho, mas publicação de assessment avaliado
exige um workflow válido.

## Invariantes do domínio

1. Exatamente um avaliador primário deve existir na publicação.
2. `InstructorGraded` pode ser o primário ou a última etapa, nunca uma etapa
   intermediária.
3. Duas flags só são válidas quando uma delas é `InstructorGraded`.
4. Três ou mais flags são sempre inválidas.
5. Grupo e peso não alteram o workflow.
6. Somente a última etapa configurada finaliza a submissão.
7. Cada estágio valida o papel do ator no servidor.
8. Alterar workflow não reinterpreta silenciosamente tentativas iniciadas; a
   tentativa usa o snapshot vigente no início.
9. Regrade posterior à finalização é uma operação distinta e auditável.
10. `AssessmentType` e `GradingMethods` permanecem dimensões independentes.

## Estados conceituais

```text
Draft
InProgress
Submitted ou Late
AwaitingPrimaryGrading
PrimaryGraded
AwaitingInstructorReview  somente quando InstructorGraded acompanha o primário
Graded
Returned
```

`AwaitingPrimaryGrading` deve ser projetado em estados específicos para a UI e
as filas: `AwaitingPeerReview`, `AwaitingAIGrading`, `AwaitingAutoGrading`,
`AwaitingSelfGrading` ou `AwaitingInstructorGrading`.

## Contrato para a UX futura

Esta fase apenas define como a escolha deverá ser apresentada. A implementação
da tela pertence à fase de autoria e publicação.

Substituir os checkboxes por:

1. seleção exclusiva `Quem fará a avaliação inicial?`;
2. toggle `Exigir revisão final do instrutor`, oculto quando o instrutor já é o
   avaliador inicial;
3. resumo visual da sequência antes de salvar;
4. configuração específica do método abaixo da seleção.

Rótulos de produto:

```text
Avaliação por pares
Avaliação por IA
Correção automática
Autoavaliação do aluno
Avaliação pelo instrutor
```

## Tarefas

- [ ] adicionar `SelfGraded = 16` sem alterar valores existentes;
- [ ] criar helper de domínio para identificar primário, revisão e ordem;
- [ ] centralizar `ValidateGradingMethods` na API;
- [ ] representar as mesmas combinações em helper client-safe TypeScript;
- [ ] rejeitar combinações inválidas em create, update e publish;
- [ ] impedir que serialização dependa da ordem de um `Set`;
- [ ] adicionar testes unitários para todos os valores de `0` a `31`.

## Critério de saída

- API e web concordam sobre os nove workflows;
- nenhuma combinação inválida pode ser salva como configuração publicável;
- mudar peso ou grupo não muda as flags;
- o contrato da UI descreve atores e ordem sem exigir a implementação da nova
  tela nesta fase;
- `AutoGraded`, `AIGraded` e `SelfGraded` não são confundidos em código, texto
  ou testes.
