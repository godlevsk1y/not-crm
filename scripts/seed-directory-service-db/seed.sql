-- Serialize concurrent runs of this seed. All statements run in one transaction.
SELECT pg_advisory_xact_lock(24092026, 1);
SET LOCAL search_path TO public;

INSERT INTO locations (id, name, country, region, city, district, street, house_number, postal_code)
SELECT md5('notcrm-seed-v1:location:' || i)::uuid,
       format('Seed · %s · %s №%s',
              (ARRAY['Москва','Санкт-Петербург','Казань','Новосибирск','Екатеринбург'])[(i-1)%5+1],
              (ARRAY['Офис','Склад','Сервисный центр','Производство'])[(i-1)%4+1], i),
       'Россия', NULL,
       (ARRAY['Москва','Санкт-Петербург','Казань','Новосибирск','Екатеринбург'])[(i-1)%5+1],
       NULL,
       (ARRAY['Центральная','Лесная','Садовая','Заводская','Парковая'])[(i-1)%5+1],
       ((i-1)/5+1)::text, NULL
FROM generate_series(1, 2500) AS s(i)
ON CONFLICT (id) DO NOTHING;

INSERT INTO positions (id, name)
SELECT md5('notcrm-seed-v1:position:' || i)::uuid,
       format('Seed · %s: %s №%s',
              (ARRAY['Специалист','Инженер','Аналитик','Менеджер','Руководитель',
                     'Координатор','Консультант','Администратор','Эксперт','Ассистент'])[(i-1)%10+1],
              (ARRAY['разработка','продажи','финансы','логистика','поддержка',
                     'маркетинг','кадры','закупки','качество','безопасность'])[((i-1)/10)%10+1], i)
FROM generate_series(1, 500) AS s(i)
ON CONFLICT (id) DO NOTHING;

-- Ten roots, at most five direct children per node. Parents precede children.
-- Slugs use only letters/digits, valid both for the domain and for ltree labels.
WITH RECURSIVE nodes AS (
    SELECT i, CASE WHEN i <= 10 THEN NULL ELSE (i-11)/5+1 END AS parent_number,
           'seeddept' || i AS slug
    FROM generate_series(1, 15000) AS s(i)
), tree AS (
    SELECT i, parent_number, slug, slug::ltree AS path, 0 AS depth
    FROM nodes WHERE parent_number IS NULL
    UNION ALL
    SELECT n.i, n.parent_number, n.slug, t.path || n.slug::ltree, t.depth+1
    FROM nodes n JOIN tree t ON t.i = n.parent_number
)
INSERT INTO departments (id, name, slug, path, parent_id, depth)
SELECT md5('notcrm-seed-v1:department:' || i)::uuid,
       format('Seed · %s №%s',
              (ARRAY['Разработка','Продажи','Финансы','Логистика','Поддержка',
                     'Маркетинг','Кадры','Закупки','Контроль качества','Безопасность'])[(i-1)%10+1], i),
       slug, path, md5('notcrm-seed-v1:department:' || parent_number)::uuid, depth
FROM tree ORDER BY i
ON CONFLICT (id) DO NOTHING;

-- One to three distinct locations per department, exactly one primary.
INSERT INTO department_locations (id, department_id, location_id, is_primary)
SELECT md5('notcrm-seed-v1:department-location:' || i || ':' || j)::uuid,
       md5('notcrm-seed-v1:department:' || i)::uuid,
       md5('notcrm-seed-v1:location:' || ((i-1+j*137)%2500+1))::uuid,
       j = 0
FROM generate_series(1, 15000) AS d(i)
CROSS JOIN LATERAL generate_series(0, (i-1)%3) AS l(j)
ON CONFLICT (department_id, location_id) DO NOTHING;

-- Two to six distinct positions per department; all 500 positions are used.
INSERT INTO department_positions (id, department_id, position_id)
SELECT md5('notcrm-seed-v1:department-position:' || i || ':' || j)::uuid,
       md5('notcrm-seed-v1:department:' || i)::uuid,
       md5('notcrm-seed-v1:position:' || ((i-1+j*37)%500+1))::uuid
FROM generate_series(1, 15000) AS d(i)
CROSS JOIN LATERAL generate_series(0, 1+(i-1)%5) AS p(j)
ON CONFLICT (department_id, position_id) DO NOTHING;
