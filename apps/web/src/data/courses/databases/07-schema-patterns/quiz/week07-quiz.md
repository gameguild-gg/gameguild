# Week 07 Quiz: Schema Patterns, TCL, DCL & ORM

This quiz covers schema design patterns, referential actions, functions/procedures/triggers, transactions, access control, scalability, and ORM/query builders.

---

## Schema Patterns

### Question 1
What is the primary advantage of soft delete over hard delete?

- A) Better query performance
- B) Ability to recover deleted data
- C) Reduced storage space
- D) Faster delete operations

### Question 2
Which soft delete implementation correctly handles unique constraints?

- A) `ALTER TABLE users ADD COLUMN deleted_at TIMESTAMP;`
- B) `CREATE UNIQUE INDEX idx_email ON users (email);`
- C) `CREATE UNIQUE INDEX idx_email ON users (email) WHERE deleted_at IS NULL;`
- D) `ALTER TABLE users ADD CONSTRAINT uk_email UNIQUE (email, deleted_at);`

### Question 3
What pattern is being implemented here?
```sql
ALTER TABLE products ADD COLUMN version INT DEFAULT 1;
UPDATE products SET name = 'New Name', version = version + 1 
WHERE id = 1 AND version = 5;
```

- A) Soft delete
- B) Audit trail
- C) Optimistic locking
- D) Checksum validation

### Question 4
Which anti-pattern stores data like `'tag1,tag2,tag3'` in a single column?

- A) Entity-Attribute-Value (EAV)
- B) Comma-separated values
- C) Polymorphic association
- D) One True Lookup Table (OTLT)

### Question 5
What is the purpose of storing a checksum with data?

- A) To encrypt the data
- B) To compress the data
- C) To detect data corruption or tampering
- D) To speed up queries

### Question 6
Which statement about history tables is TRUE?

- A) They replace the original table
- B) They store previous versions of rows
- C) They must have the same primary key as the original
- D) They require optimistic locking

### Question 7
What is a key drawback of the Entity-Attribute-Value (EAV) pattern?

- A) Uses too much disk space
- B) Queries become complex and slow
- C) Cannot store NULL values
- D) Requires foreign keys

### Question 8
Which SQL creates an audit trail for update operations?

- A) `CREATE TRIGGER audit BEFORE UPDATE`
- B) `CREATE TRIGGER audit AFTER UPDATE`
- C) `CREATE INDEX audit ON changes`
- D) `CREATE VIEW audit AS SELECT *`

---

## Referential Actions

### Question 9
What happens when you delete a parent row with `ON DELETE CASCADE`?

- A) The delete fails
- B) Child rows are also deleted
- C) Child foreign key columns are set to NULL
- D) Child rows remain unchanged

### Question 10
Which referential action sets the foreign key to its default value when the parent is deleted?

- A) CASCADE
- B) SET NULL
- C) SET DEFAULT
- D) RESTRICT

### Question 11
What is the difference between `RESTRICT` and `NO ACTION`?

- A) RESTRICT allows deferred checks; NO ACTION doesn't
- B) NO ACTION allows deferred checks; RESTRICT doesn't
- C) They are completely identical
- D) RESTRICT cascades; NO ACTION doesn't

### Question 12
When is `ON DELETE SET NULL` most appropriate?

- A) Parent-child hierarchies where children must be deleted
- B) Optional relationships where children can exist without parent
- C) When the foreign key column is NOT NULL
- D) Self-referencing tables with managers

### Question 13
What happens with `ON UPDATE CASCADE` when a parent's primary key changes?

- A) The update fails
- B) Child foreign keys are updated to match
- C) Child rows are deleted
- D) Child foreign keys become NULL

### Question 14
Which SQL correctly defines multiple referential actions?

- A) `FOREIGN KEY (dept_id) REFERENCES departments(id) ON DELETE SET NULL CASCADE`
- B) `FOREIGN KEY (dept_id) REFERENCES departments(id) ON DELETE SET NULL ON UPDATE CASCADE`
- C) `FOREIGN KEY (dept_id) REFERENCES departments(id) CASCADE SET NULL`
- D) `FOREIGN KEY (dept_id) ON DELETE SET NULL REFERENCES departments(id)`

### Question 15
What is a cascading chain?

- A) Multiple foreign keys in one table
- B) Referential actions that propagate through multiple tables
- C) Circular references between tables
- D) Self-referencing foreign keys

