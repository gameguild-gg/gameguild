# Parte 5. Orquestracao da expansao de linguagens do Emception

Status: planejado; execucao incremental por subplano.

## Objetivo

Orquestrar a evolucao aditiva do Emception para novas linguagens sem reunir
investigacao, runtime, supply chain, testes e integracao de todos os idiomas em
uma unica entrega.

Este arquivo define ordem, gates comuns e estado. Cada linguagem possui um
subplano proprio e pode ser aprovada, adiada ou rejeitada sem bloquear as
demais. Nenhum subplano autoriza mudanca destrutiva no Emception.

Pre-requisito recomendado: partes 1 a 4 concluidas nas linguagens ja
suportadas.

## Baseline observada

No baseline atual:

- `ToolchainPreset` possui C, C++, Python, CMake e variantes graficas de C/C++;
- `WorkspaceBuildConfig`, `WorkspaceOptions` e `RunType` usam unions fechadas;
- os presets publicos cobrem canvas, terminal WASI, CMake e script Python;
- o browser adapter, toolchain versionado e VFS possuem lifecycle e testes
  proprios;
- nao ha suporte publico comprovado para JavaScript, TypeScript, Ruby, SQL ou
  C# como workspace executavel do Emception.

Isso e uma fotografia a confirmar no subplano comum, nao autorizacao para
substituir os contratos existentes.

## Invariantes

- Emception permanece produto e tool independente de grading.
- Toda evolucao e aditiva, opt-in e retrocompativel.
- Presets, exports, defaults, toolchains, run types e consumidores atuais
  permanecem operacionais.
- Uma necessidade exclusiva de grading e resolvida no adapter ou na
  `coding-surface`, nao no Emception.
- Nenhuma linguagem entra por import privado, CDN nao fixada ou dependencia de
  producao adicionada antes de spike e aprovacao.
- Runtime e toolchain sao avaliados no codigo-fonte e em documentacao primaria;
  popularidade ou semelhanca de API nao substituem evidencia.
- Suporte no Emception, preview na GameGuild e grading oficial sao estados
  diferentes e devem ser aprovados separadamente.
- Cada linguagem usa branch, PR para `develop`, evidencias e rollback
  operacional proprios.
- Versionamento e release seguem o processo existente do repositorio; este
  plano nao cria publicacao independente por linguagem.

## Subplanos

| ID | Subplano | Dependencia | Entrega |
| --- | --- | --- | --- |
| 0 | [`00-shared-capability-baseline`](./05-emception-language-expansion/00-shared-capability-baseline.md) | nenhuma | baseline e modelo aditivo de extensao aprovados |
| 1 | [`01-javascript`](./05-emception-language-expansion/01-javascript.md) | subplano 00 | runtime JavaScript isolado |
| 2 | [`02-typescript`](./05-emception-language-expansion/02-typescript.md) | subplanos 00 e 01 | toolchain TypeScript sobre runtime aprovado |
| 3 | [`03-ruby`](./05-emception-language-expansion/03-ruby.md) | subplano 00 | runtime Ruby isolado |
| 4 | [`04-sql`](./05-emception-language-expansion/04-sql.md) | subplano 00 | workspace SQL deterministico |
| 5 | [`05-csharp`](./05-emception-language-expansion/05-csharp.md) | subplano 00 | toolchain C# isolada |

O subplano 00 e obrigatorio. JavaScript deve anteceder TypeScript. Ruby, SQL e
C# podem ser investigados em paralelo depois da baseline, mas cada mudanca
upstream deve permanecer em entrega separada.
Cada suporte so e integrado depois de sua implementacao e validacao completas.
Seu PR deve partir da `develop` atualizada e nao pode depender de commits ainda
isolados na branch de outra linguagem.

## Estados por linguagem

Cada subplano registra separadamente:

