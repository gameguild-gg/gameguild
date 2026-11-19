# 4.6. Aceite e integracao em develop

Status: planejado; depende de `CODE-E2E-05`.

IDs: `CODE-E2E-06`, `CODE-E2E-07`.

## Objetivo

Executar a validacao consolidada, revisar humanamente os reports e preparar o
PR integrado das partes 01 a 04 sem incluir expansao de linguagem ou Code
Studio.

## Implementacao

1. Executar `coding:validation:contracts`, `coding:validation:smoke` e
   `coding:validation:full` sobre a branch reconciliada com `develop`.
2. Executar builds warning-clean, suites backend afetadas e verificacoes de
   arquitetura/autorizacao exigidas pelo `AGENTS.md`.
3. Revisar cada report e promover o status canonico somente por decisao humana.
4. Validar o `DUPLICATION-REGISTER.md` contra o diff real.
5. Confirmar que os consumidores de grading migrados na parte 3 usam
   `coding-surface` e que os consumidores preservados continuam funcionando.
6. Mapear toda assertiva restante do runner anterior para case ID ou divida de
   harness explicita.
7. Registrar blockers de linguagem na parte 5 e questoes de Code Studio no
   plano correspondente, sem inclui-los neste aceite.
8. Revisar o diff para excluir trabalho alheio ao programa.
9. Abrir um unico PR para `develop` cobrindo as partes 01 a 04, com comandos,
   resultados, case IDs, riscos e blockers aceitos.

`workflow_dispatch` e draft PR podem ser usados antes para obter artifact e
evidencia de CI. Eles nao substituem o PR final integrado.

## Preservacao obrigatoria

- nao remover ou remodelar `@game-guild/emception-ui`;
- nao remover capabilities, toolchains ou consumidores de
  `tools/emception/**`;
- nao remover o coding assignment preservado por este programa;
- nao alterar schema, migration ou modulo de plataforma para fechar o harness;
- nao criar compatibilidade artificial com contratos intermediarios deste
  roadmap;
- nao incluir linguagens da parte 5 nem refundacao do Code Studio.

## Gate de aceite

- todos os casos obrigatorios estao `pass` ou possuem blocker externo aceito e
  documentado;
- `function` e `standard-io` passam pelo fluxo completo;
- validacao humana e automatica usam o mesmo lifecycle;
- nenhum aceite final depende de leitura direta do banco;
- toda duplicacao conhecida esta registrada;
- nao existem condicionais de code no core generico;
- artifact exato e browser E2E estao verdes;
- builds e suites obrigatorias passam na branch reconciliada;
- o diff do PR corresponde somente ao dominio, adapter, backend, surface,
  integracao e validacao aprovados nas partes 01 a 04.

## Correcao posterior

Implementacoes transitorias so podem ser removidas em trabalho separado com
evidencia de cobertura equivalente. Um bug encontrado pelo programa deve ser
corrigido no modulo owner e revalidado; nao reduzir o catalogo para obter
verde.

