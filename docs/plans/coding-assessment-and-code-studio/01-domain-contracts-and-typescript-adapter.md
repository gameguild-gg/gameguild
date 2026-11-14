# Parte 1. Dominio coding e adapter TypeScript

Status: planejado.

## Objetivo

Criar as fronteiras TypeScript que permitem integrar exercicios de codigo ao
grading sem fazer o core conhecer code e sem usar componentes visuais como
fonte de contratos.

Ao final desta fase existirao:

- `@game-guild/coding`, com o dominio serializavel de coding;
- `@game-guild/grading-adapter-code`, com a traducao para grading;
- fixtures compartilhadas entre parser, adapter e implementacao backend;
- uma decisao explicita para execucao assincrona ou externa;
- uma definicao TypeScript autoritativa para os novos packages de grading;
- um registro explicito das formas preservadas que continuarao duplicadas em
  `@game-guild/emception-ui` e na aplicacao ate revisao posterior.

Regras globais: [`README.md`](./README.md).

## Diagnostico atual

- os contratos de coding estao duplicados em
  `apps/web/src/lib/coding-assignment/types.ts` e
  `packages/infrastructure/ui-emception/src/assessment/types.ts`;
- `@game-guild/emception-ui` possui seus proprios DTOs, sessao, presets,
  storage e React e deve permanecer inalterado por este programa;
- o adapter generico TypeScript de grading foi desenhado inicialmente para
  avaliacao deterministica sincrona, enquanto code depende de execucao externa;
- pesos de testes de code usam numeros fracionarios em partes do contrato
  atual, embora o grading academico use inteiros escalados;
- nomes, formatos e visibilidade de testes publicos e privados precisam ser
  fechados antes de implementar os novos packages e extrair o backend.

## Escopo

- contratos autorais e learner-safe de coding;
- parsing, validacao e serializacao versionada;
- projecao publica e remocao de answer keys/testes privados;
- envelope de resposta learner;
- resultado tecnico normalizado de execucao;
- traducao para `GradeResultV1` e evidencias genericas;
- classificacao de capabilities e review methods suportados;
- fixtures de conformidade consumiveis por TypeScript e C#.

## Fora do escopo

- React, Monaco, terminal, canvas ou layout;
- API routes, EF, banco, fila ou worker concreto;
- mover, editar, consolidar, depreciar ou remover
  `packages/infrastructure/ui-emception`;
- mudancas em `tools/emception/**`;
- novas linguagens;
- Code Studio e Lexical;
- publicacao, gradebook ou autorizacao de rotas.

## Contratos alvo

### `@game-guild/coding`

O package deve expor somente formas de dominio e funcoes puras. A definicao
autoral precisa representar, no minimo:

| Conceito | Regra |
| --- | --- |
| schema | possui versao explicita e parser fail-closed |
| toolchain | identidade estavel, nao um objeto privado do Emception |
| arquivos | path normalizado, encoding, conteudo, mutabilidade e visibilidade |
| testes | ID estavel, tipo, visibilidade, peso inteiro e expectativa |
| limites | valores inteiros para tempo, memoria, output e quantidade |
| entry point | path explicito ou regra deterministica validada |
| submission | somente arquivos permitidos e metadados versionados |
| report | outcomes tecnicos, sem declarar nota oficial por conta propria |

`Public` e `Private` fazem parte do contrato de seguranca. A projecao learner
nao pode depender de a UI esconder campos; o objeto entregue ao learner ja deve
estar redigido.

Pesos autorais de testes serao inteiros positivos (`weightUnits`). Eles
representam proporcao relativa, nao pontos com casas decimais. A alocacao do
`maxScore` em unidades inteiras deve ser deterministica, conservar exatamente o
total e usar ID estavel como desempate de restos. A formula sera documentada e
coberta por fixtures. O formato preservado em `emception-ui` nao sera removido
nem reescrito por esta fase.

### `@game-guild/grading-adapter-code`

O package depende apenas de APIs publicas de `@game-guild/grading` e
`@game-guild/coding`. Ele deve possuir:

- identificadores de adapter, handler, payload e versao;
- projecao de uma definicao de coding para itens de grading;
- geracao da entrega learner-safe;
- encode/decode do envelope de resposta;
- mapeamento do report tecnico para items, score, feedback e evidencias;
- fixtures e testes de conformidade;
- classificacao de suporte a `AutomatedReview` e `InstructorReview`.

O adapter nao abre worker, nao executa processo, nao conhece Next.js e nao
importa Emception. Um report vindo de executor externo e entrada do adapter; o
adapter nao torna o browser autoridade academica.

## Execucao assincrona

Code nao sera encaixado artificialmente em `evaluateDeterministic` sincrono.
Antes da implementacao, deve ser registrada uma decisao entre duas formas:

| Opcao | Uso |
| --- | --- |
| adapter puro + executor somente no servidor | preferida se o browser apenas projeta contratos |
| porta generica assincrona em grading | usada somente se houver consumidor real independente de code |

