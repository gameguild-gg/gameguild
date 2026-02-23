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

### Question 9 - ON DELETE SET NULL

**Consider the following table:**

```sql
CREATE TABLE products (
    id INT PRIMARY KEY,
    sku VARCHAR(50) UNIQUE,
    name VARCHAR(200) NOT NULL,
    price DECIMAL(10, 2) NOT NULL CHECK (price > 0),
    category_id INT REFERENCES categories(id) ON DELETE SET NULL
);
```

**What happens when a category is deleted from the `categories` table?**

- A) All products in that category are also deleted
- B) The delete operation fails if any products reference that category
- C) Products in that category have their `category_id` set to `NULL`
- D) Products in that category have their `category_id` set to `0`

---

### Question 10 - ON DELETE CASCADE

**Which `ON DELETE` action should you use when deleting a parent record should also delete all related child records?**

- A) `ON DELETE RESTRICT`
- B) `ON DELETE SET NULL`
- C) `ON DELETE CASCADE`
- D) `ON DELETE NO ACTION`

---

### Question 11

What happens when you delete a parent row with `ON DELETE CASCADE`?

- A) The delete fails
- B) Child rows are also deleted
- C) Child foreign key columns are set to NULL
- D) Child rows remain unchanged

### Question 12

Which referential action sets the foreign key to its default value when the parent is deleted?

- A) CASCADE
- B) SET NULL
- C) SET DEFAULT
- D) RESTRICT

### Question 13

What is the difference between `RESTRICT` and `NO ACTION`?

- A) RESTRICT allows deferred checks; NO ACTION doesn't
- B) NO ACTION allows deferred checks; RESTRICT doesn't
- C) They are completely identical
- D) RESTRICT cascades; NO ACTION doesn't

### Question 14

When is `ON DELETE SET NULL` most appropriate?

- A) Parent-child hierarchies where children must be deleted
- B) Optional relationships where children can exist without parent
- C) When the foreign key column is NOT NULL
- D) Self-referencing tables with managers

### Question 15

What happens with `ON UPDATE CASCADE` when a parent's primary key changes?

- A) The update fails
- B) Child foreign keys are updated to match
- C) Child rows are deleted
- D) Child foreign keys become NULL

### Question 16

Which SQL correctly defines multiple referential actions?

- A) `FOREIGN KEY (dept_id) REFERENCES departments(id) ON DELETE SET NULL CASCADE`
- B) `FOREIGN KEY (dept_id) REFERENCES departments(id) ON DELETE SET NULL ON UPDATE CASCADE`
- C) `FOREIGN KEY (dept_id) REFERENCES departments(id) CASCADE SET NULL`
- D) `FOREIGN KEY (dept_id) ON DELETE SET NULL REFERENCES departments(id)`

### Question 17

What is a cascading chain?

- A) Multiple foreign keys in one table
- B) Referential actions that propagate through multiple tables
- C) Circular references between tables
- D) Self-referencing foreign keys

### Question 18

Which situation would cause a circular reference problem?

- A) Table A references Table B, Table B references Table C
- B) Table A references Table B, Table B references Table A
- C) Table A has multiple foreign keys
- D) Table A references itself

---

## Functions, Procedures & Triggers

### Question 19

What is the main difference between a function and a procedure in PostgreSQL?

- A) Functions can have parameters; procedures cannot
- B) Procedures can control transactions; functions cannot
- C) Functions are faster than procedures
- D) Procedures can return values; functions cannot

### Question 20

Which keyword is used to return a value from a PostgreSQL function?

- A) OUTPUT
- B) RETURN
- C) YIELD
- D) RESULT

### Question 21

What does `RETURNS SETOF` indicate in a function definition?

- A) The function returns a single row
- B) The function returns multiple rows
- C) The function has no return value
- D) The function returns a set of parameters

### Question 22

How do you call a stored procedure in PostgreSQL?

- A) `SELECT procedure_name()`
- B) `EXECUTE procedure_name()`
- C) `CALL procedure_name()`
- D) `RUN procedure_name()`

### Question 23

What is the purpose of `RETURNS TRIGGER` in a function definition?

