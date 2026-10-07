# Parte 3. Acesso contextual e personas reais

Status: pendente; liberada pelo fechamento da Parte 2.

## Objetivo

Comprovar o fluxo entregue nas Partes 1 e 2 com usuários autenticados que
representem os contextos reais do curso, antes de expandir os métodos de review.
Esta parte não cria roles de produto. Ela compõe a membership `Member` já
existente com matrícula ativa, autoria do curso ou permissions por recurso.

Regras globais: [`08-implementation-sequence.md`](../08-implementation-sequence.md).

## Estado encontrado

- registro local e OAuth já provisionam membership `Member` no tenant padrão;
- a jornada `/learn` já exige autenticação e usa enrollment para acesso ao
  curso, tentativa oficial e leitura do resultado;
- assessments já reconhecem parcialmente `CreatorId` e
  `Program.{courseId}.Review`;
- a consulta web `canEditCourse` já combina owner e resource permission, mas o
  layout autoral e o SpeedGrader ainda não consomem uma projeção única de
  capacidades;
- checagens de curso estão duplicadas entre filtros genéricos, controllers e
  web e podem divergir para owner, collaborator e reviewer;
- o E2E PostgreSQL de grading já usa atores distintos, porém autenticação por
  headers de teste, seed direto no banco e a string de role `Instructor` não
  comprovam a sessão real nem a composição contextual pretendida.

Portanto, esta parte integra e prova o sistema existente. Ela não o substitui.

## Limites arquiteturais

- não criar roles `Student`, `Instructor`, `Reviewer` ou `TA`;
- não criar tabela, coluna, migration, permission store ou sistema de sessão;
- não alterar a semântica de tenant, membership, actor context ou DAC mantida
  pelos módulos `GameGuild.Identity.*`;
- obter tenant exclusivamente do request/actor context; route, body e query não
  podem escolher o tenant autoritativo da operação;
- consumir somente os contratos públicos existentes de autenticação, tenant e
  autorização. Um defeito nesses contratos deve ser registrado para o owner do
  módulo, não contornado dentro de Learning;
- manter toda composição de course access no domínio `GameGuild.Learning.*` ou
  na borda web. Módulo de plataforma não pode depender de Learning;
- usar `Enrollment` ativa como fonte canônica para participação acadêmica nos
  novos cenários. Não criar outro estado de aluno;
- manter a API como autoridade. Gates no frontend melhoram a UX, mas não
  substituem autorização em endpoint ou handler;
- manter `SystemAdmin` apenas na preparação administrativa. Nenhuma prova
  funcional positiva pode depender desse bypass;
- não alterar score, review workflow, release, gradebook ou runtime de grading,
  salvo para substituir uma checagem de acesso inconsistente pela política
  contextual aprovada nesta parte.

Se a implementação demonstrar necessidade de schema, a parte para e apresenta
o problema separadamente. Não há `SCHEMA-GATE` planejado para `ACCESS-01` a
`ACCESS-05`.

## Matriz canônica de personas

As personas são estados contextuais e podem variar por curso para a mesma
conta.

| Persona de teste | Composição | Pode | Não pode |
| --- | --- | --- | --- |
| Aluno | `Member` ativa + `Enrollment` ativa no curso | acessar `/learn`, iniciar e enviar a própria tentativa e ler resultado liberado | autorar, publicar, revisar ou ler tentativa alheia |
| Instrutor proprietário | `Member` ativa + `Program.CreatorId` igual ao ator, no mesmo tenant | administrar o próprio curso, assessment, publicação, review e release | usar autoria sobre curso de outro tenant ou de outro owner |
| Instrutor colaborador | `Member` ativa + permissions explícitas no recurso `Program` | executar somente as ações concedidas, como `Edit`, `Publish` ou `Review` | herdar poderes de owner ou tenant admin por rótulo de UI |
| Revisor/TA | `Member` ativa + `Program.{courseId}.Review` | abrir a fila segura, revisar, resolver e liberar quando a policy permitir | editar conteúdo ou publicar sem grants próprios |
| Outsider | `Member` ativa, sem matrícula e sem grants no curso | acessar somente superfícies públicas permitidas | entrar em `/learn`, autoria, fila ou resultado privado |
| Administrador | contexto administrativo já existente | preparar tenant, membership e grants | substituir qualquer persona nas provas de aceitação |

Regras de composição:

- autoria concede capacidades ao proprietário somente no curso cujo
  `CreatorId` corresponde ao ator e cujo tenant corresponde ao request context;
- permissions por recurso concedem a operação nomeada. `Edit`, `Publish` e
  `Review` não se implicam silenciosamente entre si;
