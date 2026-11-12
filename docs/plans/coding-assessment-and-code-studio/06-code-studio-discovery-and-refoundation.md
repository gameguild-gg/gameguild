# Parte 6. Descoberta e eventual refundacao do Code Studio

Status: planejado; implementacao condicional.

## Objetivo

Decidir se um novo Code Studio deve existir e, somente depois, definir sua forma
como plugin de codigo para o Lexical baseado no Emception.

Esta fase nao parte da premissa de preservar o Code Studio antigo. Ela comeca
com perguntas de produto, auditoria tecnica e prototipos pequenos. O resultado
valido pode ser:

- nao criar um novo package;
- criar apenas uma integracao Lexical minima;
- criar `@game-guild/code-studio` com escopo aprovado;
- adiar ate uma API publica do Emception ou Lexical estar disponivel.

## Identidade do produto

Code Studio e uma experiencia embutida em conteudo Lexical para experimentar
codigo. Ele nao e um assessment do tipo LeetCode e nao produz grade. Exercicios
oficiais de codigo pertencem a `coding-surface` + `grading-adapter-code`.

Code Studio pode reutilizar capacidades do Emception e, quando fizer sentido,
contratos neutros de `@game-guild/coding`. Ele nao reutiliza workflow academico,
private tests, submission, review ou gradebook.

## Restricoes

- o diretorio antigo em `block-content-editor` e referencia opcional;
- o desbloqueio preparatorio do
  [`plano 00`](./00-legacy-code-studio-build-unblock.md) pode substituir e
  remover a dependencia da Codingame para restaurar o ambiente web, mas nao
  autoriza apagar node, JSON, UI ou consumidores do Code Studio antigo;
- nenhuma collection, asset, runner, layout ou schema antigo e requisito;
- nao recriar editor, filesystem, terminal, canvas ou toolchain ja fornecidos
  pelo Emception;
- nao alterar `tools/emception/**` sem concluir o baseline 5.0 e o subplano da
  linguagem correspondente;
- mesmo depois dessas aprovacoes, solicitar somente capabilities aditivas; o Code
  Studio deve se adaptar ao Emception e nao mudar defaults ou retirar recursos
  existentes;
- nao alterar `@game-guild/emception-ui` nem `@game-guild/coding-surface` para
  acomodar Code Studio sem uma decisao propria;
- nao fazer `lexical-surface` importar uma feature concreta;
- nao introduzir compatibilidade legacy sem provar dados reais que a exijam;
- nao iniciar UI final antes de fechar casos de uso e persistencia.

## Pre-requisitos

- partes 1 a 4 concluidas ou estaveis o suficiente para nao confundir assessment
  com studio;
- baseline comum e decisao dos subplanos de linguagem pertinentes da parte 5;
- inventario atual de consumidores do Code Studio antigo;
- disponibilidade do responsavel de produto para responder ao discovery.

## `DISCOVERY-GATE`: perguntas obrigatorias

Estas perguntas devem ser apresentadas novamente quando esta fase for iniciada.
Respostas antigas podem ser contexto, mas nao substituem confirmacao.

| Tema | Pergunta a fechar |
| --- | --- |
| usuario | quem cria e quem executa um bloco de Code Studio? |
| objetivo | demonstracao, playground, tutorial guiado ou outro caso? |
| autoria | quais controles o autor realmente precisa configurar? |
| leitura | o aluno pode editar, executar, resetar, baixar ou apenas observar? |
| persistencia | quais dados pertencem ao JSON Lexical e quais sao estado local? |
| runtime | workspace deve sobreviver a refresh, usuario ou dispositivo? |
| seguranca | codigo e executado somente no browser ou tambem no servidor? |
| linguagens | quais linguagens sao necessarias na primeira entrega? |
| saida | terminal, canvas, preview web, banco ou combinacao? |
| compartilhamento | existe necessidade real de assets, collections ou templates? |
| colaboracao | edicao colaborativa ou autosave fazem parte do primeiro corte? |
| acessibilidade | qual experiencia read-only, teclado e mobile e exigida? |
| compatibilidade | existe conteudo antigo real que precisa abrir? |
| ownership | qual package possui node, serializacao, UI e runtime config? |

O gate falha se respostas forem deduzidas da implementacao antiga.

## Etapas de descoberta

### `STUDIO-DISC-01`. Inventariar consumidores e dados reais

Implementacao:

- localizar imports, nodes, serializers e rotas do Code Studio antigo;
- separar uso produtivo, demo, teste e codigo morto;
- verificar se existe JSON persistido real e qual versao;
- localizar usos pontuais de runners antigos por outros componentes;
- nao mover codigo ainda.

Gate:

- consumidores e dados reais estao comprovados por referencia;
- ausencia de consumidor nao e confundida com autorizacao para apagar;
- qualquer compatibilidade proposta possui dado concreto que a justifica.

### `STUDIO-DISC-02`. Responder ao questionario de produto

Implementacao:

- conduzir as perguntas do `DISCOVERY-GATE`;
- escrever casos de uso e nao-requisitos;
- priorizar um primeiro corte pequeno;
- definir metricas de aceite humano;
- registrar decisoes pendentes sem inventar defaults irreversiveis.