- A) The function can create triggers
- B) The function is designed to be called by a trigger
- C) The function will trigger other functions
- D) The function returns trigger metadata

### Question 24

Which trigger timing runs BEFORE the operation?

- A) `CREATE TRIGGER trg AFTER INSERT`
- B) `CREATE TRIGGER trg BEFORE INSERT`
- C) `CREATE TRIGGER trg INSTEAD OF INSERT`
- D) `CREATE TRIGGER trg DURING INSERT`

### Question 25

What does the `NEW` variable contain in an UPDATE trigger?

- A) The original row before the update
- B) The row as it will be after the update
- C) The difference between old and new values
- D) NULL for UPDATE operations

### Question 26

What does `FOR EACH ROW` mean in a trigger definition?

- A) The trigger fires once per statement
- B) The trigger fires once for each affected row
- C) The trigger only affects one row
- D) The trigger runs on every row in the table

### Question 27

Which statement correctly creates a trigger?

- A) `CREATE TRIGGER trg INSERT ON orders EXECUTE fn_audit()`
- B) `CREATE TRIGGER trg ON orders BEFORE INSERT AS fn_audit`
- C) `CREATE TRIGGER trg BEFORE INSERT ON orders EXECUTE FUNCTION fn_audit()`
- D) `TRIGGER trg CREATE BEFORE INSERT orders fn_audit()`

### Question 28

What value should a BEFORE INSERT trigger return to cancel the operation?

- A) FALSE
- B) NULL
- C) 0
- D) CANCEL

### Question 29

What does `TG_OP` contain in a trigger function?

- A) The name of the trigger
- B) The operation type (INSERT, UPDATE, DELETE)
- C) The table name
- D) The number of rows affected

### Question 30

Which language is used for complex PostgreSQL trigger functions?

- A) SQL
- B) PL/pgSQL
- C) JavaScript
- D) T-SQL

---

## Transactions (TCL)

### Question 31

Which TCL command permanently saves all changes made in a transaction?

- A) SAVE
- B) COMMIT
- C) PERSIST
- D) END

### Question 32

What does ROLLBACK do?

- A) Saves changes and ends the transaction
- B) Undoes all changes since BEGIN
- C) Creates a savepoint
- D) Ends the transaction without changes

### Question 33

What command creates a point within a transaction to which you can roll back?

- A) CHECKPOINT
- B) SAVEPOINT
- C) MARK
- D) BOOKMARK

### Question 34

What does the "A" in ACID stand for?

- A) Availability
- B) Atomicity
- C) Authentication
- D) Accuracy

### Question 35

Which ACID property ensures that committed data survives system crashes?

- A) Atomicity
- B) Consistency
- C) Isolation
- D) Durability

### Question 36

What isolation level allows reading uncommitted data from other transactions?

- A) Read Committed
- B) Read Uncommitted
- C) Repeatable Read
- D) Serializable

### Question 37

Which isolation level is the DEFAULT in PostgreSQL?

- A) Read Uncommitted
- B) Read Committed
- C) Repeatable Read
- D) Serializable

### Question 38

What is a "dirty read"?

- A) Reading data that was never committed
- B) Reading the same data twice with different results
- C) Reading data that was added by another transaction
- D) Reading corrupted data

### Question 39

Which isolation level prevents phantom reads?

- A) Read Uncommitted
- B) Read Committed
- C) Repeatable Read (in PostgreSQL)
- D) Only Serializable

### Question 40

What happens when a deadlock occurs?

- A) Both transactions complete successfully
- B) PostgreSQL automatically aborts one transaction
- C) The database server crashes
- D) All tables are locked indefinitely

### Question 41

Which command would you use to lock specific rows for update?

- A) `LOCK TABLE`
- B) `SELECT ... FOR UPDATE`
- C) `SELECT ... WITH LOCK`
- D) `LOCK ROWS`

### Question 42

What is a "non-repeatable read"?

- A) A query returns different results when run twice in the same transaction
- B) A query that cannot be executed
- C) A query that times out
- D) A query that reads uncommitted data

### Question 43

How does `SELECT FOR UPDATE SKIP LOCKED` behave?

- A) Waits until locked rows become available
- B) Throws an error if rows are locked
- C) Skips over rows that are already locked
- D) Locks all rows in the table

