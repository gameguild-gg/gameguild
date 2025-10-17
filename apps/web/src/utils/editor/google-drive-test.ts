/**
 * Google Drive API Test Script
 * Run this in the browser console to verify if the new Google Identity Services are working
 */

console.log('🧪 Testando Google Drive API...')

// Test 1: Check environment variables
console.log('📋 Variáveis de ambiente:')
console.log('CLIENT_ID:', process.env.NEXT_PUBLIC_GOOGLE_DRIVE_CLIENT_ID || '❌ NÃO CONFIGURADO')
console.log('API_KEY:', process.env.NEXT_PUBLIC_GOOGLE_DRIVE_API_KEY || '❌ NÃO CONFIGURADO')

// Test 2: Check if new Google Identity Services are available
console.log('\n🔐 Google Identity Services:')
if (typeof window !== 'undefined') {
  if (window.google?.accounts?.oauth2) {
    console.log('✅ Google Identity Services carregada')
  } else {
    console.log('❌ Google Identity Services não encontrada')
    console.log('Carregando Google Identity Services...')
    
    const gisScript = document.createElement('script')
    gisScript.src = 'https://accounts.google.com/gsi/client'
    gisScript.onload = () => {
      console.log('✅ Google Identity Services carregada com sucesso')
      console.log('window.google.accounts.oauth2:', !!window.google?.accounts?.oauth2)
    }
    gisScript.onerror = () => console.log('❌ Falha ao carregar Google Identity Services')
    document.head.appendChild(gisScript)
  }
}

// Test 3: Check if GAPI is available
console.log('\n📁 Google API Client:')
if (typeof window !== 'undefined') {
  if (window.gapi?.client) {
    console.log('✅ GAPI Client carregada')
  } else {
    console.log('❌ GAPI Client não encontrada')
    console.log('Carregando GAPI...')
    
    const gapiScript = document.createElement('script')
    gapiScript.src = 'https://apis.google.com/js/api.js'
    gapiScript.onload = () => {
      console.log('✅ GAPI carregada com sucesso')
      window.gapi.load('client', () => {
        console.log('✅ GAPI Client inicializada')
      })
    }
    gapiScript.onerror = () => console.log('❌ Falha ao carregar GAPI')
    document.head.appendChild(gapiScript)
  }
}

// Test 4: Simulate authentication flow (manual test)
console.log('\n🎯 Para testar autenticação manualmente:')
console.log('1. Certifique-se de que as variáveis de ambiente estão configuradas')
console.log('2. Abra o dialog de configuração do Google Drive')
console.log('3. Clique em "Conectar com Google Drive"')
console.log('4. Autorize as permissões')
console.log('5. Verifique se o checkbox fica marcado com ✓')

export {}
