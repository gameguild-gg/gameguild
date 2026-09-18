# Fluxo ponta a ponta de quiz, avaliação e nota

Status: diagnóstico arquivado.

Data da avaliação: 2026-08-19. Revisado em 2026-08-20.

> O plano executável e as decisões canônicas foram separados em
> [`quiz-grading-end-to-end/`](./quiz-grading-end-to-end/README.md) em
> 2026-08-21. Este documento preserva a auditoria detalhada do estado
> encontrado e não deve ser usado isoladamente para implementar workflows.

> Atualização posterior: o modelo canônico usa métodos de **review** para
> identificar quem ou o que analisa a submissão, enquanto grading é o efeito
> que produz score, feedback e resultado. `PeerReview` é um método primário
> válido. O fluxo existente já registra revisões individuais em
> `AssessmentPeerReview`, mas ainda não as agrega em nota oficial nem finaliza
> `AssessmentSubmission`. Os nomes, workflows e fronteiras vigentes estão no
> [`plano canônico`](./quiz-grading-end-to-end/README.md); a consolidação de
> pares está detalhada em
> [`04-graders-and-instructor-review.md`](./quiz-grading-end-to-end/04-graders-and-instructor-review.md).
> O corpo arquivado abaixo antecede essa decisão e, por isso, ainda usa nomes
> `*Graded`, trata `AutoGraded` como autoavaliação e descreve somente sete
> combinações. Essas passagens são evidência histórica, não especificação.

## Resumo executivo

O sistema já possui partes importantes do fluxo, mas elas ainda não formam um
único caminho confiável do professor até o aluno.

Hoje existem quatro bases relevantes:

- autoria de questões e experiência visual maduras em `@game-guild/quiz`,
  `@game-guild/quiz-content` e `@game-guild/quiz-surface`;
- contrato de grading e avaliador determinístico em TypeScript em
  `@game-guild/grading`;
- entidade `Assessment` para a configuração operacional da atividade;
- entidade `AssessmentSubmission` para tentativas, submissão, nota, feedback,
  fila docente e retorno da avaliação ao aluno.

O principal problema não é a ausência dessas partes. É a falta da ligação
confiável entre elas.

Há atualmente dois fluxos de aluno:

1. o fluxo antigo renderiza o quiz real e produz respostas estruturadas, mas
   salva pela interação genérica de `ProgramContent`, fora do ciclo oficial de
   `AssessmentSubmission`;
2. o fluxo atual de atividades usa `AssessmentSubmission`, mas renderiza uma
   textarea genérica e envia `{ "answer": "..." }`, em vez do documento de
   respostas estruturadas do quiz.

Além disso, `GradingMethods` é hoje principalmente uma configuração persistida.
A API ainda não executa o pipeline completo indicado pelas flags. Em especial,
`AutoGraded` representa autoavaliação pelo próprio aluno, não correção
automática pelo sistema. Os fluxos de IA, autoavaliação e revisão final pelo
instrutor ainda não estão conectados de ponta a ponta.

A conclusão é:

- o fluxo manual genérico está relativamente próximo, pois a API já recebe a
  tentativa e permite ao professor aplicar uma nota global;
- o fluxo manual específico de quiz ainda precisa unir a interface do quiz à
  submissão oficial e mostrar questões e respostas corretamente ao professor;
- o fluxo por IA está mais distante, pois exige entrega segura da prova,
  versão imutável da definição, avaliador no servidor e persistência do
  resultado por questão;
- grupo e peso controlam participação no gradebook, mas não escolhem quem
  avalia nem obrigam revisão humana;
- `InstructorGraded`, quando combinado com outro método, representa sempre a
  última etapa do pipeline e impede a publicação até a revisão do professor;
- sem `InstructorGraded`, o resultado do avaliador primário pode ser finalizado
  e publicado diretamente, conforme a política de feedback.

Antes de qualquer disponibilização para alunos, há um bloqueio de segurança:
um aluno matriculado pode receber o `ProgramContent.JsonBody` completo pela API
genérica. Para quiz com grading, esse JSON contém respostas corretas e dados de
autoria. A redação feita hoje no Next.js não protege contra uma chamada direta
à API.

## Objetivo do produto

O fluxo completo deve permitir:

1. o professor criar um quiz dentro de um curso;
2. o professor criar e organizar questões, pontos e forma de avaliação;
3. o professor configurar no `Assessment` disponibilidade, tentativas,
   apresentação e colocação no gradebook;
4. o aluno iniciar uma tentativa e receber somente uma definição segura;
5. o aluno responder usando a interface real de `quiz-surface`;
6. a API receber respostas estruturadas e imutáveis para aquela tentativa;
7. o instrutor escolher exatamente um avaliador primário: pares, IA, aluno ou
   o próprio instrutor;
8. o instrutor poder acrescentar revisão docente final aos workflows de pares,
   IA ou autoavaliação;
9. a API executar as etapas na ordem canônica, sempre deixando
   `InstructorGraded` por último quando presente;
10. o avaliador primário publicar diretamente quando não houver revisão do
    instrutor;
11. o professor poder aprovar ou alterar o resultado na etapa final, com toda
    alteração registrada em auditoria;
12. o resultado final corrente ser persistido com histórico imutável de suas
    etapas, aprovações e regrades;
13. o aluno ver estado, nota e feedback conforme a política configurada;
14. o gradebook consumir somente resultados que concluíram todo o pipeline.

## Vocabulário e fonte de verdade

### Content

`ProgramContent` é a fonte de verdade da atividade autoral no curso. Para um
quiz, seu `JsonBody` contém o `QuizContentDocument` completo, incluindo blocos,
ordem, configuração de grading e dados privados necessários à correção.

### Assessment

`Assessment` é a projeção operacional de um conteúdo avaliável. Ele não deve
ser uma segunda fonte de verdade para as questões.

Deve possuir ou projetar:

- vínculo com o conteúdo;
- valor máximo consolidado;
- modalidade `StructuredAnswer`;
- disponibilidade e prazo;
- limite de tempo e tentativas;
- modo de apresentação;
- grupo de assessment e respectivo papel no gradebook;
- workflow de avaliação escolhido pelo instrutor.

### Assessment group

O grupo decide somente a participação no resultado do curso:

- sem grupo: assessment ainda não organizado para o gradebook;
- grupo de peso zero: prática ou avaliação formativa, com resultado oficial,
  mas sem contribuição para a nota final;
- grupo com peso positivo: avaliação que contribui para o gradebook conforme o
  peso configurado.

O peso não limita `GradingMethods`. O instrutor pode selecionar qualquer
workflow válido, inclusive publicação direta após revisão entre alunos, IA ou
autoavaliação em um grupo de peso positivo. Essa é uma decisão acadêmica
explícita do instrutor, não uma regra inferida pelo gradebook.

Isso substitui a necessidade de um `resultUse` separado. `feedback` e
`gradebook` não são propriedades do quiz nem modos alternativos do motor de
grading. São interpretações da colocação do assessment na estrutura do curso.

### Pipeline de avaliação

`GradingMethods` representa atores e fases do workflow:

- `PeerReview`: alunos revisam submissões de outros alunos, e essas revisões
  produzem a avaliação primária;
- `AIGraded`: a IA produz a avaliação primária;
- `AutoGraded`: o próprio aluno produz sua autoavaliação e nota;
- `InstructorGraded`: o instrutor avalia diretamente ou, quando combinado com
  outro método, revisa e finaliza o resultado produzido anteriormente.

Combinações válidas:

| Flags | Ordem canônica | Publicação |
| --- | --- | --- |
| `PeerReview` | revisão entre alunos | após concluir a política de revisão entre alunos |
| `AIGraded` | IA | após concluir a avaliação por IA |
| `AutoGraded` | aluno | após concluir a autoavaliação |
| `InstructorGraded` | instrutor | após a avaliação do instrutor |
| `PeerReview,InstructorGraded` | revisão entre alunos -> instrutor | após revisão do instrutor |
| `AIGraded,InstructorGraded` | IA -> instrutor | após revisão do instrutor |
| `AutoGraded,InstructorGraded` | aluno -> instrutor | após revisão do instrutor |

