import { ProgramContent } from '../types';

// Week 13 — Time Series & Search Engines
export const week13TimeseriesSearchContent: ProgramContent = {
  id: 'databases-week13',
  title: 'Week 13 — Time Series & Search Engines',
  description: 'TimescaleDB (time-series databases), Elasticsearch (search engines), inverted indices, aggregations',
  type: 'week',
  topics: [
    'TimescaleDB',
    'Time-series databases',
    'Hypertables and chunks',
    'time_bucket() downsampling',
    'Compression policies',
    'Retention policies',
    'Continuous aggregates',
    'Elasticsearch',
    'Search engines',
    'Inverted indices',
    'Analyzers and tokenizers',
    'Query DSL (match, term, bool)',
    'Aggregations (terms, stats, histogram)',
    'Drizzle ORM integration',
    '@elastic/elasticsearch client'
  ],
  objectives: [
    'Understand when to use time-series databases vs relational databases',
    'Create and query TimescaleDB hypertables',
    'Use time_bucket() for downsampling time-series data',
    'Configure compression and retention policies',
    'Build continuous aggregates for precomputed analytics',
    'Understand inverted indices and how they enable fast full-text search',
    'Define Elasticsearch mappings with text and keyword fields',
    'Write queries using Elasticsearch Query DSL (match, term, bool)',
    'Create aggregations for analytics (terms, stats, histogram)',
    'Integrate TimescaleDB and Elasticsearch with TypeScript'
  ],
  estimatedMinutes: 360,
  metadata: {
    week: 13,
    date: '2026-04-06',
    assessments: ['Quiz 11', 'Final Project Checkpoint #2'],
    technologies: ['TimescaleDB', 'Elasticsearch', 'Drizzle ORM', '@elastic/elasticsearch', 'Kibana']
  }
};

export const week13TimescaleDBContent: ProgramContent = {
  id: 'databases-week13-timescaledb',
  title: 'TimescaleDB Fundamentals',
  description: 'Time-series database built on PostgreSQL: hypertables, compression, retention policies, continuous aggregates',
  type: 'lesson',
  topics: [
    'What are time-series databases?',
    'TimescaleDB vs PostgreSQL comparison',
    'Hypertables and automatic partitioning',
    'Chunks (time-based partitioning)',
    'Creating hypertables',
    'time_bucket() for downsampling',
    'time_bucket_gapfill() for missing data',
    'Compression policies (10x-20x storage reduction)',
    'Columnar compression',
    'Retention policies (automatic data deletion)',
    'Continuous aggregates (materialized views)',
    'IoT sensor monitoring use case',
    'APM (Application Performance Monitoring)',
    'Financial stock ticks',
    'Drizzle ORM integration',
    'Performance best practices',
    'Common pitfalls'
  ],
  objectives: [
    'Understand the difference between time-series and relational databases',
    'Create hypertables with automatic time-based partitioning',
    'Query time-series data with time_bucket() for downsampling',
    'Configure compression policies to reduce storage by 10x-20x',
    'Set up retention policies for automatic data deletion',
    'Build continuous aggregates for fast precomputed analytics',
    'Integrate TimescaleDB with TypeScript and Drizzle ORM',
    'Apply best practices for high-performance time-series workloads'
  ],
  estimatedMinutes: 120,
  parent: 'databases-week13',
  metadata: {
    contentType: 'fundamentals',
    file: 'timescaledb-fundamentals.md'
  }
};

