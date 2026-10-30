# Directory search performance check

Measured on 2026-10-30 for issue #130 against an isolated disposable database. These are local synthetic workload observations, not production latency guarantees.

## Environment and dataset

- macOS arm64, .NET SDK 10.0.301 / runtime 10.0.9, Debug build, real Kestrel HTTP server on loopback.
- PostgreSQL 17 in Docker Desktop; 10 CPUs available to Docker and a container memory limit of approximately 7.75 GiB (shared Docker VM). Database collation/ctype: `en_US.utf8`. Default planner settings; no forced scan choices.
- All service migrations applied, including the existing department partial GIN index and the new location/position partial GIN indexes. No schema changes to the department index.
- 100,000 rows per table (300,000 total); 90,000 active and 10,000 soft-deleted per table. Unique location names; department and position names use the same distribution. No relationships are needed for these projection-only queries.
- Names mix Cyrillic and English; all include `Group`, 40,000 active names contain `Разработка`, and 10 active names contain `quartzneedle`. UUIDs are randomly generated. Every tenth row is soft-deleted.
- `VACUUM (ANALYZE)` ran after seeding. Counts below are active matches before limiting; count queries are benchmark diagnostics and are never run by the endpoints.

```text
PostgreSQL 17.11 (Debian 17.11-1.pgdg13+2) on aarch64-unknown-linux-gnu, compiled by gcc (Debian 14.2.0-19) 14.2.0, 64-bit
max_parallel_workers_per_gather=2
random_page_cost=4
shared_buffers=16384 (8kB blocks, 128 MiB)
work_mem=4096 (kB, 4 MiB)
```

## Query plans

The actual endpoint predicates, order and extra-row limit were checked using `EXPLAIN (ANALYZE, BUFFERS)` with `LIMIT 21 OFFSET 0`. Every entity/case has its full plan below. Values are a single execution after match counting warmed the data; timings include server execution only.

| Query | Table | Active matches | Scan nodes | Execution ms |
| --- | --- | ---: | --- | ---: |
| ра | departments | 40000 | Seq Scan | 33.135 |
| ра | locations | 40000 | Index Scan | 15.849 |
| ра | positions | 40000 | Seq Scan | 31.702 |
| quartzneedle | departments | 10 | Bitmap Heap Scan, Bitmap Index Scan | 0.087 |
| quartzneedle | locations | 10 | Bitmap Heap Scan, Bitmap Index Scan | 0.094 |
| quartzneedle | positions | 10 | Bitmap Heap Scan, Bitmap Index Scan | 0.103 |
| group | departments | 90000 | Seq Scan | 21.924 |
| group | locations | 90000 | Index Scan | 0.061 |
| group | positions | 90000 | Seq Scan | 21.499 |
| nonexistentneedle | departments | 0 | Bitmap Heap Scan, Bitmap Index Scan | 0.049 |
| nonexistentneedle | locations | 0 | Bitmap Heap Scan, Bitmap Index Scan | 0.047 |
| nonexistentneedle | positions | 0 | Bitmap Heap Scan, Bitmap Index Scan | 0.055 |

## HTTP measurements

Each endpoint/query received one unmeasured warm-up followed by 10 sequential measured requests. Each sample includes connection establishment, full body transfer and loopback overhead; no compression requested. Bytes are UTF-8 JSON response-body bytes, excluding HTTP headers. The range accounts for variable timestamp serialization. Combined search uses `limitPerType=20`; per-type endpoints use `page=2&pageSize=20`. All responses were HTTP 200 with the standard success envelope.

