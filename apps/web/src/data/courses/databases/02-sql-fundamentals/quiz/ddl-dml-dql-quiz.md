# Quiz: DDL, DML, and DQL Translation

!!! quiz
{
"title": "Creating Products Table - DDL",
"question": "Requirement: The e-commerce team needs to create a new table called products with the following columns: product_id (auto-incrementing integer, primary key), name (text, cannot be null, maximum 200 characters), price (decimal with 10 digits total and 2 decimal places), created_at (timestamp that defaults to the current time). Which SQL statement correctly implements this requirement?",
"options": [
"CREATE TABLE products (product_id INT PRIMARY KEY AUTO_INCREMENT, name TEXT(200) NOT NULL, price DECIMAL(2, 10), created_at TIMESTAMP DEFAULT NOW());",
"CREATE TABLE products (product_id SERIAL PRIMARY KEY, name VARCHAR(200) NOT NULL, price DECIMAL(10, 2), created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);",
"CREATE products TABLE (product_id SERIAL PRIMARY KEY, name VARCHAR(200), price DECIMAL(10, 2), created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);",
"INSERT INTO products (product_id, name, price, created_at) VALUES (SERIAL, VARCHAR(200), DECIMAL(10,2), CURRENT_TIMESTAMP);"
],
"answers": ["CREATE TABLE products (product_id SERIAL PRIMARY KEY, name VARCHAR(200) NOT NULL, price DECIMAL(10, 2), created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);"]
}
!!!

!!! quiz
{
"title": "ALTER TABLE Operations - DDL",
"question": "Given this SQL statement:\n\nALTER TABLE employees\nADD COLUMN department_id INT REFERENCES departments(id),\nDROP COLUMN temp_notes,\nALTER COLUMN salary SET NOT NULL;\n\nWhat operations does this statement perform?",
"options": [
"Creates a new table employees with three columns: department_id, temp_notes, and salary",
"Adds a foreign key column linking to departments, removes an existing column, and makes salary required",
"Updates all employee records to set their department, clear their notes, and validate their salary",
"Deletes employees without a department and those with null salaries"
],
"answers": ["Adds a foreign key column linking to departments, removes an existing column, and makes salary required"]
}
!!!

!!! quiz
{
"title": "Inventory Update - DML",
"question": "Requirement: The inventory manager needs to add 50 units of product SKU 'LAPTOP-PRO-15' to warehouse 'WH-EAST'. The inventory table has columns: id, sku, warehouse_code, quantity, last_updated. Which SQL statement correctly implements this requirement?",
"options": [
"SELECT quantity + 50 FROM inventory WHERE sku = 'LAPTOP-PRO-15' AND warehouse_code = 'WH-EAST';",
"UPDATE inventory SET quantity = 50, last_updated = NOW() WHERE sku = 'LAPTOP-PRO-15' AND warehouse_code = 'WH-EAST';",
"UPDATE inventory SET quantity = quantity + 50, last_updated = NOW() WHERE sku = 'LAPTOP-PRO-15' AND warehouse_code = 'WH-EAST';",
"INSERT INTO inventory (sku, warehouse_code, quantity) VALUES ('LAPTOP-PRO-15', 'WH-EAST', 50);"
],
"answers": ["UPDATE inventory SET quantity = quantity + 50, last_updated = NOW() WHERE sku = 'LAPTOP-PRO-15' AND warehouse_code = 'WH-EAST';"]
}
!!!

!!! quiz
{
"title": "SELECT Query Analysis - DQL",
"question": "Given this SQL statement:\n\nSELECT product_name, price, category\nFROM products\nWHERE price > 100 AND category = 'electronics'\nORDER BY price DESC\nLIMIT 10;\n\nWhat does this query retrieve?",
"options": [
"All electronics products sorted by price",
"The 10 most expensive electronics products priced above $100",
"Updates the top 10 electronics products to have a price over 100",
"Deletes electronics products that cost more than $100"
],
"answers": ["The 10 most expensive electronics products priced above $100"]
}
!!!

!!! quiz
{
"title": "Employee Deletion - DML",
"question": "Requirement: The HR system needs to permanently remove all employee records from the employees table where the termination_date is before January 1, 2024, and the status is 'inactive'. Which SQL statement correctly implements this requirement?",
"options": [
"DELETE employees WHERE termination_date < '2024-01-01' OR status = 'inactive';",
"DROP FROM employees WHERE termination_date < '2024-01-01' AND status = 'inactive';",
"DELETE FROM employees WHERE termination_date < '2024-01-01' AND status = 'inactive';",
"UPDATE employees SET deleted = true WHERE termination_date < '2024-01-01' AND status = 'inactive';"
],
"answers": ["DELETE FROM employees WHERE termination_date < '2024-01-01' AND status = 'inactive';"]
}
!!!