- matrícula não concede autoria ou review;
- permission de review não concede edição ou publicação;
- uma mesma conta deve poder ser aluna no curso A e proprietária,
  colaboradora ou revisora no curso B.

## Gate de entrada `03-0`

O fechamento `CLOSE-01` a `CLOSE-04` deve permanecer aprovado. Suas provas de
runtime continuam válidas; esta parte acrescenta identidade e autorização
reais, sem reimplementar `SEQ-07` a `SEQ-11`.

## `ACCESS-01`. Fechar o contrato de acesso do curso

### Resultado

Existe uma única matriz de capacidades de Learning, rastreável até os
mecanismos existentes de membership, enrollment, autoria e resource
permissions.

### Implementação

- inventariar os gates atuais de criação, leitura, edição, publicação, review,
  release e tentativa oficial;
- definir capacidades orientadas a ação, no mínimo: `Learn`, `Edit`, `Publish`
  e `Review`, sem transformá-las em roles persistidas;
- documentar qual contrato público existente concede e consulta resource
  permissions, incluindo invalidação, versionamento e auditoria já oferecidos;
- definir `CreatorId` como regra contextual de owner, sempre limitada ao tenant;
- separar acesso de catálogo, acesso de learner e acesso de authoring;
- localizar checagens duplicadas, especialmente `CanManageCourseAsync`, filtros
  genéricos de `Program`, `canEditCourse` e gates de SpeedGrader;
- adicionar testes da matriz antes de consolidar os consumidores.

### Gate

- cada ação possui uma fonte de decisão explícita e fail-closed;
- nenhuma decisão depende de string `Instructor`, `Student`, `Reviewer` ou `TA`;
- não há alteração em módulos de plataforma nem em schema;
- a matriz comprova que permissions diferentes não se promovem implicitamente.

## `ACCESS-02`. Tornar a autorização da API coerente

### Resultado

Endpoints e handlers de Learning tomam a mesma decisão para o mesmo ator,
tenant, curso e ação.

### Implementação

- introduzir ou consolidar um avaliador de capacidades no domínio Learning que
  consuma `IActorContextAccessor`, autoria e os contratos públicos de
  autorização existentes;
- substituir cópias locais da política por esse avaliador nos fluxos de curso,
  content, assessment, test run, grading queue, instructor review e release;
- preservar `[Authorize]` ou `[AllowAnonymous]` explícito em todos os endpoints;
- fazer criação de curso por um `Member` autorizado resultar em owner por
  `CreatorId`, sem grant paralelo obrigatório nem role especial;
- garantir que learner só inicia ou consulta sua própria submission mediante
  matrícula ativa e que revisor recebe apenas a projeção necessária;
- negar tenant ausente, tenant divergente, enrollment inativa, grant expirado e
  recurso inexistente sem revelar dados de outro contexto;
- manter mutations de permission exclusivamente nos commands existentes, com
  ator vindo do contexto, security version e audit log já definidos pelo módulo
  de autorização.

### Regra de parada

Se o contrato público existente não conseguir conceder e consultar
`Program.{courseId}.{Permission}` de forma coerente, interromper o marco e
entregar um diagnóstico ao owner de Authorization. É proibido escrever direto
em tabelas, introduzir um segundo resolver ou enfraquecer o gate para avançar.

### Gate

- testes de aplicação/API cobrem as cinco personas não administrativas;
- owner, grants e enrollment produzem decisões idênticas em todos os endpoints
  equivalentes;
- collaborator e reviewer seguem least privilege;
- outsider, cross-tenant e tentativa de ler submission alheia falham fechado;
- nenhum teste positivo usa `SystemAdmin`.

## `ACCESS-03`. Expor capacidades e atribuições na web

### Resultado

O workspace mostra apenas as operações permitidas e permite atribuir acesso ao
curso pelo sistema existente, sem apresentar personas como roles globais.

### Implementação

- aplicar gate server-side no layout de
  `/workspace/learning/courses/[course]` e nas rotas de assessments;
- proteger a entrada do SpeedGrader por capability `Review`, mantendo a API
  como segunda barreira obrigatória;
- derivar navegação, botões de edição, publicação e review das capacidades
  retornadas pelo servidor;
- manter `/learn` vinculado a enrollment e separado do workspace autoral;
- listar no workspace apenas cursos criados pelo ator ou compartilhados por
  resource permission compatível;
- oferecer na área de acesso do curso atribuições de colaborador e revisor como
  conjuntos de permissions existentes. A UI deve mostrar ações granulares e
  não persistir os nomes das personas;
- usar somente endpoints guardados de share/update/revoke; não aceitar
  `GrantedBy`, `CreatedBy` ou identidade equivalente enviada pelo cliente;
- tratar revoke e grant expirado imediatamente na navegação e no próximo
  request protegido.

### Gate