### Question 16
Which situation would cause a circular reference problem?

- A) Table A references Table B, Table B references Table C
- B) Table A references Table B, Table B references Table A
- C) Table A has multiple foreign keys
- D) Table A references itself

---

## Functions, Procedures & Triggers

### Question 17
What is the main difference between a function and a procedure in PostgreSQL?

- A) Functions can have parameters; procedures cannot
- B) Procedures can control transactions; functions cannot
- C) Functions are faster than procedures
- D) Procedures can return values; functions cannot

### Question 18
Which keyword is used to return a value from a PostgreSQL function?

- A) OUTPUT
- B) RETURN
- C) YIELD
- D) RESULT

### Question 19
What does `RETURNS SETOF` indicate in a function definition?

- A) The function returns a single row
- B) The function returns multiple rows
- C) The function has no return value
- D) The function returns a set of parameters

### Question 20
How do you call a stored procedure in PostgreSQL?

- A) `SELECT procedure_name()`
- B) `EXECUTE procedure_name()`
- C) `CALL procedure_name()`
- D) `RUN procedure_name()`

### Question 21
What is the purpose of `RETURNS TRIGGER` in a function definition?

- A) The function can create triggers
- B) The function is designed to be called by a trigger
- C) The function will trigger other functions
- D) The function returns trigger metadata

### Question 22
Which trigger timing runs BEFORE the operation?

- A) `CREATE TRIGGER trg AFTER INSERT`
- B) `CREATE TRIGGER trg BEFORE INSERT`
- C) `CREATE TRIGGER trg INSTEAD OF INSERT`
- D) `CREATE TRIGGER trg DURING INSERT`

### Question 23
What does the `NEW` variable contain in an UPDATE trigger?

- A) The original row before the update
- B) The row as it will be after the update
- C) The difference between old and new values
- D) NULL for UPDATE operations

### Question 24
What does `FOR EACH ROW` mean in a trigger definition?

- A) The trigger fires once per statement
- B) The trigger fires once for each affected row
- C) The trigger only affects one row
- D) The trigger runs on every row in the table

### Question 25
Which statement correctly creates a trigger?

- A) `CREATE TRIGGER trg INSERT ON orders EXECUTE fn_audit()`
- B) `CREATE TRIGGER trg ON orders BEFORE INSERT AS fn_audit`
- C) `CREATE TRIGGER trg BEFORE INSERT ON orders EXECUTE FUNCTION fn_audit()`
- D) `TRIGGER trg CREATE BEFORE INSERT orders fn_audit()`

### Question 26
What value should a BEFORE INSERT trigger return to cancel the operation?

- A) FALSE
- B) NULL
- C) 0
- D) CANCEL

### Question 27
What does `TG_OP` contain in a trigger function?

- A) The name of the trigger
- B) The operation type (INSERT, UPDATE, DELETE)
- C) The table name
- D) The number of rows affected

### Question 28
Which language is used for complex PostgreSQL trigger functions?

- A) SQL
- B) PL/pgSQL
- C) JavaScript
- D) T-SQL

---

## Transactions (TCL)

### Question 29
Which TCL command permanently saves all changes made in a transaction?

- A) SAVE
- B) COMMIT
- C) PERSIST
- D) END

### Question 30
What does ROLLBACK do?

- A) Saves changes and ends the transaction
- B) Undoes all changes since BEGIN
- C) Creates a savepoint
- D) Ends the transaction without changes

### Question 31
What command creates a point within a transaction to which you can roll back?

- A) CHECKPOINT
- B) SAVEPOINT
- C) MARK
- D) BOOKMARK

### Question 32
What does the "A" in ACID stand for?

- A) Availability
- B) Atomicity
- C) Authentication
- D) Accuracy

### Question 33
Which ACID property ensures that committed data survives system crashes?

- A) Atomicity
- B) Consistency
- C) Isolation
- D) Durability

### Question 34
What isolation level allows reading uncommitted data from other transactions?

- A) Read Committed
- B) Read Uncommitted
- C) Repeatable Read
- D) Serializable

### Question 35
Which isolation level is the DEFAULT in PostgreSQL?

- A) Read Uncommitted
- B) Read Committed
- C) Repeatable Read
- D) Serializable

### Question 36
What is a "dirty read"?

- A) Reading data that was never committed
- B) Reading the same data twice with different results
- C) Reading data that was added by another transaction
- D) Reading corrupted data

