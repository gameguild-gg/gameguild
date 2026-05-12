# Quiz: Schema Patterns, TCL, DCL & ORM (Week 07)

## Instructions

This quiz tests your understanding of **schema design patterns**, **referential actions** (CASCADE, RESTRICT, SET NULL), **functions, procedures, and triggers**, **transactions (TCL)**, **access control (DCL)**, **scalability concepts**, and **ORMs/query builders**.

**Total: 92 questions**

Time estimate: 75-90 minutes

---

# PART A: Schema Patterns

---

!!! quiz
{
"title": "Schema Patterns 01",
"question": "What is the primary advantage of soft delete over hard delete?",
"options": ["Ability to recover deleted data", "Faster delete operations", "Better query performance", "Reduced storage space"],
"answers": ["Ability to recover deleted data"]
}
!!!

---

!!! quiz
{
"title": "Schema Patterns 02",
"question": "Which soft delete implementation correctly handles unique constraints?",
"options": ["CREATE UNIQUE INDEX idx_email ON users (email) WHERE deleted_at IS NULL;", "ALTER TABLE users ADD COLUMN deleted_at TIMESTAMP;", "ALTER TABLE users ADD CONSTRAINT uk_email UNIQUE (email, deleted_at);", "CREATE UNIQUE INDEX idx_email ON users (email);"],
"answers": ["CREATE UNIQUE INDEX idx_email ON users (email) WHERE deleted_at IS NULL;"]
}
!!!

---

!!! quiz
{
"title": "Schema Patterns 03",
"question": "What pattern is being implemented with a version column that increments on update and is checked in the WHERE clause?",
"options": ["Optimistic locking", "Checksum validation", "Audit trail", "Soft delete"],
"answers": ["Optimistic locking"]
}
!!!

---

!!! quiz
{
"title": "Schema Patterns 04",
"question": "Which anti-pattern stores data like 'tag1,tag2,tag3' in a single column?",
"options": ["Comma-separated values", "Polymorphic association", "Entity-Attribute-Value (EAV)", "One True Lookup Table (OTLT)"],
"answers": ["Comma-separated values"]
}
!!!

---

!!! quiz
{
"title": "Schema Patterns 05",
"question": "What is the purpose of storing a checksum with data?",
"options": ["To detect data corruption or tampering", "To speed up queries", "To compress the data", "To encrypt the data"],
"answers": ["To detect data corruption or tampering"]
}
!!!

---

!!! quiz
{
"title": "Schema Patterns 06",
"question": "Which statement about history tables is TRUE?",
"options": ["They store previous versions of rows", "They must have the same primary key as the original", "They replace the original table", "They require optimistic locking"],
"answers": ["They store previous versions of rows"]
}
!!!

---

!!! quiz
{
"title": "Schema Patterns 07",
"question": "What is a key drawback of the Entity-Attribute-Value (EAV) pattern?",
"options": ["Queries become complex and slow", "Cannot store NULL values", "Uses too much disk space", "Requires foreign keys"],
"answers": ["Queries become complex and slow"]
}
!!!

---

!!! quiz
{
"title": "Schema Patterns 08",
"question": "Which SQL creates an audit trail for update operations?",
"options": ["CREATE TRIGGER audit AFTER UPDATE", "CREATE VIEW audit AS SELECT *", "CREATE INDEX audit ON changes", "CREATE TRIGGER audit BEFORE UPDATE"],
"answers": ["CREATE TRIGGER audit AFTER UPDATE"]
}
!!!

---

# PART B: Referential Actions

---

!!! quiz
{
"title": "Referential Actions 01",
"question": "What happens when a category is deleted from a categories table if products reference it with ON DELETE SET NULL?",
"options": ["Products in that category have their category_id set to NULL", "The delete operation fails if any products reference that category", "Products in that category have their category_id set to 0", "All products in that category are also deleted"],
"answers": ["Products in that category have their category_id set to NULL"]
}
!!!