export const week13ElasticsearchContent: ProgramContent = {
  id: 'databases-week13-elasticsearch',
  title: 'Elasticsearch Fundamentals',
  description: 'Search engine for full-text search, log analysis, and real-time analytics',
  type: 'lesson',
  topics: [
    'What are search engines?',
    'Inverted indices explained',
    'Documents and indices',
    'Mappings (schema definitions)',
    'Field types (text, keyword, numeric, date)',
    'Analyzers and tokenizers',
    'Standard analyzer',
    'English analyzer (stemming)',
    'Custom analyzers',
    'Query DSL (Domain Specific Language)',
    'match query (full-text search)',
    'term query (exact match)',
    'bool query (AND, OR, NOT)',
    'range query (numeric, date ranges)',
    'Aggregations (analytics)',
    'terms aggregation (GROUP BY)',
    'stats aggregation (min, max, avg, sum)',
    'histogram aggregation (bucketing)',
    'E-commerce product search use case',
    'Log analysis (ELK stack)',
    'Autocomplete implementation',
    '@elastic/elasticsearch client',
    'Performance best practices',
    'Common pitfalls'
  ],
  objectives: [
    'Understand inverted indices and how they enable fast full-text search',
    'Define mappings with appropriate field types (text vs keyword)',
    'Configure analyzers for text processing (tokenization, stemming)',
    'Write full-text search queries with match, term, and bool',
    'Create aggregations for analytics (counts, averages, histograms)',
    'Integrate Elasticsearch with TypeScript using @elastic/elasticsearch',
    'Build product search with faceted filters and autocomplete',
    'Apply performance optimizations (filter context, keyword fields, bulk indexing)'
  ],
  estimatedMinutes: 120,
  parent: 'databases-week13',
  metadata: {
    contentType: 'fundamentals',
    file: 'elasticsearch-fundamentals.md'
  }
};

export const week13QuizContent: ProgramContent = {
  id: 'databases-week13-quiz',
  title: 'Quiz 11 — Time Series & Search Engines',
  description: '12 questions covering TimescaleDB (hypertables, compression, continuous aggregates) and Elasticsearch (inverted indices, query DSL, aggregations)',
  type: 'assessment',
  topics: [
    'TimescaleDB hypertables and chunks',
    'time_bucket() downsampling',
    'Compression policies',
    'Retention policies',
    'Continuous aggregates',
    'Inverted indices',
    'text vs keyword field types',
    'match vs term queries',
    'bool query (must, should, filter, must_not)',
    'Analyzers',
    'terms aggregation',
    'filter vs must context'
  ],
  objectives: [
    'Demonstrate understanding of TimescaleDB core features',
    'Explain how inverted indices enable fast full-text search',
    'Choose appropriate field types for different data',
    'Write effective Elasticsearch queries',
    'Apply performance optimizations in both technologies'
  ],
  estimatedMinutes: 30,
  parent: 'databases-week13',
  metadata: {
    assessmentType: 'quiz',
    questionCount: 12,
    dueDate: '2026-04-09',
    file: 'quiz/timeseries-search-quiz.md'
  }
};

export const week13ReadingsContent: ProgramContent = {
  id: 'databases-week13-readings',
  title: 'Week 13 Readings',
  description: 'Curated resources for TimescaleDB and Elasticsearch: official docs, tutorials, tools, videos, books, practice datasets',
  type: 'reading',
  topics: [
    'TimescaleDB official documentation',
    'Hypertables and chunks deep dive',
    'Compression guide',
    'Continuous aggregates guide',
    'Data retention policies',
    'Elasticsearch reference documentation',
    'Mapping types',
    'Query DSL reference',
    'Aggregations reference',
    'Analyzers and tokenizers',
    'Grafana for TimescaleDB',
    'Kibana for Elasticsearch',
    'ELK stack (Elasticsearch, Logstash, Kibana)',
    'Time-series database comparison',
    'Search engine comparison',
    'Practice datasets (NYC Taxi, IoT sensors)',
    'Video tutorials',
    'Books (Elasticsearch in Action, Relevant Search)'
  ],
  objectives: [
    'Access official documentation for both technologies',
    'Explore tutorials and hands-on labs',
    'Learn visualization tools (Grafana, Kibana)',
    'Compare time-series databases and search engines',
    'Practice with real-world datasets'
  ],
  estimatedMinutes: 180,
  parent: 'databases-week13',
  metadata: {
    contentType: 'readings',
    resourceCount: 50,
    file: 'readings-13.md'
  }
};

// Export all content
export const week13Content = [
  week13TimeseriesSearchContent,
  week13TimescaleDBContent,
  week13ElasticsearchContent,
  week13QuizContent,
  week13ReadingsContent
];