Se uma porta assincrona for necessaria, ela deve falar em submission,
capability, report e cancellation genericos. Ela nao pode citar source files,
compiler, toolchain ou tests. O adapter de quiz continua usando sua avaliacao
deterministica atual sem wrapper de compatibilidade desnecessario.

## Etapas

### `CODE-PKG-01`. Caracterizar o contrato existente

Implementacao:

- inventariar todos os imports das duas definicoes atuais;
- registrar cada sobreposicao conhecida em
  [`DUPLICATION-REGISTER.md`](./DUPLICATION-REGISTER.md);
- capturar fixtures validas e invalidas atualmente aceitas;
- registrar versoes `1`, `2` e `3` que ja possam aparecer em snapshots ou
  manifests backend;
- registrar diferencas reais entre DTO web, definicao backend e plano do
  Emception;
- adicionar testes que comprovem redacao de testes privados e normalizacao de
  paths antes da extracao.

Gate:

- nenhum campo e removido sem classificacao `dominio`, `UI`, `execucao` ou
  `obsoleto comprovado`;
- fixtures nao contem segredo, token ou answer key real;
- o baseline falha de forma reproduzivel para payloads malformados.

### `CODE-PKG-02`. Fechar o contrato numerico e versionado

Implementacao:

- definir schemas de authoring, delivery, response e execution report;
- substituir peso fracionario por unidades inteiras no novo contrato;
- definir limites, overflow, arredondamento e distribuicao de score;
- definir IDs estaveis de arquivo, teste, item e evidencia;
- documentar quais campos podem aparecer para author, learner e grader;
- definir a politica de leitura das versoes congeladas existentes.

Gate:

- nenhuma fonte academica de verdade usa ponto flutuante;
- soma dos scores de teste conserva exatamente o `maxScore` capturado;
- parser de versao desconhecida falha fechado;
- projecao learner nao inclui testes privados nem expectativa privada.

### `CODE-PKG-03`. Criar `@game-guild/coding`

Implementacao:

- criar package em `packages/features/coding` seguindo os manifests de features;
- implementar tipos, schemas, parsers, normalizadores e projetores puros;
- exportar subpaths somente quando houver ownership claro;
- manter fixtures em um export de testing separado;
- proibir dependencias de React, Next.js, Emception e grading.

Gate:

- package compila e testa isoladamente;
- API publica possui teste de exports;
- uma busca por imports proibidos retorna vazia;
- roundtrip preserva a forma canonica e rejeita dados ambiguos.

### `CODE-PKG-04`. Criar `@game-guild/grading-adapter-code`

Implementacao:

- criar package em `packages/features/grading-adapter-code`;
- implementar as traducoes puras para os contratos genericos;
- manter detalhes de code em evidencias versionadas, nunca nos value objects de
  grading;
- definir comportamento de resultado parcial, erro tecnico e item nao avaliado;
- impedir que falha do executor seja convertida silenciosamente em score zero;
- validar paridade das fixtures com a implementacao backend.

Gate:

- `@game-guild/grading` nao importa coding nem o adapter;
- `@game-guild/coding` nao importa grading;
- somente o adapter conhece os dois dominios;
- erros de infraestrutura, resposta invalida e falha do teste possuem estados
  distintos.

### `CODE-PKG-05`. Adotar os contratos nos novos consumidores de grading

Implementacao:

- usar os novos exports somente em `grading-adapter-code`, `coding-surface` e
  nos fluxos de grading migrados posteriormente;
- nao alterar imports, tipos ou testes internos de `@game-guild/emception-ui`;
- copiar parsers ou mapeamentos necessarios em vez de transformar o package
  preservado em dependencia compartilhada;
- registrar origem, destino e divergencia de cada copia;
- manter as duas implementacoes independentes ate a revisao com o responsavel
  pelo Emception.

Gate:

- toda duplicacao intencional aparece no registro;
- o diff de `packages/infrastructure/ui-emception/**` permanece vazio;
- os consumidores existentes do package preservado continuam cobertos por seus
  testes atuais;
- nenhuma rota ou componente precisou importar internals do novo package.

## Validacao

Os packages novos devem fornecer `test` e `typecheck`, permitindo:

```bash
pnpm --filter @game-guild/coding test
pnpm --filter @game-guild/coding typecheck
pnpm --filter @game-guild/grading-adapter-code test
pnpm --filter @game-guild/grading-adapter-code typecheck
pnpm --filter @game-guild/grading test
pnpm --filter @game-guild/grading typecheck
```

Tambem executar os testes web diretamente afetados pela migracao de contratos.
O build amplo nao substitui os testes de redacao e roundtrip.

## Criterios de conclusao

- existe uma fonte TypeScript autoritativa para os novos fluxos de grading;
- grading continua sem dependencia de code;
- o adapter de code e testavel sem browser e sem executor concreto;
- pesos e scores academicos usam inteiros;
- contratos privados nao chegam ao learner;
- fixtures permitem verificar paridade TypeScript/C#;
- `@game-guild/emception-ui` permanece inalterado e independente;
- duplicacoes conhecidas estao registradas para revisao futura;
- nenhuma tabela, migration ou fonte do Emception foi alterada.
