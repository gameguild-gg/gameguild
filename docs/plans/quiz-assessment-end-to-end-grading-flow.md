# Fluxo ponta a ponta de quiz, avaliação e nota

Status: proposto.

Data da avaliação: 2026-08-19.

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

Além disso, `AutoGraded` é hoje apenas uma flag. A API não executa o avaliador
de quiz ao receber a submissão. O avaliador existente está no package
TypeScript e ainda não existe no limite confiável da API C#.

A conclusão é:

- o fluxo manual genérico está relativamente próximo, pois a API já recebe a
  tentativa e permite ao professor aplicar uma nota global;
- o fluxo manual específico de quiz ainda precisa unir a interface do quiz à
  submissão oficial e mostrar questões e respostas corretamente ao professor;
- o fluxo automático está mais distante, pois exige entrega segura da prova,
  versão imutável da definição, avaliador no servidor e persistência do
  resultado por questão;
- o fluxo híbrido, com questões automáticas e manuais no mesmo quiz, deve ser
  tratado como resultado natural dos dois fluxos, não como uma terceira
  implementação independente.

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
7. a API avaliar automaticamente as questões determinísticas;
8. questões manuais seguirem para a fila do professor;
9. o resultado final ser calculado e persistido uma única vez;
10. o aluno ver estado, nota e feedback conforme a política configurada;
11. o gradebook consumir somente resultados oficiais produzidos pela API.

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
- capacidades de correção derivadas das questões.

### Assessment group

O grupo decide a participação no resultado do curso:

- sem grupo: assessment ainda não organizado para o gradebook;
- grupo de peso zero: prática ou avaliação formativa, com resultado oficial,
  mas sem contribuição para a nota final;
- grupo com peso positivo: avaliação que contribui para o gradebook.

Isso substitui a necessidade de um `resultUse` separado. `feedback` e
`gradebook` não são propriedades do quiz nem modos alternativos do motor de
grading. São interpretações da colocação do assessment na estrutura do curso.

### Assessment submission

`AssessmentSubmission` é a tentativa oficial do aluno. Deve ser a única fonte
de verdade para:

- respostas enviadas;
- número e tempo da tentativa;
- estado da correção;
- resultado por questão;
- nota total;
- aprovação;
- feedback;
- auditoria da correção automática ou docente.

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

As questões são classificadas automaticamente pelo sincronizador. Ainda não há
uma experiência clara para o professor confirmar que uma questão será
automática, manual ou bloqueada por falta de suporte.

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
- `AutoGraded,InstructorGraded` fixo.

Quando grading é desligado, a aplicação remove logicamente o assessment.

Problemas atuais:

- content e assessment podem divergir se a primeira chamada funcionar e a
  segunda falhar;
- a API não impõe um único assessment ativo para um conteúdo;
- `GradingMethods` não é derivado do inventário real de questões;
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

### 5. Correção automática no servidor

Estado atual: contrato e algoritmo de referência existem; execução oficial não
existe.

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

Ele não verifica `AutoGraded`, não carrega uma resposta correta versionada, não
executa um avaliador e não chama `Grade` automaticamente.

Portanto, marcar `AutoGraded` hoje não produz correção automática.

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

Para quiz manual ou híbrido, falta:

- carregar a mesma revisão imutável usada na tentativa;
- renderizar a questão e a resposta do aluno em contexto;
- permitir score e feedback por questão manual;
- preservar os pontos automáticos em quizzes híbridos;
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
- indicação clara de `aguardando correção` em quizzes manuais ou híbridos;
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
| Correção automática confiável no servidor | ausente | 15% |
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

### P1. `AutoGraded` não tem comportamento

A flag deve ser derivada da capacidade das questões e acionar um caso de uso
real na API. Ela não deve depender de um checkbox livre na tela de assessment.

### P1. Resultado por questão não é persistido oficialmente

O `GradeResult` do package possui itens, mas `AssessmentSubmission` guarda
apenas score total, aprovação, feedback e rubrica. Sem resultado por item não é
possível fechar corretamente o fluxo híbrido ou fazer auditoria de uma nota
automática.

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
- submissões ainda pendentes;
- arredondamento;
- nota final do curso.