---

!!! quiz
{
"title": "Referential Actions 02",
"question": "Which ON DELETE action should you use when deleting a parent record should also delete all related child records?",
"options": ["ON DELETE CASCADE", "ON DELETE NO ACTION", "ON DELETE SET NULL", "ON DELETE RESTRICT"],
"answers": ["ON DELETE CASCADE"]
}
!!!

---

!!! quiz
{
"title": "Referential Actions 03",
"question": "What happens when you delete a parent row with ON DELETE CASCADE?",
"options": ["Child rows are also deleted", "Child rows remain unchanged", "Child foreign key columns are set to NULL", "The delete fails"],
"answers": ["Child rows are also deleted"]
}
!!!

---

!!! quiz
{
"title": "Referential Actions 04",
"question": "Which referential action sets the foreign key to its default value when the parent is deleted?",
"options": ["SET DEFAULT", "RESTRICT", "SET NULL", "CASCADE"],
"answers": ["SET DEFAULT"]
}
!!!

---

!!! quiz
{
"title": "Referential Actions 05",
"question": "What is the difference between RESTRICT and NO ACTION?",
"options": ["NO ACTION allows deferred checks; RESTRICT doesn't", "They are completely identical", "RESTRICT cascades; NO ACTION doesn't", "RESTRICT allows deferred checks; NO ACTION doesn't"],
"answers": ["NO ACTION allows deferred checks; RESTRICT doesn't"]
}
!!!

---

!!! quiz
{
"title": "Referential Actions 06",
"question": "When is ON DELETE SET NULL most appropriate?",
"options": ["Optional relationships where children can exist without parent", "Self-referencing tables with managers", "Parent-child hierarchies where children must be deleted", "When the foreign key column is NOT NULL"],
"answers": ["Optional relationships where children can exist without parent"]
}
!!!

---

!!! quiz
{
"title": "Referential Actions 07",
"question": "What happens with ON UPDATE CASCADE when a parent's primary key changes?",
"options": ["Child foreign keys are updated to match", "Child rows are deleted", "Child foreign keys become NULL", "The update fails"],
"answers": ["Child foreign keys are updated to match"]
}
!!!

---

!!! quiz
{
"title": "Referential Actions 08",
"question": "Which SQL correctly defines multiple referential actions?",
"options": ["FOREIGN KEY (dept_id) REFERENCES departments(id) ON DELETE SET NULL ON UPDATE CASCADE", "FOREIGN KEY (dept_id) REFERENCES departments(id) CASCADE SET NULL", "FOREIGN KEY (dept_id) ON DELETE SET NULL REFERENCES departments(id)", "FOREIGN KEY (dept_id) REFERENCES departments(id) ON DELETE SET NULL CASCADE"],
"answers": ["FOREIGN KEY (dept_id) REFERENCES departments(id) ON DELETE SET NULL ON UPDATE CASCADE"]
}
!!!

---

!!! quiz
{
"title": "Referential Actions 09",
"question": "What is a cascading chain?",
"options": ["Referential actions that propagate through multiple tables", "Multiple foreign keys in one table", "Self-referencing foreign keys", "Circular references between tables"],
"answers": ["Referential actions that propagate through multiple tables"]
}
!!!

---

!!! quiz
{
"title": "Referential Actions 10",
"question": "Which situation would cause a circular reference problem?",
"options": ["Table A references Table B, Table B references Table A", "Table A references itself", "Table A references Table B, Table B references Table C", "Table A has multiple foreign keys"],
"answers": ["Table A references Table B, Table B references Table A"]
}
!!!

---

# PART C: Functions, Procedures & Triggers

---

!!! quiz
{
"title": "Functions & Triggers 01",
"question": "What is the main difference between a function and a procedure in PostgreSQL?",
"options": ["Procedures can control transactions; functions cannot", "Functions are faster than procedures", "Procedures can return values; functions cannot", "Functions can have parameters; procedures cannot"],
"answers": ["Procedures can control transactions; functions cannot"]
}
!!!