| Query | Endpoint | Median ms | Max ms | Body bytes min–max |
| --- | --- | ---: | ---: | ---: |
| ра | /api/search | 81.10 | 85.06 | 5545–5546 |
| ра | /api/search/departments | 21.38 | 24.09 | 1890–1892 |
| ра | /api/search/locations | 16.74 | 18.08 | 1890–1892 |
| ра | /api/search/positions | 20.87 | 22.25 | 1892–1892 |
| quartzneedle | /api/search | 1.22 | 1.57 | 2639–2639 |
| quartzneedle | /api/search/departments | 0.95 | 1.03 | 113–114 |
| quartzneedle | /api/search/locations | 0.93 | 0.99 | 114–114 |
| quartzneedle | /api/search/positions | 0.94 | 1.01 | 114–114 |
| group | /api/search | 42.57 | 43.87 | 4945–4946 |
| group | /api/search/departments | 22.77 | 23.39 | 1691–1692 |
| group | /api/search/locations | 1.03 | 1.15 | 1691–1692 |
| group | /api/search/positions | 20.98 | 22.04 | 1692–1692 |
| nonexistentneedle | /api/search | 0.95 | 1.15 | 211–212 |
| nonexistentneedle | /api/search/departments | 0.77 | 0.84 | 113–114 |
| nonexistentneedle | /api/search/locations | 0.75 | 0.86 | 114–114 |
| nonexistentneedle | /api/search/positions | 0.73 | 0.75 | 112–114 |

## Interpretation

The rare long substring and absent substring can use trigram bitmap index scans. A two-character unanchored pattern such as `%ра%` has no complete extractable trigram, so the index does not promise an acceleration; sequential scans and sorting can be appropriate. Frequent matches can also favor a sequential scan. Location name uniqueness provides an existing B-tree index which the planner may choose for ordered early termination.

Returning only 20 items per type bounds the payload, not the number of rows read, filtered or sorted. The three SELECTs of combined search execute sequentially in one Dapper `QueryMultipleAsync` command and one connection; their execution cost accumulates. Offset pagination can scan/discard more rows at deep pages. Changing data between HTTP requests can shift pages; these measurements use an unchanged dataset. Production distributions, cache state, network and concurrency require separate capacity measurements.

## Reproduction

Use a fresh disposable PostgreSQL 17 database, apply all migrations with `dotnet ef database update --connection <isolated-connection>`, then seed with the SQL below. Start DirectoryService with that connection and `ASPNETCORE_URLS=http://127.0.0.1:5130`. Do not seed a shared database.

```sql
CREATE TEMP TABLE search_seed AS
SELECT gen_random_uuid() AS id, i,
       'Group ' || CASE WHEN i % 10000 = 1 THEN 'quartzneedle '
                       WHEN i % 2 = 0 THEN 'Разработка '
                       ELSE 'Operations ' END || lpad(i::text, 6, '0') AS name,
       CASE WHEN i % 10 = 0 THEN now() ELSE NULL END AS deleted_at
FROM generate_series(1, 100000) AS i;
INSERT INTO departments(id, name, slug, path, depth, deleted_at)
SELECT id, name, 'seed' || i, ('seed' || i)::ltree, 0, deleted_at FROM search_seed;
INSERT INTO locations(id, name, country, city, street, house_number, deleted_at)
SELECT id, name, 'Russia', 'Moscow', 'Street', '1', deleted_at FROM search_seed;
INSERT INTO positions(id, name, deleted_at)
SELECT id, name, deleted_at FROM search_seed;
VACUUM (ANALYZE) departments;
VACUUM (ANALYZE) locations;
VACUUM (ANALYZE) positions;
```

For each query in the tables above, count active matches and execute the full plan SQL recorded below. To reproduce HTTP samples, use Python's standard library with the same query string and endpoint settings:

```python
import statistics, time, urllib.parse, urllib.request
url = "http://127.0.0.1:5130/api/search?" + urllib.parse.urlencode(
    {"q": "ра", "limitPerType": 20})
samples, sizes = [], []
for sample in range(11):
    start = time.perf_counter()
    with urllib.request.urlopen(url) as response:
        assert response.status == 200
        body = response.read()
    elapsed = (time.perf_counter() - start) * 1000
    if sample:  # discard warm-up
        samples.append(elapsed)
        sizes.append(len(body))
print(statistics.median(samples), max(samples), min(sizes), max(sizes))
```

## Full execution plans

