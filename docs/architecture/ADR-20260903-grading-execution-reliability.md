# ADR: Execução, rodadas, idempotência e eventos de grading

- Status: Aceito
- Data: 2026-09-03
- Escopo: Learning Assessments e Grading

## Contexto

Test runs e submissions oficiais reutilizam o mesmo pipeline técnico, mas
possuem efeitos diferentes. Retries, concorrência e múltiplos consumidores não
podem duplicar nota, auditoria, release ou projeções.

## Decisão

`GradingExecution` é a raiz persistente comum e possui exatamente um owner
relacional: `AssessmentTestRunSubject` ou `AssessmentSubmission`. Não existe
`IsTest` em submission nem `ownerId` polimórfico sem FK.

Cada execução possui rodadas. Regrade cria uma rodada imutável que referencia a
anterior e reutiliza revisão, manifest, entrega e respostas originais. Avaliar
outra definição exige nova submission e nova execução.

Comandos mutáveis usam `IdempotentCommandEnvelopeV1`, escopado por tenant,
recurso, comando e ator, com chave, request hash canônico e outcome persistido.
Após autorização, mesma chave e mesmo hash retornam o outcome; mesma chave com
payload diferente produz conflito. Concorrência usa versão esperada.

Eventos acadêmicos são gravados em outbox na mesma transação da mudança de
estado. Eles não passam simultaneamente pelo publisher em processo de
`SaveChangesAsync`. O dispatch ocorre depois do commit. A rota de consumers é
congelada por evento e cada consumer obrigatório confirma sua entrega por
`(EventId, ConsumerKey)`; a mensagem só termina quando todos confirmam.

A conclusão comum da execução é neutra. `AuthorTest` persiste diagnóstico e
telemetria operacional. Somente o adapter `OfficialSubmission` grava
`GradeResultFinalized` e efeitos acadêmicos subsequentes.

## Consequências

- crash depois do commit e antes do dispatch não perde evento;
- retry não duplica efeitos ou auditoria;
- falha de um consumer não remove confirmações dos demais;
- adicionar consumer não reprocessa histórico implicitamente;
- o browser nunca possui autoridade para produzir resultado oficial.

## Alternativas rejeitadas

- cache em processo para idempotência;
- publicação acadêmica dentro de `SaveChangesAsync`;
- sobrescrever rodada durante regrade;
- reutilizar uma submission oficial para test run.
