"use client"

import type React from "react"

interface PreviewHeadingProps {
  node: any
  children: React.ReactNode
}

export function PreviewHeading({ node, children }: PreviewHeadingProps) {
  const headingClasses = ["font-bold my-4"]
  if (node.format === "left") headingClasses.push("text-left")
  else if (node.format === "center") headingClasses.push("text-center")
  else if (node.format === "right") headingClasses.push("text-right")
  else if (node.format === "justify") headingClasses.push("text-justify")

  // Get inline styles from the node if they exist
  const headingStyles: React.CSSProperties = {}
  if (node.style) {
    const styleString = node.style
    const styleRules = styleString.split(";").filter((rule: string) => rule.trim())

    styleRules.forEach((rule: { split: (arg0: string) => { (): any; new(): any; map: { (arg0: (s: any) => any): [any, any]; new(): any } } }) => {
      const [property, value] = rule.split(":").map((s) => s.trim())
      if (property && value) {
        const camelCaseProperty = property.replace(/-([a-z])/g, (match: any, letter: string) => letter.toUpperCase())
        headingStyles[camelCaseProperty as keyof React.CSSProperties] = value
      }
    })
  }

  const commonProps = {
    className: headingClasses.join(" "),
    style: headingStyles,
    children,
  }

  switch (node.tag) {
    case "h1":
    case 1:
      return (
        <h1 className={`text-4xl ${headingClasses.join(" ")}`} style={headingStyles}>
          {children}
        </h1>
      )
    case "h2":
    case 2:
      return (
        <h2 className={`text-3xl ${headingClasses.join(" ")}`} style={headingStyles}>
          {children}
        </h2>
      )
    case "h3":
    case 3:
      return (
        <h3 className={`text-2xl ${headingClasses.join(" ")}`} style={headingStyles}>
          {children}
        </h3>
      )
    case "h4":
    case 4:
      return (
        <h4 className={`text-xl ${headingClasses.join(" ")}`} style={headingStyles}>
          {children}
        </h4>
      )
    case "h5":
    case 5:
      return (
        <h5 className={`text-lg ${headingClasses.join(" ")}`} style={headingStyles}>
          {children}
        </h5>
      )
    case "h6":
    case 6:
      return (
        <h6 className={`text-base ${headingClasses.join(" ")}`} style={headingStyles}>
          {children}
        </h6>
      )
    default:
      return (
        <h2 className={`text-3xl ${headingClasses.join(" ")}`} style={headingStyles}>
          {children}
        </h2>
      )
  }
}
