# 4.4. Browser por persona

Status: planejado; depende de `CODE-E2E-03`.

ID: `CODE-E2E-04`.

## Objetivo

Provar a jornada real em browser, com sessoes independentes, para os dois tipos
de exercicio e para os estados retido e liberado do resultado.

Partir da superficie aceita na 3.6 e mapear os requisitos `CS-REF-*` aos casos
do catalogo. Reaproveitar fluxos representativos existentes e ampliar negativos
e combinacoes; nao restaurar a UI anterior para preservar seletores antigos.

## Jornadas obrigatorias

Para `function` e `standard-io`:

1. instructor cria, valida, salva e reabre a definicao;
2. instructor executa author test sem criar efeito academico;
3. atividade e aprovada e publicada pelo fluxo oficial;
4. learner ve somente delivery publica e executa public tests;
5. learner envia a submission oficial;
6. automated review produz evidence e score quando configurado;
7. instructor revisa ou confirma o resultado quando configurado;
8. learner ainda nao ve resultado retido;
9. resultado e liberado;
10. refresh e nova sessao learner veem grade e feedback permitidos.

Casos negativos devem provar que outsider, learner sem enrollment e reviewer
sem grant nao obtêm acesso apenas porque conhecem a URL.

## Regras do harness

- Playwright usa contextos separados por persona;
- troca de persona nao ocorre por props ou localStorage;
- o runtime usa o artifact Emception schema 2 exato;
- observer, transport e runtime passam isoladamente antes da jornada;
- a suite pesada roda com um worker e nao concorre com outro build pesado;
- screenshot e trace sao retidos em falha e sanitizados;
- seletores usam papel, nome acessivel ou contrato estavel, nao detalhes
  incidentais do layout.

## Gate

- UI e API concordam sobre estado, score e release;
- author test nao aparece em submissions ou gradebook;
- private tests nunca aparecem no contexto learner;
- refresh e nova sessao observam o mesmo resultado liberado;
- falha de worker aparece como falha tecnica;
- ambos os exercise types passam sem fallback entre perfis.