### Two-character Cyrillic substring: departments

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, name FROM departments WHERE deleted_at IS NULL AND name ILIKE '%ра%' ESCAPE '!' ORDER BY name ASC, id ASC LIMIT 21 OFFSET 0;
```

```text
Limit  (cost=4173.19..4173.25 rows=21 width=44) (actual time=33.124..33.126 rows=21 loops=1)
  Buffers: shared hit=1728
  ->  Sort  (cost=4173.19..4284.57 rows=44552 width=44) (actual time=33.123..33.124 rows=21 loops=1)
        Sort Key: name, id
        Sort Method: top-N heapsort  Memory: 27kB
        Buffers: shared hit=1728
        ->  Seq Scan on departments  (cost=0.00..2972.00 rows=44552 width=44) (actual time=0.005..24.474 rows=40000 loops=1)
              Filter: ((deleted_at IS NULL) AND ((name)::text ~~* '%ра%'::text))
              Rows Removed by Filter: 60000
              Buffers: shared hit=1722
Planning:
  Buffers: shared hit=146
Planning Time: 0.428 ms
Execution Time: 33.135 ms
```

### Two-character Cyrillic substring: locations

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, name FROM locations WHERE deleted_at IS NULL AND name ILIKE '%ра%' ESCAPE '!' ORDER BY name ASC, id ASC LIMIT 21 OFFSET 0;
```

```text
Limit  (cost=0.65..6.23 rows=21 width=45) (actual time=15.835..15.837 rows=21 loops=1)
  Buffers: shared hit=2004
  ->  Incremental Sort  (cost=0.65..12049.36 rows=45364 width=45) (actual time=15.834..15.835 rows=21 loops=1)
        Sort Key: name, id
        Presorted Key: name
        Full-sort Groups: 1  Sort Method: quicksort  Average Memory: 27kB  Peak Memory: 27kB
        Buffers: shared hit=2004
        ->  Index Scan using uq_location_name on locations  (cost=0.42..10007.98 rows=45364 width=45) (actual time=15.793..15.800 rows=22 loops=1)
              Filter: ((deleted_at IS NULL) AND ((name)::text ~~* '%ра%'::text))
              Rows Removed by Filter: 50005
              Buffers: shared hit=1995
Planning:
  Buffers: shared hit=135
Planning Time: 0.335 ms
Execution Time: 15.849 ms
```

### Two-character Cyrillic substring: positions

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, name FROM positions WHERE deleted_at IS NULL AND name ILIKE '%ра%' ESCAPE '!' ORDER BY name ASC, id ASC LIMIT 21 OFFSET 0;
```

```text
Limit  (cost=3615.40..3615.45 rows=21 width=45) (actual time=31.690..31.692 rows=21 loops=1)
  Buffers: shared hit=1143
  ->  Sort  (cost=3615.40..3729.30 rows=45561 width=45) (actual time=31.689..31.690 rows=21 loops=1)
        Sort Key: name, id
        Sort Method: top-N heapsort  Memory: 27kB
        Buffers: shared hit=1143
        ->  Seq Scan on positions  (cost=0.00..2387.00 rows=45561 width=45) (actual time=0.004..23.265 rows=40000 loops=1)
              Filter: ((deleted_at IS NULL) AND ((name)::text ~~* '%ра%'::text))
              Rows Removed by Filter: 60000
              Buffers: shared hit=1137
Planning:
  Buffers: shared hit=113
Planning Time: 0.322 ms
Execution Time: 31.702 ms
```

### Rare long substring: departments

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, name FROM departments WHERE deleted_at IS NULL AND name ILIKE '%quartzneedle%' ESCAPE '!' ORDER BY name ASC, id ASC LIMIT 21 OFFSET 0;
```

