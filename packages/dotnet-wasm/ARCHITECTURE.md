# .NET C# Runner - Architecture Documentation

## Overview

O **dotnet-web** é um compilador e runtime C# completo rodando 100% no navegador usando .NET 8 WebAssembly. Ele permite compilar e executar código C# dinamicamente sem qualquer servidor backend.

## Arquitetura

### Componentes Principais

```
dotnet-web/
├── dotnet-runtime/          # Projeto .NET 8 que compila para WASM
│   ├── Program.cs           # Wrapper do Roslyn com JSExport
│   ├── main.js              # Entry point JavaScript
│   └── RoslynWrapper.csproj # Configuração do projeto
├── public/managed/          # Arquivos WASM compilados (~50MB)
│   ├── *.wasm               # Assemblies .NET em formato Webcil
│   ├── dotnet.js            # Runtime .NET 8
│   ├── blazor.boot.json     # Manifest do Blazor
│   └── main.js              # Entry point copiado
├── src/                     # Integração TypeScript (Code Studio)
└── build-dotnet.sh          # Script de build
```

### Stack Tecnológico

1. **.NET 8 SDK** (`browser-wasm` runtime)
   - Compila C# para WebAssembly
   - Usa Blazor framework internamente
   - Output em formato Webcil (.wasm em vez de .dll)

2. **Roslyn 4.9.2** (Microsoft.CodeAnalysis.CSharp)
   - Compilador C# completo
   - ~9MB de assemblies
   - Suporta C# 12

3. **Basic.Reference.Assemblies.Net80**
   - Fornece referências de assembly em memória
   - Resolve o problema de `Assembly.Location` vazio em WASM
   - Inclui todas as APIs do .NET 8

4. **Blazor WebAssembly**
   - Carrega e inicializa o runtime .NET
   - Gerencia recursos e integridade (SHA256)
   - Fornece interop JavaScript via JSExport

## Fluxo de Execução

### 1. Build Time

```bash
./build-dotnet.sh
```

**Etapas:**

1. `dotnet publish -c Release -r browser-wasm`
   - Compila Program.cs com Roslyn
   - Gera 200+ arquivos .wasm (assemblies)
   - Cria blazor.boot.json com hashes SHA256
   - Output: `bin/Release/net8.0/browser-wasm/AppBundle/_framework/`

2. Copia recursivamente `_framework/*` → `public/managed/`
   - Inclui supportFiles/, locale folders (cs/, de/, es/, etc.)
   - Preserva estrutura de diretórios

3. Copia `main.js` do source para `public/managed/`

**Resultado:** ~50MB de arquivos WASM prontos para servir

### 2. Runtime Initialization

```html
<script type="module" src="/managed/main.js"></script>
```

**Fluxo:**

1. **main.js** importa `dotnet.js`
   ```javascript
   import { dotnet } from './dotnet.js'
   ```

2. **dotnet.create()** inicializa .NET runtime
   - Carrega `dotnet.native.wasm` (2.8MB runtime)
   - Lê `blazor.boot.json` para listar assemblies
   - Valida SHA256 de cada arquivo
   - Carrega assemblies sob demanda (lazy loading)

3. **getAssemblyExports()** acessa funções C# com [JSExport]
   ```javascript
   const exports = await getAssemblyExports('RoslynWrapper.dll');
   window.CSharpCompiler = {
       compileAndRun: exports.RoslynWrapper.Program.CompileAndRun
   };
   ```

4. Console.log "C# Compiler initialized and ready!"

### 3. Code Compilation & Execution

```javascript
window.CSharpCompiler.compileAndRun(userCode)
```

**Fluxo em Program.cs:**

1. **Inicialização (lazy)**
   ```csharp
   InitializeReferences()
   // Carrega Net80.References.All (Basic.Reference.Assemblies)
   // ~160 referências de assembly em memória
   ```

2. **Parse**
   ```csharp
   var syntaxTree = CSharpSyntaxTree.ParseText(code);
   ```

3. **Compilação**
   ```csharp
   var compilation = CSharpCompilation.Create(
       "DynamicAssembly",
       new[] { syntaxTree },
       references, // Net80.References.All
       new CSharpCompilationOptions(
           OutputKind.ConsoleApplication,
           concurrentBuild: false  // CRÍTICO: evita threading em WASM
       )
   );
   ```

4. **Emit para memória**
   ```csharp
   using var ms = new MemoryStream();
   EmitResult result = compilation.Emit(ms);
   ```

5. **Carregar assembly dinamicamente**
   ```csharp
   var assembly = Assembly.Load(ms.ToArray());
   ```

6. **Encontrar e invocar Main()**
   ```csharp
   var type = assembly.GetType("Program");
   var method = type.GetMethod("Main", BindingFlags.Static | ...);
   ```

7. **Capturar Console.WriteLine**
   ```csharp
   var oldOut = Console.Out;
   using var sw = new StringWriter();
   Console.SetOut(sw);
   method.Invoke(null, null);
   Console.SetOut(oldOut);
   return sw.ToString(); // Retorna output
   ```

## Decisões Técnicas Críticas

### Por que Blazor?

**.NET 8 browser-wasm é Blazor-centric:**
- Não funciona standalone sem Blazor framework
- Requer `blazor.boot.json` para listar assemblies
- Usa SHA256 verification obrigatório
- Tentativas de usar dotnet.js manualmente falharam 15+ vezes

**Solução:** Aceitar Blazor, usar JSExport para expor funções.

### Por que Basic.Reference.Assemblies?

