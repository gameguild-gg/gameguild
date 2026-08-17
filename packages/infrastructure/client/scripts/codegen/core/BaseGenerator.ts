/**
 * Base Generator - Template Method Pattern
 */

import type { OpenApiSpec } from '../../fetch-spec.js';

export abstract class BaseGenerator {
  constructor(protected spec: OpenApiSpec) {}

  /**
   * Template method defining the generation algorithm
   */
  generate(): string {
    const lines: string[] = [];
    lines.push(this.generateHeader());
    lines.push('');
    lines.push(this.generateImports());
    lines.push('');
    lines.push(this.generateContent());
    lines.push('');
    lines.push(this.generateFooter());

    return lines.filter(Boolean).join('\n');
  }

  protected abstract generateContent(): string;

  protected generateHeader(): string {
    return `/**
 * @game-guild/client - Generated ${this.getFileDescription()}
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 *
 * Generated from: ${this.spec.info?.title || 'Unknown API'}
 * API Version: ${this.spec.info?.version || 'unknown'}
 */`;
  }

  protected generateImports(): string {
    return `/* eslint-disable @typescript-eslint/no-explicit-any */`;
  }

  protected generateFooter(): string {
    return '';
  }

  protected abstract getFileDescription(): string;
}