Somente um método entre `PeerReview`, `AIGraded` e `AutoGraded` pode ser o
avaliador primário. `InstructorGraded` pode aparecer sozinho ou junto de um
deles. Assim, há no máximo duas flags e, quando há duas, a etapa do instrutor
sempre possui precedência de finalização e ocorre por último.

Como uma bitmask não preserva ordem, a precedência não depende da ordem textual
recebida pela API. Ela deve ser uma regra canônica validada no domínio.

O professor continua podendo revisar uma avaliação já finalizada. Qualquer
mudança posterior é um regrade e deve exigir motivo, preservar o valor anterior
e gerar evento de auditoria.

### Assessment submission

`AssessmentSubmission` é a tentativa oficial do aluno. Deve ser a única fonte
de verdade para:

- respostas enviadas;
- número e tempo da tentativa;
- estado da correção;
- resultado por questão;
- método primário, resultado e atores de cada estágio;
- estado e autor da revisão docente, quando configurada;
- nota total;
- resultado de aprovação ou reprovação acadêmica;
- feedback;
- auditoria de cada estágio, revisão, override ou regrade.

### Packages de quiz e grading

- `@game-guild/quiz` possui a semântica de uma questão e de sua resposta;
- `@game-guild/quiz-content` possui o documento persistido e sua projeção para
  o aluno;
- `@game-guild/quiz-surface` possui as interfaces de autoria, execução e
  visualização;
- `@game-guild/grading` possui os contratos genéricos, a adaptação de quiz, a
  normalização de respostas e a referência de avaliação determinística.

Nenhum desses packages substitui a API como limite de confiança. O browser não
produz nota oficial.

## Mapa atual do fluxo

### 1. Criação do quiz no curso

Estado atual: funcional para autoria.

Ao criar um conteúdo `Questionnaire`, a aplicação inicializa um
`QuizContentDocument` e permite a edição com `QuizCollectionEditor`. A
organização das questões, os blocos, o drag-and-drop, os editores específicos e
o player estão nos packages dedicados.

O quiz inicialmente não cria um `Assessment`. O assessment passa a ser criado
quando grading é ativado e o conteúdo é salvo.

Arquivos centrais:

```text
apps/web/src/lib/learning/actions.ts
apps/web/src/components/learning/console/courses/[course]/content/[contentId]/quiz-content-editor.tsx
packages/features/quiz-content/src/types.ts
packages/features/quiz-surface/src
```

Proximidade estimada: 80%.

Falta principalmente validação de publicação no servidor e persistência
atômica da definição com sua projeção operacional.

### 2. Definição de grading no content

Estado atual: parcialmente funcional.

O `QuizContentDocument` pode carregar `ContentGradingDefinition`. A definição
referencia as questões pelos IDs estáveis dos blocos e atribui:

- pontos;
- `gradingKind` determinístico, manual ou não suportado;
- política de score;
- tentativas, feedback e apresentação no contrato completo.

Na tela atual, porém, o professor expõe diretamente apenas uma parte pequena
desse contrato. O editor mostra essencialmente:

- grading ligado ou desligado;
- `maxScore`;
- `passingScore`.

As questões são classificadas pelo sincronizador conforme a capacidade técnica
do motor de correção. Essa classificação não é `GradingMethods`: ela descreve
como um item pode ser processado dentro de uma etapa, enquanto
`GradingMethods` escolhe quem produz e quem finaliza a avaliação inteira.

Há também uma contradição a resolver: o package ainda admite `passingScore` no
conteúdo, enquanto a API calcula aprovação usando `Program.PassingScore`. A
regra deve existir em apenas um lugar. A direção atual da API indica que o
curso é a fonte canônica; nesse caso, o campo por quiz deve ser removido da UI,
dos contratos ativos e do modelo persistente remanescente.

Proximidade estimada: 60%.

### 3. Projeção do content para Assessment

Estado atual: funcional em cenário simples, mas frágil.

Depois de salvar o `ProgramContent`, a aplicação chama
`reconcileQuizAssessment` em uma segunda operação HTTP. Quando grading está
ligado, ela cria ou atualiza um `Assessment` com:

- tipo `Quiz`;
- `ContentId` do quiz;
- modalidade `StructuredAnswer`;
- score máximo;
- tentativas e limite de tempo;
- apresentação;
- `AutoGraded,InstructorGraded` fixo, apesar de `AutoGraded` significar
  autoavaliação do aluno e não ser um default neutro para quizzes.

Quando grading é desligado, a aplicação remove logicamente o assessment.

A escolha já é persistida em `Assessments.GradingMethods`, uma coluna inteira
que representa o enum `[Flags]`. A tela atual converte as flags para checkboxes
e salva cada alteração imediatamente. Portanto, não falta estrutura para
armazenar a escolha; faltam semântica de workflow, validação e uma apresentação
adequada ao professor.

Problemas atuais:

- content e assessment podem divergir se a primeira chamada funcionar e a
  segunda falhar;
- a API não impõe um único assessment ativo para um conteúdo;
- `GradingMethods` não é validado como um pipeline de no máximo um avaliador
  primário mais revisão opcional do instrutor;
- os métodos aparecem como checkboxes técnicos na lateral e permitem
  combinações sem explicar o fluxo resultante;
- a tela de assessment permite alterar propriedades derivadas e criar
  combinações incoerentes com o quiz;
- `Assessment.DefinitionPayload` existe, mas o salvamento do quiz não o usa
  como projeção versionada da definição;
- o valor máximo é arredondado para inteiro durante a projeção, enquanto o
  contrato de grading aceita número;
- desligar grading remove a projeção mesmo que já existam tentativas, situação
  que precisa de regra explícita.

Proximidade estimada: 50%.

### 4. Aluno realiza o quiz

Estado atual: duas metades desconectadas.

#### Caminho antigo de conteúdo

`CourseContentViewer` usa `ActivityComponent`, que:

- renderiza `QuizPlayer` e `QuizPracticePlayer`;
- coleta respostas tipadas por questão;
- gera `answers` indexado pelo ID do bloco;
- usa `toStructuredGradingAnswer`;
- consegue fazer avaliação local para prática sem grading.

Porém, o envio passa por `submitActivity` e pelo fluxo genérico de interação de
`ProgramContent`. Ele não cria nem conclui a tentativa oficial de
`AssessmentSubmission`.

#### Caminho atual de activities

A rota abaixo usa o lifecycle correto de assessment:

```text
apps/web/src/app/[locale]/learn/courses/[slug]/activities/[activityId]/page.tsx
```

Ela inicia uma `AssessmentSubmission` e a envia pela API. Entretanto, para
quiz, apresenta `LearnerActivityForm`, uma textarea genérica. O payload enviado
é:

```json
{
  "answer": "texto digitado"
}
```

O contrato esperado pelo quiz é equivalente a:

```json
{
  "schemaVersion": 1,
  "answers": {
    "question-block-id": {
      "selectedOptionIds": ["option-id"]
    }
  }
}
```

Assim, nenhum dos dois caminhos entrega simultaneamente a interface correta e
o lifecycle oficial.

Proximidade estimada: 30% para o fluxo oficial completo.

### 5. Avaliação por IA no servidor

Estado atual: existe um avaliador determinístico de referência, mas a execução
oficial do workflow `AIGraded` não existe.

`@game-guild/grading` implementa `gradeDeterministicQuizSubmission`. Ele:

- normaliza respostas conforme uma whitelist;
- relaciona respostas e questões por IDs estáveis;
- avalia questões determinísticas;
- retorna resultado por item;
- retorna `pending` quando há questões manuais;
- retorna `unsupported` quando há questões sem avaliador;
- soma a nota quando todos os itens foram resolvidos.

Esse código roda em TypeScript. A API principal é C# e não chama esse package.

`AssessmentService.SubmitAsync` atualmente:

- valida a tentativa;
- valida a modalidade;
- grava o payload;
- muda o estado para `Submitted` ou `Late`;
- notifica professores sobre trabalho pendente.