| Estado | Significado |
| --- | --- |
| `investigating` | runtime e arquitetura ainda em avaliacao |
| `emception-supported` | autoria e execucao funcionam pela API publica do Emception |
| `gameguild-preview-ready` | `coding-surface` pode consumir a capability no browser |
| `official-grading-ready` | existe executor servidor compativel, versionado e autoritativo |
| `deferred` | custo ou risco impede a entrega atual |
| `rejected` | alternativa foi rejeitada com justificativa registrada |

`emception-supported` nao implica `official-grading-ready`. O Emception pode
receber uma linguagem util para sua tool sem que ela seja imediatamente aceita
como executor academico.

## Gates comuns

### `EMC-OWNER-GATE`

O responsavel pelo Emception aprova extension point, runtime, ownership,
versionamento e distribuicao antes de qualquer mudanca de producao.

### `EMCEPTION-NON-REGRESSION-GATE`

Cada subplano preserva a baseline do subplano 00 e passa as suites atuais do
Emception e de `@game-guild/emception-ui`. Nao se remove ou reconfigura uma
capability existente para acomodar a nova linguagem.

### `EMC-RUNTIME-SUPPLY-GATE`

Runtime, compiler, transpiler ou banco precisa de versao fixada, origem,
licenca, integridade, estrategia de cache e processo reproduzivel de update.

### `EMC-ISOLATION-GATE`

Worker, cancelamento, timeout, memoria, output, filesystem, rede e teardown sao
testados. Falha em isolamento impede publicacao da capability.

### `EMC-PARITY-GATE`

Quando houver intencao de grading oficial, browser e servidor compartilham
identidade de linguagem/toolchain, fixtures e semantica observavel. Diferencas
aceitas sao documentadas; preview nunca vira autoridade academica.

### `EMC-INTEGRATION-GATE`

Cada linguagem conclui todos os seus gates e suites antes de abrir um PR
exclusivo para `develop`. O PR inclui somente a linguagem em questao e as
alteracoes aditivas estritamente necessarias, apresenta evidencias e cumpre o
requisito de changeset vigente quando aplicavel. O pipeline posterior de
versionamento e release nao e alterado por este plano.

JavaScript precisa estar integrado em `develop` antes do PR de TypeScript. Ruby,
SQL e C# podem ter branches paralelas depois da baseline, mas cada uma deve ser
reconciliada e retestada contra a `develop` atual antes do proprio PR.

## Validacao comum

Todo subplano que alterar o Emception executa, no minimo:

```bash
pnpm build:emception
pnpm test:emception
pnpm test:emception:types
pnpm test:emception:e2e
pnpm --filter @game-guild/emception-ui test
```

O subplano adiciona testes proprios do novo runtime e do consumidor afetado.
Mudanca no nome de um script nao permite retirar cobertura equivalente.

## Estado inicial

| ID | Estado | Proxima evidencia |
| --- | --- | --- |
| `EMC-BASELINE` | pendente | subplano 00 aprovado |
| `EMC-JAVASCRIPT` | bloqueado por baseline | decisao do subplano 01 |
| `EMC-TYPESCRIPT` | bloqueado por baseline e JavaScript | `EMC-JAVASCRIPT` suportado |
| `EMC-RUBY` | bloqueado por baseline | decisao do subplano 03 |
| `EMC-SQL` | bloqueado por baseline | decisao do subplano 04 |
| `EMC-CSHARP` | bloqueado por baseline | decisao do subplano 05 |

## Criterios de conclusao da parte 5

- baseline comum foi registrada e aprovada pelo owner;
- cada linguagem possui estado, evidencia e blocker proprios;
- nenhuma linguagem depende de internals ou altera defaults existentes;
- planos upstream e integracoes GameGuild permanecem separados;
- cada linguagem concluida possui PR independente e validado para `develop`;
- nenhuma capability existente foi reduzida por causa de grading;
- o roadmap indica claramente quais linguagens estao prontas apenas no
  Emception, no preview GameGuild ou no grading oficial.