---

!!! quiz
{
"title": "Functions & Triggers 02",
"question": "Which keyword is used to return a value from a PostgreSQL function?",
"options": ["RETURN", "RESULT", "YIELD", "OUTPUT"],
"answers": ["RETURN"]
}
!!!

---

!!! quiz
{
"title": "Functions & Triggers 03",
"question": "What does RETURNS SETOF indicate in a function definition?",
"options": ["The function returns multiple rows", "The function returns a set of parameters", "The function returns a single row", "The function has no return value"],
"answers": ["The function returns multiple rows"]
}
!!!

---

!!! quiz
{
"title": "Functions & Triggers 04",
"question": "How do you call a stored procedure in PostgreSQL?",
"options": ["CALL procedure_name()", "EXECUTE procedure_name()", "RUN procedure_name()", "SELECT procedure_name()"],
"answers": ["CALL procedure_name()"]
}
!!!

---

!!! quiz
{
"title": "Functions & Triggers 05",
"question": "What is the purpose of RETURNS TRIGGER in a function definition?",
"options": ["The function is designed to be called by a trigger", "The function returns trigger metadata", "The function can create triggers", "The function will trigger other functions"],
"answers": ["The function is designed to be called by a trigger"]
}
!!!

---

!!! quiz
{
"title": "Functions & Triggers 06",
"question": "Which trigger timing runs BEFORE the operation?",
"options": ["CREATE TRIGGER trg BEFORE INSERT", "CREATE TRIGGER trg INSTEAD OF INSERT", "CREATE TRIGGER trg AFTER INSERT", "CREATE TRIGGER trg DURING INSERT"],
"answers": ["CREATE TRIGGER trg BEFORE INSERT"]
}
!!!

---

!!! quiz
{
"title": "Functions & Triggers 07",
"question": "What does the NEW variable contain in an UPDATE trigger?",
"options": ["The row as it will be after the update", "The original row before the update", "NULL for UPDATE operations", "The difference between old and new values"],
"answers": ["The row as it will be after the update"]
}
!!!

---

!!! quiz
{
"title": "Functions & Triggers 08",
"question": "What does FOR EACH ROW mean in a trigger definition?",
"options": ["The trigger fires once for each affected row", "The trigger runs on every row in the table", "The trigger fires once per statement", "The trigger only affects one row"],
"answers": ["The trigger fires once for each affected row"]
}
!!!

---

!!! quiz
{
"title": "Functions & Triggers 09",
"question": "Which statement correctly creates a trigger?",
"options": ["CREATE TRIGGER trg BEFORE INSERT ON orders EXECUTE FUNCTION fn_audit()", "CREATE TRIGGER trg INSERT ON orders EXECUTE fn_audit()", "TRIGGER trg CREATE BEFORE INSERT orders fn_audit()", "CREATE TRIGGER trg ON orders BEFORE INSERT AS fn_audit"],
"answers": ["CREATE TRIGGER trg BEFORE INSERT ON orders EXECUTE FUNCTION fn_audit()"]
}
!!!

---

!!! quiz
{
"title": "Functions & Triggers 10",
"question": "What value should a BEFORE INSERT trigger return to cancel the operation?",
"options": ["NULL", "CANCEL", "FALSE", "0"],
"answers": ["NULL"]
}
!!!

---

!!! quiz
{
"title": "Functions & Triggers 11",
"question": "What does TG_OP contain in a trigger function?",
"options": ["The operation type (INSERT, UPDATE, DELETE)", "The number of rows affected", "The table name", "The name of the trigger"],
"answers": ["The operation type (INSERT, UPDATE, DELETE)"]
}
!!!

---

!!! quiz
{
"title": "Functions & Triggers 12",
"question": "Which language is used for complex PostgreSQL trigger functions?",
"options": ["PL/pgSQL", "T-SQL", "JavaScript", "SQL"],
"answers": ["PL/pgSQL"]
}
!!!

