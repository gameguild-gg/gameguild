# Access Control & Data Control Language (DCL)

PostgreSQL implements a robust role-based access control (RBAC) system to manage who can do what in the database.

---

## Core Concepts

### Users vs Roles

In PostgreSQL, **users and roles are the same thing**. The terms are used interchangeably.

- `CREATE USER` is an alias for `CREATE ROLE ... LOGIN`
- A role can log in (user) or just hold privileges (group role)

### Privileges

**Privileges** determine what actions a role can perform:
- SELECT, INSERT, UPDATE, DELETE on tables
- EXECUTE on functions
- USAGE on schemas
- CREATE on databases

### Ownership

Every database object has an **owner**. The owner automatically has all privileges on that object.

---

## Creating Roles

### Basic Role Creation

```sql
-- Create a login role (user)
CREATE ROLE app_user LOGIN PASSWORD 'secure_password';

-- Or equivalently:
CREATE USER app_user WITH PASSWORD 'secure_password';

-- Create a group role (no login)
CREATE ROLE readonly;

-- Create a superuser (has all privileges)
CREATE ROLE admin SUPERUSER LOGIN PASSWORD 'admin_pass';
```

### Role Attributes

```sql
CREATE ROLE developer WITH
    LOGIN                   -- Can log in
    PASSWORD 'dev_pass'     -- Login password
    CREATEDB                -- Can create databases
    CREATEROLE              -- Can create other roles
    VALID UNTIL '2025-12-31'; -- Password expiration
```

| Attribute | Description |
|-----------|-------------|
| LOGIN / NOLOGIN | Can/cannot log in |
| SUPERUSER / NOSUPERUSER | Has/doesn't have all privileges |
| CREATEDB / NOCREATEDB | Can/cannot create databases |
| CREATEROLE / NOCREATEROLE | Can/cannot create roles |
| INHERIT / NOINHERIT | Does/doesn't inherit privileges |
| REPLICATION / NOREPLICATION | Can/cannot initiate replication |
| CONNECTION LIMIT n | Maximum concurrent connections |

### Modifying Roles

```sql
-- Change password
ALTER ROLE app_user WITH PASSWORD 'new_password';

-- Add attribute
ALTER ROLE developer WITH CREATEDB;

-- Remove attribute
ALTER ROLE developer WITH NOCREATEDB;

-- Rename role
ALTER ROLE old_name RENAME TO new_name;
```

### Dropping Roles

```sql
-- Remove a role (must not own objects)
DROP ROLE app_user;

-- Transfer ownership first, then drop
REASSIGN OWNED BY old_user TO new_user;
DROP OWNED BY old_user;
DROP ROLE old_user;
```

---

## GRANT

The `GRANT` statement gives privileges to roles.

### Table Privileges

```sql
-- Grant SELECT on a table
GRANT SELECT ON products TO app_user;

-- Grant multiple privileges
GRANT SELECT, INSERT, UPDATE ON orders TO app_user;

-- Grant all privileges on a table
GRANT ALL PRIVILEGES ON customers TO admin_role;

-- Grant on all tables in schema
GRANT SELECT ON ALL TABLES IN SCHEMA public TO readonly_role;
```

### Column-Level Privileges

```sql
-- Grant access to specific columns only
GRANT SELECT (id, name, email) ON users TO support_role;
GRANT UPDATE (email, phone) ON users TO support_role;
```

### Schema Privileges

```sql
-- Allow role to access objects in schema
GRANT USAGE ON SCHEMA sales TO app_user;

-- Allow role to create objects in schema
GRANT CREATE ON SCHEMA sales TO developer;

-- Combined
GRANT USAGE, CREATE ON SCHEMA sales TO developer;
```

### Database Privileges

```sql
-- Connect to database
GRANT CONNECT ON DATABASE myapp TO app_user;

-- Create schemas in database
GRANT CREATE ON DATABASE myapp TO developer;
```

### Sequence Privileges

```sql
-- Allow using a sequence
GRANT USAGE ON SEQUENCE orders_id_seq TO app_user;

-- Allow using all sequences in schema
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO app_user;
```

### Function Privileges

```sql
-- Allow executing a function
GRANT EXECUTE ON FUNCTION calculate_total(int) TO app_user;

-- Allow executing all functions in schema
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO app_user;
```

### WITH GRANT OPTION

Allows the grantee to grant the same privilege to others:

```sql
GRANT SELECT ON products TO team_lead WITH GRANT OPTION;

-- team_lead can now do:
GRANT SELECT ON products TO developer;
```

---

## REVOKE

The `REVOKE` statement removes privileges from roles.

### Basic Revoke

```sql
-- Revoke SELECT privilege
REVOKE SELECT ON products FROM app_user;

-- Revoke multiple privileges
REVOKE INSERT, UPDATE, DELETE ON products FROM app_user;

-- Revoke all privileges
REVOKE ALL PRIVILEGES ON customers FROM old_employee;
```

