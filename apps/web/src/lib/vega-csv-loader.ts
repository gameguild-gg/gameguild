import * as d3Dsv from 'd3-dsv'

/**
 * Processa uma especificação Vega-Lite e substitui URLs data: por dados inline
 */
export function loadCsvDataIntoSpec(
  spec: string,
  csvData: Record<string, string>
): any {
  try {
    const parsedSpec = JSON.parse(spec)
    return processDataNode(parsedSpec, csvData)
  } catch (error) {
    console.error('Erro ao processar CSV no spec:', error)
    return spec
  }
}

/**
 * Processa recursivamente um nó do spec procurando por propriedades "data"
 */
function processDataNode(node: any, csvData: Record<string, string>): any {
  if (!node || typeof node !== 'object') {
    return node
  }

  // Se é um array, processa cada item
  if (Array.isArray(node)) {
    return node.map(item => processDataNode(item, csvData))
  }

  // Cria uma cópia do objeto
  const processed: any = {}

  for (const [key, value] of Object.entries(node)) {
    // Se encontrou uma propriedade "data" com "url"
    if (key === 'data' && value && typeof value === 'object' && 'url' in value) {
      const dataValue = value as { url: string; [key: string]: any }
      
      // Verifica se é uma URL data:
      if (dataValue.url.startsWith('data:')) {
        const filename = dataValue.url.substring(5) // Remove "data:"
        
        // Se temos os dados CSV para esse arquivo
        if (csvData[filename]) {
          try {
            // Parse CSV para JSON
            const csvContent = csvData[filename]
            const parsedData = d3Dsv.csvParse(csvContent)
            
            // Substitui url por values
            processed[key] = {
              ...dataValue,
              values: parsedData
            }
            
            // Remove a propriedade url
            delete processed[key].url
            
            continue
          } catch (error) {
            console.error(`Erro ao parsear CSV ${filename}:`, error)
            // Mantém a URL original em caso de erro
            processed[key] = value
            continue
          }
        } else {
          console.warn(`Arquivo CSV não encontrado: ${filename}`)
          // Mantém a URL original
          processed[key] = value
          continue
        }
      }
    }

    // Para outros casos, processa recursivamente
    processed[key] = processDataNode(value, csvData)
  }

  return processed
}

/**
 * Verifica se um spec contém referências a dados CSV
 */
export function hasDataReferences(spec: string): boolean {
  try {
    return spec.includes('"data:')
  } catch {
    return false
  }
}

/**
 * Extrai nomes de arquivos CSV referenciados no spec
 */
export function extractCsvFilenames(spec: string): string[] {
  const filenames: string[] = []
  const regex = /"data:([^"]+\.csv)"/g
  let match

  while ((match = regex.exec(spec)) !== null) {
    if (match[1] && !filenames.includes(match[1])) {
      filenames.push(match[1])
    }
  }

  return filenames
}