---

# PART E: Transactions (TCL)

---

!!! quiz
{
"title": "Transactions 01",
"question": "Which TCL command permanently saves all changes made in a transaction?",
"options": ["COMMIT", "PERSIST", "END", "SAVE"],
"answers": ["COMMIT"]
}
!!!

---

!!! quiz
{
"title": "Transactions 02",
"question": "What does ROLLBACK do?",
"options": ["Undoes all changes since BEGIN", "Ends the transaction without changes", "Creates a savepoint", "Saves changes and ends the transaction"],
"answers": ["Undoes all changes since BEGIN"]
}
!!!

---

!!! quiz
{
"title": "Transactions 03",
"question": "What command creates a point within a transaction to which you can roll back?",
"options": ["SAVEPOINT", "BOOKMARK", "CHECKPOINT", "MARK"],
"answers": ["SAVEPOINT"]
}
!!!

---

!!! quiz
{
"title": "Transactions 04",
"question": "What does the A in ACID stand for?",
"options": ["Atomicity", "Authentication", "Accuracy", "Availability"],
"answers": ["Atomicity"]
}
!!!

---

!!! quiz
{
"title": "Transactions 05",
"question": "Which ACID property ensures that committed data survives system crashes?",
"options": ["Durability", "Consistency", "Isolation", "Atomicity"],
"answers": ["Durability"]
}
!!!

---

!!! quiz
{
"title": "Transactions 06",
"question": "What isolation level allows reading uncommitted data from other transactions?",
"options": ["Read Uncommitted", "Serializable", "Read Committed", "Repeatable Read"],
"answers": ["Read Uncommitted"]
}
!!!

---

!!! quiz
{
"title": "Transactions 07",
"question": "Which isolation level is the DEFAULT in PostgreSQL?",
"options": ["Read Committed", "Serializable", "Repeatable Read", "Read Uncommitted"],
"answers": ["Read Committed"]
}
!!!

---

!!! quiz
{
"title": "Transactions 08",
"question": "What is a dirty read?",
"options": ["Reading data that was never committed", "Reading corrupted data", "Reading data that was added by another transaction", "Reading the same data twice with different results"],
"answers": ["Reading data that was never committed"]
}
!!!

---

!!! quiz
{
"title": "Transactions 09",
"question": "Which isolation level prevents phantom reads?",
"options": ["Repeatable Read (in PostgreSQL)", "Only Serializable", "Read Committed", "Read Uncommitted"],
"answers": ["Repeatable Read (in PostgreSQL)"]
}
!!!

---

!!! quiz
{
"title": "Transactions 10",
"question": "What happens when a deadlock occurs?",
"options": ["PostgreSQL automatically aborts one transaction", "Both transactions complete successfully", "All tables are locked indefinitely", "The database server crashes"],
"answers": ["PostgreSQL automatically aborts one transaction"]
}
!!!

---

!!! quiz
{
"title": "Transactions 11",
"question": "Which command would you use to lock specific rows for update?",
"options": ["SELECT ... FOR UPDATE", "LOCK ROWS", "SELECT ... WITH LOCK", "LOCK TABLE"],
"answers": ["SELECT ... FOR UPDATE"]
}
!!!

---

!!! quiz
{
"title": "Transactions 12",
"question": "What is a non-repeatable read?",
"options": ["A query returns different results when run twice in the same transaction", "A query that reads uncommitted data", "A query that cannot be executed", "A query that times out"],
"answers": ["A query returns different results when run twice in the same transaction"]
}
!!!

---

!!! quiz
{
"title": "Transactions 13",
"question": "How does SELECT FOR UPDATE SKIP LOCKED behave?",
"options": ["Skips over rows that are already locked", "Locks all rows in the table", "Waits until locked rows become available", "Throws an error if rows are locked"],
"answers": ["Skips over rows that are already locked"]
}
!!!

---

