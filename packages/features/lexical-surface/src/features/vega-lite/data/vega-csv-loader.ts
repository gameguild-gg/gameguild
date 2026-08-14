import * as d3Dsv from "d3-dsv";

/**
 * Processa uma especificação Vega-Lite e substitui URLs data: por dados inline
 */
export function loadCsvDataIntoSpec(
  spec: string,
  dataFiles: Record<string, string> = {},
): any {
  try {
    const parsedSpec = JSON.parse(spec);
    return processDataNode(parsedSpec, dataFiles);
  } catch (error) {
    console.error("Erro ao processar dados no spec:", error);
    return spec;
  }
}

/**
 * Processa recursivamente um nó do spec procurando por propriedades "data"
 */
function processDataNode(node: any, dataFiles: Record<string, string>): any {
  if (!node || typeof node !== "object") {
    return node;
  }

  // Se é um array, processa cada item
  if (Array.isArray(node)) {
    return node.map((item) => processDataNode(item, dataFiles));
  }

  // Cria uma cópia do objeto
  const processed: any = {};

  for (const [key, value] of Object.entries(node)) {
    // Se encontrou uma propriedade "data" com "url"
    if (
      key === "data" &&
      value &&
      typeof value === "object" &&
      "url" in value
    ) {
      const dataValue = value as { url: string; [key: string]: any };

      // Verifica se é uma URL data:
      if (dataValue.url.startsWith("data:")) {
        const filename = dataValue.url.substring(5); // Remove "data:"

        // Verifica se temos esse arquivo
        if (dataFiles[filename]) {
          const content = dataFiles[filename];

          // Verifica se é CSV
          if (filename.endsWith(".csv")) {
            try {
              // Parse CSV para JSON
              const parsedData = d3Dsv.csvParse(content);

              // Substitui url por values
              processed[key] = {
                ...dataValue,
                values: parsedData,
              };

              // Remove a propriedade url
              delete processed[key].url;

              continue;
            } catch (error) {
              console.error(`Erro ao parsear CSV ${filename}:`, error);
              // Mantém a URL original em caso de erro
              processed[key] = value;
              continue;
            }
          }

          // Verifica se é JSON
          if (filename.endsWith(".json")) {
            try {
              // Parse JSON
              const parsedData = JSON.parse(content);

              // Substitui url por values
              processed[key] = {
                ...dataValue,
                values: Array.isArray(parsedData) ? parsedData : [parsedData],
              };

              // Remove a propriedade url
              delete processed[key].url;

              continue;
            } catch (error) {
              console.error(`Erro ao parsear JSON ${filename}:`, error);
              // Mantém a URL original em caso de erro
              processed[key] = value;
              continue;
            }
          }
        }

        console.warn(`Arquivo não encontrado: ${filename}`);
        // Mantém a URL original
        processed[key] = value;
        continue;
      }
    }

    // Para outros casos, processa recursivamente
    processed[key] = processDataNode(value, dataFiles);
  }

  return processed;
}

/**
 * Verifica se um spec contém referências a dados CSV
 */
export function hasDataReferences(spec: string): boolean {
  try {
    return spec.includes('"data:');
  } catch {
    return false;
  }
}

/**
 * Extrai nomes de arquivos CSV ou JSON referenciados no spec
 */
export function extractDataFilenames(spec: string): {
  csv: string[];
  json: string[];
} {
  const csvFiles: string[] = [];
  const jsonFiles: string[] = [];
  const regex = /"data:([^"]+\.(csv|json))"/g;
  let match;

  while ((match = regex.exec(spec)) !== null) {
    const filename = match[1];
    const extension = match[2];

    if (extension === "csv" && filename && !csvFiles.includes(filename)) {
      csvFiles.push(filename);
    } else if (
      extension === "json" &&
      filename &&
      !jsonFiles.includes(filename)
    ) {
      jsonFiles.push(filename);
    }
  }

  return { csv: csvFiles, json: jsonFiles };
}
