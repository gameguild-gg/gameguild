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
  let customProps = {}
  
  if (node.listType === "bullet") {
    // Para listas não ordenadas (bullet)
    const listStyleType = node.listStyleType || 
                         node['data-list-style-type'] ||
                         'disc'
                         
    switch (listStyleType) {
      case "disc":
        listClass = "list-disc list-inside"
        break
      case "circle":
        customStyle = { listStyleType: 'circle' }
        listClass = "list-inside"
        break
      case "square":
        customStyle = { listStyleType: 'square' }
        listClass = "list-inside"
        break
      case "arrow":
        listClass = "arrow-list my-4"
        customProps = { 'data-arrow-list': 'true' }
        break
      case "star":
        listClass = "star-list my-4"
        customProps = { 'data-star-list': 'true' }
        break
      default:
        listClass = "list-disc list-inside"
        break
    }
  } else {
    // Para listas ordenadas
    const listStyleType = node.listStyleType || 
                         node['data-list-style-type'] || 
                         (node.style && node.style.includes('upper-alpha') ? 'upper-alpha' :
                          node.style && node.style.includes('lower-alpha') ? 'lower-alpha' : 'decimal')
    
    listClass = "list-inside"
    
    switch (listStyleType) {
      case "upper-alpha":
        customStyle = { listStyleType: 'upper-alpha' }
        break
      case "lower-alpha":
        customStyle = { listStyleType: 'lower-alpha' }
        break
      case "upper-roman":
        customStyle = { listStyleType: 'upper-roman' }
        break
      case "lower-roman":
        customStyle = { listStyleType: 'lower-roman' }
        break
      case "decimal-leading-zero":
        customStyle = { listStyleType: 'decimal-leading-zero' }
        break
      case "decimal":
      default:
        customStyle = { listStyleType: 'decimal' }
        break
    }
  }

  return (
    <ListTag 
      className={`${listClass} my-4`}
      style={customStyle}
      {...customProps}
    >
      {children}
    </ListTag>
  )
}