!!! quiz
{
"title": "Transactions 14",
"question": "What does ROLLBACK TO SAVEPOINT name do?",
"options": ["Undoes changes back to the savepoint but keeps the transaction open", "Creates a new savepoint", "Deletes the savepoint", "Ends the entire transaction"],
"answers": ["Undoes changes back to the savepoint but keeps the transaction open"]
}
!!!

---

# PART F: Access Control (DCL)

---

!!! quiz
{
"title": "Access Control 01",
"question": "Which command gives privileges to a role?",
"options": ["GRANT", "PERMIT", "ENABLE", "ALLOW"],
"answers": ["GRANT"]
}
!!!

---

!!! quiz
{
"title": "Access Control 02",
"question": "Which command removes privileges from a role?",
"options": ["REVOKE", "REMOVE", "DENY", "DELETE"],
"answers": ["REVOKE"]
}
!!!

---

!!! quiz
{
"title": "Access Control 03",
"question": "In PostgreSQL, what is the relationship between users and roles?",
"options": ["Users and roles are the same thing", "Users can have multiple roles", "Users inherit from roles only", "Roles can have multiple users"],
"answers": ["Users and roles are the same thing"]
}
!!!

---

!!! quiz
{
"title": "Access Control 04",
"question": "Which privilege allows a role to access objects within a schema?",
"options": ["USAGE", "CONNECT", "ACCESS", "SELECT"],
"answers": ["USAGE"]
}
!!!

---

!!! quiz
{
"title": "Access Control 05",
"question": "What does WITH GRANT OPTION allow?",
"options": ["The role can grant the same privilege to other roles", "The grant is temporary", "The privilege is automatically inherited", "The role can revoke the privilege from others"],
"answers": ["The role can grant the same privilege to other roles"]
}
!!!

---

!!! quiz
{
"title": "Access Control 06",
"question": "Which command creates a role that can log in?",
"options": ["Both A and B", "CREATE USER app", "Neither A nor B", "CREATE ROLE app LOGIN"],
"answers": ["Both A and B"]
}
!!!

---

!!! quiz
{
"title": "Access Control 07",
"question": "What does Row-Level Security (RLS) control?",
"options": ["Which rows a user can see", "Which schemas a user can use", "Which columns a user can see", "Which tables a user can access"],
"answers": ["Which rows a user can see"]
}
!!!

---

!!! quiz
{
"title": "Access Control 08",
"question": "How do you enable Row-Level Security on a table?",
"options": ["ALTER TABLE tablename ENABLE ROW LEVEL SECURITY", "GRANT ROW SECURITY ON tablename", "ENABLE RLS ON tablename", "SET ROW SECURITY = ON FOR tablename"],
"answers": ["ALTER TABLE tablename ENABLE ROW LEVEL SECURITY"]
}
!!!

---

!!! quiz
{
"title": "Access Control 09",
"question": "Which command sets privileges for future objects?",
"options": ["ALTER DEFAULT PRIVILEGES", "GRANT DEFAULT", "DEFAULT PRIVILEGES", "SET DEFAULT GRANTS"],
"answers": ["ALTER DEFAULT PRIVILEGES"]
}
!!!

---

!!! quiz
{
"title": "Access Control 10",
"question": "What happens if you try to drop a role that owns objects?",
"options": ["The drop fails", "Ownership transfers to the current user", "The objects become orphaned", "The objects are also dropped"],
"answers": ["The drop fails"]
}
!!!

---

!!! quiz
{
"title": "Access Control 11",
"question": "Which SQL grants SELECT access to specific columns only?",
"options": ["GRANT SELECT (id, name) ON users TO role", "GRANT SELECT users.id, users.name TO role", "GRANT COLUMN SELECT id, name ON users TO role", "GRANT SELECT ON users TO role"],
"answers": ["GRANT SELECT (id, name) ON users TO role"]
}
!!!

---

