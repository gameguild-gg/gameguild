# Transactions & ACID Properties

Transactions ensure that database operations are executed reliably, even in the face of errors, crashes, or concurrent access.

---

## What is a Transaction?

A **transaction** is a sequence of operations performed as a single logical unit of work. Either ALL operations complete successfully, or NONE of them do.

### The Classic Example: Bank Transfer

```sql
-- Transfer $500 from Account A to Account B
UPDATE accounts SET balance = balance - 500 WHERE account_id = 'A';
UPDATE accounts SET balance = balance + 500 WHERE account_id = 'B';
```

What if the system crashes between these two statements?
- Account A loses $500
- Account B never receives it
- Money disappears!

**Solution:** Wrap both operations in a transaction.

---

## Transaction Control Language (TCL)

### BEGIN

Starts a new transaction:

```sql
BEGIN;
-- or
BEGIN TRANSACTION;
-- or
START TRANSACTION;
```

### COMMIT

Saves all changes made during the transaction:

```sql
BEGIN;
UPDATE accounts SET balance = balance - 500 WHERE account_id = 'A';
UPDATE accounts SET balance = balance + 500 WHERE account_id = 'B';
COMMIT;  -- Both updates are now permanent
```

### ROLLBACK

Undoes all changes made during the transaction:

```sql
BEGIN;
UPDATE accounts SET balance = balance - 500 WHERE account_id = 'A';
-- Oops, wrong amount!
ROLLBACK;  -- The update is undone
```

### Complete Example

```sql
BEGIN;

-- Debit source account
UPDATE accounts SET balance = balance - 500 WHERE account_id = 'A';

-- Check if source had enough funds
DO $$
BEGIN
    IF (SELECT balance FROM accounts WHERE account_id = 'A') < 0 THEN
        RAISE EXCEPTION 'Insufficient funds';
    END IF;
END $$;

-- Credit destination account
UPDATE accounts SET balance = balance + 500 WHERE account_id = 'B';

COMMIT;
```

If any error occurs, PostgreSQL automatically rolls back the transaction.

---

## SAVEPOINT

Savepoints allow partial rollbacks within a transaction:

```sql
BEGIN;

INSERT INTO orders (customer_id, total) VALUES (1, 100);
SAVEPOINT order_created;

INSERT INTO order_items (order_id, product_id, quantity) VALUES (1, 101, 2);
SAVEPOINT items_added;

-- Something goes wrong with payment
-- We want to keep the order but remove items
ROLLBACK TO SAVEPOINT order_created;

-- Add different items
INSERT INTO order_items (order_id, product_id, quantity) VALUES (1, 102, 1);

COMMIT;
```

### Savepoint Commands

```sql
SAVEPOINT savepoint_name;           -- Create savepoint
ROLLBACK TO SAVEPOINT savepoint_name;  -- Rollback to savepoint
RELEASE SAVEPOINT savepoint_name;   -- Remove savepoint (optional)
```

---

## ACID Properties

Transactions guarantee four properties, known as **ACID**:

### Atomicity

**"All or Nothing"**

Either all operations in a transaction complete successfully, or none of them do.

```sql
BEGIN;
UPDATE accounts SET balance = balance - 100 WHERE id = 1;  -- Succeeds
UPDATE accounts SET balance = balance + 100 WHERE id = 999;  -- Fails (no such account)
COMMIT;
-- Result: BOTH updates are rolled back
```

### Consistency

**"Database Rules Always Enforced"**

The database moves from one valid state to another. All constraints, triggers, and rules are satisfied.

```sql
BEGIN;
-- This violates a CHECK constraint
UPDATE products SET price = -50 WHERE id = 1;  
COMMIT;
-- Transaction fails; database remains consistent
```

### Isolation

**"Transactions Don't Interfere"**

Concurrent transactions see a consistent view of the database, as if they were running one at a time.

```sql
-- Transaction 1                    -- Transaction 2
BEGIN;                              BEGIN;
SELECT balance FROM accounts;       
-- Returns: 1000                    
                                    UPDATE accounts SET balance = 500;
                                    COMMIT;
SELECT balance FROM accounts;       
-- What does this return?           
-- (Depends on isolation level)     
COMMIT;
```

### Durability

**"Committed = Permanent"**

Once a transaction is committed, its changes survive system crashes, power failures, etc.

```sql
BEGIN;
INSERT INTO critical_data VALUES (...);
COMMIT;  -- Data is now safely on disk
-- Even if server crashes NOW, data is safe
```

---

## Isolation Levels

Isolation levels control how much transactions can see of each other's uncommitted changes.

### Read Uncommitted

Transactions can see uncommitted changes from other transactions ("dirty reads").

```sql
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
```

**Problems:** Dirty reads, non-repeatable reads, phantom reads