### Revoke on Multiple Objects

```sql
-- Revoke from all tables in schema
REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM old_user;

-- Revoke from all sequences
REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM old_user;
```

### REVOKE GRANT OPTION

Remove only the ability to grant to others:

```sql
REVOKE GRANT OPTION FOR SELECT ON products FROM team_lead;
-- team_lead can still SELECT, but can't grant it to others
```

### CASCADE vs RESTRICT

```sql
-- Also revoke from anyone who received the privilege through this role
REVOKE SELECT ON products FROM team_lead CASCADE;

-- Fail if others received the privilege through this role
REVOKE SELECT ON products FROM team_lead RESTRICT;
```

---

## Role Membership (Groups)

Roles can be members of other roles, inheriting their privileges.

### Creating Group Roles

```sql
-- Create group roles for different access levels
CREATE ROLE readonly;
CREATE ROLE readwrite;
CREATE ROLE admin;

-- Grant privileges to groups
GRANT SELECT ON ALL TABLES IN SCHEMA public TO readonly;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO readwrite;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO admin;
```

### Assigning Users to Groups

```sql
-- Add user to group
GRANT readonly TO alice;
GRANT readwrite TO bob;
GRANT admin TO charlie;

-- User inherits all privileges from the group
```

### Checking Effective Privileges

```sql
-- See current roles
SELECT current_user, session_user;

-- See all roles the current user belongs to
SELECT * FROM pg_roles WHERE rolname = current_user;

-- Check specific privileges
SELECT has_table_privilege('alice', 'products', 'SELECT');
SELECT has_schema_privilege('alice', 'public', 'USAGE');
```

### Role Switching

```sql
-- Temporarily switch to another role (must be a member)
SET ROLE readonly;

-- Switch back
RESET ROLE;

-- Check current role
SELECT current_user;
```

---

## Default Privileges

Set privileges for objects created in the future:

```sql
-- All future tables created by developer will be readable by app_user
ALTER DEFAULT PRIVILEGES FOR ROLE developer 
IN SCHEMA public 
GRANT SELECT ON TABLES TO app_user;

-- All future sequences created in public schema
ALTER DEFAULT PRIVILEGES IN SCHEMA public 
GRANT USAGE ON SEQUENCES TO app_user;

-- All future functions
ALTER DEFAULT PRIVILEGES IN SCHEMA public 
GRANT EXECUTE ON FUNCTIONS TO app_user;
```

---

## Row-Level Security (RLS)

Control access to specific rows, not just tables.

### Enable RLS

```sql
-- Create a table with tenant data
CREATE TABLE documents (
    id SERIAL PRIMARY KEY,
    tenant_id INT NOT NULL,
    title TEXT,
    content TEXT
);

-- Enable row-level security
ALTER TABLE documents ENABLE ROW LEVEL SECURITY;
```

### Create Policies

```sql
-- Users can only see their own tenant's documents
CREATE POLICY tenant_isolation ON documents
    FOR SELECT
    USING (tenant_id = current_setting('app.tenant_id')::int);

-- Users can only insert documents for their tenant
CREATE POLICY tenant_insert ON documents
    FOR INSERT
    WITH CHECK (tenant_id = current_setting('app.tenant_id')::int);

-- Combined policy for all operations
CREATE POLICY tenant_all ON documents
    USING (tenant_id = current_setting('app.tenant_id')::int)
    WITH CHECK (tenant_id = current_setting('app.tenant_id')::int);
```

### Using RLS in Applications

```sql
-- Set tenant context before queries
SET app.tenant_id = '42';

-- Now queries are automatically filtered
SELECT * FROM documents;  -- Only sees tenant 42's documents
```

### Bypass RLS

```sql
-- Table owner and superusers bypass RLS by default
-- Force them to follow policies:
ALTER TABLE documents FORCE ROW LEVEL SECURITY;

-- Create a bypass policy for admins
CREATE POLICY admin_all ON documents
    TO admin_role
    USING (true)
    WITH CHECK (true);
```

---

## Common Access Control Patterns

### 1. Application Role Pattern

```sql
-- One database user for the application
CREATE ROLE myapp_user LOGIN PASSWORD 'app_password';

-- Grant minimum necessary privileges
GRANT USAGE ON SCHEMA public TO myapp_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO myapp_user;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO myapp_user;

-- Set default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA public 
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO myapp_user;
```

### 2. Read Replica Pattern

```sql
-- Read-only role for reporting/analytics
CREATE ROLE analytics_user LOGIN PASSWORD 'analytics_pass';

GRANT USAGE ON SCHEMA public TO analytics_user;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO analytics_user;

-- Prevent any writes
-- (SELECT-only grant means no INSERT/UPDATE/DELETE)
```

### 3. Microservice Pattern

