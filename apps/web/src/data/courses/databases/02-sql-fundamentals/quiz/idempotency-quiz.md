# Quiz: Idempotency in SQL Operations

!!! quiz
{
"title": "Statement A - Absolute Balance Update",
"question": "UPDATE accounts SET balance = 5000.00 WHERE account_id = 101;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Idempotent"]
}
!!!

!!! quiz
{
"title": "Statement B - Balance Increment",
"question": "UPDATE accounts SET balance = balance + 500.00 WHERE account_id = 101;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Non-idempotent"]
}
!!!

!!! quiz
{
"title": "Statement C - Audit Log with NOW()",
"question": "INSERT INTO audit_log (action, user_id, created_at)\nVALUES ('password_change', 42, NOW());\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Non-idempotent"]
}
!!!

!!! quiz
{
"title": "Statement D - Delete Expired Sessions",
"question": "DELETE FROM expired_sessions WHERE user_id = 88;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Idempotent"]
}
!!!

!!! quiz
{
"title": "Statement E - Insert with ON CONFLICT DO NOTHING",
"question": "INSERT INTO users (id, email, name)\nVALUES (1, 'alice@company.com', 'Alice')\nON CONFLICT (id) DO NOTHING;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Idempotent"]
}
!!!

!!! quiz
{
"title": "Statement F - Create Table",
"question": "CREATE TABLE reports (id SERIAL PRIMARY KEY, title TEXT);\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Non-idempotent"]
}
!!!

!!! quiz
{
"title": "Statement G - Create Table IF NOT EXISTS",
"question": "CREATE TABLE IF NOT EXISTS reports (id SERIAL PRIMARY KEY, title TEXT);\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Idempotent"]
}
!!!

!!! quiz
{
"title": "Statement H - Drop Table IF EXISTS",
"question": "DROP TABLE IF EXISTS temp_calculations;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Idempotent"]
}
!!!

!!! quiz
{
"title": "Statement I - Insert Order with CURRENT_TIMESTAMP",
"question": "INSERT INTO orders (customer_id, total, order_date)\nVALUES (15, 299.99, CURRENT_TIMESTAMP);\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Non-idempotent"]
}
!!!

!!! quiz
{
"title": "Statement J - Sequence Nextval",
"question": "SELECT nextval('invoice_number_seq');\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Non-idempotent"]
}
!!!

!!! quiz
{
"title": "Statement K - Absolute Price Update",
"question": "UPDATE products SET price = 19.99 WHERE sku = 'WIDGET-001';\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Idempotent"]
}
!!!

!!! quiz
{
"title": "Statement L - Stock Count Decrement",
"question": "UPDATE products SET stock_count = stock_count - 1 WHERE sku = 'WIDGET-001';\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Non-idempotent"]
}
!!!

!!! quiz
{
"title": "Statement M - Upsert with Fixed Value",
"question": "INSERT INTO inventory (product_id, quantity, warehouse_id)\nVALUES (500, 100, 'WH-EAST')\nON CONFLICT (product_id, warehouse_id)\nDO UPDATE SET quantity = EXCLUDED.quantity;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Idempotent"]
}
!!!

!!! quiz
{
"title": "Statement N - Upsert with Accumulation",
"question": "INSERT INTO inventory (product_id, quantity, warehouse_id)\nVALUES (500, 100, 'WH-EAST')\nON CONFLICT (product_id, warehouse_id)\nDO UPDATE SET quantity = inventory.quantity + EXCLUDED.quantity;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Non-idempotent"]
}
!!!

!!! quiz
{
"title": "Statement O - Fixed Timestamp Update",
"question": "UPDATE events SET processed_at = '2026-01-15 14:30:00' WHERE event_id = 777;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Idempotent"]
}
!!!

!!! quiz
{
"title": "Statement P - NOW() Timestamp Update",
"question": "UPDATE events SET processed_at = NOW() WHERE event_id = 777;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Non-idempotent"]
}
!!!

!!! quiz
{
"title": "Statement Q - Random UUID and NOW()",
"question": "INSERT INTO tokens (user_id, token, expires_at)\nVALUES (10, gen_random_uuid(), NOW() + INTERVAL '1 hour');\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Non-idempotent"]
}
!!!

!!! quiz
{
"title": "Statement R - Truncate Table",
"question": "TRUNCATE TABLE staging_data;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Idempotent"]
}
!!!

!!! quiz
{
"title": "Statement S - Delete Cache Entry",
"question": "DELETE FROM cache_entries WHERE cache_key = 'user:profile:42';\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Idempotent"]
}
!!!

!!! quiz
{
"title": "Statement T - Alter Table Add Column",
"question": "ALTER TABLE customers ADD COLUMN loyalty_points INT DEFAULT 0;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Non-idempotent"]
}
!!!

!!! quiz
{
"title": "Statement U - Drop Table",
"question": "DROP TABLE products;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Non-idempotent"]
}
!!!

!!! quiz
{
"title": "Statement V - Idempotency Key Pattern",
"question": "INSERT INTO payments (idempotency_key, amount, currency, status)\nVALUES ('req_abc123xyz', 149.99, 'USD', 'completed')\nON CONFLICT (idempotency_key) DO NOTHING;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Idempotent"]
}
!!!

!!! quiz
{
"title": "Statement W - Value Multiplication",
"question": "UPDATE counters SET value = value \* 2 WHERE counter_name = 'page_views';\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Non-idempotent"]
}
!!!

!!! quiz
{
"title": "Statement X - Theme Setting Update",
"question": "UPDATE settings SET theme = 'dark' WHERE user_id = 5;\n\nIs this statement idempotent or non-idempotent?",
"options": ["Idempotent", "Non-idempotent"],
"answers": ["Idempotent"]
}
!!!
