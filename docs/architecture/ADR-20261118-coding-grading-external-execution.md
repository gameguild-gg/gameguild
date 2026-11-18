# ADR: Adapter puro de coding com execucao autoritativa externa

- Status: Aceito
- Data: 2026-11-18
- Escopo: Coding Assessments e Grading TypeScript

## Contexto

O contrato TypeScript `AssessmentTypeAdapterV1` possui uma operacao
`evaluateDeterministic` sincrona adequada ao quiz. Coding, por outro lado,
depende de toolchain congelada, isolamento, limites operacionais, cancelamento
e um executor confiavel. Fazer o browser produzir a nota oficial ou encaixar
um worker externo em uma funcao sincrona tornaria o contrato enganoso.

O backend ja preserva as versoes congeladas `1`, `2` e `3` do adapter de code.
Snapshots que referenciam essas versoes precisam continuar executaveis e nao
podem ser reinterpretados por um novo contrato TypeScript.

## Decisao

`@game-guild/grading-adapter-code` e um adapter puro e usa a versao `4` para os
novos contratos. Ele:

1. projeta o authoring versionado para o snapshot privado de grading;
2. produz uma delivery learner-safe;
3. valida e normaliza somente arquivos permitidos da resposta;
4. recebe um `CodingExecutionReportV1` produzido por executor confiavel no
   servidor;
5. traduz o report em `GradeResultV1` e evidencia versionada.

O package nao abre worker, processo, fila ou conexao e nao importa Emception.
O browser pode executar previews, mas seu report nao e autoridade academica.

Nao sera adicionada nesta fase uma porta assincrona ao core TypeScript de
grading. A necessidade atual e especifica da composicao servidor de coding e
ja pode ser atendida mantendo o report como entrada do adapter puro. Uma porta
generica futura exige outro consumidor real e deve usar somente submission,
capability, report e cancellation, sem vocabulario de code.

As versoes backend `1`, `2` e `3` permanecem congeladas. A versao `4` nao e
fallback para snapshots anteriores; sua adocao backend pertence ao plano
`02-backend-code-adapter-boundary.md`.

## Semantica de falha

- teste executado com resultado `failed` e resultado tecnico valido e pode
  contribuir zero para sua parcela da nota;
- report parcial, timeout, cancelamento ou falha do executor permanecem
  pendentes e nunca viram nota zero silenciosamente;
- resposta learner invalida e rejeitada antes da execucao;
- report completo sem todos os testes congelados e invalido;
- toolchain divergente da projecao congelada e rejeitada.

## Consequencias

- grading continua independente de code;
- quiz preserva seu adapter deterministico atual;
- a distribuicao academica usa somente inteiros e conserva o `maxScore`;
- worker e backend podem evoluir sem levar detalhes de toolchain ao core;
- a fase backend precisa implementar paridade com as fixtures publicadas pelos
  packages novos.

## Alternativas rejeitadas

- executar code no browser como autoridade oficial;
- fazer `evaluateDeterministic` disparar trabalho externo escondido;
- adicionar uma porta assincrona generica sem segundo consumidor;
- reutilizar a versao `3` e reinterpretar snapshots congelados;
- importar contratos privados do Emception no adapter de grading.
