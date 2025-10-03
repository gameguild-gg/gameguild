# Google Drive Integration - Troubleshooting Guide

## ✅ PROBLEMA RESOLVIDO: "idpiframe_initialization_failed"

### 🔧 **Problema Identificado e Corrigido**
```
Failed to load Google API: 
Object { error: "idpiframe_initialization_failed", details: "You have created a new client application that uses libraries for user authentication or authorization that are deprecated. New clients must use the new libraries instead. See the [Migration Guide](https://developers.google.com/identity/gsi/web/guides/gis-migration) for more information." }
```

### ✅ **Solução Implementada**
O problema foi resolvido migrando da **API deprecada** (`gapi.auth2`) para a **nova Google Identity Services (GIS)**:

**Antes (Deprecado):**
```javascript
// ❌ API antiga - causava erro
window.gapi.load('client:auth2', resolve)
window.gapi.auth2.getAuthInstance().signIn()
```

**Depois (Novo):**
```javascript
// ✅ Nova API - funciona corretamente
// 1. Carrega Google Identity Services
script.src = 'https://accounts.google.com/gsi/client'

// 2. Usa novo método de autenticação
const tokenClient = window.google.accounts.oauth2.initTokenClient({
  client_id: CLIENT_ID,
  scope: SCOPES,
  callback: (response) => { /* token */ }
})
```

### 🎯 **O que mudou na implementação:**
1. **Duas bibliotecas separadas**: GIS para auth + GAPI para Drive API
2. **Token-based auth**: Não usa mais sessões persistentes
3. **Callback pattern**: Resposta via callback em vez de Promise
4. **Manual token management**: App gerencia tokens diretamente

### 🚀 **Como testar se está funcionando:**
1. **Abra o console** (F12)
2. **Recarregue a página**
3. **Clique em "Configurar" no Google Drive**
4. **Deve abrir popup** do Google sem erros
5. **Após autorização**: Checkbox deve ficar marcado com ✓

### Diagnóstico Rápido
1. **Abra o Console do Navegador** (F12)
2. **Recarregue a página**
3. **Procure por erros** relacionados a:
   - `apis.google.com`
   - `CORS`
   - `Content Security Policy`
   - `undefined CLIENT_ID`

## Soluções por Ordem de Prioridade

### 🔧 1. Configurar Variáveis de Ambiente

**Problema**: Variáveis não definidas
```bash
# Verifique se existem
echo $NEXT_PUBLIC_GOOGLE_DRIVE_CLIENT_ID
echo $NEXT_PUBLIC_GOOGLE_DRIVE_API_KEY
```

**Solução**: Criar `.env.local`
```env
NEXT_PUBLIC_GOOGLE_DRIVE_CLIENT_ID=123456789-abcdefgh.apps.googleusercontent.com
NEXT_PUBLIC_GOOGLE_DRIVE_API_KEY=AIzaSyABCDEF123456789GHIJKLMNOP
```

### 🛡️ 2. Configurar Google Cloud Console

1. **Acesse**: https://console.cloud.google.com/
2. **Ativar APIs**: Google Drive API
3. **Criar Credenciais**:
   
   **OAuth 2.0 Client ID**:
   - Tipo: Aplicação Web
   - Origens autorizadas: `http://localhost:3000`, `https://seudominio.com`
   - URIs de redirecionamento: `http://localhost:3000`, `https://seudominio.com`
   
   **API Key**:
   - Restringir à Google Drive API
   - Restringir por referenciador (opcional)

### 🌐 3. Problemas de CORS/CSP

**Sintoma**: Erros de CORS no console

**Solução**: Adicionar ao `next.config.js`
```javascript
/** @type {import('next').NextConfig} */
const nextConfig = {
  async headers() {
    return [
      {
        source: '/:path*',
        headers: [
          {
            key: 'Content-Security-Policy',
            value: `
              script-src 'self' 'unsafe-inline' 'unsafe-eval' https://apis.google.com;
              connect-src 'self' https://*.googleapis.com https://www.googleapis.com;
              frame-src 'self' https://accounts.google.com;
            `.replace(/\n/g, ' ').trim()
          }
        ]
      }
    ]
  }
}

module.exports = nextConfig
```