```text
Limit  (cost=133.55..133.57 rows=9 width=44) (actual time=0.060..0.060 rows=10 loops=1)
  Buffers: shared hit=37
  ->  Sort  (cost=133.55..133.57 rows=9 width=44) (actual time=0.059..0.059 rows=10 loops=1)
        Sort Key: name, id
        Sort Method: quicksort  Memory: 25kB
        Buffers: shared hit=37
        ->  Bitmap Heap Scan on departments  (cost=99.24..133.40 rows=9 width=44) (actual time=0.020..0.046 rows=10 loops=1)
              Recheck Cond: (((name)::text ~~* '%quartzneedle%'::text) AND (deleted_at IS NULL))
              Heap Blocks: exact=10
              Buffers: shared hit=31
              ->  Bitmap Index Scan on ix_departments_active_name_trgm  (cost=0.00..99.24 rows=9 width=0) (actual time=0.015..0.015 rows=10 loops=1)
                    Index Cond: ((name)::text ~~* '%quartzneedle%'::text)
                    Buffers: shared hit=21
Planning:
  Buffers: shared hit=146
Planning Time: 0.400 ms
Execution Time: 0.087 ms
```

### Rare long substring: locations

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, name FROM locations WHERE deleted_at IS NULL AND name ILIKE '%quartzneedle%' ESCAPE '!' ORDER BY name ASC, id ASC LIMIT 21 OFFSET 0;
```

```text
Limit  (cost=133.43..133.46 rows=9 width=45) (actual time=0.069..0.070 rows=10 loops=1)
  Buffers: shared hit=37
  ->  Sort  (cost=133.43..133.46 rows=9 width=45) (actual time=0.068..0.068 rows=10 loops=1)
        Sort Key: name, id
        Sort Method: quicksort  Memory: 25kB
        Buffers: shared hit=37
        ->  Bitmap Heap Scan on locations  (cost=99.24..133.29 rows=9 width=45) (actual time=0.020..0.050 rows=10 loops=1)
              Recheck Cond: (((name)::text ~~* '%quartzneedle%'::text) AND (deleted_at IS NULL))
              Heap Blocks: exact=10
              Buffers: shared hit=31
              ->  Bitmap Index Scan on ix_locations_active_name_trgm  (cost=0.00..99.24 rows=9 width=0) (actual time=0.015..0.015 rows=10 loops=1)
                    Index Cond: ((name)::text ~~* '%quartzneedle%'::text)
                    Buffers: shared hit=21
Planning:
  Buffers: shared hit=135
Planning Time: 0.375 ms
Execution Time: 0.094 ms
```

### Rare long substring: positions

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, name FROM positions WHERE deleted_at IS NULL AND name ILIKE '%quartzneedle%' ESCAPE '!' ORDER BY name ASC, id ASC LIMIT 21 OFFSET 0;
```

```text
Limit  (cost=133.10..133.12 rows=9 width=45) (actual time=0.065..0.066 rows=10 loops=1)
  Buffers: shared hit=37
  ->  Sort  (cost=133.10..133.12 rows=9 width=45) (actual time=0.064..0.064 rows=10 loops=1)
        Sort Key: name, id
        Sort Method: quicksort  Memory: 25kB
        Buffers: shared hit=37
        ->  Bitmap Heap Scan on positions  (cost=99.24..132.95 rows=9 width=45) (actual time=0.021..0.044 rows=10 loops=1)
              Recheck Cond: (((name)::text ~~* '%quartzneedle%'::text) AND (deleted_at IS NULL))
              Heap Blocks: exact=10
              Buffers: shared hit=31
              ->  Bitmap Index Scan on ix_positions_active_name_trgm  (cost=0.00..99.24 rows=9 width=0) (actual time=0.016..0.016 rows=10 loops=1)
                    Index Cond: ((name)::text ~~* '%quartzneedle%'::text)
                    Buffers: shared hit=21
Planning:
  Buffers: shared hit=111
Planning Time: 0.326 ms
Execution Time: 0.103 ms
```

