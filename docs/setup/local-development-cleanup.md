# Limpeza e recuperação do ambiente de desenvolvimento local

Este guia descreve como encerrar, diagnosticar e recuperar o ambiente iniciado
por `pnpm run dev`. Execute os comandos a partir da raiz do repositório.

## Como o ambiente local funciona

`pnpm run dev` inicia dois grupos de processos:

1. o Docker Compose mantém PostgreSQL, Redis, Garage e `garage-init`;
2. a máquina local executa API, web e os watchers do client gerado.

| Componente | Execução padrão | Porta local |
| --- | --- | --- |
| Web | processo local do Next.js | `3000` |
| API | processo local do .NET | `8080` |
| PostgreSQL | Docker Compose | `5432` |
| Redis | Docker Compose | `6379` |
| Garage S3 | Docker Compose | `3900` |
| Garage RPC, web e admin | Docker Compose | `3901` a `3903` |

Os containers `api` e `web` do Compose pertencem ao profile `app` e não são
iniciados pelo `pnpm run dev` normal. Não execute `pnpm run dev` e
`pnpm run dev:compose` ao mesmo tempo, pois ambos podem disputar as portas
`3000` e `8080`.

## Encerramento normal

No terminal que executa o ambiente, pressione `Ctrl+C` uma vez e aguarde a
mensagem `[dev] shutting down...`. O orquestrador encerra API, web e watchers.

Os containers de infraestrutura permanecem ativos intencionalmente para
acelerar a próxima inicialização. Para encerrar também esses containers:

```bash
pnpm run dev:stop
```

Esse comando:

- encerra os processos que escutam nas portas `3000` e `8080`;
- executa `docker compose down --remove-orphans`;
- preserva os volumes e, portanto, os dados locais.

Evite pressionar `Ctrl+C` duas vezes em sequência. A segunda interrupção força
a saída imediata e pode não dar tempo para o encerramento normal dos filhos.

## Recuperação recomendada

Quando uma execução anterior deixou processos órfãos, quando houve merge com
mudanças grandes ou quando `.next`, client gerado ou artefatos .NET ficaram
obsoletos, execute:

```bash
pnpm run dev:repair
pnpm run dev
```

`dev:repair` executa `dev:stop` e remove somente artefatos regeneráveis:

- `.turbo`;
- `apps/web/.next` e `apps/web/.turbo`;
- arquivos `tsconfig*.tsbuildinfo` de `apps/web`;
- `packages/infrastructure/client/dist` e seu cache do Turbo;
- diretórios `bin` e `obj` da API.

Ele não remove:

- `.env` ou configurações locais;
- `node_modules`;
- volumes do Docker;
- dados do PostgreSQL, Redis ou Garage;
- arquivos versionados.

A primeira inicialização após o reparo pode demorar mais porque a API, o client
e a web serão recompilados.

## Reset completo dos dados locais

Use esta opção somente quando os dados locais puderem ser descartados:

```bash
pnpm run dev:reset:data
pnpm run dev
```

Além da limpeza de artefatos, esse comando executa
`docker compose down --remove-orphans --volumes` e apaga os volumes locais
declarados pelo projeto, incluindo:

- banco PostgreSQL;
- estado do Redis;
- objetos do Garage;
- chaves locais de data protection.

Um erro de porta, cache ou compilação não exige reset do banco. Antes de apagar
os volumes por um erro de API, verifique os logs. O reset também não deve ser
usado para ocultar defeitos em migrations ou no modelo EF.

## Reinstalação de dependências

Se o erro indicar dependências ausentes ou inconsistentes depois de uma mudança
no lockfile, tente primeiro:

```bash
pnpm install --frozen-lockfile
```

Se houver evidência de instalação corrompida, faça a limpeza profunda:

```bash
pnpm run clean
pnpm install --frozen-lockfile
pnpm run dev
```

`pnpm run clean` remove `node_modules`, `dist`, `coverage`, `build`, `.next` e
`.turbo` em todo o monorepo. Ele é mais caro que `dev:repair` e não deve fazer
parte da rotina diária.

## Diagnóstico

### Containers

```bash
pnpm run dev:compose:ps
docker compose -f compose.yaml ps -a
docker compose -f compose.yaml logs --tail=200 postgres redis garage garage-init
```

No modo normal, a API roda localmente. Portanto, seus erros aparecem no terminal
de `pnpm run dev`, e não em `docker compose logs api`.

### Portas

```bash
lsof -nP -iTCP:3000 -sTCP:LISTEN
lsof -nP -iTCP:8080 -sTCP:LISTEN
lsof -nP -iTCP:5432 -sTCP:LISTEN
lsof -nP -iTCP:6379 -sTCP:LISTEN
lsof -nP -iTCP:3900 -sTCP:LISTEN
```

Para encerrar apenas os listeners locais da web e da API:

```bash
pnpm run kill:ports
```

O comando consulta cada porta separadamente, restringe a seleção a sockets TCP
em estado `LISTEN` e não chama `kill` quando não encontra PID. Ele usa `SIGKILL`
porque é destinado à recuperação de processos órfãos; prefira `Ctrl+C` para o
encerramento normal.

Não mate automaticamente processos nas portas de PostgreSQL, Redis ou Garage.
Primeiro identifique se a porta pertence aos containers deste projeto ou a
outro serviço local.

### Saúde da API e da web

```bash
curl -i http://localhost:8080/live
curl -i http://localhost:8080/health
curl -i http://localhost:3000/api/health
```

### Rede externa do Compose

O Compose espera a rede externa `web-development-public`. Se ela não existir:

```bash
docker network inspect web-development-public >/dev/null 2>&1 || \
  docker network create web-development-public
```

## Guia rápido por sintoma

| Sintoma | Primeira ação |
| --- | --- |
| `port 8080 is already in use` | `pnpm run dev:repair` |
| Next.js informa que já existe servidor em `3000` | `pnpm run dev:repair` |
| aviso de container órfão, como MailHog antigo | `pnpm run dev:stop` |
| tipos de rotas antigas em `.next/types` | `pnpm run dev:repair` |
| client gerado ou `dist` inconsistente | `pnpm run dev:repair` |
| dependências não correspondem ao lockfile | `pnpm install --frozen-lockfile` |
| instalação de dependências corrompida | `pnpm run clean`, reinstalar e iniciar |
| schema local descartável incompatível após análise dos logs | `pnpm run dev:reset:data` |
| Compose não encontra a rede externa | criar `web-development-public` |

## Comandos que devem ser evitados

Não use indiscriminadamente:

```bash
git clean -fdX
docker system prune -a --volumes
```

`git clean -fdX` remove tudo que o Git considera ignorado. Neste repositório,
isso pode incluir `.env` e diretórios de código mantidos fora do índice atual,
não apenas caches. Para apenas inspecionar o que seria removido, sem apagar:

```bash
git clean -ndX
```

`docker system prune -a --volumes` afeta todos os projetos da máquina. Para
este repositório, use os comandos `dev:stop` e `dev:reset:data`, que limitam a
operação ao Compose do Game Guild.

## Sequência padrão após problemas

Use esta ordem e avance somente se a etapa anterior não resolver:

1. `pnpm run dev:stop`;
2. `pnpm run dev:repair` e `pnpm run dev`;
3. inspecionar logs, portas e endpoints de saúde;
4. `pnpm install --frozen-lockfile` se houve mudança de dependências;
5. `pnpm run clean` apenas se a instalação estiver corrompida;
6. `pnpm run dev:reset:data` somente após confirmar que os dados podem ser
   descartados e que o problema está relacionado ao estado persistido local.