- links e comandos inacessíveis não são renderizados;
- acesso direto por URL ainda é negado pela API;
- owner consegue administrar sem role especial;
- colaborador e revisor veem somente suas capacidades;
- aluno e outsider não entram em superfícies autorais.

## `ACCESS-04`. Criar fixture de sessões reais

### Resultado

Existe um arranjo de teste reproduzível com contas reais e contextos isolados,
sem headers sintéticos como única prova de identidade.

### Implementação

- criar contas `Member` por meio do fluxo suportado de autenticação e
  provisionamento de membership;
- abrir browser context, cookie ou token independente para owner, aluno,
  colaborador, revisor e outsider;
- criar curso como owner; criar enrollment do aluno e resource grants pelos
  casos de uso oficiais existentes;
- usar administrador somente para o bootstrap que realmente exigir
  administração do tenant;
- manter dados aleatórios e isolados por execução, com cleanup idempotente;
- corrigir o cenário relacional existente que usa `TenantMember.Role =
  "Instructor"`: ele deve usar `Member`, pois a autoridade vem de `CreatorId`
  ou grant contextual;
- preservar os testes HTTP com authentication handler sintético como testes
  rápidos de runtime, mas não tratá-los como prova suficiente do login e da
  sessão reais.

### Gate

- cada persona possui sessão própria e tenant correto;
- nenhum cenário funcional troca ator apenas por header de teste;
- refresh/reabertura preserva a decisão esperada;
- o mesmo usuário é testado em contextos diferentes sem mudança de role.

## `ACCESS-05`. Revalidar o E2E de grading por persona

### Resultado

O fluxo principal de grading está comprovado do login ao resultado liberado
com os atores que serão usados na Parte 4.

### Cenários obrigatórios

1. owner cria curso, quiz e assessment, configura review e publica;
2. aluno matriculado realiza `AutomatedReview` e vê somente o resultado
   liberado;
3. aluno realiza `AutomatedReview + InstructorReview`; owner ou ator com
   `Review` conclui a pendência e libera o resultado;
4. revisor com apenas `Review` usa a fila e não consegue editar ou publicar;
5. colaborador executa somente cada ação explicitamente concedida;
6. outsider é negado em tentativa, submission, fila, review e resultado;
7. aluno não lê resultado retido nem submission de outro aluno;
8. acesso revogado deixa de funcionar sem depender de novo login;
9. tentativa cross-tenant falha fechada;
10. uma conta aluna em um curso e owner em outro recebe capacidades distintas.

### Verificação acumulada

- repetir os E2Es oficiais de `SEQ-10` e `SEQ-11`;
- repetir testes de authoring, learner activity, result, grading queue e
  SpeedGrader tocados;
- executar os testes de autorização e arquitetura exigidos pelo repositório;
- repetir, no mínimo, `GameGuild.Identity.Authorization.UnitTests`,
  `GameGuild.Identity.Authentication.UnitTests` e as suítes de
  arquitetura/segurança de `GameGuild.API.UnitTests`;
- executar build da API e typecheck/testes direcionados da web;
- registrar falhas de módulos externos para seus owners sem expandir o escopo.

## Definição de pronto da Parte 3

- `ACCESS-01` a `ACCESS-05` concluídos;
- as personas são composições contextuais sobre `Member`, nunca roles de
  produto;
- a API resolve as capacidades e a web consome essa projeção, sem duplicar a
  política; a API permanece a autoridade;
- owner, aluno, colaborador, revisor e outsider passam em sessões separadas;
- nenhum fluxo positivo depende de `SystemAdmin` ou de acesso direto ao banco;
- nenhuma tabela, migration ou semântica de plataforma foi adicionada;
- as evidências das Partes 1 e 2 continuam aprovadas;
- a Parte 4 recebe atores reais para `SelfReview`, `PeerReview`, `AIReview` e
  operação avançada.

## Acompanhamento

| Marco | Status | Evidência esperada |
| --- | --- | --- |
| gate `03-0` | aprovado | `CLOSE-01` a `CLOSE-04` permanecem aprovados |
| `ACCESS-01` | pendente | contrato e matriz de capacidades fechados |
| `ACCESS-02` | pendente | autorização uniforme e testes negativos na API |
| `ACCESS-03` | pendente | gates e atribuições contextuais na web |
| `ACCESS-04` | pendente | fixture com sessões reais e `Member` contextual |
| `ACCESS-05` | pendente | E2E acumulado por persona aprovado |

## Gate para a Parte 4

`SEQ-12` só pode começar depois de `ACCESS-05`. O gate deve demonstrar que os
reviews avançados receberão learner, owner, reviewer e peers autenticados no
curso correto, sem depender de role inventada, admin ou identity sintética.