### 🔄 5. Teste Rápido - Verificar se API Carrega

**Objetivo**: Testar se o Google API carrega corretamente

**Passos**:
1. **Abra o console do navegador** (F12)
2. **Cole este código** para testar:
```javascript
// Teste 1: Verificar se script carregou
console.log('GAPI disponível:', !!window.gapi)

// Teste 2: Tentar carregar API manualmente
if (!window.gapi) {
  const script = document.createElement('script')
  script.src = 'https://apis.google.com/js/api.js'
  script.onload = () => console.log('✅ Google API carregada com sucesso')
  script.onerror = () => console.log('❌ Falha ao carregar Google API')
  document.head.appendChild(script)
}

// Teste 3: Verificar credenciais
console.log('CLIENT_ID:', process.env.NEXT_PUBLIC_GOOGLE_DRIVE_CLIENT_ID || 'NÃO CONFIGURADO')
console.log('API_KEY:', process.env.NEXT_PUBLIC_GOOGLE_DRIVE_API_KEY || 'NÃO CONFIGURADO')
```

### 🚀 6. Solução de Emergência - Fallback Local

Se o Google Drive não funcionar, sempre use o **Local Storage** que está funcionando:

1. **Marque apenas "Local Storage"**
2. **Desmarque Google Drive** temporariamente  
3. **Use normalmente** - projetos ficam salvos localmente
4. **Configure Google Drive depois** quando resolver as credenciais

### 📋 Checklist de Resolução

#### ✅ Pré-requisitos
- [ ] Credenciais configuradas no `.env.local`
- [ ] Google Drive API ativada no Cloud Console
- [ ] Popup desbloqueado no navegador
- [ ] Internet funcionando

#### ✅ Diagnóstico Rápido
- [ ] Console sem erros de CORS
- [ ] `window.gapi` disponível após carregamento
- [ ] Variáveis de ambiente visíveis no código

#### ✅ Testes Funcionais  
- [ ] Consegue clicar em "Configurar" no Google Drive
- [ ] Popup de autenticação do Google abre
- [ ] Consegue selecionar conta Google
- [ ] Retorna com sucesso após autenticação

## 🆘 Suporte Rápido

**Se ainda não funcionar:**

1. **Use Local Storage** (sempre funciona)
2. **Teste em modo incógnito** (para descartar extensões)
3. **Verifique cotas da API** no Google Cloud Console
4. **Entre em contato** com logs específicos do console
4. **Problema na validação imediata do token**

### Soluções

#### 1. Verificar Configuração no Google Cloud Console