**Problema:** Em WASM, `Assembly.Location` retorna string vazia.
- Roslyn precisa de MetadataReferences para compilar
- Não podemos usar `MetadataReference.CreateFromFile()`
- Tentativas com `TryGetRawMetadata()` falharam (API não existe em WASM)

**Solução:** `Basic.Reference.Assemblies.Net80`
- Fornece `Net80.References.All` - referências em memória
- 160+ assemblies do .NET 8 como byte arrays
- Funciona perfeitamente com Roslyn

### Por que concurrentBuild: false?

**Problema:** Roslyn usa threads por padrão.
```
Cannot wait on monitors on this runtime.
at System.Threading.Monitor.ObjWait
at Microsoft.CodeAnalysis.CSharp.ClsComplianceChecker.WaitForWorkers()
```

**Solução:** Desabilitar builds concorrentes.
- Threading não funciona em browser WASM (single-threaded)
- `concurrentBuild: false` força compilação sequencial

### Por que PublishTrimmed: false?

**Problema:** Trimming remove metadados necessários para reflexão.
- Roslyn precisa de metadados completos
- Assembly.Load() precisa de tipos completos

**Solução:** Desabilitar trimming.
- Trade-off: 50MB em vez de ~10MB
- Mas funcionamento garantido

## Limitações Conhecidas

### 1. Tamanho (~50MB)
- Roslyn: ~9MB
- .NET Runtime: ~3MB
- BCL Assemblies: ~30MB
- Locale resources: ~8MB

**Possível otimização:** Remover locales não usados, trimming seletivo.

### 2. Performance
- Primeira compilação: ~2-3s (carrega references)
- Compilações seguintes: ~500ms-1s
- Roslyn não foi otimizado para WASM

### 3. APIs Não Suportadas
- Threading (Task.Run, Thread, Parallel)
- File I/O real (apenas in-memory)
- Network sockets diretos
- Algumas APIs Win32

### 4. Memória
- Each compilation consome ~5-10MB
- Assemblies dinâmicos não são GC'd facilmente
- Pode causar memory leaks em uso intenso

## Integração com Code Studio

```typescript
// src/runners/dotnet-runner.ts
export class DotNetRunner implements IRunner {
  async run(code: string): Promise<RunResult> {
    // Carrega /managed/main.js (se ainda não carregado)
    await this.ensureRuntime();
    
    // Chama função exportada
    const output = window.CSharpCompiler.compileAndRun(code);
    
    // Parse output
    if (output.startsWith('COMPILATION_ERROR')) {
      return { error: output };
    }
    return { output };
  }
}
```

## Comparação com Outras Abordagens

### Try .NET / .NET REPL
- **Vantagem:** Oficial da Microsoft
- **Desvantagem:** Requer servidor backend, não é 100% client-side

### Monaco Editor + Language Server
- **Vantagem:** Autocomplete avançado
- **Desvantagem:** Não compila/executa código, só valida sintaxe

### WebAssembly direto (sem Blazor)
- **Tentado:** 15+ abordagens diferentes
- **Resultado:** .NET 8 não suporta, requer Blazor

### Compiladores online (compiler.net, etc.)
- **Vantagem:** Menor bundle size (usa server)
- **Desvantagem:** Não funciona offline, requer backend

## Performance Metrics

### Build Time
- Clean build: ~30-60s
- Incremental build: ~5-10s

### Runtime Loading
- First load (cold): ~5-8s
- Cached load: ~1-2s
- Lazy loading: Assemblies carregados sob demanda

### Compilation
- Simple hello world: ~500ms
- Complex code (LINQ, generics): ~1-2s
- Very large files (>1000 lines): ~3-5s

## Troubleshooting

### "Cannot wait on monitors"
- **Causa:** Threading usado na compilação
- **Fix:** `concurrentBuild: false`

### "Assembly.Location is empty"
- **Causa:** WASM não tem filesystem
- **Fix:** Use `Basic.Reference.Assemblies`

### "SHA256 mismatch"
- **Causa:** Arquivo modificado após build
- **Fix:** Rebuild completo com `./build-dotnet.sh`

### "Module not found: main.js"
- **Causa:** Build script não copiou main.js
- **Fix:** Verificar se `cp main.js ../public/managed/` está no script

### "System.Object not defined"
- **Causa:** Referências vazias
- **Fix:** Verificar `Net80.References.All` está carregado

## Próximos Passos

### Otimizações Planejadas
1. **Lazy reference loading:** Carregar apenas refs usadas
2. **Assembly caching:** Cache de assemblies compilados
3. **Locale trimming:** Remover idiomas não usados (-5MB)
4. **Brotli compression:** Comprimir .wasm files (50MB → ~15MB)

### Features Futuras
1. **Multi-file support:** Múltiplos arquivos .cs
2. **NuGet packages:** Suporte a packages externos
3. **Debugging:** Source maps, breakpoints
4. **IntelliSense:** Autocomplete baseado em Roslyn

## Conclusão

O **dotnet-web** é uma implementação completa e funcional de um compilador C# no browser. Apesar de desafios como tamanho de bundle e limitações de threading, ele fornece uma experiência robusta para execução de código C# client-side.

A arquitetura aproveita:
- ✅ .NET 8 WASM oficial
- ✅ Roslyn completo (não um subset)
- ✅ Blazor para runtime management
- ✅ JSExport para interop limpo
- ✅ Basic.Reference.Assemblies para metadados

**Status:** ✅ **FUNCIONANDO** - C# compilation e execution 100% no navegador!
