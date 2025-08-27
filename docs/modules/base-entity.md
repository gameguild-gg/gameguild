# Base Entity & Inheritance

Consolidated summary.

## Core Components

- `IEntity`
- `BaseEntity<TKey>` generic
- Concrete `BaseEntity` (int convenience)

## Provided Functionality

- GUID or numeric IDs
- CreatedAt / UpdatedAt timestamps
- Optional soft delete pattern
- Partial constructor for ergonomic initialization

## Usage Tips

Call `Touch()` after internal state changes; raise domain events inside entity methods to encapsulate side-effects.
