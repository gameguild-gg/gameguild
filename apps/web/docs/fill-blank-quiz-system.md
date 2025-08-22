# Sistema de Quiz Fill-in-the-Blank Reformado

## Visão Geral

O sistema de quiz "Fill-in-the-Blank" foi completamente reformado para oferecer uma experiência mais intuitiva e funcional para professores e alunos.

## Como Funciona

### 1. Criação da Pergunta

- Use `___` (três underscores) para criar espaços em branco na pergunta
- Exemplo: "A capital do Brasil é ___ e fica no estado de ___."

### 2. Configuração Automática

- O sistema detecta automaticamente os espaços em branco quando você digita `___`
- Para cada espaço, um campo de configuração é criado automaticamente
- Os campos são numerados sequencialmente (Blank #1, Blank #2, etc.)

### 3. Configuração dos Campos

#### Palavras Esperadas
- Para cada campo, você pode definir múltiplas palavras aceitáveis
- Separe as palavras com vírgulas
- Exemplo: "Brasília, brasilia, BRASILIA"

#### Alternativas Adicionais
- Cada campo pode ter conjuntos alternativos de palavras
- Útil para sinônimos, variações ortográficas, etc.
- Exemplo: "Rio de Janeiro, Rio, RJ"

### 4. Interface do Usuário

- A pergunta é exibida com campos de entrada inline
- Os usuários veem apenas a pergunta com campos vazios
- Não veem os `___` - apenas campos de texto para preenchimento

## Exemplo Prático

### Pergunta Original
```
A ___ é a maior cidade do Brasil e fica no estado de ___.
```

### Configuração dos Campos

**Blank #1:**
- Palavras Esperadas: "São Paulo, Sao Paulo, SÃO PAULO"
- Alternativas: "SP, Sao Paulo"

**Blank #2:**
- Palavras Esperadas: "São Paulo, Sao Paulo, SÃO PAULO"
- Alternativas: "SP, Sao Paulo"

### Como o Usuário Vê
```
A [campo de texto] é a maior cidade do Brasil e fica no estado de [campo de texto].
```

## Vantagens do Novo Sistema

1. **Detecção Automática**: Não precisa configurar manualmente cada campo
2. **Flexibilidade**: Múltiplas respostas corretas por campo
3. **Alternativas**: Suporte a variações e sinônimos
4. **Interface Limpa**: Usuários veem apenas a pergunta, não os marcadores técnicos
5. **Validação Inteligente**: Aceita respostas com diferentes capitalizações

## Casos de Uso

### Línguas
- **Português**: "O ___ é o maior rio do Brasil" (Amazonas, amazonas, AMAZONAS)
- **Inglês**: "The ___ is the capital of England" (London, london, LONDON)

### Ciências
- **Química**: "A fórmula da água é ___" (H2O, h2o, H₂O)
- **Biologia**: "A célula é a unidade básica da ___" (vida, VIDA, Vida)

### História
- **Datas**: "O Brasil foi descoberto em ___" (1500, 1500 d.C., 1500 DC)

## Configuração Avançada

### Adicionar Campos Manualmente
- Use o botão "Add Blank" para adicionar campos extras
- Útil quando você quer mais controle sobre a posição dos campos

### Remover Campos
- Use o botão de lixeira para remover campos desnecessários
- O sistema reorganiza automaticamente as posições

### Editar Alternativas
- Cada campo pode ter múltiplas alternativas
- Use o botão "Add Alternative" para criar variações aceitáveis

## Validação

O sistema valida as respostas considerando:
- Palavras exatas (case-insensitive)
- Múltiplas respostas corretas por campo
- Alternativas configuradas
- Espaços em branco e pontuação

## Dicas de Uso

1. **Use `___` consistentemente** para criar espaços em branco
2. **Configure múltiplas respostas** para maior flexibilidade
3. **Teste com o preview** antes de salvar
4. **Use alternativas** para variações comuns
5. **Mantenha as respostas simples** para facilitar a validação

## Suporte Técnico

Para problemas ou sugestões sobre o sistema de quiz fill-in-the-blank, consulte a documentação do projeto ou entre em contato com a equipe de desenvolvimento.
