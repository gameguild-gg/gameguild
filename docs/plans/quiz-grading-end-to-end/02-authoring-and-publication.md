# 02. Autoria e publicação

## Objetivo

Fazer com que quiz, grading, revisão autoral e assessment sejam validados e
persistidos por um único caso de uso da API, com uma UX clara para o professor.

Assessment é o braço operacional do content avaliável. Ele referencia e
configura como o conteúdo será aplicado e avaliado, mas não se torna uma segunda
fonte de verdade para enunciados, opções ou answer keys.

## Problemas atuais

- o quiz é salvo como `ProgramContent.JsonBody`;
- a web chama `reconcileQuizAssessment` depois, em outra operação;
- falha entre as chamadas pode deixar content e assessment divergentes;
- o host grava `AutoGraded,InstructorGraded` fixo;
- a lateral do assessment expõe flags como checkboxes e salva imediatamente;
- `gradingKind` por questão pode ser confundido com o ator de
  `GradingMethods`;
- campos derivados podem ser alterados dos dois lados.

## Ownership

### Quiz content

- enunciados, opções e resposta correta;
- pontos e configuração por questão;
- feedback autoral;
- capacidades técnicas do item;
- apresentação autoral do quiz.

### Assessment

- workflow de grading;
- disponibilidade, prazo e atraso;
- tentativas e tempo;
- grupo e peso por referência;
- rubrica e política de pares;
- estado de publicação operacional.

### Derivado

- `MaxScore` vem da soma validada dos itens;
- modalidade de quiz é `StructuredAnswer`;
- vínculo com content é imutável depois de existirem tentativas;
- revisão publicada captura content e configuração operacional.

## Caso de uso atômico

Criar um comando de API equivalente a:

```text
SaveQuizAssessmentDefinition
  -> authorize course management
  -> validate QuizContentDocument
  -> validate ContentGradingDefinition
  -> validate GradingMethods
  -> calculate MaxScore and capabilities
  -> save ProgramContent
  -> create/update linked Assessment
  -> create immutable definition revision when publishing
  -> commit once
```

A web não deve coordenar duas gravações independentes.

## UX do assessment

Criar uma seção principal `Workflow de avaliação`, próxima de scoring:

1. cards ou radio group para o avaliador primário;
2. toggle de revisão final do instrutor;
3. resumo da sequência;
4. configuração contextual do método;
5. mensagens de indisponibilidade por executor ainda não implementado;
6. salvamento com o formulário, sem request por checkbox.

Grupo e peso ficam visualmente separados do workflow. Nenhuma mudança de peso
altera o método selecionado.

Após salvar uma configuração válida, mostrar `Testar assessment`. Essa ação
abre o test run operacional definido na fase seguinte. Alterações ainda não
salvas devem ser salvas explicitamente antes do teste para que revisão,
learner-safe payload e resultado usem a mesma fonte de verdade.

## Configurações específicas

- `PeerReview`: número de revisores, anonimato e política de consolidação;
- `AIGraded`: modelo/política permitidos e fallback de falha;
- `AutoGraded`: confirmação de que todas as questões possuem avaliador
  determinístico publicado;
- `SelfGraded`: rubrica/instruções e campos que o aluno deverá preencher;
- `InstructorGraded`: rubrica e fila docente;
- revisão final: exigência de motivo para override e política de feedback.

O professor pode escolher o método independentemente do peso, mas publicação
deve ser bloqueada quando faltarem pré-requisitos técnicos do método escolhido.

## Tarefas

- [ ] mapear e remover a coordenação `save content -> reconcile assessment` da
  web;
- [ ] criar comando transacional na API;
- [ ] impor um único assessment ativo por content quando aplicável;
- [ ] remover o workflow fixo de quiz;
- [ ] usar `InstructorGraded` como default explícito de novos drafts até o
  professor escolher outro workflow;
- [ ] impedir edição de campos derivados no assessment editor;
- [ ] implementar o seletor de workflow definido no plano de domínio;
- [ ] persistir a seleção somente ao salvar o formulário;
- [ ] validar pré-requisitos por método na publicação;
- [ ] criar revisão imutável no publish, não em cada tecla ou preview;
- [ ] definir comportamento ao desligar grading quando já existem tentativas;
- [ ] adicionar a ação `Testar assessment` somente para definição salva e
  válida;
- [ ] atualizar testes de content editor, assessment editor e actions.

## Arquivos principais

```text
apps/web/src/components/learning/console/courses/[course]/content/[contentId]/content-item-editor.tsx
apps/web/src/components/learning/console/courses/[course]/content/[contentId]/quiz-content-editor.tsx
apps/web/src/components/learning/console/courses/[course]/assessments/[assessmentId]/assessment-editor.tsx
apps/web/src/lib/learning/actions.ts
apps/api/Source/Modules/GameGuild.Learning.Assessments/Controllers/AssessmentsController.cs
apps/api/Source/Modules/GameGuild.Learning.Assessments/Services/AssessmentService.cs
apps/api/Source/Modules/GameGuild.Learning.Courses/Controllers/ProgramContentController.cs
```

## Testes

- salvar content e assessment com sucesso em uma transação;
- rollback integral sob erro de validação ou persistência;
- concorrência não cria dois assessments;
- cada um dos nove workflows reabre corretamente;
- alterar grupo ou peso preserva workflow;
- publicação rejeita método sem pré-requisitos;
- edição posterior cria nova revisão sem modificar tentativas existentes.

## Critério de saída

- não existe divergência observável entre quiz e assessment;
- professor escolhe o workflow sem manipular flags;
- a API é a autoridade final da publicação;
- o assessment publicado aponta para uma revisão imutável e válida.