!!! quiz
{
"title": "Access Control 12",
"question": "What is the principle of least privilege?",
"options": ["Grant only the minimum privileges necessary", "Deny all privileges by default", "Use the same privileges for all users", "Grant all privileges and revoke as needed"],
"answers": ["Grant only the minimum privileges necessary"]
}
!!!

---

# PART G: Scalability

---

!!! quiz
{
"title": "Scalability 01",
"question": "What is the difference between vertical and horizontal scaling?",
"options": ["Vertical adds resources; horizontal adds servers", "They are the same thing", "Vertical adds servers; horizontal adds resources", "Vertical scales reads; horizontal scales writes"],
"answers": ["Vertical adds resources; horizontal adds servers"]
}
!!!

---

!!! quiz
{
"title": "Scalability 02",
"question": "What is a read replica?",
"options": ["A copy of the database that only handles read queries", "A table that stores frequently read data", "A backup that is never accessed", "A copy of the database that handles both reads and writes"],
"answers": ["A copy of the database that only handles read queries"]
}
!!!

---

!!! quiz
{
"title": "Scalability 03",
"question": "In primary-replica replication, which server handles writes?",
"options": ["Primary", "Replica", "Neither", "Both equally"],
"answers": ["Primary"]
}
!!!

---

!!! quiz
{
"title": "Scalability 04",
"question": "What is database partitioning?",
"options": ["Splitting a table into smaller physical pieces", "Creating multiple schemas", "Dividing queries across connections", "Distributing data across multiple servers"],
"answers": ["Splitting a table into smaller physical pieces"]
}
!!!

---

!!! quiz
{
"title": "Scalability 05",
"question": "Which partitioning type divides data by value ranges?",
"options": ["Range partitioning", "Key partitioning", "List partitioning", "Hash partitioning"],
"answers": ["Range partitioning"]
}
!!!

---

!!! quiz
{
"title": "Scalability 06",
"question": "What is database sharding?",
"options": ["Distributing data across multiple independent databases", "Compressing database files", "Splitting a table by columns", "Creating indexes on all columns"],
"answers": ["Distributing data across multiple independent databases"]
}
!!!

---

!!! quiz
{
"title": "Scalability 07",
"question": "What makes a good shard key?",
"options": ["Even distribution with queries hitting single shards", "Low cardinality values", "Values that frequently change", "Timestamps that always increase"],
"answers": ["Even distribution with queries hitting single shards"]
}
!!!

---

!!! quiz
{
"title": "Scalability 08",
"question": "What is the purpose of connection pooling?",
"options": ["To reuse database connections instead of creating new ones", "To balance load across replicas", "To encrypt connections", "To speed up query execution"],
"answers": ["To reuse database connections instead of creating new ones"]
}
!!!

---

!!! quiz
{
"title": "Scalability 09",
"question": "Which tool is commonly used for PostgreSQL connection pooling?",
"options": ["PgBouncer", "HAProxy", "Nginx", "Redis"],
"answers": ["PgBouncer"]
}
!!!

---

!!! quiz
{
"title": "Scalability 10",
"question": "What is the main challenge of sharding?",
"options": ["Cross-shard queries become complex", "Reduced query performance", "Increased storage costs", "Data corruption"],
"answers": ["Cross-shard queries become complex"]
}
!!!

---

!!! quiz
{
"title": "Scalability 11",
"question": "What should you optimize FIRST before scaling horizontally?",
"options": ["Optimize queries and add proper indexes", "Set up replication", "Add more servers", "Implement sharding"],
"answers": ["Optimize queries and add proper indexes"]
}
!!!

---

# PART H: ORM & Query Builders

---

!!! quiz
{
"title": "ORMs & Query Builders 01",
"question": "What is the main advantage of using a query builder over raw SQL?",
"options": ["Type-safe query construction and SQL injection protection", "Better indexing", "Faster query execution", "Smaller database size"],
"answers": ["Type-safe query construction and SQL injection protection"]
}
!!!

---

