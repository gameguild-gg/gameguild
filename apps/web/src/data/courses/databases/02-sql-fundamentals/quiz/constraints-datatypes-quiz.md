# Quiz: Constraints and Data Types

!!! quiz
{
"title": "Multiple PRIMARY KEY Columns",
"question": "True or False: A table can have multiple columns defined as PRIMARY KEY as long as they are in different statements.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

!!! quiz
{
"title": "UNIQUE Constraint and NULL Values",
"question": "True or False: A UNIQUE constraint allows multiple NULL values in PostgreSQL.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

!!! quiz
{
"title": "VARCHAR vs CHAR Storage",
"question": "True or False: VARCHAR(100) and CHAR(100) both store exactly 100 characters for every value inserted.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

!!! quiz
{
"title": "Foreign Key Index Creation",
"question": "True or False: A FOREIGN KEY constraint automatically creates an index on the referencing column.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

!!! quiz
{
"title": "DECIMAL Precision Limits",
"question": "True or False: DECIMAL(5, 2) can store the value 999.99 but not 1000.00.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

!!! quiz
{
"title": "DEFAULT and NOT NULL Compatibility",
"question": "True or False: A column with a DEFAULT value cannot also have a NOT NULL constraint.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

!!! quiz
{
"title": "SERIAL Data Type",
"question": "True or False: SERIAL is a true data type in PostgreSQL that stores auto-incrementing values.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

!!! quiz
{
"title": "CHECK Constraint Scope",
"question": "True or False: A CHECK constraint can reference columns from other tables.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

!!! quiz
{
"title": "TIMESTAMP WITH TIME ZONE Storage",
"question": "True or False: TIMESTAMP WITH TIME ZONE stores the timezone information alongside the timestamp.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

!!! quiz
{
"title": "TEXT vs VARCHAR Performance",
"question": "True or False: TEXT and VARCHAR without a length limit have identical performance characteristics in PostgreSQL.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

!!! quiz
{
"title": "Account Balance Data Type",
"question": "Which data type is most appropriate for storing a user's account balance in a financial application?",
"options": ["FLOAT", "REAL", "DECIMAL(12, 2)", "INTEGER"],
"answers": ["DECIMAL(12, 2)"]
}
!!!

!!! quiz
{
"title": "Orders Table Constraints",
"question": "Given this table definition:\n\nCREATE TABLE orders (\n id SERIAL PRIMARY KEY,\n customer_id INT NOT NULL REFERENCES customers(id),\n total DECIMAL(10, 2) CHECK (total >= 0),\n status VARCHAR(20) DEFAULT 'pending'\n);\n\nWhich statement about the constraints is INCORRECT?",
"options": [
"id will auto-increment and must be unique",
"customer_id must exist in the customers table",
"total can be NULL as long as it's not negative",
"status will be NULL if not provided during insert"
],
"answers": ["status will be NULL if not provided during insert"]
}
!!!

!!! quiz
{
"title": "CHAR Column Overflow",
"question": "What happens when you try to insert a string 'Hello World' into a CHAR(5) column?",
"options": [
"It stores 'Hello' (truncated to 5 characters)",
"It raises an error because the value is too long",
"It stores 'Hello World' and ignores the length limit",
"It stores 'Hello' padded with spaces"
],
"answers": ["It raises an error because the value is too long"]
}
!!!

!!! quiz
{
"title": "Unique Non-Empty Email Constraint",
"question": "Which constraint combination ensures that an email column contains unique, non-empty values?",
"options": [
"email VARCHAR(255) PRIMARY KEY",
"email VARCHAR(255) UNIQUE",
"email VARCHAR(255) UNIQUE NOT NULL",
"email VARCHAR(255) CHECK (email IS NOT NULL)"
],
"answers": ["email VARCHAR(255) UNIQUE NOT NULL"]
}
!!!

!!! quiz
{
"title": "Foreign Key Syntax",
"question": "Which of the following is a valid way to define a foreign key relationship when creating a table?",
"options": [
"CREATE TABLE orders (id SERIAL PRIMARY KEY, customer_id INT, FOREIGN KEY customer_id REFERENCES customers(id));",
"CREATE TABLE orders (id SERIAL PRIMARY KEY, customer_id INT REFERENCES customers(id));",
"CREATE TABLE orders (id SERIAL PRIMARY KEY, customer_id INT, LINK customer_id TO customers(id));",
"CREATE TABLE orders (id SERIAL PRIMARY KEY, customer_id INT FOREIGN customers(id));"
],
"answers": ["CREATE TABLE orders (id SERIAL PRIMARY KEY, customer_id INT REFERENCES customers(id));"]
}
!!!

!!! quiz
{
"title": "Distributed System Identifiers",
"question": "Which data type should you use to store a PostgreSQL-generated unique identifier that is safe for distributed systems?",
"options": ["SERIAL", "BIGSERIAL", "UUID", "INT"],
"answers": ["UUID"]
}
!!!

!!! quiz
{
"title": "BOOLEAN Data Type",
"question": "Which statement about BOOLEAN data type in PostgreSQL is correct?",
"options": [
"It can only store TRUE or FALSE, not NULL",
"It accepts values like 'yes', 'no', '1', '0', 't', 'f' as valid boolean inputs",
"It is stored as a string internally",
"PostgreSQL does not support a native BOOLEAN type"
],
"answers": ["It accepts values like 'yes', 'no', '1', '0', 't', 'f' as valid boolean inputs"]
}
!!!

!!! quiz
{
"title": "Composite Primary Key",
"question": "Which statement correctly defines a composite primary key?",
"options": [
"CREATE TABLE order_items (order_id INT PRIMARY KEY, product_id INT PRIMARY KEY, quantity INT);",
"CREATE TABLE order_items (order_id INT, product_id INT, quantity INT, PRIMARY KEY (order_id, product_id));",
"CREATE TABLE order_items (order_id INT UNIQUE, product_id INT UNIQUE, quantity INT);",
"CREATE TABLE order_items (order_id INT NOT NULL, product_id INT NOT NULL, quantity INT, UNIQUE (order_id, product_id));"
],
"answers": ["CREATE TABLE order_items (order_id INT, product_id INT, quantity INT, PRIMARY KEY (order_id, product_id));"]
}
!!!

!!! quiz
{
"title": "Integer Type Differences",
"question": "What is the difference between SMALLINT, INT, and BIGINT?",
"options": [
"They store the same range but with different performance characteristics",
"SMALLINT uses 2 bytes, INT uses 4 bytes, BIGINT uses 8 bytes, each with increasing value ranges",
"They are aliases for the same underlying data type",
"BIGINT is for decimal numbers, INT and SMALLINT are for whole numbers only"
],
"answers": ["SMALLINT uses 2 bytes, INT uses 4 bytes, BIGINT uses 8 bytes, each with increasing value ranges"]
}
!!!

!!! quiz
{
"title": "Multi-Timezone Timestamps",
"question": "You need to store timestamps that will be used across multiple timezones. Users in New York and Tokyo should see the correct local time. Which data type should you use?",
"options": ["TIMESTAMP", "TIMESTAMP WITH TIME ZONE", "DATE", "TIME"],
"answers": ["TIMESTAMP WITH TIME ZONE"]
}
!!!

!!! quiz
{
"title": "INT Maximum Value",
"question": "What is the maximum value that can be stored in an INT (4-byte signed integer)?",
"options": ["32,767", "2,147,483,647", "9,223,372,036,854,775,807", "4,294,967,295"],
"answers": ["2,147,483,647"]
}
!!!

!!! quiz
{
"title": "NOT NULL Constraint Behavior",
"question": "Which statement about NOT NULL constraints is correct?",
"options": [
"NOT NULL is implied for PRIMARY KEY columns",
"NOT NULL cannot be added to an existing column with ALTER TABLE",
"NOT NULL prevents empty strings ('') from being inserted",
"NOT NULL is automatically applied to all FOREIGN KEY columns"
],
"answers": ["NOT NULL is implied for PRIMARY KEY columns"]
}
!!!

!!! quiz
{
"title": "Discount Percent Range Constraint",
"question": "You want to ensure that a discount_percent column only accepts values between 0 and 100. Which constraint should you use?",
"options": [
"discount_percent INT UNIQUE",
"discount_percent INT NOT NULL",
"discount_percent INT CHECK (discount_percent >= 0 AND discount_percent <= 100)",
"discount_percent INT DEFAULT 0"
],
"answers": ["discount_percent INT CHECK (discount_percent >= 0 AND discount_percent <= 100)"]
}
!!!

!!! quiz
{
"title": "Successful INSERT Statement",
"question": "Given this table, which INSERT statement will succeed?\n\nCREATE TABLE users (\n id SERIAL PRIMARY KEY,\n username VARCHAR(50) UNIQUE NOT NULL,\n email VARCHAR(100) NOT NULL,\n age INT CHECK (age >= 18),\n role VARCHAR(20) DEFAULT 'user'\n);",
"options": [
"INSERT INTO users (username, email) VALUES ('alice', 'alice@test.com');",
"INSERT INTO users (username, email, age) VALUES ('bob', 'bob@test.com', 16);",
"INSERT INTO users (email, age) VALUES ('charlie@test.com', 25);",
"INSERT INTO users (username, email, age) VALUES (NULL, 'dan@test.com', 30);"
],
"answers": ["INSERT INTO users (username, email) VALUES ('alice', 'alice@test.com');"]
}
!!!

!!! quiz
{
"title": "Geographic Coordinates Data Type",
"question": "Which data type combination is most appropriate for a table storing geographic coordinates (latitude/longitude)?",
"options": [
"latitude VARCHAR(20), longitude VARCHAR(20)",
"latitude INT, longitude INT",
"latitude DECIMAL(9, 6), longitude DECIMAL(9, 6)",
"latitude FLOAT, longitude FLOAT"
],
"answers": ["latitude DECIMAL(9, 6), longitude DECIMAL(9, 6)"]
}
!!!
