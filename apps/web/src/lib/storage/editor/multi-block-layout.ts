/**
 * Multi-Block Layout System
 * Sistema para gerenciar layouts complexos com múltiplos blocos,
 * painéis e abas (inspirado no LeetCode)
 */

export interface BlockTab {
  blockId: string  // b1, b2, b3, etc.
  label?: string   // Optional custom label
}

export interface PanelConfig {
  id: string       // panel-1, panel-2, etc.
  tabs: BlockTab[] // Blocks dentro deste painel (como abas)
  activeTabIndex: number // Index da aba ativa
  width?: number   // Width percentage (para layout horizontal)
  height?: number  // Height percentage (para layout vertical)
}

export interface MultiBlockLayout {
  panels: PanelConfig[]
  direction: 'horizontal' | 'vertical' | 'grid'
  version: number // Para versionamento do layout
}

/**
 * Criar layout padrão baseado no número de blocos
 */
export function createDefaultLayout(blockIds: string[]): MultiBlockLayout {
  const blockCount = blockIds.length

  if (blockCount === 1) {
    // 1 bloco: painel único
    return {
      panels: [
        {
          id: 'panel-1',
          tabs: [{ blockId: blockIds[0]! }],
          activeTabIndex: 0,
          width: 100,
        },
      ],
      direction: 'horizontal',
      version: 1,
    }
  } else if (blockCount === 2) {
    // 2 blocos: lado a lado (50/50)
    return {
      panels: [
        {
          id: 'panel-1',
          tabs: [{ blockId: blockIds[0]! }],
          activeTabIndex: 0,
          width: 50,
        },
        {
          id: 'panel-2',
          tabs: [{ blockId: blockIds[1]! }],
          activeTabIndex: 0,
          width: 50,
        },
      ],
      direction: 'horizontal',
      version: 1,
    }
  } else if (blockCount === 3) {
    // 3 blocos: 2 à esquerda (em abas), 1 à direita
    return {
      panels: [
        {
          id: 'panel-1',
          tabs: [{ blockId: blockIds[0]! }, { blockId: blockIds[1]! }],
          activeTabIndex: 0,
          width: 50,
        },
        {
          id: 'panel-2',
          tabs: [{ blockId: blockIds[2]! }],
          activeTabIndex: 0,
          width: 50,
        },
      ],
      direction: 'horizontal',
      version: 1,
    }
  } else {
    // 4+ blocos: grid ou customizado
    // Default: 2 painéis, blocos divididos em abas
    const half = Math.ceil(blockCount / 2)
    const leftBlocks = blockIds.slice(0, half).map(id => ({ blockId: id }))
    const rightBlocks = blockIds.slice(half).map(id => ({ blockId: id }))

    return {
      panels: [
        {
          id: 'panel-1',
          tabs: leftBlocks,
          activeTabIndex: 0,
          width: 50,
        },
        {
          id: 'panel-2',
          tabs: rightBlocks,
          activeTabIndex: 0,
          width: 50,
        },
      ],
      direction: 'horizontal',
      version: 1,
    }
  }
}

/**
 * Adicionar bloco ao layout
 */
export function addBlockToLayout(
  layout: MultiBlockLayout,
  blockId: string,
  panelId?: string
): MultiBlockLayout {
  const targetPanelId = panelId || layout.panels[0]?.id

  if (!targetPanelId) {
    // Criar primeiro painel
    return {
      ...layout,
      panels: [
        {
          id: 'panel-1',
          tabs: [{ blockId }],
          activeTabIndex: 0,
          width: 100,
        },
      ],
    }
  }

  return {
    ...layout,
    panels: layout.panels.map(panel =>
      panel.id === targetPanelId
        ? {
            ...panel,
            tabs: [...panel.tabs, { blockId }],
          }
        : panel
    ),
  }
}

/**
 * Remover bloco do layout
 */
export function removeBlockFromLayout(
  layout: MultiBlockLayout,
  blockId: string
): MultiBlockLayout {
  const newPanels = layout.panels
    .map(panel => ({
      ...panel,
      tabs: panel.tabs.filter(tab => tab.blockId !== blockId),
      activeTabIndex: Math.min(panel.activeTabIndex, panel.tabs.length - 2),
    }))
    .filter(panel => panel.tabs.length > 0) // Remove painéis vazios

  return {
    ...layout,
    panels: newPanels,
  }
}

/**
 * Mover bloco entre painéis
 */