!!! quiz
{
"title": "ORMs & Query Builders 02",
"question": "What does ORM stand for?",
"options": ["Object Relational Mapping", "Object Reference Method", "Ordered Record Model", "Online Resource Management"],
"answers": ["Object Relational Mapping"]
}
!!!

---

!!! quiz
{
"title": "ORMs & Query Builders 03",
"question": "Which Drizzle function is used for equality comparisons?",
"options": ["eq()", "equal()", "is()", "equals()"],
"answers": ["eq()"]
}
!!!

---

!!! quiz
{
"title": "ORMs & Query Builders 04",
"question": "How does Drizzle handle SQL injection prevention?",
"options": ["It automatically uses parameterized queries", "It validates all input", "It encrypts all data", "It escapes all string values"],
"answers": ["It automatically uses parameterized queries"]
}
!!!

---

!!! quiz
{
"title": "ORMs & Query Builders 05",
"question": "Which SQL injection payload could return all rows?",
"options": ["' OR '1'='1", "SELECT * FROM passwords", "DROP TABLE users", "'; DELETE FROM users; --"],
"answers": ["' OR '1'='1"]
}
!!!

---

!!! quiz
{
"title": "ORMs & Query Builders 06",
"question": "What is the correct way to prevent SQL injection?",
"options": ["Both C and D", "Escape single quotes", "Use parameterized queries", "Validate that input is not malicious", "Use prepared statements"],
"answers": ["Both C and D"]
}
!!!

---

!!! quiz
{
"title": "ORMs & Query Builders 07",
"question": "How do you run a transaction in Drizzle?",
"options": ["db.transaction(async (tx) => { ... })", "db.start().transaction()", "new Transaction(db)", "db.beginTransaction()"],
"answers": ["db.transaction(async (tx) => { ... })"]
}
!!!

---

!!! quiz
{
"title": "ORMs & Query Builders 08",
"question": "What is second-order SQL injection?",
"options": ["Injection where stored data is later used unsafely in a query", "Injection that runs twice", "A backup injection method", "Injection in the second column"],
"answers": ["Injection where stored data is later used unsafely in a query"]
}
!!!

---

!!! quiz
{
"title": "ORMs & Query Builders 09",
"question": "Which cannot be parameterized in SQL?",
"options": ["Table and column names", "LIMIT values", "INSERT values", "WHERE clause values"],
"answers": ["Table and column names"]
}
!!!

---

!!! quiz
{
"title": "ORMs & Query Builders 10",
"question": "How should you handle dynamic column names in queries?",
"options": ["Use a whitelist of allowed values", "Escape the column names", "Use parameterized queries", "Use single quotes around names"],
"answers": ["Use a whitelist of allowed values"]
}
!!!

---

!!! quiz
{
"title": "ORMs & Query Builders 11",
"question": "What is the advantage of Drizzle's sql template literal?",
"options": ["It provides parameterization while allowing raw SQL", "It bypasses all safety checks", "It concatenates strings directly", "It automatically creates tables"],
"answers": ["It provides parameterization while allowing raw SQL"]
}
!!!

---

# PART I: Mixed/Integration Questions

---

!!! quiz
{
"title": "Mixed 01",
"question": "Which combination correctly implements an audit trigger with soft delete?",
"options": ["BEFORE DELETE trigger to prevent delete, AFTER UPDATE for audit", "AFTER DELETE trigger only", "INSTEAD OF DELETE trigger that sets deleted_at and logs to audit table", "BEFORE DELETE trigger that sets deleted_at, then AFTER DELETE for audit log"],
"answers": ["BEFORE DELETE trigger to prevent delete, AFTER UPDATE for audit"]
}
!!!

---

!!! quiz
{
"title": "Mixed 02",
"question": "Which is NOT a valid ACID property?",
"options": ["Accuracy", "Durability", "Isolation", "Atomicity"],
"answers": ["Accuracy"]
}
!!!

---

