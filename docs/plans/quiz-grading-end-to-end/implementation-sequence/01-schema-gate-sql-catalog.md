# Catalogo SQL do baseline atual

- Banco de origem: `gameguild_schema_inventory_20260903`
- Natureza: PostgreSQL descartavel, criado do zero pelas 130 migrations
- Data da captura: 2026-09-04
- Uso: comparar o baseline limpo da Parte 1, nunca preservar dados

## Resumo canonico

| Categoria | Quantidade | SHA-256 da representacao canonica |
| --- | ---: | --- |
| Colunas | 5.616 | `40d396ff8708f444e2f9814af230297dfa092402dcaf29e43f568ea6e21d075e` |
| Constraints | 1.008 | `31f97bd857754a7766c9e8925a761fcdcbe727232be1a9b331ab2acd4e9402b3` |
| Rotinas proprias | 127 | `d44d85826e330abcc5e3d0c4d7955ec922138ec2d89301e6da3bfc8e68f13d81` |
| Triggers nao internos | 46 | `c9deab8a0bebf5796744e7e967ba77ca3604f2fe280c059ec5f703ff4d030b5d` |
| Indices especiais | 41 | `d23fea3105116e0c46c34aeea7de426cdd058ffd17054cb65616b2afcc758d00` |
| Grants customizados | 1.376 | `25d4a24f42b4adf8c082e5b426a4c55e60e2cda4ae8b98410db027203d6576aa` |

Os hashes de rotinas, triggers e indices devem ser recalculados pela mesma
consulta de verificacao ao reconstruir o baseline. As categorias preservadas
precisam manter quantidade e definicao. Os dois triggers e as duas funcoes de
assessment sao a unica remocao aprovada e devem ser excluidos do hash esperado
pos-corte.

A consulta canonica esta em
[`01-schema-catalog-fingerprint.sql`](./01-schema-catalog-fingerprint.sql).

## Extensoes, schemas e roles

Extensoes instaladas:

- `plpgsql` 1.0;
- `pgcrypto` 1.3;
- `btree_gist` 1.7.

Schemas proprios/ativos:

- `public`;
- `assets`;
- `auth`;
- `economy_private`;
- `gameguild.authentication`;
- `gameguild.resources`;
- `gameguild.sla`;
- `resources`.

Roles de aplicacao instaladas pelo baseline:

- `gameguild_economy_migration`;
- `gameguild_economy_procedure_owner`;
- `gameguild_economy_runtime`;
- `gameguild_economy_writer`.

As roles e seus grants pertencem a Economy. Elas nao recebem permissao sobre as
novas tabelas academicas.

## Rotinas proprias

Das 127 rotinas, 125 pertencem a `economy_private`. Elas sao instaladas pela
sequencia de migrations Economy iniciada em
`20260719012556_PrepareEconomyPrivateSchema` e continuada pelos partials
`*.Security.cs`. Todas serao consolidadas no baseline sem mudanca de assinatura,
owner ou corpo.

As duas rotinas de `public` pertencem ao modelo antigo de assessments:

- `enforce_assessment_max_score()`;
- `enforce_assessment_submission_score()`.

Origem: `20260716160000_AddAssessmentIntegrityGuards.cs`. Ambas serao removidas.
Elas comparam submissions historicas ao `MaxScore` mutavel do draft e deixam de
ser semanticamente validas quando a execucao referencia revisao imutavel.

O conjunto Economy preservado inclui todas as versoes ativas destas familias:

- `activate_reserve_head`, `hydrate_posting_group_reserve_authorization`;
- `append_*_audit_event`, `append_*_evidence`;
- `create_*`, `prepare_*`, `transition_*`, `complete_*`;
- `post_*`, `reserve_*`, `confirm_*`, `observe_*`;
- `read_*` de bounty, payout, top-up e withdrawal;
- `validate_*`, `verify_*`, `guard_*` e `deny_immutable_mutation`;
- `economy_stamp_journal_hash_*`, `derive_economy_uuid`;
- `rebuild_wallet_projection` e `provision_economy_wallet`.

Versoes com nome `legacy` que ainda estao ativas em Economy nao fazem parte do
delta de grading. Elas serao preservadas sem julgamento neste corte; remove-las
exige decisao do owner de Economy.

## Triggers

Existem 46 triggers nao internos:

- 2 de assessment, nas tabelas `Assessments` e `AssessmentSubmissions`, que
  chamam as duas funcoes removidas acima;
- 44 de Economy, cobrindo imutabilidade, journal hash, lineage, reservas,
  provider facts, risk consumption, top-up, payout, withdrawal e guardas do
  cutover financeiro.

Os 44 triggers Economy e suas funcoes permanecem. O baseline de teste deve
exercitar pelo menos:

- rejeicao de update/delete em registro imutavel;
- stamp da cadeia do journal;
- conservacao de allocation/lineage;
- exclusao de sobreposicao de fragment reservation;
- guards de mutacao de top-up e wallet.

## Indices especiais

Os 41 indices parciais, de expressao ou GiST atuais estao distribuidos entre:

- Assets: 1;
- Authentication: 1;
- Billing/Payments: 4;
- Capability/security: 3;
- Content/Learning: 2;
- Economy: 17;
- Launch/Projects/Marketplace: 6;
- Social: 1;
- Testing Lab: 6.

Incluem a exclusion constraint GiST
`ex_economy_fragment_reservations_active_no_overlap` e os uniques parciais de
estado ativo de Economy, Testing Lab, Marketplace e Projects. Todos serao
reinstalados com a mesma definicao. Os novos indices de grading sao indices EF
declarativos e aparecem separadamente no diff aprovado.

## Views, policies e grants

- views proprias ativas: 0;
- policies RLS ativas: 0;
- grants customizados: 1.179 grants de tabela e 197 grants de rotina.

Os grants sao de Economy e variam por objeto. A comparacao pos-baseline usa a
tupla ordenada `(grantee, schema, object, privilege, grantable)`; comparar
somente a quantidade nao e suficiente.

## Ordem de reinstalacao

1. extensoes;
2. schemas;
3. roles;
4. tabelas, constraints e indices EF;
5. funcoes base sem dependencia circular;
6. funcoes de comando/leitura;
7. triggers;
8. indices concorrentes ou especiais;
9. owners e grants;
10. seeds estruturais indispensaveis;
11. testes funcionais e comparacao canonica do catalogo.

Operacoes que exigem `CREATE INDEX CONCURRENTLY` permanecem fora da transacao
do bloco principal, mas pertencem ao mesmo baseline de instalacao.

## Criterio de equivalencia

O banco novo e aceito somente quando:

- o modelo EF difere do inventario exclusivamente pelo delta aprovado;
- extensoes, schemas e roles preservados possuem mesma versao e owner;
- as 125 rotinas e os 44 triggers Economy possuem a mesma definicao canonica;
- os 41 indices especiais possuem a mesma definicao;
- os 1.376 grants customizados possuem as mesmas tuplas;
- testes funcionais provam os comportamentos criticos, alem da existencia dos
  nomes;
- as duas rotinas e os dois triggers antigos de assessment nao existem;
- nenhuma rotina, trigger, view, policy, role ou grant novo de grading aparece
  sem um novo gate.