### Question 37
Which isolation level prevents phantom reads?

- A) Read Uncommitted
- B) Read Committed
- C) Repeatable Read (in PostgreSQL)
- D) Only Serializable

### Question 38
What happens when a deadlock occurs?

- A) Both transactions complete successfully
- B) PostgreSQL automatically aborts one transaction
- C) The database server crashes
- D) All tables are locked indefinitely

### Question 39
Which command would you use to lock specific rows for update?

- A) `LOCK TABLE`
- B) `SELECT ... FOR UPDATE`
- C) `SELECT ... WITH LOCK`
- D) `LOCK ROWS`

### Question 40
What is a "non-repeatable read"?

- A) A query returns different results when run twice in the same transaction
- B) A query that cannot be executed
- C) A query that times out
- D) A query that reads uncommitted data

### Question 41
How does `SELECT FOR UPDATE SKIP LOCKED` behave?

- A) Waits until locked rows become available
- B) Throws an error if rows are locked
- C) Skips over rows that are already locked
- D) Locks all rows in the table

### Question 42
What does `ROLLBACK TO SAVEPOINT name` do?

- A) Ends the entire transaction
- B) Undoes changes back to the savepoint but keeps the transaction open
- C) Creates a new savepoint
- D) Deletes the savepoint

---

## Access Control (DCL)

### Question 43
Which command gives privileges to a role?

- A) ALLOW
- B) PERMIT
- C) GRANT
- D) ENABLE

### Question 44
Which command removes privileges from a role?

- A) DENY
- B) REVOKE
- C) REMOVE
- D) DELETE

### Question 45
In PostgreSQL, what is the relationship between users and roles?

- A) Users can have multiple roles
- B) Roles can have multiple users
- C) Users and roles are the same thing
- D) Users inherit from roles only

### Question 46
Which privilege allows a role to access objects within a schema?

- A) SELECT
- B) USAGE
- C) ACCESS
- D) CONNECT

### Question 47
What does `WITH GRANT OPTION` allow?

- A) The role can revoke the privilege from others
- B) The role can grant the same privilege to other roles
- C) The privilege is automatically inherited
- D) The grant is temporary

### Question 48
Which command creates a role that can log in?

- A) `CREATE ROLE app LOGIN`
- B) `CREATE USER app`
- C) Both A and B
- D) Neither A nor B

### Question 49
What does Row-Level Security (RLS) control?

- A) Which columns a user can see
- B) Which rows a user can see
- C) Which tables a user can access
- D) Which schemas a user can use

### Question 50
How do you enable Row-Level Security on a table?

- A) `ENABLE RLS ON tablename`
- B) `ALTER TABLE tablename ENABLE ROW LEVEL SECURITY`
- C) `SET ROW SECURITY = ON FOR tablename`
- D) `GRANT ROW SECURITY ON tablename`

### Question 51
Which command sets privileges for future objects?

- A) `DEFAULT PRIVILEGES`
- B) `ALTER DEFAULT PRIVILEGES`
- C) `SET DEFAULT GRANTS`
- D) `GRANT DEFAULT`

### Question 52
What happens if you try to drop a role that owns objects?

- A) The objects are also dropped
- B) The drop fails
- C) Ownership transfers to the current user
- D) The objects become orphaned

### Question 53
Which SQL grants SELECT access to specific columns only?

- A) `GRANT SELECT ON users TO role`
- B) `GRANT SELECT (id, name) ON users TO role`
- C) `GRANT COLUMN SELECT id, name ON users TO role`
- D) `GRANT SELECT users.id, users.name TO role`

### Question 54
What is the principle of least privilege?

- A) Grant all privileges and revoke as needed
- B) Grant only the minimum privileges necessary
- C) Use the same privileges for all users
- D) Deny all privileges by default

---

## Scalability

### Question 55
What is the difference between vertical and horizontal scaling?

- A) Vertical adds servers; horizontal adds resources
- B) Vertical adds resources; horizontal adds servers
- C) Vertical scales reads; horizontal scales writes
- D) They are the same thing

### Question 56
What is a read replica?

- A) A copy of the database that handles both reads and writes
- B) A copy of the database that only handles read queries
- C) A backup that is never accessed
- D) A table that stores frequently read data

### Question 57
In primary-replica replication, which server handles writes?

- A) Replica
- B) Primary
- C) Both equally
- D) Neither