Para `StructuredAnswerPayload`, a validação atual confirma somente que o texto
é JSON sintaticamente válido. Ela não confirma a versão, a forma de `answers`,
os IDs das questões nem a whitelist de campos de cada tipo.

Ele não interpreta o pipeline de `GradingMethods`, não carrega uma resposta
correta versionada, não executa o estágio `AIGraded` e não encaminha o
resultado para uma eventual revisão `InstructorGraded`.

`AutoGraded` também não possui hoje o fluxo de autoavaliação pelo aluno: não há
contrato específico para o aluno atribuir score e feedback, nem transição que
finalize ou encaminhe essa avaliação para revisão docente.

O modelo atual possui `AssessmentGroup.WeightPercent`, `GradedBy` e `GradedAt`,
mas o serviço não diferencia o ator primário, as etapas concluídas e a revisão
docente opcional antes de incluir o resultado no gradebook.

Proximidade estimada: 15%.

### 6. Correção manual pelo professor

Estado atual: infraestrutura genérica madura; experiência específica de quiz
incompleta.

A API já possui:

- fila de correção;
- seleção de submissões pendentes;
- validação de score;
- rubrica;
- persistência de nota e feedback;
- cálculo de aprovação;
- auditoria de quem corrigiu;
- fan-out de nota para submissões em grupo;
- notificação do aluno;
- passback LTI quando configurado.

O SpeedGrader também possui um `QuizViewer`, mas ele lê somente o
`StructuredAnswerPayload`. Consequentemente, mostra IDs dos blocos e respostas
serializadas sem carregar o texto e as opções da questão. O professor pode
aplicar uma nota total, mas não corrige adequadamente questão por questão.

Para avaliação docente integral ou revisão docente após outro método, falta:

- carregar a mesma revisão imutável usada na tentativa;
- renderizar a questão e a resposta do aluno em contexto;
- permitir score e feedback por questão;
- preservar o resultado do avaliador primário durante a revisão;
- calcular a nota total no servidor a partir dos resultados por item;
- impedir uma nota agregada incompatível com os itens.

Proximidade estimada: 70% para correção manual genérica e 40% para correção
manual de quiz com boa experiência.

### 7. Aluno vê nota e feedback

Estado atual: a API e a UI genérica conseguem mostrar um resultado final, mas
o fluxo do quiz não chega até ele de forma consistente.

`LearnerActivityForm` já diferencia uma submissão final e mostra:

- estado;
- score, quando presente;
- indicador de espera por correção;
- feedback do professor.

Isso funciona se a atividade tiver usado `AssessmentSubmission` e tiver sido
corrigida. Como o player real do quiz ainda não usa esse lifecycle, o caminho
completo não está fechado.

Também faltam:

- resultado por questão;
- indicação clara do estágio atual do workflow;
- aplicação real da política de liberação de feedback;
- controle de quando respostas corretas podem ser reveladas;
- histórico ou escolha canônica entre múltiplas tentativas;
- cálculo consolidado correto no gradebook.

Proximidade estimada: 55% depois que a submissão oficial existir; 30% no fluxo
real atual do quiz.

## Avaliação consolidada de proximidade

| Capacidade | Estado | Proximidade estimada |
| --- | --- | ---: |
| Autoria das questões | forte | 80% |
| Contrato de grading no content | parcial | 60% |
| Projeção para Assessment | parcial e não atômica | 50% |
| Player real ligado à tentativa oficial | ausente | 30% |
| Lifecycle genérico de submissão | forte | 80% |
| Correção manual genérica | forte | 70% |
| Correção manual específica de quiz | parcial | 40% |
| Avaliação por IA confiável no servidor | ausente | 15% |
| Autoavaliação do aluno | ausente | 10% |
| Revisão entre alunos | parcial | 35% |
| Resultado do quiz para o aluno | parcial | 30% |
| Gradebook ponderado | parcial e ambíguo | 45% |
| Segurança da definição para o aluno | bloqueio crítico | 20% |

Esses percentuais medem completude funcional, não esforço linear. A autoria
está avançada, mas a parte que falta inclui os limites de confiança e, por
isso, tem maior peso arquitetural.

## Bloqueios e riscos prioritários

### P0. O aluno pode receber o JSON de autoria

`ProgramContentController.ResolveContentAccessAsync` concede
`CanViewFullContent` a aluno matriculado. O DTO genérico inclui `JsonBody`, que
é preenchido integralmente por `ProgramContentMappingExtensions.ToDto`.

A sanitização do outline público limpa `Body`, mas não limpa `JsonBody`.

Para quiz com grading, isso pode revelar:

- alternativas corretas;
- respostas aceitas;
- dados de matching e ordering;
- configurações privadas de grading;
- qualquer resposta correta mantida no bloco autoral.

A função `prepareQuizContentForRuntime` chamada no Next.js não resolve esse
problema, pois o aluno pode chamar a API diretamente.

Decisão necessária:

- endpoints genéricos para aluno nunca devolvem `JsonBody` de autoria de um
  conteúdo avaliável;
- quiz deve possuir endpoint de definição pública controlado pela API, como já
  existe para coding assignment;
- full authoring e learner projection devem ser DTOs e autorizações distintos;
- testes de segurança devem buscar nomes, IDs e valores de respostas corretas
  no JSON retornado ao aluno e provar sua ausência.

### P0. Não há uma submissão canônica de quiz

O sistema não pode manter um player em um fluxo e a tentativa oficial em
outro. Todo quiz com grading deve passar por:

```text
start attempt -> learner-safe definition -> QuizPlayer -> structured answers
-> submit AssessmentSubmission -> grading -> result
```

O fluxo de `submitActivity` deve continuar apenas para atividades sem
assessment ou ser retirado do caminho de quiz.

### P0. A definição da tentativa não é imutável

Se o professor editar uma questão depois que o aluno iniciar ou enviar a
tentativa, a correção não pode usar silenciosamente a nova resposta correta.

Cada tentativa precisa referenciar uma revisão imutável da definição. Essa
revisão deve conter a definição completa para o servidor e permitir a projeção
segura para aluno e professor.

### P1. `GradingMethods` ainda não governa o workflow

A escolha do workflow pertence ao instrutor e já pode ser persistida em
`Assessment.GradingMethods`. O backend ainda precisa rejeitar combinações
inválidas e executar um caso de uso real para cada combinação aceita.

`AutoGraded` isolado significa que o aluno se autoavalia e o resultado é
finalizado sem revisão docente. `AutoGraded,InstructorGraded` significa que o
aluno se autoavalia e o professor revisa, altera se necessário e finaliza.
Essa semântica independe do grupo e de seu peso.

### P1. Revisão docente e override não são regras do domínio atual

`AssessmentSubmission.Grade` transforma `Submitted/Late` diretamente em
`Graded`. Ele não possui uma etapa de resultado sugerido aguardando aprovação e
não permite regrade de uma submissão já finalizada.

`GradedBy` identifica quem aplicou a nota final, mas sozinho não registra:

- o score produzido pelo avaliador primário;
- aceitação sem alterações;
- diferença entre resultado primário e resultado final;
- motivo do override;
- histórico de regrades.

Esses comportamentos precisam ser impostos na API. Esconder ou mostrar um
botão na UI não oferece a garantia acadêmica necessária.

### P1. Resultado por questão não é persistido oficialmente

O `GradeResult` do package possui itens, mas `AssessmentSubmission` guarda
apenas score total, aprovação, feedback e rubrica. Sem resultado por item não é
possível fechar corretamente um workflow em duas etapas ou fazer auditoria de
uma nota produzida por pares, IA ou pelo aluno.

### P1. O limite de tempo não é aplicado

`StartSubmissionAsync` grava `StartedAt`, mas `SubmitAsync` apenas verifica a
janela geral do assessment. Não compara o envio com
`StartedAt + TimeLimitMinutes`.

### P1. Gradebook ainda não consolida pesos

Os grupos possuem `WeightPercent`, porém o resumo do aluno em
`GetLearnerDashboardQuery.MapGrade` soma pontos obtidos e possíveis de todos os
assessments. O peso dos grupos é retornado ao cliente, mas não participa desse
cálculo.