Gate:

- personas, tarefas e dados persistidos estao definidos;
- grading e explicitamente fora do produto;
- collections/assets somente entram se houver caso aprovado.

### `STUDIO-DISC-03`. Auditar Emception e Lexical

Implementacao:

- mapear APIs publicas do Emception que atendem ao corte;
- tratar `emception-ui` e `coding-surface` apenas como referencias; qualquer
  codigo copiado entra no registro de duplicacoes;
- mapear como `lexical-surface` registra nodes e plugins;
- verificar se existe extension point publico para feature externa;
- provar serializacao/deserializacao sem private imports;
- registrar gaps em seus respectivos owners.

Gate:

- nenhuma proposta exige editar internals do Emception;
- nenhuma proposta remove, substitui ou reconfigura uma capability existente
  do Emception;
- `lexical-surface` continua generico;
- se faltar extension point Lexical, abrir plano separado antes de implementar.

### `STUDIO-DISC-04`. Prototipar sem compromisso de API

Implementacao:

- criar prototipo descartavel do menor caso aprovado;
- medir bundle, memoria, tempo de startup e comportamento em mais de um bloco;
- testar editor, read-only, run, reset e teardown conforme o escopo;
- avaliar SSR/hydration e import browser-only;
- comparar composicao direta com necessidade real de package.

Gate:

- prototipo nao vira API publica por acidente;
- multiplos blocos nao compartilham workspace indevidamente;
- recursos sao liberados ao desmontar;
- a solucao usa Emception em vez de duplicar suas capacidades.

### `STUDIO-DISC-05`. Registrar a decisao de arquitetura

Implementacao:

- escolher entre nenhuma feature, plugin minimo ou package dedicado;
- definir ownership de node, JSON, UI e configuracao;
- definir o contrato inicial e o que fica explicitamente fora;
- decidir estrategia para qualquer dado antigo comprovado;
- estimar fases e validacoes da implementacao aprovada.

Gate:

- decisao foi aprovada por produto e owners tecnicos;
- package novo so existe se remover complexidade real;
- escopo nao e uma copia do Code Studio antigo.

## Implementacao condicional

Somente se `STUDIO-DISC-05` aprovar `@game-guild/code-studio`:

### `STUDIO-IMPL-01`. Contrato e package minimo

- criar package em `packages/features`;
- usar apenas APIs publicas do Emception e extension point publico do Lexical;
- manter JSON pequeno, versionado e independente de estado efemero do workspace;
- nao depender de grading, assessments ou app routes;
- adicionar teste de exports e limites de dependencia.

Gate: package isolado compila, testa e nao duplica capacidades do Emception.

### `STUDIO-IMPL-02`. Node e serializacao Lexical

- registrar o node por composicao externa;
- implementar import/export e roundtrip;
- definir placeholder e comportamento quando runtime nao estiver disponivel;
- preservar documento mesmo se a feature falhar ao carregar;
- impedir que estado local altere silenciosamente o JSON autoral.

Gate: roundtrip, copy/paste, read-only e documentos com multiplos blocos passam.

### `STUDIO-IMPL-03`. UX aprovada

- construir somente os controles definidos no discovery;
- manter UI do Emception onde ela ja resolve a tarefa;
- adicionar comandos GameGuild apenas quando pertencerem ao produto;
- validar teclado, foco, resize, mobile e teardown;
- nao reintroduzir collections/assets por conveniencia.

Gate: testes humanos aprovam as tarefas definidas, sem exigir conhecimento da
implementacao antiga.

### `STUDIO-IMPL-04`. Cutover opcional

- migrar consumidores confirmados;
- implementar compatibilidade somente para dados comprovados e aprovados;
- remover o Code Studio antigo depois de busca e testes;
- manter runners antigos apenas se outro owner ainda os consumir;
- documentar qualquer divida explicitamente aceita.

Neste item, "remover o Code Studio antigo" se limita ao codigo e aos
consumidores comprovadamente pertencentes ao Code Studio legado. Nunca inclui
codigo, packages, exports, toolchains, testes ou consumidores do Emception ou
de `@game-guild/emception-ui`.

Gate: nenhum consumidor ou dado real e perdido; nao permanece facade permanente.

## Validacao condicional

```bash
pnpm --filter @game-guild/code-studio test
pnpm --filter @game-guild/code-studio typecheck
pnpm --filter @game-guild/lexical-surface test
pnpm --filter @game-guild/web test:browser:block-editor
pnpm --filter @game-guild/web build
```

Os dois primeiros comandos so existirao se o package for aprovado. A suite deve
incluir roundtrip JSON, documento read-only, multiplos blocos, teardown,
acessibilidade e ausencia de efeitos academicos.

## Criterios de conclusao

A fase deliberativa termina quando existe uma decisao aprovada, mesmo que seja
nao implementar. Se houver implementacao, ela termina apenas quando:

- Code Studio possui identidade separada de coding assessment;
- usa Emception por APIs publicas;
- integra Lexical por extension point publico;
- JSON e ownership estao documentados;
- apenas requisitos validados foram implementados;
- consumidores e dados antigos foram tratados por evidencia, nao por receio;
- nenhuma alteracao de schema backend ou grading foi introduzida.