### Question 58
What is database partitioning?

- A) Distributing data across multiple servers
- B) Splitting a table into smaller physical pieces
- C) Creating multiple schemas
- D) Dividing queries across connections

### Question 59
Which partitioning type divides data by value ranges?

- A) List partitioning
- B) Hash partitioning
- C) Range partitioning
- D) Key partitioning

### Question 60
What is database sharding?

- A) Splitting a table by columns
- B) Distributing data across multiple independent databases
- C) Creating indexes on all columns
- D) Compressing database files

### Question 61
What makes a good shard key?

- A) Low cardinality values
- B) Timestamps that always increase
- C) Even distribution with queries hitting single shards
- D) Values that frequently change

### Question 62
What is the purpose of connection pooling?

- A) To speed up query execution
- B) To reuse database connections instead of creating new ones
- C) To encrypt connections
- D) To balance load across replicas

### Question 63
Which tool is commonly used for PostgreSQL connection pooling?

- A) Redis
- B) PgBouncer
- C) Nginx
- D) HAProxy

### Question 64
What is the main challenge of sharding?

- A) Increased storage costs
- B) Cross-shard queries become complex
- C) Reduced query performance
- D) Data corruption

### Question 65
What should you optimize FIRST before scaling horizontally?

- A) Add more servers
- B) Implement sharding
- C) Optimize queries and add proper indexes
- D) Set up replication

---

## ORM & Query Builders

### Question 66
What is the main advantage of using a query builder over raw SQL?

- A) Faster query execution
- B) Type-safe query construction and SQL injection protection
- C) Smaller database size
- D) Better indexing

### Question 67
What does ORM stand for?

- A) Object Relational Mapping
- B) Online Resource Management
- C) Ordered Record Model
- D) Object Reference Method

### Question 68
Which Drizzle function is used for equality comparisons?

- A) `equals()`
- B) `eq()`
- C) `equal()`
- D) `is()`

### Question 69
How does Drizzle handle SQL injection prevention?

- A) It escapes all string values
- B) It automatically uses parameterized queries
- C) It validates all input
- D) It encrypts all data

### Question 70
Which SQL injection payload could return all rows?

- A) `'; DELETE FROM users; --`
- B) `' OR '1'='1`
- C) `DROP TABLE users`
- D) `SELECT * FROM passwords`

### Question 71
What is the correct way to prevent SQL injection?

- A) Escape single quotes
- B) Validate that input is not malicious
- C) Use parameterized queries
- D) Use prepared statements
- E) Both C and D

### Question 72
How do you run a transaction in Drizzle?

- A) `db.transaction(async (tx) => { ... })`
- B) `db.beginTransaction()`
- C) `db.start().transaction()`
- D) `new Transaction(db)`

### Question 73
What is "second-order SQL injection"?

- A) Injection that runs twice
- B) Injection where stored data is later used unsafely in a query
- C) Injection in the second column
- D) A backup injection method

### Question 74
Which cannot be parameterized in SQL?

- A) WHERE clause values
- B) INSERT values
- C) Table and column names
- D) LIMIT values

### Question 75
How should you handle dynamic column names in queries?

- A) Use parameterized queries
- B) Use a whitelist of allowed values
- C) Escape the column names
- D) Use single quotes around names

### Question 76
What is the advantage of Drizzle's `sql` template literal?

- A) It concatenates strings directly
- B) It provides parameterization while allowing raw SQL
- C) It automatically creates tables
- D) It bypasses all safety checks

---

## Mixed/Integration Questions

### Question 77
Which combination correctly implements an audit trigger with soft delete?

- A) BEFORE DELETE trigger that sets deleted_at, then AFTER DELETE for audit log
- B) INSTEAD OF DELETE trigger that sets deleted_at and logs to audit table
- C) BEFORE DELETE trigger to prevent delete, AFTER UPDATE for audit
- D) AFTER DELETE trigger only

### Question 78
Which is NOT a valid ACID property?

- A) Atomicity
- B) Accuracy
- C) Isolation
- D) Durability

### Question 79
What happens if a BEFORE INSERT trigger returns NULL?

- A) The insert proceeds with NULL values
- B) The insert is cancelled
- C) An error is raised
- D) The trigger is skipped

### Question 80
How would you implement multi-tenant data isolation?

- A) Create separate databases for each tenant
- B) Use Row-Level Security with tenant_id checks
- C) Create separate schemas for each tenant
- D) All of the above are valid approaches