### Question 44

What does `ROLLBACK TO SAVEPOINT name` do?

- A) Ends the entire transaction
- B) Undoes changes back to the savepoint but keeps the transaction open
- C) Creates a new savepoint
- D) Deletes the savepoint

---

## Access Control (DCL)

### Question 45

Which command gives privileges to a role?

- A) ALLOW
- B) PERMIT
- C) GRANT
- D) ENABLE

### Question 46

Which command removes privileges from a role?

- A) DENY
- B) REVOKE
- C) REMOVE
- D) DELETE

### Question 47

In PostgreSQL, what is the relationship between users and roles?

- A) Users can have multiple roles
- B) Roles can have multiple users
- C) Users and roles are the same thing
- D) Users inherit from roles only

### Question 48

Which privilege allows a role to access objects within a schema?

- A) SELECT
- B) USAGE
- C) ACCESS
- D) CONNECT

### Question 49

What does `WITH GRANT OPTION` allow?

- A) The role can revoke the privilege from others
- B) The role can grant the same privilege to other roles
- C) The privilege is automatically inherited
- D) The grant is temporary

### Question 50

Which command creates a role that can log in?

- A) `CREATE ROLE app LOGIN`
- B) `CREATE USER app`
- C) Both A and B
- D) Neither A nor B

### Question 51

What does Row-Level Security (RLS) control?

- A) Which columns a user can see
- B) Which rows a user can see
- C) Which tables a user can access
- D) Which schemas a user can use

### Question 52

How do you enable Row-Level Security on a table?

- A) `ENABLE RLS ON tablename`
- B) `ALTER TABLE tablename ENABLE ROW LEVEL SECURITY`
- C) `SET ROW SECURITY = ON FOR tablename`
- D) `GRANT ROW SECURITY ON tablename`

### Question 53

Which command sets privileges for future objects?

- A) `DEFAULT PRIVILEGES`
- B) `ALTER DEFAULT PRIVILEGES`
- C) `SET DEFAULT GRANTS`
- D) `GRANT DEFAULT`

### Question 54

What happens if you try to drop a role that owns objects?

- A) The objects are also dropped
- B) The drop fails
- C) Ownership transfers to the current user
- D) The objects become orphaned

### Question 55

Which SQL grants SELECT access to specific columns only?

- A) `GRANT SELECT ON users TO role`
- B) `GRANT SELECT (id, name) ON users TO role`
- C) `GRANT COLUMN SELECT id, name ON users TO role`
- D) `GRANT SELECT users.id, users.name TO role`

### Question 56

What is the principle of least privilege?

- A) Grant all privileges and revoke as needed
- B) Grant only the minimum privileges necessary
- C) Use the same privileges for all users
- D) Deny all privileges by default

---

## Scalability

### Question 57

What is the difference between vertical and horizontal scaling?

- A) Vertical adds servers; horizontal adds resources
- B) Vertical adds resources; horizontal adds servers
- C) Vertical scales reads; horizontal scales writes
- D) They are the same thing

### Question 58

What is a read replica?

- A) A copy of the database that handles both reads and writes
- B) A copy of the database that only handles read queries
- C) A backup that is never accessed
- D) A table that stores frequently read data

### Question 59

In primary-replica replication, which server handles writes?

- A) Replica
- B) Primary
- C) Both equally
- D) Neither

### Question 60

What is database partitioning?

- A) Distributing data across multiple servers
- B) Splitting a table into smaller physical pieces
- C) Creating multiple schemas
- D) Dividing queries across connections

### Question 61

Which partitioning type divides data by value ranges?

- A) List partitioning
- B) Hash partitioning
- C) Range partitioning
- D) Key partitioning

### Question 62

What is database sharding?

- A) Splitting a table by columns
- B) Distributing data across multiple independent databases
- C) Creating indexes on all columns
- D) Compressing database files

### Question 63

What makes a good shard key?

- A) Low cardinality values
- B) Timestamps that always increase
- C) Even distribution with queries hitting single shards
- D) Values that frequently change

### Question 64

What is the purpose of connection pooling?

- A) To speed up query execution
- B) To reuse database connections instead of creating new ones
- C) To encrypt connections
- D) To balance load across replicas