É necessário estabelecer uma regra canônica para:

- grupo de peso zero;
- assessments sem grupo;
- grupos de peso positivo;
- múltiplas tentativas;
- submissões que ainda não concluíram todas as etapas do workflow;
- arredondamento;
- nota final do curso.

### P1. Há propriedades com dupla autoridade

Precisam ser consolidadas:

- `passingScore` do quiz, `Assessment.PassingScore` remanescente e
  `Program.PassingScore`;
- pontos por item e `Assessment.MaxScore`;
- workflow escolhido em `GradingMethods` versus pré-requisitos operacionais de
  cada avaliador;
- título e descrição do content versus assessment;
- políticas de tentativas no documento e no assessment.

## Arquitetura-alvo

### 1. Autoria e publicação

O professor edita um `QuizContentDocument` completo. Ao salvar ou publicar, um
caso de uso da API deve:

1. validar o schema do quiz;
2. validar IDs estáveis e referências;
3. validar as questões de grading;
4. calcular o inventário de capacidades;
5. calcular os pontos totais;
6. rejeitar questões avaliáveis sem caminho de correção;
7. salvar `ProgramContent.JsonBody`;
8. criar ou atualizar a projeção `Assessment`;
9. gerar uma revisão da definição quando o conteúdo avaliável mudar;
10. executar tudo na mesma transação.

Não deve haver uma chamada do browser para salvar content e outra para
reconciliar assessment.

### 2. Revisão imutável da definição

Recomendação: introduzir uma entidade genérica de revisão, em vez de copiar o
quiz inteiro para cada submissão.

Direção de modelo:

```text
AssessmentDefinitionRevision
  Id
  AssessmentId
  SchemaVersion
  ContentRevisionHash
  DefinitionPayload        // servidor, inclui dados privados
  CreatedAt

AssessmentSubmission
  DefinitionRevisionId
```

O `DefinitionPayload` atual de `Assessment` pode representar somente a revisão
corrente ou ser substituído pela referência à revisão corrente. Como o produto
não foi lançado, deve-se estabelecer diretamente o modelo final, sem dual-read,
adapters legacy ou migração de formatos antigos.

Benefícios:

- tentativas antigas continuam corrigíveis;
- uma revisão é compartilhada por várias tentativas;
- a correção manual vê a mesma questão respondida pelo aluno;
- regrade pode reproduzir a regra original;
- a projeção learner-safe é sempre derivada no servidor;
- o hash evita revisões duplicadas sem mudança semântica.

### 3. Bundle de tentativa para o aluno

O início da tentativa deve devolver um contrato específico:

```ts
interface QuizAttemptBundle {
  assessment: LearnerAssessmentPolicy;
  submission: LearnerAttempt;
  definitionRevisionId: string;
  quiz: QuizLearnerContentDocument;
}
```

O `quiz` já deve estar redigido. O browser nunca recebe a answer key nem o
`QuizContentDocument` autoral.

O endpoint deve ser idempotente para uma tentativa `InProgress`: abrir a página
novamente retorna a mesma tentativa e a mesma revisão.

### 4. Submissão estruturada canônica

O player deve enviar somente respostas:

```ts
interface QuizStructuredSubmissionV1 {
  schemaVersion: 1;
  answers: Record<string, StructuredAnswer>;
}
```

A API deve validar:

- schema e versão;
- tamanho máximo;
- IDs de questões pertencentes à revisão;
- tipos e campos permitidos por questão;
- ausência de score, correção ou answer key fornecidos pelo cliente;
- uma única submissão final por tentativa;
- limite de tempo e janela de entrega.

O `StructuredAnswerPayload` não deve conter `gradeResult`. Resultado e resposta
são evidências diferentes e possuem autores diferentes.

### 5. Escolha e validação do workflow

A seleção pertence ao instrutor em `Assessment.GradingMethods` e independe do
peso do grupo. O domínio deve aceitar somente:

```text
PeerReview
AIGraded
AutoGraded
InstructorGraded
PeerReview,InstructorGraded
AIGraded,InstructorGraded
AutoGraded,InstructorGraded
```

As três primeiras flags são mutuamente exclusivas como avaliador primário.
`InstructorGraded` pode ser o avaliador único ou a etapa final de revisão. As
combinações `PeerReview,AIGraded`, `PeerReview,AutoGraded`,
`AIGraded,AutoGraded` e qualquer conjunto com três ou quatro flags devem ser
rejeitadas pela API.

O conteúdo ainda pode declarar capacidades e pré-requisitos técnicos, mas eles
não escolhem o workflow:

- `PeerReview` exige política de quantidade, distribuição e consolidação das
  avaliações dos pares;
- `AIGraded` exige avaliador disponível para todos os itens ou uma política
  explícita de falha;
- `AutoGraded` exige uma superfície de autoavaliação para o aluno, com score e
  feedback validados pelo servidor;
- `InstructorGraded` exige entrada na fila docente, seja para correção integral
  ou revisão final.

O inventário determinístico do adapter continua útil dentro do estágio de IA
ou como referência de conformidade, mas não deve ser confundido com a flag
`AutoGraded`.

### 6. Avaliador confiável no servidor

A API é C# e o package de referência é TypeScript. Não se deve executar a
correção no browser, confiar em uma server action Next.js ou duplicar regras de
forma informal.

Direção recomendada:

1. implementar um domínio de correção de quiz no módulo C# de assessments;
2. consumir o mesmo contrato JSON versionado definido pelos packages;
3. manter vetores de teste JSON independentes de linguagem;
4. executar esses vetores tanto contra `@game-guild/grading` quanto contra o
   avaliador C# em CI;
5. impedir publicação quando as implementações divergirem para um caso
   suportado.

Um serviço Node privado para grading é possível, mas adiciona implantação,
rede, retry e observabilidade desnecessários para o estágio atual. Ele só deve
ser reconsiderado se vários motores TypeScript passarem a justificar esse
limite operacional.

### 7. Resultado oficial por questão

Adicionar um contrato versionado separado da resposta:

```ts
interface QuizGradeResultV1 {
  schemaVersion: 1;
  status: "pending" | "graded" | "unsupported";
  score: number | null;
  maxScore: number;
  items: Array<{
    contentBlockId: string;
    gradingKind: "deterministic" | "manual";
    status: "pending" | "graded";
    score: number | null;
    maxScore: number;
    isCorrect?: boolean;
    feedback?: string;
    gradedBy?: string;
    gradedAt?: string;
  }>;
}
```

O resultado deve ser envolvido pelo estado operacional do pipeline, sem
misturar essa política no adapter de quiz:

```ts
interface AssessmentEvaluationV1 {
  schemaVersion: 1;
  configuredMethods: Array<
    "PeerReview" | "AIGraded" | "AutoGraded" | "InstructorGraded"
  >;
  stages: Array<{
    method: "PeerReview" | "AIGraded" | "AutoGraded" | "InstructorGraded";
    status: "pending" | "in-progress" | "completed";
    result?: QuizGradeResultV1;
    actorIds?: string[];
    completedAt?: string;
  }>;
  finalization: {
    status: "pending" | "finalized";
    method?: "PeerReview" | "AIGraded" | "AutoGraded" | "InstructorGraded";
    finalizedBy?: string;
    finalizedAt?: string;
  };
}
```

Cada estágio registra quem avaliou, qual resultado produziu e quando terminou.
`finalization` responde se todo o pipeline terminou e qual método publicou o
resultado final. A exigência de professor deriva exclusivamente da presença de
`InstructorGraded` após um avaliador primário, nunca do peso.

Recomendação inicial: persistir esse contrato em um campo JSONB versionado da
submissão. Isso mantém o resultado por item coeso e evita criar uma tabela por
resposta antes de existir necessidade real de consultas analíticas nesse
nível.

O score agregado deve ser validado e consolidado pelo servidor a partir dos
itens. Aluno, pares e professor podem enviar scores somente pelos endpoints de
seu estágio e dentro dos limites da definição; nenhum deles deve conseguir
injetar uma nota por meio do payload de respostas do quiz.

