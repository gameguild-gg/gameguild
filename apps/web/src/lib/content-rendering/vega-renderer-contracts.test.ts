import { afterEach, describe, expect, it } from 'vitest';

import { renderVegaChart } from '../../../../../packages/features/lexical-surface/src/features/vega-lite/rendering/vega-lite-chart';

const disposers = new Set<() => void>();

const barSpec = {
  $schema: 'https://vega.github.io/schema/vega-lite/v6.json',
  data: { values: [{ category: 'A', value: 2 }, { category: 'B', value: 4 }] },
  mark: 'bar',
  encoding: {
    x: { field: 'category', type: 'nominal' },
    y: { field: 'value', type: 'quantitative' },
  },
};

async function mountChart(spec: Record<string, unknown>, theme = 'default') {
  const container = document.createElement('div');
  document.body.append(container);
  const dispose = await renderVegaChart(container, spec, 'rectangular', theme);
  disposers.add(dispose);

  return {
    container,
    dispose: () => {
      if (disposers.delete(dispose)) dispose();
    },
  };
}

afterEach(() => {
  for (const dispose of disposers) dispose();
  disposers.clear();
  document.body.replaceChildren();
});

describe('shared Vega renderer with the installed SDK', () => {
  it.each([
    'default', 'dark', 'excel', 'ggplot2', 'quartz', 'vox', 'fivethirtyeight',
    'latimes', 'urbaninstitute', 'googlecharts', 'powerbi',
    'excel-dark', 'ggplot2-dark', 'quartz-dark', 'vox-dark',
    'fivethirtyeight-dark', 'latimes-dark', 'urbaninstitute-dark',
    'googlecharts-dark', 'powerbi-dark', 'unknown-custom-theme',
  ])('renders the data as SVG with theme %s and disposes it', async (theme) => {
    const chart = await mountChart(barSpec, theme);

    const svg = chart.container.querySelector('svg');
    expect(svg).not.toBeNull();
    expect(svg).toHaveTextContent('A');
    expect(svg).toHaveTextContent('B');

    chart.dispose();
    expect(chart.container.childElementCount).toBe(0);
  });

  it('preserves the dimensions of a plain Vega specification', async () => {
    const chart = await mountChart({
      $schema: 'https://vega.github.io/schema/vega/v6.json',
      width: 321,
      height: 123,
      marks: [{
        type: 'rect',
        encode: { enter: {
          x: { value: 0 }, y: { value: 0 },
          width: { value: 10 }, height: { value: 20 },
          fill: { value: 'steelblue' },
        } },
      }],
    });

    const svg = chart.container.querySelector('svg');
    expect(svg?.getAttribute('width')).toBe('321');
    expect(svg?.getAttribute('height')).toBe('123');
  });

  it('keeps the current chart when an older render is disposed', async () => {
    const first = await mountChart(barSpec);
    const disposeCurrent = await renderVegaChart(first.container, {
      ...barSpec,
      title: 'Current chart',
    });
    disposers.add(disposeCurrent);
    const currentSvg = first.container.querySelector('svg');
    expect(currentSvg).toHaveTextContent('Current chart');

    first.dispose();
    expect(first.container.querySelector('svg')).toBe(currentSvg);
    expect(currentSvg).toHaveTextContent('Current chart');

    if (disposers.delete(disposeCurrent)) disposeCurrent();
    expect(first.container.childElementCount).toBe(0);
  });
});
