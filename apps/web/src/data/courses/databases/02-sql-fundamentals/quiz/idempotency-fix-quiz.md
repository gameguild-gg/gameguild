# Quiz: Making SQL Operations Idempotent

!!! quiz
{
"title": "Newsletter Subscription Idempotency",
"question": "INSERT INTO newsletter_subscribers (email, subscribed_at)\nVALUES ('user@example.com', NOW());\n\nWhich modification makes this idempotent?",
"options": [
"INSERT INTO newsletter_subscribers (email, subscribed_at) VALUES ('user@example.com', NOW()) ON CONFLICT (email) DO UPDATE SET subscribed_at = NOW();",
"INSERT INTO newsletter_subscribers (email, subscribed_at) VALUES ('user@example.com', NOW()) ON CONFLICT (email) DO NOTHING;",
"INSERT INTO newsletter_subscribers (email, subscribed_at) VALUES ('user@example.com', '2026-01-15 12:00:00');",
"Cannot be made idempotent"
],
"answers": ["INSERT INTO newsletter_subscribers (email, subscribed_at) VALUES ('user@example.com', NOW()) ON CONFLICT (email) DO NOTHING;"]
}
!!!

!!! quiz
{
"title": "Leaderboard Score Increment",
"question": "UPDATE leaderboard SET score = score + 10 WHERE player_id = 55;\n\nWhich modification makes this idempotent?",
"options": [
"UPDATE leaderboard SET score = score + 10 WHERE player_id = 55 AND last_updated < NOW();",
"UPDATE leaderboard SET score = 10 WHERE player_id = 55;",
"INSERT INTO leaderboard (player_id, score) VALUES (55, 10) ON CONFLICT (player_id) DO UPDATE SET score = leaderboard.score + 10;",
"Cannot be made idempotent"
],
"answers": ["Cannot be made idempotent"]
}
!!!

!!! quiz
{
"title": "Click Events Logging",
"question": "INSERT INTO click_events (user_id, page_url, clicked_at)\nVALUES (42, '/products/laptop', NOW());\n\nWhich modification makes this idempotent?",
"options": [
"INSERT INTO click_events (user_id, page_url, clicked_at) VALUES (42, '/products/laptop', NOW()) ON CONFLICT DO NOTHING;",
"INSERT INTO click_events (event_id, user_id, page_url, clicked_at) VALUES ('evt_abc123', 42, '/products/laptop', NOW()) ON CONFLICT (event_id) DO NOTHING;",
"UPDATE click_events SET clicked_at = NOW() WHERE user_id = 42 AND page_url = '/products/laptop';",
"Cannot be made idempotent"
],
"answers": ["INSERT INTO click_events (event_id, user_id, page_url, clicked_at) VALUES ('evt_abc123', 42, '/products/laptop', NOW()) ON CONFLICT (event_id) DO NOTHING;"]
}
!!!

!!! quiz
{
"title": "ALTER TABLE ADD COLUMN Idempotency",
"question": "ALTER TABLE orders ADD COLUMN shipping_address TEXT;\n\nWhich modification makes this idempotent?",
"options": [
"ALTER TABLE orders ADD COLUMN IF NOT EXISTS shipping_address TEXT;",
"ALTER TABLE IF EXISTS orders ADD COLUMN shipping_address TEXT;",
"DROP COLUMN IF EXISTS shipping_address; ALTER TABLE orders ADD COLUMN shipping_address TEXT;",
"Cannot be made idempotent"
],
"answers": ["ALTER TABLE orders ADD COLUMN IF NOT EXISTS shipping_address TEXT;"]
}
!!!

!!! quiz
{
"title": "Inventory Quantity Decrement",
"question": "UPDATE inventory SET quantity = quantity - 5 WHERE product_id = 101 AND warehouse_id = 'MAIN';\n\nWhich modification makes this idempotent?",
"options": [
"UPDATE inventory SET quantity = quantity - 5 WHERE product_id = 101 AND warehouse_id = 'MAIN' AND quantity >= 5;",
"UPDATE inventory SET quantity = 95 WHERE product_id = 101 AND warehouse_id = 'MAIN';",
"UPDATE inventory SET quantity = GREATEST(quantity - 5, 0) WHERE product_id = 101 AND warehouse_id = 'MAIN';",
"Cannot be made idempotent"
],
"answers": ["Cannot be made idempotent"]
}
!!!