export function moveBlockBetweenPanels(
  layout: MultiBlockLayout,
  blockId: string,
  fromPanelId: string,
  toPanelId: string
): MultiBlockLayout {
  let blockToMove: BlockTab | undefined

  // Remover do painel origem
  const withoutBlock = layout.panels.map(panel => {
    if (panel.id === fromPanelId) {
      const tab = panel.tabs.find(t => t.blockId === blockId)
      if (tab) blockToMove = tab
      return {
        ...panel,
        tabs: panel.tabs.filter(t => t.blockId !== blockId),
        activeTabIndex: Math.min(panel.activeTabIndex, panel.tabs.length - 2),
      }
    }
    return panel
  })

  if (!blockToMove) return layout

  // Adicionar ao painel destino
  const withBlock = withoutBlock.map(panel =>
    panel.id === toPanelId
      ? {
          ...panel,
          tabs: [...panel.tabs, blockToMove!],
        }
      : panel
  )

  return {
    ...layout,
    panels: withBlock.filter(p => p.tabs.length > 0),
  }
}

/**
 * Criar novo painel
 */
export function createNewPanel(
  layout: MultiBlockLayout,
  blockId?: string
): MultiBlockLayout {
  const newPanelNumber = layout.panels.length + 1
  const newPanel: PanelConfig = {
    id: `panel-${newPanelNumber}`,
    tabs: blockId ? [{ blockId }] : [],
    activeTabIndex: 0,
    width: 100 / (layout.panels.length + 1),
  }

  // Reajustar larguras dos painéis existentes
  const adjustedPanels = layout.panels.map(panel => ({
    ...panel,
    width: 100 / (layout.panels.length + 1),
  }))

  return {
    ...layout,
    panels: [...adjustedPanels, newPanel],
  }
}

/**
 * Remover painel (move blocos para outro painel)
 */
export function removePanel(
  layout: MultiBlockLayout,
  panelId: string,
  moveToPanelId?: string
): MultiBlockLayout {
  const panelToRemove = layout.panels.find(p => p.id === panelId)
  if (!panelToRemove) return layout

  const targetPanelId = moveToPanelId || layout.panels.find(p => p.id !== panelId)?.id

  if (!targetPanelId) {
    // Último painel, não pode remover
    return layout
  }

  // Mover todas as abas para o painel de destino
  const withMovedBlocks = layout.panels.map(panel =>
    panel.id === targetPanelId
      ? {
          ...panel,
          tabs: [...panel.tabs, ...panelToRemove.tabs],
        }
      : panel
  )

  // Remover painel e reajustar larguras
  const remainingPanels = withMovedBlocks.filter(p => p.id !== panelId)
  const adjustedPanels = remainingPanels.map(panel => ({
    ...panel,
    width: 100 / remainingPanels.length,
  }))

  return {
    ...layout,
    panels: adjustedPanels,
  }
}

/**
 * Atualizar largura de painel
 */
export function updatePanelWidth(
  layout: MultiBlockLayout,
  panelId: string,
  width: number
): MultiBlockLayout {
  return {
    ...layout,
    panels: layout.panels.map(panel =>
      panel.id === panelId ? { ...panel, width } : panel
    ),
  }
}

/**
 * Alterar aba ativa em um painel
 */
export function setActiveTab(
  layout: MultiBlockLayout,
  panelId: string,
  tabIndex: number
): MultiBlockLayout {
  return {
    ...layout,
    panels: layout.panels.map(panel =>
      panel.id === panelId
        ? { ...panel, activeTabIndex: Math.min(tabIndex, panel.tabs.length - 1) }
        : panel
    ),
  }
}

/**
 * Validar layout (garantir que todos os blocos estão presentes)
 */
export function validateLayout(
  layout: MultiBlockLayout,
  blockIds: string[]
): boolean {
  const layoutBlockIds = new Set<string>()
  layout.panels.forEach(panel => {
    panel.tabs.forEach(tab => {
      layoutBlockIds.add(tab.blockId)
    })
  })

  const allBlocksPresent = blockIds.every(id => layoutBlockIds.has(id))
  const noExtraBlocks = Array.from(layoutBlockIds).every(id => blockIds.includes(id))

  return allBlocksPresent && noExtraBlocks
}

/**
 * Sincronizar layout com lista de blocos (adicionar/remover conforme necessário)
 */
export function syncLayoutWithBlocks(
  layout: MultiBlockLayout,
  blockIds: string[]
): MultiBlockLayout {
  const layoutBlockIds = new Set<string>()
  layout.panels.forEach(panel => {
    panel.tabs.forEach(tab => {
      layoutBlockIds.add(tab.blockId)
    })
  })

  let syncedLayout = { ...layout }

  // Remover blocos que não existem mais
  const blocksToRemove = Array.from(layoutBlockIds).filter(id => !blockIds.includes(id))
  blocksToRemove.forEach(blockId => {
    syncedLayout = removeBlockFromLayout(syncedLayout, blockId)
  })

  // Adicionar blocos novos
  const blocksToAdd = blockIds.filter(id => !layoutBlockIds.has(id))
  blocksToAdd.forEach(blockId => {
    syncedLayout = addBlockToLayout(syncedLayout, blockId)
  })

  return syncedLayout
}