!!! quiz
{
"title": "Schema and Table Creation - DDL",
"question": "Given this SQL statement:\n\nCREATE SCHEMA IF NOT EXISTS analytics;\nCREATE TABLE analytics.daily_metrics (\n metric_date DATE PRIMARY KEY,\n active_users INT NOT NULL DEFAULT 0,\n revenue DECIMAL(12, 2) NOT NULL DEFAULT 0.00,\n CONSTRAINT positive_users CHECK (active_users >= 0),\n CONSTRAINT positive_revenue CHECK (revenue >= 0)\n);\n\nWhat does this statement accomplish?",
"options": [
"Creates a database called analytics with a table for storing daily user and revenue data with validation rules",
"Creates a schema namespace and a table within it for daily metrics, ensuring users and revenue cannot be negative",
"Inserts default values into the daily_metrics table for each date",
"Alters an existing daily_metrics table to add constraints for positive values"
],
"answers": ["Creates a schema namespace and a table within it for daily metrics, ensuring users and revenue cannot be negative"]
}
!!!

!!! quiz
{
"title": "User Registration Query - DQL",
"question": "Requirement: The admin needs to retrieve all users who registered after January 1, 2025, showing only their ID, email, and registration date, sorted by registration date with the newest first, limited to 50 results. Which SQL statement correctly implements this requirement?",
"options": [
"SELECT id, email, registered_at FROM users WHERE registered_at > '2025-01-01' ORDER BY registered_at DESC LIMIT 50;",
"SELECT * FROM users WHERE registered_at > '2025-01-01' ORDER BY registered_at LIMIT 50;",
"UPDATE users SET registered_at = '2025-01-01' WHERE id < 50;",
"SELECT id, email, registered_at FROM users WHERE registered_at > '2025-01-01' ORDER BY registered_at ASC;"
],
"answers": ["SELECT id, email, registered_at FROM users WHERE registered_at > '2025-01-01' ORDER BY registered_at DESC LIMIT 50;"]
}
!!!

!!! quiz
{
"title": "Audit Trail INSERT SELECT - DML",
"question": "Given this SQL statement:\n\nINSERT INTO audit_trail (action, table_name, record_id, old_value, new_value, changed_by, changed_at)\nSELECT 'UPDATE', 'products', p.id, p.price::TEXT, (p.price * 1.10)::TEXT, 'system_batch', NOW()\nFROM products p\nWHERE p.category = 'electronics';\n\nWhat does this statement do?",
"options": [
"Updates all electronics products to increase their price by 10%",
"Logs planned price changes for electronics products to an audit table without modifying the products",
"Deletes electronics products and moves them to an audit archive",
"Creates a backup of the products table filtered by electronics category"
],
"answers": ["Logs planned price changes for electronics products to an audit table without modifying the products"]
}
!!!

!!! quiz
{
"title": "Multiple Table Alterations - DDL",
"question": "Requirement: The database administrator needs to: 1) Add a new column phone (VARCHAR 20) to the customers table, 2) Make the existing email column required (NOT NULL), 3) Remove the fax column which is no longer used. Which SQL statements correctly implement ALL requirements?",
"options": [
"ALTER TABLE customers ADD COLUMN phone VARCHAR(20); ALTER TABLE customers ALTER COLUMN email SET NOT NULL; ALTER TABLE customers DROP COLUMN fax;",
"INSERT INTO customers (phone) VALUES (VARCHAR(20)); UPDATE customers SET email = NOT NULL; DELETE FROM customers WHERE fax IS NOT NULL;",
"CREATE COLUMN phone VARCHAR(20) ON customers; SET email NOT NULL IN customers; DROP fax FROM customers;",
"ALTER TABLE customers ADD phone; ALTER TABLE customers MODIFY email NOT NULL; ALTER TABLE customers REMOVE COLUMN fax;"
],
"answers": ["ALTER TABLE customers ADD COLUMN phone VARCHAR(20); ALTER TABLE customers ALTER COLUMN email SET NOT NULL; ALTER TABLE customers DROP COLUMN fax;"]
}
!!!

!!! quiz
{
"title": "Order History Copy - DML",
"question": "Given this SQL statement:\n\nINSERT INTO order_history (order_id, status, changed_at, changed_by)\nSELECT id, status, updated_at, 'migration_script'\nFROM orders\nWHERE status = 'completed';\n\nWhat does this statement do?",
"options": [
"Updates all completed orders to add history records",
"Copies data from completed orders into a history table without modifying the original orders",
"Deletes completed orders after backing them up",
"Creates a new orders table from the history table"
],
"answers": ["Copies data from completed orders into a history table without modifying the original orders"]
}
!!!