!!! quiz
{
"title": "Order Items Insertion",
"question": "INSERT INTO order_items (order_id, product_id, quantity, unit_price)\nVALUES (1001, 55, 2, 29.99);\n\nAssume order_items has an auto-incrementing id primary key. Which modification makes this idempotent?",
"options": [
"INSERT INTO order_items (order_id, product_id, quantity, unit_price) VALUES (1001, 55, 2, 29.99) ON CONFLICT DO NOTHING;",
"INSERT INTO order_items (order_id, product_id, quantity, unit_price) VALUES (1001, 55, 2, 29.99) ON CONFLICT (order_id, product_id) DO NOTHING;",
"MERGE INTO order_items USING (VALUES (1001, 55, 2, 29.99)) AS src ON order_items.order_id = src.order_id;",
"Cannot be made idempotent"
],
"answers": ["INSERT INTO order_items (order_id, product_id, quantity, unit_price) VALUES (1001, 55, 2, 29.99) ON CONFLICT (order_id, product_id) DO NOTHING;"]
}
!!!

!!! quiz
{
"title": "User Session Update with NOW()",
"question": "UPDATE user_sessions SET last_active = NOW() WHERE session_id = 'sess_xyz789';\n\nWhich modification makes this idempotent?",
"options": [
"UPDATE user_sessions SET last_active = NOW() WHERE session_id = 'sess_xyz789' AND last_active < NOW();",
"UPDATE user_sessions SET last_active = '2026-01-15 14:30:00' WHERE session_id = 'sess_xyz789';",
"INSERT INTO user_sessions (session_id, last_active) VALUES ('sess_xyz789', NOW()) ON CONFLICT (session_id) DO UPDATE SET last_active = NOW();",
"Cannot be made idempotent"
],
"answers": ["UPDATE user_sessions SET last_active = '2026-01-15 14:30:00' WHERE session_id = 'sess_xyz789';"]
}
!!!

!!! quiz
{
"title": "API Requests with Random UUID",
"question": "INSERT INTO api_requests (request_id, endpoint, response_code, logged_at)\nVALUES (gen_random_uuid(), '/api/users', 200, NOW());\n\nWhich modification makes this idempotent?",
"options": [
"INSERT INTO api_requests (request_id, endpoint, response_code, logged_at) VALUES (gen_random_uuid(), '/api/users', 200, NOW()) ON CONFLICT (request_id) DO NOTHING;",
"INSERT INTO api_requests (request_id, endpoint, response_code, logged_at) VALUES ('fixed-uuid-value', '/api/users', 200, NOW()) ON CONFLICT (request_id) DO NOTHING;",
"INSERT INTO api_requests (request_id, endpoint, response_code, logged_at) VALUES ('fixed-uuid-value', '/api/users', 200, '2026-01-15 12:00:00') ON CONFLICT (request_id) DO NOTHING;",
"Cannot be made idempotent"
],
"answers": ["INSERT INTO api_requests (request_id, endpoint, response_code, logged_at) VALUES ('fixed-uuid-value', '/api/users', 200, '2026-01-15 12:00:00') ON CONFLICT (request_id) DO NOTHING;"]
}
!!!

!!! quiz
{
"title": "Temp Files Deletion",
"question": "DELETE FROM temp_files WHERE created_at < NOW() - INTERVAL '24 hours';\n\nWhich modification makes this idempotent?",
"options": [
"This statement is already idempotent",
"DELETE FROM temp_files WHERE created_at < '2026-01-14 12:00:00';",
"TRUNCATE TABLE temp_files;",
"Cannot be made idempotent"
],
"answers": ["This statement is already idempotent"]
}
!!!

!!! quiz
{
"title": "Account Balance Interest Calculation",
"question": "UPDATE account*balance\nSET balance = balance * 1.05, last*interest_date = CURRENT_DATE\nWHERE account_type = 'savings';\n\nWhich modification makes this idempotent?",
"options": [
"UPDATE account_balance SET balance = balance * 1.05, last_interest_date = CURRENT_DATE WHERE account_type = 'savings' AND last_interest_date < CURRENT_DATE;",
"UPDATE account_balance SET balance = balance \* 1.05, last_interest_date = CURRENT_DATE WHERE account_type = 'savings' AND last_interest_date != CURRENT_DATE;",
"UPDATE account_balance SET last_interest_date = CURRENT_DATE WHERE account_type = 'savings';",
"Cannot be made idempotent"
],
"answers": ["Cannot be made idempotent"]
}
!!!
