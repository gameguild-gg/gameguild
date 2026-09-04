# ADR: Sujeitos individuais e coletivos de assessment

- Status: Aceito
- Data: 2026-09-03
- Escopo: Learning Assessments, Enrollments e Groups

## Contexto

Uma atividade de grupo não deve ser transformada em várias submissions antes
do grading. Isso duplicaria execução, reviews e resultado e permitiria
divergência entre integrantes.

## Decisão

Uma submission representa exatamente um sujeito: enrollment individual ou
grupo. As referências são mutuamente exclusivas e relacionais. Submission
coletiva congela seus participantes no start e possui uma única resposta, uma
única `GradingExecution`, uma única rodada e um único resultado.

O subsistema de grupos resolve o sujeito e congela participantes antes do
grading. Depois da transição acadêmica canônica, projeta o mesmo resultado para
os participantes. O grading não recebe nem percorre a lista de membros.

Qualquer participante congelado pode editar e finalizar o draft coletivo na
policy inicial. Toda mutação usa versão esperada e envelope idempotente e
registra o ator real. `StartedByUserId` é auditoria, não propriedade exclusiva.

Test runs usam subjects sintéticos próprios. `PeerReview` de teste pode criar
vários subjects e personas, mas nunca cria enrollment ou submission oficial.
Persona representada e ator autenticado permanecem identidades distintas.

## Consequências

- entrada ou saída posterior do grupo não muda uma tentativa iniciada;
- fan-out de submission antes do grading é proibido;
- projeções posteriores são idempotentes por submission, rodada e enrollment;
- ajuste individual futuro pertence ao gradebook, não muta o resultado do
  grupo.

## Alternativas rejeitadas

- uma submission por integrante;
- `SubjectId` opaco sem integridade referencial;
- conceder controle exclusivo ao participante que iniciou a tentativa.