**Credenciais OAuth 2.0:**
- Acesse [Google Cloud Console](https://console.cloud.google.com/)
- Vá para "APIs & Services" > "Credentials"
- Clique no seu OAuth 2.0 Client ID
- Verifique:
  - **Authorized JavaScript origins**: 
    - `http://localhost:3000` (desenvolvimento)
    - `https://seu-dominio.com` (produção)
  - **Authorized redirect URIs**: Deixe vazio para aplicações JavaScript

#### 2. Verificar Variáveis de Ambiente

Certifique-se de que o arquivo `.env.local` contém:

```env
NEXT_PUBLIC_GOOGLE_DRIVE_CLIENT_ID=123456789-abc123.apps.googleusercontent.com
NEXT_PUBLIC_GOOGLE_DRIVE_API_KEY=AIzaSyABC123...
```

**Importante:** O Client ID deve terminar com `.googleusercontent.com`

#### 3. Verificar Permissões da API

No Google Cloud Console:
- Vá para "APIs & Services" > "Library"
- Certifique-se de que a "Google Drive API" está ativada
- Se não estiver, clique em "ENABLE"

#### 4. Teste com Debug Panel

Use o painel de debug (visível apenas em desenvolvimento) para identificar problemas:

1. Abra o dialog de configuração do Google Drive
2. Expanda o "Google Drive Debug Panel"
3. Clique em "Refresh" para ver o status atual
4. Verifique se todos os itens estão com status "OK"

#### 5. Limpar Cache e Cookies

1. Abra as ferramentas de desenvolvedor (F12)
2. Vá para a aba "Application" ou "Storage"
3. Limpe:
   - Local Storage
   - Session Storage
   - Cookies do domínio
4. Recarregue a página

#### 6. Verificar Console do Navegador

1. Abra as ferramentas de desenvolvedor (F12)
2. Vá para a aba "Console"
3. Procure por erros relacionados ao Google API
4. Erros comuns:
   - `popup_closed_by_user`: Popup foi fechado manualmente
   - `popup_blocked_by_browser`: Popup foi bloqueado
   - `access_denied`: Usuário negou permissões
   - `idpiframe_initialization_failed`: Problema de configuração

### Passos de Debug Detalhados

#### Passo 1: Verificar Inicialização da API
```javascript
// Cole no console do navegador
console.log('GAPI loaded:', !!window.gapi)
console.log('Auth2 ready:', !!window.gapi?.auth2)
console.log('Client initialized:', !!window.gapi?.client)
```

#### Passo 2: Verificar Credenciais
```javascript
// Cole no console do navegador
console.log('Client ID:', process.env.NEXT_PUBLIC_GOOGLE_DRIVE_CLIENT_ID)
console.log('API Key:', process.env.NEXT_PUBLIC_GOOGLE_DRIVE_API_KEY)
```

#### Passo 3: Testar Autenticação Manual
```javascript
// Cole no console do navegador (após carregar a página)
const authInstance = window.gapi.auth2.getAuthInstance()
console.log('Auth instance:', authInstance)
console.log('Is signed in:', authInstance?.isSignedIn?.get())
```

### Problemas Específicos e Soluções

#### Erro: "popup_closed_by_user"
- **Causa**: O popup de autenticação foi fechado antes da conclusão
- **Solução**: Tente novamente e não feche o popup

#### Erro: "popup_blocked_by_browser"
- **Causa**: O navegador bloqueou o popup
- **Solução**: Permita popups para o site

#### Erro: "Invalid Client ID"
- **Causa**: Client ID incorreto ou mal formatado
- **Solução**: Verifique se termina com `.googleusercontent.com`

#### Erro: "Origin not authorized"
- **Causa**: Domínio não está na lista de origens autorizadas
- **Solução**: Adicione o domínio no Google Cloud Console

#### Erro: "Token validation failed"
- **Causa**: Token inválido ou expirado
- **Solução**: Faça logout e login novamente

### Configuração de Teste Rápida

Para testar rapidamente, use estas configurações de desenvolvimento:

1. **Authorized JavaScript origins**:
   ```
   http://localhost:3000
   http://127.0.0.1:3000
   ```

2. **Ambiente de desenvolvimento**:
   ```env
   NEXT_PUBLIC_GOOGLE_DRIVE_CLIENT_ID=seu-client-id.apps.googleusercontent.com
   NEXT_PUBLIC_GOOGLE_DRIVE_API_KEY=sua-api-key
   ```

3. **Teste em incógnito**: Para evitar conflitos com outras contas Google

### Logs de Debug

Ative logs detalhados adicionando ao console:

```javascript
// Ativar logs verbosos do Google API
localStorage.setItem('gapi_debug', 'true')
```

### Próximos Passos se Problemas Persistirem

1. **Recrie as credenciais** no Google Cloud Console
2. **Use um projeto Google Cloud diferente**
3. **Teste em outro navegador**
4. **Verifique se há problemas de rede/proxy**

### Contato para Suporte

Se os problemas persistirem após seguir este guia:
1. Anote os erros específicos do console
2. Copie as informações do Debug Panel
3. Verifique se todas as configurações estão corretas