Se o professor alterar o resultado primário, o resultado final deve ser
montado a partir dos itens sobrescritos. O evento de auditoria deve registrar o
resultado anterior, o resultado final, os itens alterados, o professor e o
motivo.

### 8. Estados de correção

O fluxo de domínio precisa distinguir:

```text
InProgress
  -> Submitted ou Late
  -> AwaitingPeerReview, AwaitingAIEvaluation, AwaitingSelfAssessment
     ou AwaitingInstructorGrading, conforme o avaliador primário
  -> EvaluatedAwaitingInstructor, quando há `InstructorGraded` após o primário
  -> Graded, quando a última etapa configurada terminou
```

Esses estados podem começar como projeções de `Submitted/Late` mais o estágio
corrente em `EvaluationPayload`, mas devem existir no contrato da UI. Caso a
distinção seja importante para consultas, filas e notificações, deve virar
estado persistido com novos valores, sem renumerar os existentes.

O peso não altera as transições. A presença de `InstructorGraded` após o método
primário é o único fator que cria `EvaluatedAwaitingInstructor`.

Para execução assíncrona futura, usar outbox/job idempotente e manter uma chave
de execução por submissão e revisão. Não criar isso antes de haver necessidade
de processamento assíncrono.

### 9. Avaliação docente e workflows em duas etapas

O SpeedGrader deve receber:

- revisão autoral da questão, autorizada para professor;
- resposta estruturada do aluno;
- resultado do avaliador primário, quando houver;
- itens que ainda exigem decisão docente;
- score e feedback previamente salvos.

A UI deve reutilizar uma superfície read-only de `quiz-surface`, com extensão
para score e feedback por item. Ela não deve reconstruir questões a partir de
IDs exibidos como texto.

Quando `InstructorGraded` estiver sozinho, o professor produz a avaliação
integral. Quando estiver combinado, a tela deve carregar o resultado de pares,
IA ou autoavaliação e oferecer `Aprovar` e `Editar avaliação`. Aprovar sem
alterações também é um evento auditável. Editar exige motivo e registra o
delta.

Sem `InstructorGraded`, a conclusão do estágio primário finaliza a submissão.
Com `InstructorGraded`, ela entra na fila docente e somente a revisão final a
move para `Graded`.

### 10. Visualização do resultado pelo aluno

A rota da tentativa deve possuir uma visão de resultado que use o mesmo player
em modo read-only e obedeça à política de feedback.

Ela deve mostrar, conforme permitido:

- estado da tentativa;
- nota obtida e máxima;
- aprovado ou não, usando a regra canônica do curso;
- feedback geral;
- feedback por questão;
- resposta enviada;
- correção e resposta correta somente quando a política permitir;
- tentativas restantes e histórico permitido.

Uma prática de grupo peso zero ainda pode mostrar score e feedback. Ela apenas
não contribui para a nota ponderada do curso.

Quando `InstructorGraded` estiver pendente, o aluno deve ver `Aguardando
revisão do instrutor` e não uma nota tratada como final. A política de feedback pode
permitir mostrar que a submissão foi recebida, mas não pode fazer um resultado
provisório parecer nota aplicada.

## Divisão de responsabilidades entre Content e Assessment

| Propriedade | Fonte de verdade | Editável onde |
| --- | --- | --- |
| enunciados e opções | quiz content | editor do quiz |
| respostas corretas | quiz content privado | editor do quiz |
| pontos por questão | grading do content | editor do quiz |
| capacidades técnicas por item | derivadas das questões | exibidas, não livres |
| score máximo | soma/projeção dos itens | exibido no assessment |
| vínculo com content | projeção | bloqueado no assessment de quiz |
| modalidade estruturada | projeção | bloqueada para quiz |
| disponibilidade e prazo | assessment | editor de assessment |
| tentativas e tempo | assessment | editor de assessment |
| apresentação | assessment | editor de assessment |
| grupo e peso | assessment group | assessment/gradebook |
| avaliador primário e revisão docente | `Assessment.GradingMethods` | editor de assessment |
| passing score | curso | configurações do curso |
| resposta do aluno | submission | somente leitura após envio |
| resultado de cada estágio | submission/servidor | ator autorizado do estágio |
| override por questão | submission/servidor | professor, com motivo |
| finalização do pipeline | submission e auditoria | último método configurado |
| nota final da tentativa | submission/servidor | resultado do último estágio |

Essa matriz evita que o mesmo campo seja editável em content e assessment.

## Impacto no backend e na persistência

A estrutura de `GradingMethods` já consegue representar a escolha de workflow.
`AssessmentGroup.WeightPercent` permanece ortogonal e participa somente do
cálculo do gradebook.

### Estrutura já existente

`AssessmentGradingMethod` é um enum `[Flags]` com valores estáveis:

```text
None               0
PeerReview         1
AIGraded           2
AutoGraded         4
InstructorGraded   8
```

`Assessments.GradingMethods` já é uma coluna `integer`, com default
`InstructorGraded` e constraint que aceita somente os bits conhecidos. Não há
uma tabela de grading methods; a combinação é persistida como bitmask no
próprio assessment.

A constraint atual impede bits desconhecidos, mas ainda aceita combinações
semanticamente inválidas. Os valores válidos para assessments avaliados são:

```text
1   PeerReview
2   AIGraded
4   AutoGraded
8   InstructorGraded
9   PeerReview,InstructorGraded
10  AIGraded,InstructorGraded
12  AutoGraded,InstructorGraded
```

O valor `0` (`None`) pode ser aceito durante rascunho se esse estado for útil,
mas deve ser rejeitado na publicação. A validação deve existir no domínio e
pode ser reforçada substituindo a constraint atual por uma lista explícita de
valores, sem criar coluna ou tabela.

Na API web, as flags trafegam pelos nomes separados por vírgula, por exemplo:

```text
AutoGraded,InstructorGraded
```

Create, update, DTOs e editor já leem e gravam essa escolha. O que não existe é
a execução das flags por `SubmitAsync`, fila, aprovação e gradebook.

### Semântica a formalizar

| Método primário | Sem instrutor | Com `InstructorGraded` |
| --- | --- | --- |
| `PeerReview` | alunos revisam submissões de outros alunos e o resultado é publicado | alunos revisam submissões de outros alunos; instrutor revisa e publica |
| `AIGraded` | IA avalia e publica | IA avalia; instrutor revisa e publica |
| `AutoGraded` | aluno se autoavalia e publica | aluno se autoavalia; instrutor revisa e publica |
| `InstructorGraded` | instrutor avalia e publica | não se aplica |

O backend deve validar a cardinalidade e executar a ordem canônica. O fato de
as flags chegarem como `InstructorGraded,AIGraded` ou
`AIGraded,InstructorGraded` não muda a sequência: o instrutor sempre vem por
último.

Com essa definição, não é necessário criar `ApprovalPolicy`. A revisão já é
representada pela presença de `InstructorGraded` depois do método primário.

`AutoGraded` é um nome internamente ambíguo em inglês. Enquanto o enum for
mantido, comentários de domínio, contratos e UI devem chamá-lo explicitamente
de `Autoavaliação do aluno` e nunca de correção automática. Como o produto não
foi lançado, renomear o membro para `SelfGraded` preservando o valor persistido
`4` também é uma alternativa mais clara antes da implementação do workflow.

### Evolução mínima recomendada

Em `Assessment`, nenhuma nova coluna é necessária para escolher avaliador ou
revisão.

Em `AssessmentSubmission`, continua necessário persistir a execução do
pipeline:

```text
EvaluationPayload   jsonb, versionado
```

Responsabilidades:

- `EvaluationPayload` guarda métodos configurados, estágio corrente, resultados
  por item, atores, overrides e versão de cada avaliador;
- `GradedBy/GradedAt` podem registrar o ator humano final quando houver um único
  finalizador, mas não substituem o histórico de estágios;
- `AIGraded` pode finalizar sem `GradedBy` humano quando não houver revisão;
- `AutoGraded` deve identificar o aluno como autor da autoavaliação;
- `PeerReview` precisa preservar os alunos revisores participantes mesmo que o
  resultado consolidado não possua um único `GradedBy`;