### Question 81
Which statement about PostgreSQL replication is FALSE?

- A) Replicas can handle read queries
- B) Synchronous replication guarantees data on replica before commit
- C) Replicas can handle write queries
- D) Streaming replication uses WAL

### Question 82
What is the purpose of `FOR UPDATE NOWAIT`?

- A) Lock rows without waiting
- B) Update without acquiring locks
- C) Fail immediately if rows are locked
- D) Wait indefinitely for locks

### Question 83
Which approach helps prevent lost updates in concurrent scenarios?

- A) Soft delete pattern
- B) Optimistic locking with version column
- C) Checksum validation
- D) History tables

### Question 84
How do you view all privileges on a table in PostgreSQL?

- A) `SHOW GRANTS ON tablename`
- B) `SELECT * FROM pg_privileges`
- C) Query `information_schema.table_privileges`
- D) `DESCRIBE tablename`

### Question 85
What is the main risk of using `ON DELETE CASCADE` on all foreign keys?

- A) Slower queries
- B) Unintended deletion of large amounts of data
- C) Circular reference errors
- D) Increased storage

### Question 86
In Drizzle ORM, how do you perform a LEFT JOIN?

- A) `db.select().from(a).leftJoin(b, eq(a.id, b.aId))`
- B) `db.select().from(a).join(b, 'left')`
- C) `db.leftJoin(a, b)`
- D) `db.select().from(a, b).where(leftJoin)`

### Question 87
Which isolation level provides the strongest guarantees?

- A) Read Uncommitted
- B) Read Committed
- C) Repeatable Read
- D) Serializable

### Question 88
What is the purpose of `ALTER DEFAULT PRIVILEGES`?

- A) Change existing privileges
- B) Set privileges for objects created in the future
- C) Reset privileges to default
- D) Grant privileges to the default role

### Question 89
Why might you choose NOT to use an ORM?

- A) You need maximum query performance with complex SQL
- B) You want type safety
- C) You want protection from SQL injection
- D) You want easier code maintenance

### Question 90
Which pattern would you use to track who modified a record and when?

- A) Soft delete
- B) Audit trail / audit columns
- C) Optimistic locking
- D) Checksum pattern

---

## Answer Key

| Q | A | Q | A | Q | A | Q | A | Q | A |
|---|---|---|---|---|---|---|---|---|---|
| 1 | B | 19 | B | 37 | C | 55 | B | 73 | B |
| 2 | C | 20 | C | 38 | B | 56 | B | 74 | C |
| 3 | C | 21 | B | 39 | B | 57 | B | 75 | B |
| 4 | B | 22 | B | 40 | A | 58 | B | 76 | B |
| 5 | C | 23 | B | 41 | C | 59 | C | 77 | B |
| 6 | B | 24 | B | 42 | B | 60 | B | 78 | B |
| 7 | B | 25 | C | 43 | C | 61 | C | 79 | B |
| 8 | B | 26 | B | 44 | B | 62 | B | 80 | D |
| 9 | B | 27 | B | 45 | C | 63 | B | 81 | C |
| 10 | C | 28 | B | 46 | B | 64 | B | 82 | C |
| 11 | B | 29 | B | 47 | B | 65 | C | 83 | B |
| 12 | B | 30 | B | 48 | C | 66 | B | 84 | C |
| 13 | B | 31 | B | 49 | B | 67 | A | 85 | B |
| 14 | B | 32 | B | 50 | B | 68 | B | 86 | A |
| 15 | B | 33 | D | 51 | B | 69 | B | 87 | D |
| 16 | B | 34 | B | 52 | B | 70 | B | 88 | B |
| 17 | B | 35 | B | 53 | B | 71 | E | 89 | A |
| 18 | B | 36 | A | 54 | B | 72 | A | 90 | B |

---

## Topic Distribution

| Topic | Questions | Count |
|-------|-----------|-------|
| Schema Patterns | 1-8 | 8 |
| Referential Actions | 9-16 | 8 |
| Functions/Procedures/Triggers | 17-28 | 12 |
| Transactions (TCL) | 29-42 | 14 |
| Access Control (DCL) | 43-54 | 12 |
| Scalability | 55-65 | 11 |
| ORM & Query Builders | 66-76 | 11 |
| Mixed/Integration | 77-90 | 14 |
| **Total** | | **90** |
