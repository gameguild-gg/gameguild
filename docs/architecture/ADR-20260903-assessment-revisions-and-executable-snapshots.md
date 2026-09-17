# ADR: Revisões imutáveis e snapshots executáveis de assessment

- Status: Aceito
- Data: 2026-09-03
- Escopo: Learning Assessments e Grading

## Contexto

O draft autoral de um assessment pode mudar depois de um teste. Além disso, um
deploy pode alterar handlers, projectors e algoritmos sem que o autor tenha
alterado o conteúdo. Uma tentativa ou regrade precisa continuar reproduzível
sem confundir mudança autoral com mudança do runtime.

## Decisão

O assessment mantém um draft mutável e referencia revisões imutáveis. Preparar
uma revisão materializa três contratos distintos:

1. `AssessmentAuthoringSourceV1`, contendo somente dados controlados pelo autor;
2. `AssessmentExecutionManifestV1`, contendo chaves e versões executáveis
   exatas;
3. `AssessmentExecutionSnapshotV1`, composto pelos dois contratos anteriores.

O snapshot tambem contem `itemProjections`, indexado por ID autoral. Cada
projecao e produzida no servidor pelo adapter versionado e congela o payload
privado necessario ao runtime. Ela nao retorna ao draft e nao constitui uma
segunda fonte mutavel.

`AuthoringSourceHash` é SHA-256 sobre JCS do primeiro contrato e é a única
identidade usada para `Published` e `ChangesPending`. `ExecutionSnapshotHash` é
SHA-256 sobre JCS do snapshot completo, incluindo as projecoes privadas, e
prova quais bytes e versões foram
testados ou executados. Ambos usam a versão `sha256-jcs-v1`.

O manifest fixa, por item e stage, projector, gerador de entrega,
decoder/normalizador, handler, algoritmo, policy e provider aplicáveis.
Prepare, publish, start e regrade resolvem as versões exatas, sem fallback para
a versão mais recente.

Cada `GradingExecution` materializa uma única
`AssessmentExecutionDeliveryV1`. A entrega possui `itemOrder` explícito, bytes
JCS persistidos e `DeliveryHash`; seed não substitui a saída concreta. Todo dado
privado necessário à correção inicial deve ser derivável da revisão imutável e
da entrega pública persistida.

Capabilities são declaradas por contexto. `AuthorTest` permite somente teste
do instrutor; `OfficialSubmission` é exigida separadamente para publicar e
executar academicamente. Health transitório impede execução, mas não muda uma
revisão já preparada.

## Consequências

- editar o draft nunca altera uma revisão preparada;
- mudança somente no catálogo executável não cria `ChangesPending`;
- versões referenciadas não podem ser retiradas do artefato enquanto forem
  necessárias para execução, regrade ou rollback;
- tipos que exijam answer key privada aleatória adicional precisam de novo
  contrato e novo `SCHEMA-GATE`;
- o deploy executa preflight fail-closed antes de receber tráfego.

## Alternativas rejeitadas

- serializar entidades EF para calcular hashes;
- reconstruir o manifest durante publish ou start;
- persistir somente seed ou `jsonb` reserializável como prova da entrega;
- tratar capability de teste como capability acadêmica.