```sql
-- Each service has its own role with limited access
CREATE ROLE orders_service LOGIN PASSWORD 'orders_pass';
CREATE ROLE inventory_service LOGIN PASSWORD 'inventory_pass';

-- Orders service: full access to orders, read-only on products
GRANT ALL ON orders TO orders_service;
GRANT SELECT ON products TO orders_service;

-- Inventory service: full access to inventory, read-only on orders
GRANT ALL ON inventory TO inventory_service;
GRANT SELECT ON orders TO inventory_service;
```

### 4. Development vs Production

```sql
-- Development: more permissive
CREATE ROLE dev_user WITH LOGIN PASSWORD 'dev_pass' CREATEDB;
GRANT ALL PRIVILEGES ON DATABASE dev_db TO dev_user;

-- Production: locked down
CREATE ROLE prod_app WITH LOGIN PASSWORD 'secure_pass';
GRANT CONNECT ON DATABASE prod_db TO prod_app;
GRANT USAGE ON SCHEMA public TO prod_app;
GRANT SELECT, INSERT, UPDATE ON specified_tables TO prod_app;
-- No DELETE, no TRUNCATE, no schema modifications
```

---

## Viewing Permissions

### List All Roles

```sql
SELECT rolname, rolsuper, rolinherit, rolcreaterole, 
       rolcreatedb, rolcanlogin
FROM pg_roles
ORDER BY rolname;
```

### Table Privileges

```sql
-- Detailed privileges on a table
SELECT grantee, privilege_type
FROM information_schema.table_privileges
WHERE table_name = 'products';

-- Quick view with \dp in psql
-- \dp products
```

### Role Memberships

```sql
SELECT r.rolname AS role, 
       m.rolname AS member
FROM pg_auth_members am
JOIN pg_roles r ON r.oid = am.roleid
JOIN pg_roles m ON m.oid = am.member
ORDER BY role, member;
```

### Default Privileges

```sql
SELECT * FROM pg_default_acl;
```

---

## Security Best Practices

### 1. Principle of Least Privilege

Grant only the minimum privileges needed:

```sql
-- BAD: Over-privileged
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO app_user;

-- GOOD: Specific privileges
GRANT SELECT, INSERT ON orders TO app_user;
GRANT SELECT ON products TO app_user;
GRANT UPDATE (status) ON orders TO app_user;
```

### 2. Use Roles for Groups

```sql
-- Define roles by function
CREATE ROLE customer_support;
CREATE ROLE order_processor;
CREATE ROLE analyst;

-- Grant appropriate privileges to roles
GRANT SELECT ON customers, orders TO customer_support;
GRANT SELECT, UPDATE ON orders TO order_processor;
GRANT SELECT ON ALL TABLES IN SCHEMA analytics TO analyst;

-- Assign users to roles
GRANT customer_support TO alice, bob;
```

### 3. Separate Read and Write

```sql
CREATE ROLE app_read;
CREATE ROLE app_write;

GRANT SELECT ON ALL TABLES IN SCHEMA public TO app_read;
GRANT INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO app_write;

-- Read-heavy operations use app_read
-- Write operations use app_write
```

### 4. Audit Privilege Changes

```sql
-- Log all privilege changes
-- (Configure in postgresql.conf)
log_statement = 'ddl'
```

### 5. Regular Access Reviews

```sql
-- Script to audit who has what access
SELECT 
    t.schemaname,
    t.tablename,
    u.usename AS owner,
    array_agg(a.privilege_type) AS privileges,
    a.grantee
FROM pg_tables t
JOIN pg_user u ON t.tableowner = u.usename
LEFT JOIN information_schema.table_privileges a 
    ON a.table_name = t.tablename
GROUP BY t.schemaname, t.tablename, u.usename, a.grantee
ORDER BY t.schemaname, t.tablename;
```

---

## Practice

### Exercise 1: Role Hierarchy

Create a role hierarchy for an e-commerce application:
- `readonly`: SELECT only
- `support`: SELECT on all tables, UPDATE on orders.status
- `manager`: All privileges on orders, products
- `admin`: Superuser

### Exercise 2: Row-Level Security

Implement multi-tenant isolation where:
- Each user can only see data for their company
- Admins can see all data
- New inserts are automatically tagged with the user's company

### Exercise 3: Privilege Audit

Write a query to find all roles that have DELETE privileges on any table.

---

## Key Takeaways

1. **PostgreSQL uses roles** for both users and groups
2. **GRANT** gives privileges; **REVOKE** removes them
3. **Roles can be members of other roles**, inheriting privileges
4. **Column-level privileges** restrict access to specific columns
5. **Default privileges** apply to future objects
6. **Row-level security (RLS)** controls access to individual rows
7. **WITH GRANT OPTION** allows delegating privileges
8. **Principle of least privilege**: Grant minimum necessary access
9. **Use group roles** to simplify privilege management
10. **Regularly audit** access permissions