### Question 65

Which tool is commonly used for PostgreSQL connection pooling?

- A) Redis
- B) PgBouncer
- C) Nginx
- D) HAProxy

### Question 66

What is the main challenge of sharding?

- A) Increased storage costs
- B) Cross-shard queries become complex
- C) Reduced query performance
- D) Data corruption

### Question 67

What should you optimize FIRST before scaling horizontally?

- A) Add more servers
- B) Implement sharding
- C) Optimize queries and add proper indexes
- D) Set up replication

---

## ORM & Query Builders

### Question 68

What is the main advantage of using a query builder over raw SQL?

- A) Faster query execution
- B) Type-safe query construction and SQL injection protection
- C) Smaller database size
- D) Better indexing

### Question 69

What does ORM stand for?

- A) Object Relational Mapping
- B) Online Resource Management
- C) Ordered Record Model
- D) Object Reference Method

### Question 70

Which Drizzle function is used for equality comparisons?

- A) `equals()`
- B) `eq()`
- C) `equal()`
- D) `is()`

### Question 71

How does Drizzle handle SQL injection prevention?

- A) It escapes all string values
- B) It automatically uses parameterized queries
- C) It validates all input
- D) It encrypts all data

### Question 72

Which SQL injection payload could return all rows?

- A) `'; DELETE FROM users; --`
- B) `' OR '1'='1`
- C) `DROP TABLE users`
- D) `SELECT * FROM passwords`

### Question 73

What is the correct way to prevent SQL injection?

- A) Escape single quotes
- B) Validate that input is not malicious
- C) Use parameterized queries
- D) Use prepared statements
- E) Both C and D

### Question 74

How do you run a transaction in Drizzle?

- A) `db.transaction(async (tx) => { ... })`
- B) `db.beginTransaction()`
- C) `db.start().transaction()`
- D) `new Transaction(db)`

### Question 75

What is "second-order SQL injection"?

- A) Injection that runs twice
- B) Injection where stored data is later used unsafely in a query
- C) Injection in the second column
- D) A backup injection method

### Question 76

Which cannot be parameterized in SQL?

- A) WHERE clause values
- B) INSERT values
- C) Table and column names
- D) LIMIT values

### Question 77

How should you handle dynamic column names in queries?

- A) Use parameterized queries
- B) Use a whitelist of allowed values
- C) Escape the column names
- D) Use single quotes around names

### Question 78

What is the advantage of Drizzle's `sql` template literal?

- A) It concatenates strings directly
- B) It provides parameterization while allowing raw SQL
- C) It automatically creates tables
- D) It bypasses all safety checks

---

## Mixed/Integration Questions

### Question 79

Which combination correctly implements an audit trigger with soft delete?

- A) BEFORE DELETE trigger that sets deleted_at, then AFTER DELETE for audit log
- B) INSTEAD OF DELETE trigger that sets deleted_at and logs to audit table
- C) BEFORE DELETE trigger to prevent delete, AFTER UPDATE for audit
- D) AFTER DELETE trigger only

### Question 80

Which is NOT a valid ACID property?

- A) Atomicity
- B) Accuracy
- C) Isolation
- D) Durability

### Question 81

What happens if a BEFORE INSERT trigger returns NULL?

- A) The insert proceeds with NULL values
- B) The insert is cancelled
- C) An error is raised
- D) The trigger is skipped

### Question 82

How would you implement multi-tenant data isolation?

- A) Create separate databases for each tenant
- B) Use Row-Level Security with tenant_id checks
- C) Create separate schemas for each tenant
- D) All of the above are valid approaches

### Question 83

Which statement about PostgreSQL replication is FALSE?

- A) Replicas can handle read queries
- B) Synchronous replication guarantees data on replica before commit
- C) Replicas can handle write queries
- D) Streaming replication uses WAL

### Question 84

What is the purpose of `FOR UPDATE NOWAIT`?

- A) Lock rows without waiting
- B) Update without acquiring locks
- C) Fail immediately if rows are locked
- D) Wait indefinitely for locks

### Question 85

Which approach helps prevent lost updates in concurrent scenarios?

