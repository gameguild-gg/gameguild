# ADR: Retenção, auditoria e regrade de grading

- Status: Aceito
- Data: 2026-09-03
- Escopo: Learning Assessments e Grading

## Contexto

Regrade e auditoria exigem reproduzir o que foi efetivamente apresentado e
avaliado. Limpeza prematura de revisão, manifest, entrega ou resposta quebra
essa garantia. Test runs, porém, não são registros acadêmicos.

## Decisão

Na primeira versão não existe hard delete automático de revisões publicadas,
execuções oficiais, rodadas, respostas, evidências, releases ou auditoria. A
retenção acompanha o registro acadêmico e qualquer política futura de descarte
exigirá ADR e operação explícitos.

Revisões candidatas não publicadas e test runs terminais podem ser removidos
após 30 dias somente quando não forem referenciados por execução não terminal,
candidata atual ou ação de publicação. A limpeza é um caso de uso auditado e
idempotente; não faz parte da Parte 1.

Versões executáveis permanecem no artefato enquanto forem referenciadas por
revisão ativa, execução não terminal ou execução oficial elegível a regrade.
Retirada exige preflight limpo e preservação de um artefato de rollback.

Outbox concluída e receipts podem ser compactados depois de 30 dias, desde que
o evento de auditoria acadêmica correspondente já esteja persistido e nenhuma
entrega permaneça pendente. Outcomes de idempotência acadêmica não são apagados
automaticamente na primeira versão; outcomes exclusivamente autorais e
terminais usam retenção mínima de 30 dias.

Regrade sempre cria nova rodada sobre revisão, manifest, entrega e respostas
originais. Rodadas anteriores nunca são alteradas ou ocultadas.

## Consequências

- a primeira entrega privilegia reprodutibilidade sobre economia de storage;
- limpeza de test run não afeta gradebook ou histórico acadêmico;
- política futura de privacidade/expurgo precisará conciliar obrigação legal e
  integridade acadêmica explicitamente.

## Alternativas rejeitadas

- sobrescrever resultado anterior;
- retirar versão executável após deploy;
- aplicar TTL genérico a registros acadêmicos.
