"use client"

import type React from "react"

interface PreviewListProps {
  node: any
  children: React.ReactNode
}

export function PreviewList({ node, children }: PreviewListProps) {
  const ListTag = node.listType === "bullet" ? "ul" : "ol"
  
  let listClass = ""
  let customStyle = {}
  
  if (node.listType === "bullet") {
    listClass = "list-disc list-inside"
  } else {
    // Para listas ordenadas, verificar se há atributos customizados
    const listStyleType = node.listStyleType || 
                         node['data-list-style-type'] || 
                         (node.style && node.style.includes('upper-alpha') ? 'upper-alpha' :
                          node.style && node.style.includes('lower-alpha') ? 'lower-alpha' : 'decimal')
    
    if (listStyleType === "upper-alpha") {
      listClass = "list-inside"
      customStyle = { listStyleType: 'upper-alpha' }
    } else if (listStyleType === "lower-alpha") {
      listClass = "list-inside"
      customStyle = { listStyleType: 'lower-alpha' }
    } else {
      // Padrão: números
      listClass = "list-decimal list-inside"
    }
  }

  return (
    <ListTag 
      className={`${listClass} my-4`}
      style={customStyle}
    >
      {children}
    </ListTag>
  )
}