- A) Soft delete pattern
- B) Optimistic locking with version column
- C) Checksum validation
- D) History tables

### Question 86

How do you view all privileges on a table in PostgreSQL?

- A) `SHOW GRANTS ON tablename`
- B) `SELECT * FROM pg_privileges`
- C) Query `information_schema.table_privileges`
- D) `DESCRIBE tablename`

### Question 87

What is the main risk of using `ON DELETE CASCADE` on all foreign keys?

- A) Slower queries
- B) Unintended deletion of large amounts of data
- C) Circular reference errors
- D) Increased storage

### Question 88

In Drizzle ORM, how do you perform a LEFT JOIN?

- A) `db.select().from(a).leftJoin(b, eq(a.id, b.aId))`
- B) `db.select().from(a).join(b, 'left')`
- C) `db.leftJoin(a, b)`
- D) `db.select().from(a, b).where(leftJoin)`

### Question 89

Which isolation level provides the strongest guarantees?

- A) Read Uncommitted
- B) Read Committed
- C) Repeatable Read
- D) Serializable

### Question 90

What is the purpose of `ALTER DEFAULT PRIVILEGES`?

- A) Change existing privileges
- B) Set privileges for objects created in the future
- C) Reset privileges to default
- D) Grant privileges to the default role

### Question 91

Why might you choose NOT to use an ORM?

- A) You need maximum query performance with complex SQL
- B) You want type safety
- C) You want protection from SQL injection
- D) You want easier code maintenance

### Question 92

Which pattern would you use to track who modified a record and when?

- A) Soft delete
- B) Audit trail / audit columns
- C) Optimistic locking
- D) Checksum pattern

---

## Answer Key

| Q   | A   | Q   | A   | Q   | A   | Q   | A   | Q   | A   |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1   | B   | 19  | B   | 37  | B   | 55  | B   | 73  | E   |
| 2   | C   | 20  | B   | 38  | A   | 56  | B   | 74  | A   |
| 3   | C   | 21  | B   | 39  | C   | 57  | B   | 75  | B   |
| 4   | B   | 22  | C   | 40  | B   | 58  | B   | 76  | C   |
| 5   | C   | 23  | B   | 41  | B   | 59  | B   | 77  | B   |
| 6   | B   | 24  | B   | 42  | A   | 60  | B   | 78  | B   |
| 7   | B   | 25  | B   | 43  | C   | 61  | C   | 79  | B   |
| 8   | B   | 26  | B   | 44  | B   | 62  | B   | 80  | B   |
| 9   | C   | 27  | C   | 45  | C   | 63  | C   | 81  | B   |
| 10  | C   | 28  | B   | 46  | B   | 64  | B   | 82  | D   |
| 11  | B   | 29  | B   | 47  | C   | 65  | B   | 83  | C   |
| 12  | C   | 30  | B   | 48  | B   | 66  | B   | 84  | C   |
| 13  | B   | 31  | B   | 49  | B   | 67  | C   | 85  | B   |
| 14  | B   | 32  | B   | 50  | C   | 68  | B   | 86  | C   |
| 15  | B   | 33  | B   | 51  | B   | 69  | A   | 87  | B   |
| 16  | B   | 34  | B   | 52  | B   | 70  | B   | 88  | A   |
| 17  | B   | 35  | D   | 53  | B   | 71  | B   | 89  | D   |
| 18  | B   | 36  | B   | 54  | B   | 72  | B   | 90  | B   |
| -   | -   | -   | -   | -   | -   | -   | -   | 91  | A   |
| -   | -   | -   | -   | -   | -   | -   | -   | 92  | B   |

---

## Topic Distribution

| Topic                         | Questions | Count  |
| ----------------------------- | --------- | ------ |
| Schema Patterns               | 1-8       | 8      |
| Referential Actions           | 9-18      | 10     |
| Functions/Procedures/Triggers | 19-30     | 12     |
| Transactions (TCL)            | 31-44     | 14     |
| Access Control (DCL)          | 45-56     | 12     |
| Scalability                   | 57-67     | 11     |
| ORM & Query Builders          | 68-78     | 11     |
| Mixed/Integration             | 79-92     | 14     |
| **Total**                     |           | **92** |