### Frequent substring: departments

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, name FROM departments WHERE deleted_at IS NULL AND name ILIKE '%group%' ESCAPE '!' ORDER BY name ASC, id ASC LIMIT 21 OFFSET 0;
```

```text
Limit  (cost=4884.76..4887.17 rows=21 width=44) (actual time=21.024..21.906 rows=21 loops=1)
  Buffers: shared hit=1766
  ->  Gather Merge  (cost=4884.76..10973.32 rows=52944 width=44) (actual time=21.023..21.903 rows=21 loops=1)
        Workers Planned: 1
        Workers Launched: 1
        Buffers: shared hit=1766
        ->  Sort  (cost=3884.75..4017.11 rows=52944 width=44) (actual time=20.348..20.349 rows=21 loops=2)
              Sort Key: name, id
              Sort Method: top-N heapsort  Memory: 27kB
              Buffers: shared hit=1766
              Worker 0:  Sort Method: top-N heapsort  Memory: 27kB
              ->  Parallel Seq Scan on departments  (cost=0.00..2457.29 rows=52944 width=44) (actual time=0.009..13.944 rows=45000 loops=2)
                    Filter: ((deleted_at IS NULL) AND ((name)::text ~~* '%group%'::text))
                    Rows Removed by Filter: 5000
                    Buffers: shared hit=1722
Planning:
  Buffers: shared hit=146
Planning Time: 0.488 ms
Execution Time: 21.924 ms
```

### Frequent substring: locations

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, name FROM locations WHERE deleted_at IS NULL AND name ILIKE '%group%' ESCAPE '!' ORDER BY name ASC, id ASC LIMIT 21 OFFSET 0;
```

```text
Limit  (cost=0.54..3.82 rows=21 width=45) (actual time=0.049..0.051 rows=21 loops=1)
  Buffers: shared hit=13
  ->  Incremental Sort  (cost=0.54..14049.48 rows=89811 width=45) (actual time=0.048..0.049 rows=21 loops=1)
        Sort Key: name, id
        Presorted Key: name
        Full-sort Groups: 1  Sort Method: quicksort  Average Memory: 26kB  Peak Memory: 26kB
        Buffers: shared hit=13
        ->  Index Scan using uq_location_name on locations  (cost=0.42..10007.98 rows=89811 width=45) (actual time=0.011..0.018 rows=22 loops=1)
              Filter: ((deleted_at IS NULL) AND ((name)::text ~~* '%group%'::text))
              Buffers: shared hit=4
Planning:
  Buffers: shared hit=135
Planning Time: 0.349 ms
Execution Time: 0.061 ms
```

### Frequent substring: positions

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, name FROM positions WHERE deleted_at IS NULL AND name ILIKE '%group%' ESCAPE '!' ORDER BY name ASC, id ASC LIMIT 21 OFFSET 0;
```

```text
Limit  (cost=4302.86..4305.27 rows=21 width=45) (actual time=20.752..21.478 rows=21 loops=1)
  Buffers: shared hit=1181
  ->  Gather Merge  (cost=4302.86..10404.64 rows=53059 width=45) (actual time=20.751..21.474 rows=21 loops=1)
        Workers Planned: 1
        Workers Launched: 1
        Buffers: shared hit=1181
        ->  Sort  (cost=3302.85..3435.50 rows=53059 width=45) (actual time=20.123..20.124 rows=21 loops=2)
              Sort Key: name, id
              Sort Method: top-N heapsort  Memory: 27kB
              Buffers: shared hit=1181
              Worker 0:  Sort Method: top-N heapsort  Memory: 27kB
              ->  Parallel Seq Scan on positions  (cost=0.00..1872.29 rows=53059 width=45) (actual time=0.010..13.607 rows=45000 loops=2)
                    Filter: ((deleted_at IS NULL) AND ((name)::text ~~* '%group%'::text))
                    Rows Removed by Filter: 5000
                    Buffers: shared hit=1137
Planning:
  Buffers: shared hit=111
Planning Time: 0.292 ms
Execution Time: 21.499 ms
```

### No matches: departments

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, name FROM departments WHERE deleted_at IS NULL AND name ILIKE '%nonexistentneedle%' ESCAPE '!' ORDER BY name ASC, id ASC LIMIT 21 OFFSET 0;
```