!!! quiz
{
"title": "Mixed 03",
"question": "What happens if a BEFORE INSERT trigger returns NULL?",
"options": ["The insert is cancelled", "An error is raised", "The trigger is skipped", "The insert proceeds with NULL values"],
"answers": ["The insert is cancelled"]
}
!!!

---

!!! quiz
{
"title": "Mixed 04",
"question": "How would you implement multi-tenant data isolation?",
"options": ["All of the above are valid approaches", "Create separate schemas for each tenant", "Use Row-Level Security with tenant_id checks", "Create separate databases for each tenant"],
"answers": ["All of the above are valid approaches"]
}
!!!

---

!!! quiz
{
"title": "Mixed 05",
"question": "Which statement about PostgreSQL replication is FALSE?",
"options": ["Replicas can handle write queries", "Streaming replication uses WAL", "Replicas can handle read queries", "Synchronous replication guarantees data on replica before commit"],
"answers": ["Replicas can handle write queries"]
}
!!!

---

!!! quiz
{
"title": "Mixed 06",
"question": "What is the purpose of FOR UPDATE NOWAIT?",
"options": ["Fail immediately if rows are locked", "Wait indefinitely for locks", "Lock rows without waiting", "Update without acquiring locks"],
"answers": ["Fail immediately if rows are locked"]
}
!!!

---

!!! quiz
{
"title": "Mixed 07",
"question": "Which approach helps prevent lost updates in concurrent scenarios?",
"options": ["Optimistic locking with version column", "History tables", "Soft delete pattern", "Checksum validation"],
"answers": ["Optimistic locking with version column"]
}
!!!

---

!!! quiz
{
"title": "Mixed 08",
"question": "How do you view all privileges on a table in PostgreSQL?",
"options": ["Query information_schema.table_privileges", "DESCRIBE tablename", "SELECT * FROM pg_privileges", "SHOW GRANTS ON tablename"],
"answers": ["Query information_schema.table_privileges"]
}
!!!

---

!!! quiz
{
"title": "Mixed 09",
"question": "What is the main risk of using ON DELETE CASCADE on all foreign keys?",
"options": ["Unintended deletion of large amounts of data", "Increased storage", "Circular reference errors", "Slower queries"],
"answers": ["Unintended deletion of large amounts of data"]
}
!!!

---

!!! quiz
{
"title": "Mixed 10",
"question": "In Drizzle ORM, how do you perform a LEFT JOIN?",
"options": ["db.select().from(a).leftJoin(b, eq(a.id, b.aId))", "db.leftJoin(a, b)", "db.select().from(a, b).where(leftJoin)", "db.select().from(a).join(b, 'left')"],
"answers": ["db.select().from(a).leftJoin(b, eq(a.id, b.aId))"]
}
!!!

---

!!! quiz
{
"title": "Mixed 11",
"question": "Which isolation level provides the strongest guarantees?",
"options": ["Serializable", "Repeatable Read", "Read Committed", "Read Uncommitted"],
"answers": ["Serializable"]
}
!!!

---

!!! quiz
{
"title": "Mixed 12",
"question": "What is the purpose of ALTER DEFAULT PRIVILEGES?",
"options": ["Set privileges for objects created in the future", "Reset privileges to default", "Change existing privileges", "Grant privileges to the default role"],
"answers": ["Set privileges for objects created in the future"]
}
!!!

---

!!! quiz
{
"title": "Mixed 13",
"question": "Why might you choose NOT to use an ORM?",
"options": ["You need maximum query performance with complex SQL", "You want easier code maintenance", "You want type safety", "You want protection from SQL injection"],
"answers": ["You need maximum query performance with complex SQL"]
}
!!!

---

!!! quiz
{
"title": "Mixed 14",
"question": "Which pattern would you use to track who modified a record and when?",
"options": ["Audit trail / audit columns", "Checksum pattern", "Soft delete", "Optimistic locking"],
"answers": ["Audit trail / audit columns"]
}
!!!
