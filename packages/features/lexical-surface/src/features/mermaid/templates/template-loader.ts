import type { MermaidData } from "../mermaid-data";
import type React from "react";

export interface MermaidTemplate {
  id: string;
  type: MermaidData["type"];
  title: string;
  description: string;
  icon: React.ComponentType<{ className?: string }>;
  code: string;
  preview: string;
  category: string;
  previewImage?: string; // Optional preview image filename
}

/**
 * Dynamically import all templates from the template files
 */
async function loadTemplatesFromFolder(): Promise<MermaidTemplate[]> {
  const templates: MermaidTemplate[] = [];

  try {
    const blank = await import("./blank");
    templates.push(blank.default);

    // Flowcharts
    const simpleFlowchart = await import("./flowchart");
    templates.push(simpleFlowchart.default);

    const decisionFlowchart = await import("./flowchart-decision");
    templates.push(decisionFlowchart.default);

    const subgraphFlowchart = await import("./flowchart-subgraph");
    templates.push(subgraphFlowchart.default);

    // Sequence Diagrams
    const basicSequence = await import("./sequence-basic");
    templates.push(basicSequence.default);

    const asyncSequence = await import("./sequence-async");
    templates.push(asyncSequence.default);

    // State Diagrams
    const basicState = await import("./state-basic");
    templates.push(basicState.default);

    const compositeState = await import("./state-composite");
    templates.push(compositeState.default);

    // Class Diagrams
    const basicClass = await import("./class-basic");
    templates.push(basicClass.default);

    const relationshipClass = await import("./class-relationships");
    templates.push(relationshipClass.default);

    // Entity Relationship
    const basicER = await import("./er-basic");
    templates.push(basicER.default);

    // Pie Chart
    const basicPie = await import("./pie-chart");
    templates.push(basicPie.default);

    // Git Graph
    const basicGitGraph = await import("./gitgraph");
    templates.push(basicGitGraph.default);

    // Requirement Diagram
    const basicRequirement = await import("./requirement");
    templates.push(basicRequirement.default);

    // Architecture
    const basicArchitecture = await import("./architecture");
    templates.push(basicArchitecture.default);

    // C4 Context
    const basicC4Context = await import("./c4-context");
    templates.push(basicC4Context.default);

    // Timeline
    const basicTimeline = await import("./timeline");
    templates.push(basicTimeline.default);

    // Mindmap
    const basicMindmap = await import("./mindmap");
    templates.push(basicMindmap.default);

    // XY Chart
    const basicXYChart = await import("./xy-chart");
    templates.push(basicXYChart.default);

    // Radar Chart
    const basicRadar = await import("./radar-chart");
    templates.push(basicRadar.default);

    // Quadrant Chart
    const basicQuadrant = await import("./quadrant-chart");
    templates.push(basicQuadrant.default);

    // Sankey Diagram
    const basicSankey = await import("./sankey");
    templates.push(basicSankey.default);

    // User Journey
    const basicUserJourney = await import("./user-journey");
    templates.push(basicUserJourney.default);

    // Treemap Chart
    const basicTreemap = await import("./treemap-beta");
    templates.push(basicTreemap.default);

    // Kanban Board
    const basicKanban = await import("./kanban");
    templates.push(basicKanban.default);
  } catch (error) {
    console.error("Error loading Mermaid templates:", error);
  }

  return templates;
}

/**
 * Get all available templates
 */
export async function getAllTemplates(): Promise<MermaidTemplate[]> {
  return await loadTemplatesFromFolder();
}

/**
 * Get templates by category
 */
export async function getTemplatesByCategory(
  categoryId: string,
): Promise<MermaidTemplate[]> {
  const allTemplates = await getAllTemplates();
  return allTemplates.filter((template) => template.category === categoryId);
}

/**
 * Search templates by query
 */
export async function searchTemplates(
  query: string,
): Promise<MermaidTemplate[]> {
  const allTemplates = await getAllTemplates();
  const lowerQuery = query.toLowerCase();
  return allTemplates.filter(
    (template) =>
      template.title.toLowerCase().includes(lowerQuery) ||
      template.description.toLowerCase().includes(lowerQuery) ||
      template.type.toLowerCase().includes(lowerQuery),
  );
}
