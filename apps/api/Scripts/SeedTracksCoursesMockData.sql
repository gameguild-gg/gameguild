-- Mock data for Programs API to support Tracks and Courses
-- Tracks = Programs with hierarchical ProgramContent (containing courses)
-- Courses = Individual Programs or leaf-level ProgramContent

-- Insert Track Programs (Learning Paths)
INSERT INTO programs (id, title, description, slug, thumbnail, category, difficulty, visibility, status, estimated_hours, enrollment_status, created_at, updated_at)
VALUES 
-- Programming Tracks
('550e8400-e29b-41d4-a716-446655440001', 'Game Programming Fundamentals', 'Master the core programming concepts and tools essential for game development. This comprehensive track covers everything from basic programming to advanced game engine development.', 'game-programming-fundamentals', '/images/tracks/game-programming-fundamentals.jpg', 0, 0, 1, 1, 120.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('550e8400-e29b-41d4-a716-446655440002', 'Unity Game Development Mastery', 'Complete Unity development track from beginner to expert. Learn 2D and 3D game development, scripting, physics, and publishing.', 'unity-game-development-mastery', '/images/tracks/unity-mastery.jpg', 4, 1, 1, 1, 80.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('550e8400-e29b-41d4-a716-446655440003', 'Advanced C++ for Games', 'Advanced programming track focusing on high-performance C++ development for games and engines.', 'advanced-cpp-for-games', '/images/tracks/cpp-games.jpg', 0, 2, 1, 1, 100.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Art Tracks
('550e8400-e29b-41d4-a716-446655440004', 'Game Art and Design Essentials', 'Learn the fundamental art and design principles crucial for creating visually stunning games. From concept art to 3D modeling.', 'game-art-design-essentials', '/images/tracks/game-art-essentials.jpg', 14, 0, 1, 1, 90.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('550e8400-e29b-41d4-a716-446655440005', ' 3D Character Creation Pipeline', 'Complete pipeline for creating game-ready 3D characters using industry-standard tools and techniques.', '3d-character-creation-pipeline', '/images/tracks/3d-character-pipeline.jpg', 14, 1, 1, 1, 75.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Design Tracks
('550e8400-e29b-41d4-a716-446655440006', 'Game Design and Production', 'Explore the key aspects of game design, from concept to production and beyond. Learn design thinking and project management.', 'game-design-production', '/images/tracks/game-design-production.jpg', 10, 0, 1, 1, 95.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('550e8400-e29b-41d4-a716-446655440007', 'Level Design Masterclass', 'Advanced track focusing on level design principles, tools, and methodologies for creating engaging game environments.', 'level-design-masterclass', '/images/tracks/level-design.jpg', 10, 2, 1, 1, 60.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insert Course Programs (Individual Learning Units)
INSERT INTO programs (id, title, description, slug, thumbnail, category, difficulty, visibility, status, estimated_hours, enrollment_status, created_at, updated_at)
VALUES 
-- Programming Courses
('550e8400-e29b-41d4-a716-446655440011', 'C# Programming Basics', 'Learn the fundamentals of C# programming language for game development.', 'csharp-programming-basics', '/images/courses/csharp-basics.jpg', 0, 0, 1, 1, 15.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('550e8400-e29b-41d4-a716-446655440012', 'Unity Scripting Fundamentals', 'Master Unity scripting with C# for game development.', 'unity-scripting-fundamentals', '/images/courses/unity-scripting.jpg', 4, 0, 1, 1, 20.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('550e8400-e29b-41d4-a716-446655440013', 'Game Physics Programming', 'Learn to implement physics systems in games using Unity and custom engines.', 'game-physics-programming', '/images/courses/game-physics.jpg', 0, 1, 1, 1, 25.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('550e8400-e29b-41d4-a716-446655440014', 'AI for Game Development', 'Implement artificial intelligence systems for NPCs and game mechanics.', 'ai-for-game-development', '/images/courses/game-ai.jpg', 5, 2, 1, 1, 30.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Art Courses
('550e8400-e29b-41d4-a716-446655440015', 'Digital Painting for Games', 'Learn digital painting techniques for concept art and game assets.', 'digital-painting-for-games', '/images/courses/digital-painting.jpg', 14, 0, 1, 1, 18.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('550e8400-e29b-41d4-a716-446655440016', 'Blender 3D Modeling', 'Master 3D modeling in Blender for game asset creation.', 'blender-3d-modeling', '/images/courses/blender-modeling.jpg', 14, 1, 1, 1, 22.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('550e8400-e29b-41d4-a716-446655440017', 'Character Rigging and Animation', 'Learn character rigging and animation techniques for games.', 'character-rigging-animation', '/images/courses/character-rigging.jpg', 14, 2, 1, 1, 28.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Design Courses
('550e8400-e29b-41d4-a716-446655440018', 'Game Design Principles', 'Understand core game design principles and player psychology.', 'game-design-principles', '/images/courses/design-principles.jpg', 10, 0, 1, 1, 16.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('550e8400-e29b-41d4-a716-446655440019', 'UI/UX Design for Games', 'Design intuitive and engaging user interfaces for games.', 'ui-ux-design-for-games', '/images/courses/game-ui-ux.jpg', 10, 1, 1, 1, 20.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('550e8400-e29b-41d4-a716-446655440020', 'Game Monetization Strategies', 'Learn various monetization models and strategies for games.', 'game-monetization-strategies', '/images/courses/monetization.jpg', 11, 1, 1, 1, 12.0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insert ProgramContent to create Track -> Course hierarchies
-- Track 1: Game Programming Fundamentals contains courses
INSERT INTO program_contents (id, program_id, parent_id, title, description, type, body, sort_order, is_required, estimated_minutes, visibility, created_at, updated_at)
VALUES 
('660e8400-e29b-41d4-a716-446655440001', '550e8400-e29b-41d4-a716-446655440001', NULL, 'C# Programming Basics', 'Learn the fundamentals of C# programming language', 0, '{"program_ref": "550e8400-e29b-41d4-a716-446655440011"}', 1, true, 900, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('660e8400-e29b-41d4-a716-446655440002', '550e8400-e29b-41d4-a716-446655440001', NULL, 'Game Physics Programming', 'Learn to implement physics systems in games', 0, '{"program_ref": "550e8400-e29b-41d4-a716-446655440013"}', 2, true, 1500, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('660e8400-e29b-41d4-a716-446655440003', '550e8400-e29b-41d4-a716-446655440001', NULL, 'AI for Game Development', 'Implement AI systems for games', 0, '{"program_ref": "550e8400-e29b-41d4-a716-446655440014"}', 3, false, 1800, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Track 2: Unity Game Development Mastery contains courses
INSERT INTO program_contents (id, program_id, parent_id, title, description, type, body, sort_order, is_required, estimated_minutes, visibility, created_at, updated_at)
VALUES 
('660e8400-e29b-41d4-a716-446655440004', '550e8400-e29b-41d4-a716-446655440002', NULL, 'Unity Scripting Fundamentals', 'Master Unity scripting with C#', 0, '{"program_ref": "550e8400-e29b-41d4-a716-446655440012"}', 1, true, 1200, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('660e8400-e29b-41d4-a716-446655440005', '550e8400-e29b-41d4-a716-446655440002', NULL, 'UI/UX Design for Games', 'Design game interfaces', 0, '{"program_ref": "550e8400-e29b-41d4-a716-446655440019"}', 2, true, 1200, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Track 4: Game Art and Design Essentials contains courses
INSERT INTO program_contents (id, program_id, parent_id, title, description, type, body, sort_order, is_required, estimated_minutes, visibility, created_at, updated_at)
VALUES 
('660e8400-e29b-41d4-a716-446655440006', '550e8400-e29b-41d4-a716-446655440004', NULL, 'Digital Painting for Games', 'Learn digital painting techniques', 0, '{"program_ref": "550e8400-e29b-41d4-a716-446655440015"}', 1, true, 1080, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('660e8400-e29b-41d4-a716-446655440007', '550e8400-e29b-41d4-a716-446655440004', NULL, 'Blender 3D Modeling', 'Master 3D modeling in Blender', 0, '{"program_ref": "550e8400-e29b-41d4-a716-446655440016"}', 2, true, 1320, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Track 5: 3D Character Creation Pipeline contains courses
INSERT INTO program_contents (id, program_id, parent_id, title, description, type, body, sort_order, is_required, estimated_minutes, visibility, created_at, updated_at)
VALUES 
('660e8400-e29b-41d4-a716-446655440008', '550e8400-e29b-41d4-a716-446655440005', NULL, 'Blender 3D Modeling', 'Master 3D modeling fundamentals', 0, '{"program_ref": "550e8400-e29b-41d4-a716-446655440016"}', 1, true, 1320, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('660e8400-e29b-41d4-a716-446655440009', '550e8400-e29b-41d4-a716-446655440005', NULL, 'Character Rigging and Animation', 'Learn character rigging and animation', 0, '{"program_ref": "550e8400-e29b-41d4-a716-446655440017"}', 2, true, 1680, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Track 6: Game Design and Production contains courses
INSERT INTO program_contents (id, program_id, parent_id, title, description, type, body, sort_order, is_required, estimated_minutes, visibility, created_at, updated_at)
VALUES 
('660e8400-e29b-41d4-a716-446655440010', '550e8400-e29b-41d4-a716-446655440006', NULL, 'Game Design Principles', 'Understand core game design principles', 0, '{"program_ref": "550e8400-e29b-41d4-a716-446655440018"}', 1, true, 960, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

('660e8400-e29b-41d4-a716-446655440011', '550e8400-e29b-41d4-a716-446655440006', NULL, 'Game Monetization Strategies', 'Learn monetization models', 0, '{"program_ref": "550e8400-e29b-41d4-a716-446655440020"}', 2, false, 720, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