- o evento de auditoria diferencia aprovação sem mudança, override e regrade;
- o ID do professor sempre vem do ator autenticado. O controller atual já
  substitui `GradedBy` recebido pelo ID desse ator.

`EvaluatedAwaitingInstructor` pode inicialmente ser um novo valor no enum
persistido `SubmissionStatus`, sem adicionar outra coluna de status. Deve-se
usar valor numérico novo, sem renumerar estados existentes.

### Auditoria

A tabela geral `AuditLogs` já existente pode receber eventos de:

```text
EvaluationStageCompleted
InstructorReviewApproved
EvaluationOverridden
EvaluationRegraded
```

Cada evento deve identificar `AssessmentSubmission`, revisão da definição,
versão do avaliador, ator, score anterior, score novo, itens alterados e motivo.
Uma tabela acadêmica de auditoria separada só se justifica se retenção,
consultas ou permissões futuras não puderem ser atendidas por `AuditLogs`.

`AuditLogs` registra história; não deve ser usado como fonte do estado corrente
da avaliação.

### O que exige mudança de regra no backend

- `SubmitAsync` precisa iniciar o estágio primário correto;
- filas distintas precisam encaminhar trabalho a pares, IA, aluno ou instrutor;
- `GradeSubmissionAsync` precisa autorizar o ator do estágio e distinguir
  avaliação, revisão, override e regrade;
- uma submissão `Graded` precisa aceitar regrade autorizado sem apagar seu
  histórico;
- o gradebook precisa ignorar resultados cujo pipeline ainda não terminou;
- endpoints de aluno precisam distinguir resultado provisório de nota final;
- toda alteração deve produzir evento de auditoria com antes, depois e motivo.

Portanto, isso não é uma alteração apenas de package ou interface. A ordem e a
autorização de cada estágio devem existir no servidor para não poderem ser
contornadas.

### Zelo na migration

A migration necessária para o resultado por estágios deve ser única e coesa,
com:

- `EvaluationPayload` como `jsonb` nullable enquanto não houver avaliação;
- índice apenas se uma consulta real justificar;
- nenhuma tabela por questão;
- nenhum dual-read, campo legacy ou fallback permanente, pois o produto ainda
  não foi lançado;
- testes do modelo EF e do banco para os estados permitidos.

Se a garantia também for aplicada no banco, a mesma evolução deve substituir
`CK_Assessments_GradingMethods` por uma constraint que aceite somente
`0, 1, 2, 4, 8, 9, 10, 12`. Isso altera apenas uma regra da coluna existente.
Não cria estrutura de persistência adicional.

Não deve ser criada coluna para um workflow já representado por
`GradingMethods`. A alteração de schema fica restrita ao estado que realmente
não existe hoje: o resultado detalhado e os estágios da avaliação.

## UX da escolha de grading

Os checkboxes crus na lateral expõem o formato de armazenamento, não uma
decisão compreensível do professor. A combinação de flags deve continuar no
contrato e no banco, mas a interface deve apresentar workflows válidos.

### Localização

Criar uma seção principal `Workflow de avaliação` logo após `Scoring`, antes de
rubrica e disponibilidade. A lateral pode mostrar somente um resumo do workflow
escolhido.

Essa decisão não deve ficar misturada a linked content, apresentação, grupo e
outros campos operacionais menores.

### Controle

Usar dois controles sequenciais, em vez de expor combinações de flags.

Primeiro, uma seleção exclusiva de avaliador primário:

```text
Revisão entre alunos     PeerReview
Avaliação por IA          AIGraded
Autoavaliação do aluno    AutoGraded
Avaliação pelo instrutor  InstructorGraded
```

Quando o primário não for o instrutor, mostrar em seguida:

```text
[ ] Exigir revisão final do instrutor
    O professor aprova ou ajusta o resultado antes da publicação.
```

Ativar essa opção acrescenta `InstructorGraded`. Se o avaliador primário já
for o instrutor, o controle de revisão não aparece. A UI pode mostrar abaixo um
resumo da sequência, por exemplo `IA -> revisão do instrutor -> publicação`.

### Capacidade e validação

- grupo e peso não habilitam nem bloqueiam métodos;
- exatamente um avaliador primário é obrigatório para um assessment avaliado;
- somente `InstructorGraded` pode ser acrescentado ao primário;
- a API normaliza a ordem e rejeita todas as outras combinações;
- cada método deve indicar seus pré-requisitos ainda não configurados;
- `PeerReview` mantém quantidade e política de alunos revisores em sua seção
  específica;
- `AIGraded` deve ficar indisponível enquanto seu executor não estiver
  operacional;
- `AutoGraded` exige configurar a experiência de autoavaliação do aluno;
- o backend repete todas as validações, independentemente da UI;
- a escolha deve ser salva com o restante do assessment, evitando chamadas
  imediatas por checkbox que possam deixar configuração parcial.

## APIs-alvo

Os nomes finais podem seguir a convenção do módulo, mas o conjunto de casos de
uso deve ser equivalente a:

```text
PUT  /courses/{courseId}/content/{contentId}/quiz
GET  /courses/{courseId}/content/{contentId}/quiz/full
POST /assessments/{assessmentId}/quiz-attempts/start
GET  /assessment-submissions/{submissionId}/quiz-attempt
POST /assessment-submissions/{submissionId}/quiz-submit
GET  /assessment-submissions/{submissionId}/quiz-review
POST /assessment-submissions/{submissionId}/self-evaluation
POST /assessment-submissions/{submissionId}/peer-evaluations
POST /assessment-submissions/{submissionId}/ai-evaluation
POST /assessment-submissions/{submissionId}/instructor-evaluation
POST /assessment-submissions/{submissionId}/instructor-review
POST /assessment-submissions/{submissionId}/override
POST /assessment-submissions/{submissionId}/regrade
GET  /assessment-submissions/{submissionId}/quiz-result
```

Regras:

- `full` exige permissão de gestão;
- `quiz-attempt` retorna somente a projeção learner-safe;
- `quiz-review` exige correção ou gestão e retorna definição e respostas em
  contexto;
- cada endpoint de estágio valida o método configurado e o papel do ator;
- `instructor-review` aceita o resultado primário sem alteração e registra o
  professor;
- `override` altera itens antes da primeira aprovação e exige motivo;
- `regrade` altera resultado já finalizado, exige motivo e preserva histórico;
- `quiz-result` retorna apenas campos liberados ao aluno;
- o endpoint genérico de content não substitui nenhum desses contratos;
- salvar quiz e reconciliar assessment ocorre no mesmo caso de uso da API.

## Plano de execução

### Fase 0. Consolidar contratos e remover ambiguidades

Objetivo: impedir que a implementação nova perpetue autoridades duplicadas.

Tarefas:

- confirmar `Program.PassingScore` como regra canônica de aprovação;
- remover `passingScore` por quiz e o estado remanescente por assessment, se a
  decisão for mantida;
- definir score inteiro ou decimal de ponta a ponta, sem arredondamento oculto;
- fixar `QuizStructuredSubmissionV1` e `QuizGradeResultV1`;
- definir a classificação das questões suportadas;
- definir que assessment group controla a participação no gradebook;
- formalizar `PeerReview`, `AIGraded`, `AutoGraded` e `InstructorGraded` como
  atores e etapas do pipeline;
- validar um único avaliador primário e `InstructorGraded` opcional por último;
- retirar qualquer limitação de workflow baseada em grupo ou peso;
- definir eventos de auditoria para conclusão de estágio, revisão, override e
  regrade;
- registrar a matriz de ownership deste documento em testes de arquitetura;
- atualizar os mapas em `docs/types` depois da estabilização.

Critério de saída:

- cada propriedade possui uma única fonte de verdade;
- fixtures JSON comuns validam quiz, grading, resposta e resultado;
- não há `resultUse`, aliases ou caminhos legacy.

### Fase 1. Fechar o limite de segurança e versionamento

Objetivo: permitir que um aluno receba um quiz sem receber respostas corretas.