### P1. Há propriedades com dupla autoridade

Precisam ser consolidadas:

- `passingScore` do quiz, `Assessment.PassingScore` remanescente e
  `Program.PassingScore`;
- pontos por item e `Assessment.MaxScore`;
- `GradingMethods` livre na tela versus capacidade derivada das questões;
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

### 5. Classificação das questões

Na publicação, o servidor deve derivar o modo do assessment:

| Inventário | `GradingMethods` derivado | Resultado no envio |
| --- | --- | --- |
| somente determinísticas | `AutoGraded` | nota final imediata |
| somente manuais | `InstructorGraded` | aguarda professor |
| determinísticas e manuais | `AutoGraded,InstructorGraded` | parcial, aguarda professor |
| qualquer item sem suporte | publicação bloqueada | não publicável |

O professor pode escolher explicitamente tornar uma questão manual, mesmo que
ela tenha avaliador determinístico. O contrário não é permitido: uma questão
sem avaliador não pode ser forçada a automática.

Inventário atual aproximado do adapter:

- determinísticas: single choice, multiple choice, true/false, fill in the
  blank, short answer, matching, ordering, categorization, rating quando há
  chave, hotspot e highlight;
- manuais: essay e qualquer questão marcada explicitamente como manual;
- sem suporte automático atual: numeric e formula;
- incompletas: questões sem chave suficiente devem bloquear publicação ou ser
  convertidas explicitamente para manual.

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

Recomendação inicial: persistir esse contrato em um campo JSONB versionado da
submissão. Isso mantém o resultado por item coeso e evita criar uma tabela por
resposta antes de existir necessidade real de consultas analíticas nesse
nível.

O score agregado deve ser calculado pelo servidor a partir dos itens. O
professor não deve enviar um total arbitrário que contradiga o detalhamento.

### 8. Estados de correção

O fluxo de domínio precisa distinguir:

```text
InProgress
  -> Submitted ou Late
  -> AwaitingReview, quando restam itens manuais
  -> Graded, quando todos os itens foram resolvidos
```

`AwaitingReview` pode ser inicialmente uma projeção de `Submitted/Late` mais
resultado pendente, mas deve existir no contrato da UI. Caso essa distinção
seja importante para consultas e notificações, deve virar estado persistido.

Para correção totalmente automática, `Submit` pode chegar a `Graded` na mesma
transação.

Para execução assíncrona futura, usar outbox/job idempotente e manter uma chave
de execução por submissão e revisão. Não criar isso antes de haver necessidade
de processamento assíncrono.

### 9. Correção manual e híbrida

O SpeedGrader deve receber:

- revisão autoral da questão, autorizada para professor;
- resposta estruturada do aluno;
- resultado automático já produzido;
- itens manuais ainda pendentes;
- score e feedback previamente salvos.

A UI deve reutilizar uma superfície read-only de `quiz-surface`, com extensão
para score e feedback por item. Ela não deve reconstruir questões a partir de
IDs exibidos como texto.

Em quiz híbrido:

1. o servidor corrige os itens determinísticos no envio;
2. grava os resultados automáticos como imutáveis ou regradáveis com auditoria;
3. coloca na fila somente os itens manuais pendentes;
4. o professor pontua os itens manuais;
5. o servidor consolida a nota final;
6. a submissão passa para `Graded`;
7. o aluno é notificado conforme a política de feedback.

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

## Divisão de responsabilidades entre Content e Assessment

| Propriedade | Fonte de verdade | Editável onde |
| --- | --- | --- |
| enunciados e opções | quiz content | editor do quiz |
| respostas corretas | quiz content privado | editor do quiz |
| pontos por questão | grading do content | editor do quiz |
| capacidade auto/manual | derivada das questões | exibida, não livre |
| score máximo | soma/projeção dos itens | exibido no assessment |
| vínculo com content | projeção | bloqueado no assessment de quiz |
| modalidade estruturada | projeção | bloqueada para quiz |
| disponibilidade e prazo | assessment | editor de assessment |
| tentativas e tempo | assessment | editor de assessment |
| apresentação | assessment | editor de assessment |
| grupo e peso | assessment group | assessment/gradebook |
| passing score | curso | configurações do curso |
| resposta do aluno | submission | somente leitura após envio |
| resultado por questão | submission/servidor | automático ou professor |
| nota final da tentativa | submission/servidor | derivada dos itens |

