# Alternative Approaches for Rust in Browser

Compilar mrustc para WASM é complexo e experimental. Aqui estão alternativas mais práticas:

## 1. Rust Playground API (Recomendado) ⭐

Use o backend oficial do Rust Playground.

### Implementação

```typescript
async function compileRust(code: string): Promise<{output: string, error?: string}> {
  const response = await fetch('https://play.rust-lang.org/execute', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      channel: 'stable',
      mode: 'debug',
      edition: '2021',
      crateType: 'bin',
      tests: false,
      code: code,
    })
  })
  
  const result = await response.json()
  return {
    output: result.stdout,
    error: result.stderr
  }
}
```

### Prós
- ✅ Funciona imediatamente
- ✅ Mantido pela equipe Rust
- ✅ Suporta todas as features do Rust
- ✅ Acesso a crates.io (dependências)

### Contras
- ❌ Requer conexão com internet
- ❌ Dependência de serviço externo
- ❌ Rate limiting

## 2. @runno/wasi com Rust Pré-compilado

Use WASI para executar binários Rust pré-compilados.

### Implementação

```typescript
import { WASI, File, Directory } from '@runno/wasi'

const wasi = new WASI()
const binary = await fetch('/rust-programs/hello.wasm').then(r => r.arrayBuffer())

const result = await wasi.start(binary, {
  args: [],
  env: {},
  stdin: '',
})
```

### Prós
- ✅ Execução local (offline)
- ✅ Rápido
- ✅ Você já usa @runno/wasi para outras linguagens

### Contras
- ❌ Não compila código - apenas executa binários pré-compilados
- ❌ Precisa compilar programas antecipadamente

## 3. Server-Side rustc

Execute rustc no seu próprio servidor.

### Implementação

```typescript
// Backend (Node.js)
app.post('/api/rust/compile', async (req, res) => {
  const { code } = req.body
  
  // Salvar código
  await fs.writeFile('/tmp/main.rs', code)
  
  // Compilar
  const { stdout, stderr } = await exec('rustc /tmp/main.rs -o /tmp/program')
  
  if (stderr) {
    return res.json({ error: stderr })
  }
  
  // Executar
  const { stdout: output } = await exec('/tmp/program')
  res.json({ output })
})
```

### Prós
- ✅ Controle total
- ✅ Sem limitações
- ✅ Pode usar features instáveis

### Contras
- ❌ Requer infraestrutura de servidor
- ❌ Custos de hosting
- ❌ Preocupações de segurança (sandbox necessário)

## 4. Hybrid: REPL Local + Compilação Remota

Execute código simples localmente, envie código complexo para API.

```typescript
class HybridRustRunner {
  async execute(code: string) {
    if (this.isSimple(code)) {
      // Interpreta localmente (mini-interpreter)
      return this.interpret(code)
    } else {
      // Compila remotamente
      return this.compileRemote(code)
    }
  }
  
  isSimple(code: string): boolean {
    // Sem imports, structs, etc.
    return !code.includes('use ') && !code.includes('struct ')
  }
}
```

## Recomendação

Para o Code Studio da GameGuild:

1. **Curto prazo**: Use **Rust Playground API** (opção 1)
   - Implementação rápida
   - Experiência completa do Rust
   
2. **Médio prazo**: Adicione **@runno/wasi** (opção 2) para exemplos offline
   - Compile exemplos comuns antecipadamente
   - Melhor experiência offline
   
3. **Longo prazo**: Se houver demanda, considere **servidor próprio** (opção 3)
   - Mais controle
   - Possibilidade de monetização

## Exemplo: Rust Playground Integration

Vou criar uma implementação usando Rust Playground como fallback:

```typescript
// src/rust/playground-api.ts
export class RustPlaygroundCompiler {
  async compile(code: string): Promise<RustResult> {
    try {
      const response = await fetch('https://play.rust-lang.org/execute', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          channel: 'stable',
          mode: 'debug',
          edition: '2021',
          crateType: 'bin',
          tests: false,
          code,
        })
      })
      
      if (!response.ok) {
        throw new Error(`Playground API error: ${response.statusText}`)
      }
      
      const result = await response.json()
      
      return {
        output: result.stdout,
        error: result.stderr || (result.success ? undefined : 'Compilation failed'),
        exitCode: result.success ? 0 : 1,
        executionTime: 0 // API não retorna tempo
      }
    } catch (error) {
      return {
        error: `Failed to compile: ${error instanceof Error ? error.message : String(error)}`,
        exitCode: 1,
        executionTime: 0
      }
    }
  }
}
```

Quer que eu implemente a integração com Rust Playground API?