Tarefas:

- retirar `JsonBody` autoral dos DTOs genéricos acessíveis ao aluno e do
  outline público;
- criar endpoints `full` e learner-safe de quiz;
- mover parsing, validação e redação para o limite da API;
- criar a revisão imutável da definição;
- vincular cada tentativa à revisão usada no início;
- criar testes de autorização e de ausência de answer key;
- impedir alteração ou remoção destrutiva de assessment com tentativas sem uma
  política explícita.

Critério de saída:

- nenhum endpoint de aluno retorna uma resposta correta de quiz com grading;
- uma tentativa continua exibível e corrigível após edição posterior do quiz.

### Fase 2. Tornar a projeção Content -> Assessment atômica

Objetivo: eliminar divergência entre autoria e operação.

Tarefas:

- criar caso de uso da API que salva quiz, grading, revisão e assessment na
  mesma transação;
- remover `reconcileQuizAssessment` do browser;
- impor no banco um único assessment ativo por `ContentId` quando aplicável;
- validar cardinalidade, combinações e pré-requisitos de `GradingMethods`;
- substituir os checkboxes laterais por workflows exclusivos na área principal
  do editor;
- salvar workflow, grupo e demais alterações de assessment de forma coerente,
  sem persistência imediata isolada por checkbox;
- projetar `MaxScore`, modalidade e apresentação sem permitir edição
  contraditória;
- manter no assessment editor somente configurações operacionais;
- validar publicação e rejeitar itens sem caminho de correção.

Critério de saída:

- não existe estado em que quiz foi salvo e a projeção falhou;
- reabrir content e assessment mostra valores coerentes;
- o professor escolhe um workflow compreensível sem manipular flags cruas;
- concorrência não cria assessments duplicados.

### Fase 3. Unificar a experiência do aluno

Objetivo: usar a interface real do quiz sobre `AssessmentSubmission`.

Tarefas:

- criar `QuizAssessmentActivity` na rota oficial de activities;
- iniciar ou reutilizar a tentativa antes de carregar o bundle;
- renderizar `QuizPlayer` com `QuizLearnerContentDocument`;
- coletar `StructuredAnswer` por ID de bloco;
- enviar `QuizStructuredSubmissionV1` pelo endpoint oficial;
- retirar quiz com grading de `submitActivity`;
- manter `QuizPracticePlayer` apenas para prática local sem grading;
- aplicar limite de tempo no servidor e refletir o tempo restante na UI;
- tratar recarga, dupla submissão, erro de rede e tentativa expirada.

Critério de saída:

- uma resposta feita no player aparece em `AssessmentSubmission` no schema
  canônico;
- não há textarea genérica para assessment de tipo quiz;
- cada clique de envio produz no máximo uma submissão final.

### Fase 4. Completar primeiro o fluxo manual

Objetivo: entregar um MVP confiável com `InstructorGraded` como método único.

Tarefas:

- permitir que todos os itens sejam explicitamente manuais;
- enviar submissões manuais para a fila existente;
- carregar definição, resposta e revisão no SpeedGrader;
- criar review read-only de quiz com score e feedback por item;
- consolidar a nota no servidor;
- concluir a nota quando o instrutor terminar o estágio configurado;
- registrar aprovação, alterações e motivo no audit log;
- notificar o aluno;
- exibir nota, estado e feedback na rota da tentativa;
- respeitar a política de liberação de feedback.

Critério de saída:

- professor cria, publica, recebe, corrige e devolve um quiz totalmente manual;
- aluno vê o resultado oficial correto;
- grade e feedback não dependem do browser do professor para cálculo agregado.

### Fase 5. Implementar `AIGraded`

Objetivo: executar a avaliação por IA como método primário oficial.

Tarefas:

- implementar o avaliador confiável no servidor, usando os avaliadores
  determinísticos quando aplicável e IA somente conforme o contrato definido;
- adicionar vetores de conformidade compartilhados com
  `@game-guild/grading`;
- validar definição e resposta pela revisão da tentativa;
- persistir resultado, versão do avaliador e evidências em
  `EvaluationPayload`;
- com `AIGraded`, finalizar e publicar ao concluir o estágio;
- com `AIGraded,InstructorGraded`, encaminhar o resultado para revisão docente;
- permitir aprovação sem alteração e override por item com motivo;
- tornar submit e avaliação transacionais ou idempotentes;
- permitir regrade administrativo versionado sem apagar o resultado anterior;
- garantir que o cliente não consiga injetar o resultado da IA.

Critério de saída:

- os mesmos vetores suportados produzem resultados compatíveis em TypeScript e
  C#;
- a presença de `InstructorGraded`, e somente ela, decide se há revisão final;
- grupo e peso não alteram o pipeline;
- retries não duplicam nota, notificação ou tentativa.

### Fase 6. Implementar `AutoGraded` e completar `PeerReview`

Objetivo: fechar os demais avaliadores primários e suas combinações com revisão
docente.

Tarefas:

- criar a superfície de autoavaliação para o aluno atribuir score e feedback;
- validar no servidor identidade, limites e estrutura da autoavaliação;
- completar distribuição, quantidade e consolidação de avaliações por pares;
- registrar todos os atores sem expor identidades quando a política de pares
  exigir anonimato;
- finalizar diretamente `AutoGraded` e `PeerReview` isolados;
- encaminhar combinações com `InstructorGraded` para a fila docente;
- permitir ao professor aprovar ou alterar o resultado primário;
- definir override e regrade com auditoria.

Critério de saída:

- aluno, pares e instrutor somente atuam no estágio para o qual possuem
  autorização;
- todas as sete combinações válidas chegam a uma única nota final reproduzível;
- combinações inválidas são rejeitadas pela API.

### Fase 7. Fechar gradebook e experiência de resultado

Objetivo: transformar a nota da tentativa em informação acadêmica coerente.

Tarefas:

- implementar cálculo ponderado por assessment group;
- excluir grupos de peso zero da nota final sem ocultar seus resultados;
- excluir do gradebook toda avaliação cujo pipeline ainda não terminou;
- não alterar ou reenfileirar o workflow quando o peso do grupo mudar;
- definir política de tentativa usada no gradebook: última, melhor ou outra
  opção explicitamente configurada;
- tratar assessments sem grupo;
- aplicar `Program.PassingScore` de forma consistente;
- expor breakdown de grupo e assessment ao aluno;
- mostrar resultado por questão conforme feedback policy;
- remover cálculos paralelos em `ContentInteraction` para quizzes avaliáveis.

Critério de saída:

- a soma ponderada no backend corresponde ao breakdown mostrado ao aluno e ao
  professor;
- o mesmo resultado é usado em dashboard, progresso, analytics e integrações.

### Fase 8. Endurecimento e observabilidade

Objetivo: tornar o fluxo seguro para publicação.

Tarefas:

- métricas para tentativas iniciadas, enviadas, pendentes e corrigidas;
- logs com `assessmentId`, `submissionId`, revisão e versão do avaliador;
- eventos auditáveis por estágio, revisão, override e regrade,
  com score anterior, novo score, itens alterados e motivo;
- alertas para submissões presas em qualquer estágio;
- testes de concorrência em start, submit, avaliação e revisão;
- testes de autorização em todos os DTOs full, review e learner-safe;
- testes E2E para os quatro avaliadores primários, revisão docente, atraso,
  timeout e múltiplas tentativas;
- testes de edição do quiz após início e após envio;
- testes de feedback antes e depois da liberação;
- limpeza dos caminhos antigos somente após cobertura funcional equivalente.

Critério de saída:

- os quatro avaliadores primários e as três combinações com revisão docente
  possuem E2E reproduzível e telemetria suficiente para diagnóstico.

## Ordem recomendada de entrega

### Marco A. MVP manual seguro

Executar Fases 0 a 4.

Esse marco é o caminho mais curto até um fluxo completo real:

```text
professor cria -> aluno responde -> professor corrige -> aluno vê resultado
```

Ele aproveita a parte mais madura da API e não exige adiar segurança ou
versionamento.

### Marco B. Avaliação por IA

Executar Fase 5 sobre a base do Marco A.