Essa matriz evita que o mesmo campo seja editável em content e assessment.

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
PUT  /assessment-submissions/{submissionId}/quiz-manual-results
GET  /assessment-submissions/{submissionId}/quiz-result
```

Regras:

- `full` exige permissão de gestão;
- `quiz-attempt` retorna somente a projeção learner-safe;
- `quiz-review` exige correção ou gestão e retorna definição e respostas em
  contexto;
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
- derivar `GradingMethods` do inventário real;
- projetar `MaxScore`, modalidade e apresentação sem permitir edição
  contraditória;
- manter no assessment editor somente configurações operacionais;
- validar publicação e rejeitar itens sem caminho de correção.

Critério de saída:

- não existe estado em que quiz foi salvo e a projeção falhou;
- reabrir content e assessment mostra valores coerentes;
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

Objetivo: entregar um MVP confiável mesmo antes da correção automática.

Tarefas:

- permitir que todos os itens sejam explicitamente manuais;
- enviar submissões manuais para a fila existente;
- carregar definição, resposta e revisão no SpeedGrader;
- criar review read-only de quiz com score e feedback por item;
- consolidar a nota no servidor;
- notificar o aluno;
- exibir nota, estado e feedback na rota da tentativa;
- respeitar a política de liberação de feedback.

Critério de saída:

- professor cria, publica, recebe, corrige e devolve um quiz totalmente manual;
- aluno vê o resultado oficial correto;
- grade e feedback não dependem do browser do professor para cálculo agregado.

### Fase 5. Implementar correção automática no servidor

Objetivo: fechar a modalidade automática para tipos determinísticos.

Tarefas:

- implementar o avaliador C# por tipo de questão suportado;
- adicionar vetores de conformidade compartilhados com
  `@game-guild/grading`;
- validar answer key e resposta pela revisão da tentativa;
- persistir `QuizGradeResultV1`;
- concluir submissões totalmente determinísticas como `Graded`;
- registrar origem, versão do avaliador e auditoria;
- tornar submit + grading transacional e idempotente;
- permitir regrade administrativo versionado sem apagar o resultado anterior;
- garantir que o cliente não consiga injetar score ou correção.

Critério de saída:

- os mesmos vetores produzem o mesmo resultado em TypeScript e C#;
- uma submissão automática recebe nota sem intervenção humana;
- retries não duplicam nota, notificação ou tentativa.

### Fase 6. Completar quizzes híbridos

Objetivo: combinar itens automáticos e manuais em uma tentativa.

Tarefas:

- persistir imediatamente resultados determinísticos;
- manter itens manuais como pendentes;
- filtrar a fila para mostrar somente trabalho humano necessário;
- impedir liberação de nota final antes de todos os itens obrigatórios;
- permitir ao professor revisar o resultado automático sem alterá-lo
  silenciosamente;
- consolidar score e estado após o último item manual;
- definir override e regrade com auditoria.

Critério de saída:

- quiz misto mantém pontos automáticos, recebe pontos manuais e gera uma única
  nota final reproduzível.

### Fase 7. Fechar gradebook e experiência de resultado

Objetivo: transformar a nota da tentativa em informação acadêmica coerente.

Tarefas:

- implementar cálculo ponderado por assessment group;
- excluir grupos de peso zero da nota final sem ocultar seus resultados;
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
- alertas para submissões automáticas presas;
- testes de concorrência em start, submit, auto-grade e correção manual;
- testes de autorização em todos os DTOs full, review e learner-safe;
- testes E2E para manual, automático, híbrido, atraso, timeout e múltiplas
  tentativas;
- testes de edição do quiz após início e após envio;
- testes de feedback antes e depois da liberação;
- limpeza dos caminhos antigos somente após cobertura funcional equivalente.

Critério de saída:

- os três modos, prática sem grading, grading manual e grading automático ou
  híbrido, possuem E2E reproduzível e telemetria suficiente para diagnóstico.

## Ordem recomendada de entrega

### Marco A. MVP manual seguro

Executar Fases 0 a 4.

Esse marco é o caminho mais curto até um fluxo completo real:

```text
professor cria -> aluno responde -> professor corrige -> aluno vê resultado
```

Ele aproveita a parte mais madura da API e não exige adiar segurança ou
versionamento.

### Marco B. MVP automático

Executar Fase 5 sobre a base do Marco A.

Não é recomendável criar uma correção automática antes de unificar a tentativa
e proteger a definição, pois isso produziria uma nota sem uma cadeia de
evidência confiável.

### Marco C. Híbrido e gradebook completo

Executar Fases 6 e 7.

O híbrido deixa de ser complexo depois que resultado por item e correção manual
já são oficiais. O gradebook deve ser fechado antes de considerar o fluxo
acadêmico completo.

## Testes ponta a ponta obrigatórios

### Quiz automático

1. professor cria questões determinísticas e define pontos;
2. publicação cria assessment e revisão;
3. aluno recebe documento sem respostas corretas;
4. aluno inicia e envia respostas estruturadas;
5. API calcula cada item e a nota total;
6. submission termina como `Graded`;
7. aluno vê nota e feedback permitido;
8. gradebook usa a tentativa conforme grupo e política;
9. reenvio da mesma requisição não duplica efeitos.

### Quiz manual

1. professor cria questão manual ou marca questões como manuais;
2. aluno responde pela interface correta;
3. submission fica aguardando revisão;
4. professor vê enunciado e resposta na revisão original;
5. professor aplica score e feedback por item;
6. API calcula o total e conclui a submission;
7. aluno vê a avaliação;
8. gradebook é atualizado quando o grupo tem peso positivo.

### Quiz híbrido

1. professor combina questões determinísticas e manuais;
2. API corrige os itens automáticos no envio;
3. nota final permanece pendente;
4. professor vê somente os itens que exigem decisão humana, sem perder o
   contexto completo;
5. conclusão manual consolida exatamente uma nota;
6. aluno recebe resultado conforme a política.

### Segurança e consistência

1. aluno não encontra answer key em nenhum endpoint genérico ou específico;
2. professor autorizado recebe a definição full;
3. alteração posterior do quiz não muda uma tentativa existente;
4. questão sem suporte não é publicada como automática;
5. timeout é verificado pelo relógio do servidor;
6. content e assessment não divergem sob falha ou concorrência;
7. score agregado sempre equivale à soma dos itens;
8. grupo peso zero não afeta a nota final.

## Definição de fluxo completo

O fluxo somente deve ser considerado completo quando todas as afirmações forem
verdadeiras:

- quiz e assessment são salvos atomicamente;
- cada conteúdo avaliável possui no máximo um assessment ativo;
- cada tentativa referencia uma definição imutável;
- o aluno nunca recebe dados privados de correção;
- o player oficial envia respostas estruturadas pela submission oficial;
- limite de tempo e tentativas são impostos no servidor;
- questões automáticas são avaliadas no servidor;
- questões manuais aparecem com contexto no SpeedGrader;
- quizzes híbridos preservam e consolidam resultados por item;
- score oficial não é calculado ou fornecido pelo cliente;
- aluno vê estado, nota e feedback conforme política;
- grupo e peso controlam a participação no gradebook;
- dashboard, progresso e integrações leem o mesmo resultado oficial;
- testes E2E cobrem automático, manual, híbrido e segurança.

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

O projeto está mais perto de um fluxo manual completo do que de um fluxo
automático. A maior parte da infraestrutura genérica de tentativa e correção
manual já existe. A autoria e a experiência visual do quiz também estão
avançadas.

O próximo passo não deve ser adicionar mais controles ao editor isoladamente.
Deve ser fechar a cadeia de confiança:

```text
definição versionada e segura
-> tentativa oficial
-> resposta estruturada
-> resultado oficial
-> revisão docente quando necessária
-> visualização do aluno
-> gradebook
```

A sequência recomendada é entregar primeiro o MVP manual seguro, depois ligar o
avaliador automático no servidor e, por fim, consolidar o híbrido e o
gradebook. Essa ordem produz valor completo a cada marco e evita construir
automação sobre um fluxo de submissão ainda fragmentado.
