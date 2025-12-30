## Data types

Data can take various forms, in the terms of Databases they are but a bit more specialized by its use cases than what we usually have when we code in general purpose programming languages. Here is a non-exhaustive list of common data types used in databases.


[![Data Types Meme](https://preview.redd.it/we-support-all-3-database-types-and-this-constantly-is-an-v0-f2hv3twxwb1a1.jpg?width=320&crop=smart&auto=webp&s=80db294ca98006b9b582544dca14177df54a6fd6)](https://reddit.com)

::: warning

Each type may have different names or variations depending on the specific database system (e.g., PostgreSQL, MySQL, MongoDB, etc.). Always refer to the documentation of the database you are using for precise definitions and capabilities.

:::

### Primitive Types

- **Text/String/Char Varying** (e.g., names, addresses, descriptions, emails)
- **Character** (e.g., single letters, symbols)
- **Integer** (e.g., counts, IDs, quantities)
- **Floating-point** (e.g., measurements, percentages, scientific data)
- **Decimal/Numeric** (e.g., currency values with exact precision)
- **Boolean** (e.g., true/false flags, active/inactive status)
- **Date** (e.g., birthdays, deadlines)
- **Time** (e.g., appointment times, durations)
- **Timestamp/DateTime** (e.g., created_at, last_login)
- **Interval** (e.g., time spans, durations between events)

### Binary Types

- **BLOB (Binary Large Object)** (e.g., images, audio files, videos)
- **BYTEA** (e.g., raw binary data, usually encrypted content)
- **Bit/Bit Varying** (e.g., flags, permissions masks, where each bit can be addressed individually)

### Structured Types

- **JSON** (e.g., API responses, configuration, flexible schemas)
- **XML** (e.g., legacy documents, SOAP messages)
- **Arrays** (e.g., tags, categories, multiple values)
- **Composite/Record** (e.g., address with street, city, zip)
- **Enum** (e.g., status values like 'pending', 'approved', 'rejected')

### Specialized Types

- **UUID/GUID** (e.g., globally unique identifiers)
- **Geospatial/Geometry** (e.g., coordinates, polygons, maps)
- **IP Address** (e.g., IPv4, IPv6 network addresses)
- **MAC Address** (e.g., network interface identifiers)
- **Range** (e.g., date ranges, numeric ranges)
- **Vector/Embedding** (e.g., ML embeddings, similarity search)
- **Time Series** (e.g., sensor readings, stock prices over time)

### Graph Types

- **Nodes** (e.g., users, products, entities)
- **Edges/Relationships** (e.g., friendships, purchases, connections)
- **Properties** (e.g., attributes on nodes or edges)

### Document Types

- **Key-Value** (e.g., cache entries, session data)
- **Document** (e.g., MongoDB documents, nested structures)
- **Wide-Column** (e.g., Cassandra column families)

### Other Specialized Types
    
- **Biometric data** (e.g., fingerprints, facial recognition data, eye scans)
- **Currency/Money** (e.g., monetary values with currency code)
- **Full-text search** (e.g., indexed text for search queries)
- **CIDR** (e.g., network address blocks)

## Database Taxonomy