Não é recomendável criar avaliação por IA antes de unificar a tentativa
e proteger a definição, pois isso produziria uma nota sem uma cadeia de
evidência confiável.

`AIGraded` isolado publica o resultado ao concluir. Com
`InstructorGraded`, o mesmo resultado permanece provisório até a revisão do
professor. O peso não altera nenhum dos dois caminhos.

### Marco C. Autoavaliação, pares e gradebook completo

Executar Fases 6 e 7.

Autoavaliação e revisão entre alunos reutilizam o resultado por estágios e a
revisão docente já implementados. O gradebook deve ser fechado antes de
considerar o fluxo acadêmico completo.

## Testes ponta a ponta obrigatórios

### Avaliação por IA

1. professor seleciona `AIGraded`;
2. publicação cria assessment e revisão;
3. aluno recebe documento sem respostas corretas e envia respostas estruturadas;
4. IA produz resultado por item e nota total;
5. sem `InstructorGraded`, submission termina como `Graded` e publica o
   resultado;
6. com `InstructorGraded`, submission aguarda revisão docente;
7. professor aprova sem alterações ou registra override com motivo;
8. grupo e peso não mudam as transições;
9. reenvio da mesma requisição não duplica efeitos.

### Avaliação pelo instrutor

1. professor cria questão manual ou marca questões como manuais;
2. aluno responde pela interface correta;
3. submission fica aguardando revisão;
4. professor vê enunciado e resposta na revisão original;
5. professor aplica score e feedback por item;
6. professor aprova o resultado final;
7. API calcula o total e conclui a submission;
8. aluno vê a avaliação;
9. gradebook é atualizado quando o grupo tem peso positivo.

### Autoavaliação

1. professor seleciona `AutoGraded`;
2. após responder, o aluno recebe a superfície de autoavaliação;
3. o aluno atribui scores e feedback dentro dos limites permitidos;
4. a API valida e consolida o resultado;
5. sem `InstructorGraded`, a autoavaliação é finalizada e publicada;
6. com `InstructorGraded`, o professor revisa e pode alterar com motivo;
7. a autoria do aluno e qualquer alteração docente permanecem auditáveis.

### Revisão entre alunos

1. professor seleciona `PeerReview` e configura sua política;
2. a API distribui cada submissão somente a outros alunos revisores elegíveis;
3. as revisões dos alunos são consolidadas conforme a política;
4. sem `InstructorGraded`, o resultado consolidado é publicado;
5. com `InstructorGraded`, o resultado segue para revisão docente;
6. anonimato, conflitos e atores permanecem auditáveis conforme a política.

### Segurança e consistência

1. aluno não encontra answer key em nenhum endpoint genérico ou específico;
2. professor autorizado recebe a definição full;
3. alteração posterior do quiz não muda uma tentativa existente;
4. método sem executor ou pré-requisito não pode ser publicado;
5. timeout é verificado pelo relógio do servidor;
6. content e assessment não divergem sob falha ou concorrência;
7. score agregado sempre equivale à soma dos itens;
8. grupo peso zero não afeta a nota final;
9. mudar o peso não altera o workflow nem reabre submissões finalizadas;
10. combinações fora das sete permitidas são rejeitadas;
11. `InstructorGraded` combinado sempre é executado por último;
12. conclusão de estágio, revisão sem alteração, override e regrade geram
    eventos distintos;
13. regrade preserva valor anterior, novo valor, ator, data e motivo.

## Definição de fluxo completo

O fluxo somente deve ser considerado completo quando todas as afirmações forem
verdadeiras:

- quiz e assessment são salvos atomicamente;
- cada conteúdo avaliável possui no máximo um assessment ativo;
- cada tentativa referencia uma definição imutável;
- o aluno nunca recebe dados privados de correção;
- o player oficial envia respostas estruturadas pela submission oficial;
- limite de tempo e tentativas são impostos no servidor;
- `GradingMethods` admite somente as sete combinações formalizadas;
- pares, IA, aluno ou instrutor executam somente seus estágios autorizados;
- `InstructorGraded` combinado sempre revisa e finaliza por último;
- sem `InstructorGraded`, o avaliador primário finaliza o resultado;
- peso e grupo não alteram o workflow escolhido;
- o score informado pelo aluno em `AutoGraded` passa por contrato e validação
  próprios, separado do payload de respostas;
- questões e resultados aparecem com contexto nas superfícies de cada ator;
- professor pode aprovar, sobrescrever e reavaliar com histórico imutável;
- aluno vê estado, nota e feedback conforme política;
- grupo e peso controlam a participação no gradebook;
- dashboard, progresso e integrações leem o mesmo resultado oficial;
- testes E2E cobrem os quatro métodos primários, revisão docente e segurança.

## Arquivos e módulos diretamente envolvidos

### Packages

```text
packages/features/quiz
packages/features/quiz-content
packages/features/quiz-surface
packages/features/grading
```

### Autoria web

```text
apps/web/src/components/learning/console/courses/[course]/content/[contentId]/quiz-content-editor.tsx
apps/web/src/components/learning/console/courses/[course]/content/[contentId]/content-item-editor.tsx
apps/web/src/components/learning/console/courses/[course]/assessments/[assessmentId]/assessment-editor.tsx
apps/web/src/lib/learning/actions.ts
```

### Experiência do aluno

```text
apps/web/src/app/[locale]/learn/courses/[slug]/activities/[activityId]/page.tsx
apps/web/src/components/learning/learner-activity-form.tsx
apps/web/src/lib/learner/activity-actions.ts
apps/web/src/lib/learner/activity-contracts.ts
apps/web/src/components/courses/learning/activity-component.tsx
apps/web/src/lib/courses/server-actions.ts
```

### Correção docente

```text
apps/web/src/app/[locale]/(speedgrader)/speedgrader/assessments/[assessmentId]/quiz-viewer.tsx
apps/web/src/app/[locale]/(speedgrader)/speedgrader/assessments/[assessmentId]/submission-viewer.tsx
```

### API

```text
apps/api/Source/Modules/GameGuild.Learning.Courses/Controllers/ProgramContentController.cs
apps/api/Source/Modules/GameGuild.Learning.Courses/Extensions/ProgramContentMappingExtensions.cs
apps/api/Source/Modules/GameGuild.Learning.Assessments/Controllers/AssessmentsController.cs
apps/api/Source/Modules/GameGuild.Learning.Assessments/Entities/Assessment.cs
apps/api/Source/Modules/GameGuild.Learning.Assessments/Services/AssessmentService.cs
apps/api/Source/Modules/GameGuild.Learning.Assessments/Services/GradingQueueService.cs
apps/api/Source/Modules/GameGuild.Learning.Assessments/Configuration/AssessmentsModelConfiguration.cs
apps/api/Source/Modules/GameGuild.Learning.Workspaces/Queries/GetLearnerDashboardQuery.cs
```

## Conclusão

O projeto está mais perto do fluxo `InstructorGraded` isolado. A maior parte da
infraestrutura genérica de tentativa e correção docente já existe. A autoria e
a experiência visual do quiz também estão avançadas. `PeerReview` está
parcialmente implementado; `AIGraded`, `AutoGraded` e os workflows em duas
etapas ainda precisam de execução real.

O próximo passo não deve ser adicionar mais controles ao editor isoladamente.
Deve ser fechar a cadeia de confiança:

```text
definição versionada e segura
-> tentativa oficial
-> resposta estruturada
-> avaliador primário autorizado
-> revisão docente, somente quando configurada
-> resultado oficial auditável
-> visualização do aluno
-> gradebook
```

A sequência recomendada é entregar primeiro o workflow docente seguro, depois
ligar `AIGraded` e a revisão opcional e, por fim, completar autoavaliação,
revisão entre alunos e gradebook. Essa ordem produz valor completo a cada marco
e evita construir novos avaliadores sobre um fluxo de submissão ainda
fragmentado.

Peso define contribuição acadêmica, não precedência. A presença de
`InstructorGraded` é a única configuração que exige revisão final do professor;
sem ela, o avaliador primário publica o resultado.
