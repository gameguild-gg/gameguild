# 07. Roadmap de entrega e testes

## Objetivo

Executar o plano em fatias verticais pequenas, sem uma refatoração longa que
deixe contratos, UI e backend em estados incompatíveis.

## Sequência de entrega

### Fase 0. Domínio e contrato

- adicionar `SelfGraded = 16`;
- centralizar combinações válidas;
- atualizar serialização e constraint;
- fechar schemas de resposta, estágio e resultado;
- adicionar testes de domínio e round-trip.

Gate: API e web concordam sobre os nove workflows.

### Fase 1. Segurança e revisão imutável

- learner-safe DTO;
- snapshot/revision por execução;
- remoção do JSON autoral das rotas de aluno;
- validação server-side completa do payload.

Gate: nenhuma answer key alcança o aluno e uma edição não muda tentativa
existente.

### Fase 2. Autoria e publicação atômicas

- comando único de save/publish;
- remoção da reconciliação no browser;
- novo seletor de workflow;
- pré-requisitos e revisão no publish.

Gate: quiz e assessment não divergem sob falha ou concorrência.

### Fase 3. Test run do professor

- agregado isolado `AssessmentTestRun`;
- `QuizPlayer` em persona de aluno;
- submissão estruturada;
- orquestrador compartilhado;
- bloqueio de efeitos acadêmicos;
- restart e retomada.

Gate: professor responde um assessment salvo sem criar enrollment, submission
oficial, progresso ou gradebook.

### Fase 4. `InstructorGraded`

- resultado por item;
- superfície de correção contextual no test run;
- finalização e feedback de teste;
- override e regrade auditáveis.

Gate: primeiro fluxo vertical completo no modo de teste.

### Fase 5. `AutoGraded`

- avaliador determinístico C#;
- fixtures compartilhadas com `@game-guild/grading`;
- finalização direta e revisão docente opcional no test run.

Gate: correção reproduzível e idempotente no servidor.

### Fase 6. `SelfGraded`

- contrato e superfície de autoavaliação na persona de aluno;
- validação e finalização;
- revisão docente opcional.

Gate: persona de aluno autoavalia sem conseguir alterar respostas ou limites.

### Fase 7. `PeerReview`

- peers sintéticos e consolidação no test run;
- configuração de anonimato, quantidade e política;
- revisão docente opcional.

Gate: o test run reproduz a consolidação da política sem usar contas reais.

### Fase 8. `AIGraded`

- provider e execução assíncrona;
- versionamento, evidência, custo e retries;
- revisão docente opcional.

Gate: falha de IA nunca publica nota silenciosamente.

### Fase 9. Tentativa oficial do aluno

- `QuizPlayer` sobre `AssessmentSubmission`;
- start/reopen idempotente;
- submissão estruturada;
- autoavaliação real quando configurada;
- distribuição real de peer review;
- resultado read-only do aluno;
- políticas de tempo, tentativas e feedback.

Gate: o aluno percorre o pipeline já validado sem caminhos paralelos de quiz.

### Fase 10. Gradebook e operação

- pesos e política de tentativas;
- filas, notificações, passback e dashboards;
- métricas, alertas e limpeza dos caminhos antigos.

Gate: todas as projeções consomem o mesmo resultado final.

## Estratégia de PRs

Cada fase pode ter múltiplos PRs, mas um PR não deve misturar:

- migration estrutural e redesign amplo de UI;
- novo executor e refatoração do lifecycle inteiro;
- mudança de contrato sem consumidores atualizados;
- remoção de caminho antigo antes do E2E equivalente.

Ordem interna recomendada:

```text
contrato e testes -> domínio API -> persistência -> endpoints -> web -> E2E
```

## Matriz E2E de workflows

A matriz é executada em duas passagens:

1. nas Fases 4 a 8, pelo test run do professor, validando resposta, estágios,
   revisão e resultado sem efeitos acadêmicos;
2. nas Fases 9 e 10, pela tentativa oficial, acrescentando identidade real,
   políticas acadêmicas, resultado do aluno e gradebook.

| Primário | Direto | Com instrutor |
| --- | --- | --- |
| `PeerReview` | obrigatório | obrigatório |
| `AIGraded` | obrigatório | obrigatório |
| `AutoGraded` | obrigatório | obrigatório |
| `SelfGraded` | obrigatório | obrigatório |
| `InstructorGraded` | obrigatório | não aplicável |

No test run, cada cenário deve verificar:

1. criação e publicação;
2. start e revisão imutável;
3. payload learner-safe;
4. submissão estruturada;
5. ator autorizado;
6. resultado por item;
7. transição correta;
8. publicação ou espera pelo instrutor;
9. isolamento de efeitos acadêmicos;
10. idempotência.

Na tentativa oficial, acrescentar:

1. enrollment, identidade e políticas reais;
2. distribuição real para pares;
3. resultado e feedback do aluno;
4. gradebook conforme grupo e peso;
5. auditoria acadêmica e notificações.

## Testes transversais

### Segurança

- answer key ausente em todos os DTOs do aluno;
- score não pode ser injetado no payload de resposta;
- `SelfGraded` aceita somente a persona do test run ou o aluno da tentativa,
  conforme o contexto;
- peer acessa somente submissão atribuída;
- instrutor precisa de permissão no curso;
- serviço de IA e automático não usam credencial de usuário.

### Consistência

- todos os valores de bitmask de `0` a `31` são aceitos ou rejeitados conforme
  a matriz;
- ordem textual das flags não altera o pipeline;
- peso não altera métodos;
- total equivale aos itens;
- mudança de definição não altera execução iniciada;
- regrade preserva versão anterior.

### Concorrência

- duplo start reutiliza tentativa;
- duplo submit não duplica estágio;
- dois workers não executam o mesmo grading;
- dois reviewers não finalizam simultaneamente;
- retry não duplica evento, notificação ou passback.

### Contratos

- fixtures JSON válidas e inválidas;
- compatibilidade TypeScript/C#;
- round-trip de enums flags;
- redaction learner-safe;
- versionamento rejeita versão futura desconhecida.

## Sem legacy

O produto não foi lançado. A implementação deve:

- atualizar os produtores e consumidores na mesma fase;
- não manter aliases permanentes;
- não criar dual-read ou dual-write;
- não migrar documentos inexistentes;
- remover caminhos substituídos depois do gate E2E.

Isso não autoriza editar migrations já aplicadas em ambientes compartilhados.
Evolução do banco continua forward-only quando houver qualquer banco compartilhado.

## Checklist final

- [ ] nove workflows cobertos;
- [ ] cinco atores/executores autorizados;
- [ ] revisão docente opcional e sempre final;
- [ ] test run do professor isolado de efeitos acadêmicos;
- [ ] revisão imutável por tentativa;
- [ ] resultado por item e final persistidos;
- [ ] aluno vê resultado correto;
- [ ] gradebook ponderado correto;
- [ ] auditoria e regrade completos;
- [ ] filas e observabilidade operacionais;
- [ ] caminhos antigos removidos;
- [ ] mapas em `docs/types` atualizados com o código final.