> **Note:** PostgreSQL doesn't truly support READ UNCOMMITTED; it's treated as READ COMMITTED.

### Read Committed (Default in PostgreSQL)

Transactions only see committed changes. Each query sees a fresh snapshot.

```sql
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- Or set as default
BEGIN;
-- This is the default level
```

**Example:**
```sql
-- Transaction 1                    -- Transaction 2
BEGIN;                              BEGIN;
SELECT price FROM products          
WHERE id = 1;  -- Returns: 100      
                                    UPDATE products SET price = 150 
                                    WHERE id = 1;
                                    COMMIT;
SELECT price FROM products          
WHERE id = 1;  -- Returns: 150      
-- (Non-repeatable read)            
COMMIT;
```

**Problems:** Non-repeatable reads, phantom reads

### Repeatable Read

Transactions see a consistent snapshot from the start. Same query always returns the same result.

```sql
SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
```

**Example:**
```sql
-- Transaction 1                    -- Transaction 2
BEGIN;                              BEGIN;
SET TRANSACTION ISOLATION LEVEL 
REPEATABLE READ;
SELECT price FROM products          
WHERE id = 1;  -- Returns: 100      
                                    UPDATE products SET price = 150 
                                    WHERE id = 1;
                                    COMMIT;
SELECT price FROM products          
WHERE id = 1;  -- Still returns: 100
-- (Consistent snapshot)            
COMMIT;
```

**Problems:** Phantom reads (in some databases, not PostgreSQL)

### Serializable

Strongest isolation. Transactions behave as if executed one at a time.

```sql
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
```

**Example:**
```sql
-- Transaction 1                    -- Transaction 2
BEGIN;                              BEGIN;
SET TRANSACTION ISOLATION LEVEL     SET TRANSACTION ISOLATION LEVEL 
SERIALIZABLE;                       SERIALIZABLE;
                                    
SELECT SUM(balance) FROM accounts;  SELECT SUM(balance) FROM accounts;
-- Returns: 10000                   -- Returns: 10000

UPDATE accounts SET balance = 0    
WHERE id = 1;
                                    UPDATE accounts SET balance = 0 
                                    WHERE id = 2;
                                    
COMMIT;                             COMMIT;
-- One of these will fail with a serialization error
```

### Isolation Level Comparison

| Level | Dirty Read | Non-Repeatable Read | Phantom Read | Performance |
|-------|------------|---------------------|--------------|-------------|
| Read Uncommitted | Yes | Yes | Yes | Fastest |
| Read Committed | No | Yes | Yes | Fast |
| Repeatable Read | No | No | Yes* | Medium |
| Serializable | No | No | No | Slowest |

*PostgreSQL's REPEATABLE READ also prevents phantom reads.

### Choosing an Isolation Level

| Use Case | Recommended Level |
|----------|------------------|
| Most OLTP applications | Read Committed (default) |
| Reports that need consistent snapshot | Repeatable Read |
| Financial transactions requiring exactness | Serializable |
| Maximum concurrency, can tolerate inconsistency | Read Committed |

---

## Concurrent Access Problems

### Dirty Read

Reading uncommitted data from another transaction.

```sql
-- Transaction 1                    -- Transaction 2
BEGIN;                              
UPDATE accounts SET balance = 0;    
                                    BEGIN;
                                    SELECT balance FROM accounts;
                                    -- Sees: 0 (uncommitted!)
ROLLBACK;                           
                                    -- Data was never actually 0!
```

**Prevented by:** Read Committed and higher

### Non-Repeatable Read

Same query returns different results within a transaction.

```sql
-- Transaction 1                    -- Transaction 2
BEGIN;                              
SELECT balance FROM accounts;       
-- Returns: 1000                    
                                    UPDATE accounts SET balance = 500;
                                    COMMIT;
SELECT balance FROM accounts;       
-- Returns: 500 (different!)        
COMMIT;
```

**Prevented by:** Repeatable Read and higher

### Phantom Read

New rows appear in a repeated query.

```sql
-- Transaction 1                    -- Transaction 2
BEGIN;                              
SELECT COUNT(*) FROM orders;        
-- Returns: 100                     
                                    INSERT INTO orders VALUES (...);
                                    COMMIT;
SELECT COUNT(*) FROM orders;        
-- Returns: 101 (phantom row!)      
COMMIT;
```