```text
Limit  (cost=185.21..185.23 rows=9 width=44) (actual time=0.022..0.022 rows=0 loops=1)
  Buffers: shared hit=37
  ->  Sort  (cost=185.21..185.23 rows=9 width=44) (actual time=0.021..0.022 rows=0 loops=1)
        Sort Key: name, id
        Sort Method: quicksort  Memory: 25kB
        Buffers: shared hit=37
        ->  Bitmap Heap Scan on departments  (cost=150.91..185.07 rows=9 width=44) (actual time=0.014..0.014 rows=0 loops=1)
              Recheck Cond: (((name)::text ~~* '%nonexistentneedle%'::text) AND (deleted_at IS NULL))
              Buffers: shared hit=31
              ->  Bitmap Index Scan on ix_departments_active_name_trgm  (cost=0.00..150.90 rows=9 width=0) (actual time=0.012..0.012 rows=0 loops=1)
                    Index Cond: ((name)::text ~~* '%nonexistentneedle%'::text)
                    Buffers: shared hit=31
Planning:
  Buffers: shared hit=146
Planning Time: 0.410 ms
Execution Time: 0.049 ms
```

### No matches: locations

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, name FROM locations WHERE deleted_at IS NULL AND name ILIKE '%nonexistentneedle%' ESCAPE '!' ORDER BY name ASC, id ASC LIMIT 21 OFFSET 0;
```

```text
Limit  (cost=185.10..185.12 rows=9 width=45) (actual time=0.023..0.023 rows=0 loops=1)
  Buffers: shared hit=37
  ->  Sort  (cost=185.10..185.12 rows=9 width=45) (actual time=0.022..0.022 rows=0 loops=1)
        Sort Key: name, id
        Sort Method: quicksort  Memory: 25kB
        Buffers: shared hit=37
        ->  Bitmap Heap Scan on locations  (cost=150.91..184.95 rows=9 width=45) (actual time=0.013..0.013 rows=0 loops=1)
              Recheck Cond: (((name)::text ~~* '%nonexistentneedle%'::text) AND (deleted_at IS NULL))
              Buffers: shared hit=31
              ->  Bitmap Index Scan on ix_locations_active_name_trgm  (cost=0.00..150.90 rows=9 width=0) (actual time=0.011..0.011 rows=0 loops=1)
                    Index Cond: ((name)::text ~~* '%nonexistentneedle%'::text)
                    Buffers: shared hit=31
Planning:
  Buffers: shared hit=135
Planning Time: 0.323 ms
Execution Time: 0.047 ms
```

### No matches: positions

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, name FROM positions WHERE deleted_at IS NULL AND name ILIKE '%nonexistentneedle%' ESCAPE '!' ORDER BY name ASC, id ASC LIMIT 21 OFFSET 0;
```

```text
Limit  (cost=184.76..184.78 rows=9 width=45) (actual time=0.028..0.029 rows=0 loops=1)
  Buffers: shared hit=37
  ->  Sort  (cost=184.76..184.78 rows=9 width=45) (actual time=0.028..0.028 rows=0 loops=1)
        Sort Key: name, id
        Sort Method: quicksort  Memory: 25kB
        Buffers: shared hit=37
        ->  Bitmap Heap Scan on positions  (cost=150.91..184.62 rows=9 width=45) (actual time=0.015..0.015 rows=0 loops=1)
              Recheck Cond: (((name)::text ~~* '%nonexistentneedle%'::text) AND (deleted_at IS NULL))
              Buffers: shared hit=31
              ->  Bitmap Index Scan on ix_positions_active_name_trgm  (cost=0.00..150.90 rows=9 width=0) (actual time=0.011..0.011 rows=0 loops=1)
                    Index Cond: ((name)::text ~~* '%nonexistentneedle%'::text)
                    Buffers: shared hit=31
Planning:
  Buffers: shared hit=111
Planning Time: 0.352 ms
Execution Time: 0.055 ms
```