**Prevented by:** Serializable (and PostgreSQL's Repeatable Read)

### Lost Update

Two transactions update the same row; one update is lost.

```sql
-- Transaction 1                    -- Transaction 2
BEGIN;                              BEGIN;
SELECT balance FROM accounts;       SELECT balance FROM accounts;
-- Returns: 1000                    -- Returns: 1000

UPDATE accounts                     
SET balance = 1000 + 100;           UPDATE accounts 
-- Sets to: 1100                    SET balance = 1000 + 200;
                                    -- Sets to: 1200
COMMIT;                             COMMIT;
-- Final balance: 1200              
-- (Transaction 1's +100 is lost!)  
```

**Prevention:** Use row-level locking or Serializable isolation.

---

## Locking

### Implicit Locks

PostgreSQL automatically acquires locks as needed:

```sql
-- SELECT acquires a "Share" lock (allows other reads)
SELECT * FROM accounts WHERE id = 1;

-- UPDATE acquires an "Exclusive" lock (blocks other writes)
UPDATE accounts SET balance = 500 WHERE id = 1;
```

### Explicit Locks

#### SELECT FOR UPDATE

Lock rows for later update:

```sql
BEGIN;
SELECT * FROM accounts WHERE id = 1 FOR UPDATE;
-- This row is now locked; other transactions wait

-- Do some calculations...

UPDATE accounts SET balance = calculated_value WHERE id = 1;
COMMIT;
```

#### SELECT FOR UPDATE NOWAIT

Don't wait if locked:

```sql
BEGIN;
SELECT * FROM accounts WHERE id = 1 FOR UPDATE NOWAIT;
-- If already locked, immediately throws an error instead of waiting
```

#### SELECT FOR UPDATE SKIP LOCKED

Skip locked rows:

```sql
-- Useful for job queues
BEGIN;
SELECT * FROM jobs 
WHERE status = 'pending' 
ORDER BY created_at 
LIMIT 1 
FOR UPDATE SKIP LOCKED;
-- Gets first unlocked pending job
```

### Table-Level Locks

```sql
-- Prevent all writes
LOCK TABLE accounts IN SHARE MODE;

-- Prevent all access
LOCK TABLE accounts IN ACCESS EXCLUSIVE MODE;
```

---

## Deadlocks

A **deadlock** occurs when two transactions wait for each other indefinitely.

```sql
-- Transaction 1                    -- Transaction 2
BEGIN;                              BEGIN;
UPDATE accounts SET ... WHERE id=1; UPDATE accounts SET ... WHERE id=2;
-- Holds lock on row 1              -- Holds lock on row 2
                                    
UPDATE accounts SET ... WHERE id=2; UPDATE accounts SET ... WHERE id=1;
-- Waits for row 2...               -- Waits for row 1...
-- DEADLOCK!
```

**PostgreSQL detects deadlocks** and aborts one transaction with an error.

**Prevention:**
1. Always lock rows in the same order
2. Keep transactions short
3. Use NOWAIT to fail fast
4. Use Serializable isolation

---

## Best Practices

### 1. Keep Transactions Short

Long transactions hold locks and block other users.

```sql
-- BAD: Long transaction
BEGIN;
SELECT * FROM orders;  -- Lock acquired
-- User thinks for 5 minutes...
UPDATE orders SET status = 'shipped' WHERE id = 1;
COMMIT;

-- GOOD: Do work outside transaction
-- Fetch data, let user think, then:
BEGIN;
UPDATE orders SET status = 'shipped' WHERE id = 1;
COMMIT;
```

### 2. Don't Mix User Interaction

Never wait for user input inside a transaction.

### 3. Handle Errors Properly

```sql
BEGIN;
-- ... operations ...
-- If any error, PostgreSQL auto-rolls back
COMMIT;

-- Or explicitly in application code:
try {
    await db.query('BEGIN');
    await db.query('UPDATE ...');
    await db.query('COMMIT');
} catch (error) {
    await db.query('ROLLBACK');
    throw error;
}
```

### 4. Use Appropriate Isolation Level

Default (Read Committed) is fine for most cases. Only use Serializable when truly needed.

---

## Practice

### Exercise 1: Basic Transaction

Write a transaction that:
1. Inserts a new order
2. Inserts two order items
3. Updates the customer's total_orders count
4. If any step fails, all changes should be rolled back

### Exercise 2: Savepoint Usage

Write a transaction that processes multiple payments. If one payment fails, continue with the others using savepoints.

### Exercise 3: Isolation Level Effects

Demonstrate the difference between Read Committed and Repeatable Read using two concurrent sessions.

---

## Key Takeaways

1. **Transactions ensure ACID properties** - Atomicity, Consistency, Isolation, Durability
2. **BEGIN...COMMIT** wraps operations as a single unit
3. **ROLLBACK** undoes all changes since BEGIN
4. **SAVEPOINT** allows partial rollbacks
5. **Isolation levels** control visibility of concurrent changes
6. **Read Committed** (default) is appropriate for most applications
7. **Serializable** prevents all anomalies but reduces concurrency
8. **FOR UPDATE** locks rows for modification
9. **Keep transactions short** to reduce lock contention
10. **PostgreSQL detects and resolves deadlocks** automatically
