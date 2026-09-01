package main

import (
	"context"
	"fmt"
	"log"
	"seed-db/seeders"

	"github.com/jackc/pgx/v5"
)

type commitSeedDataCommand struct {
	departments []*seeders.Department
	positions   []*seeders.Position
	locations   []*seeders.Location

	departmentPositions []*seeders.DepartmentPosition
	departmentLocations []*seeders.DepartmentLocation
}

func commitSeedData(ctx context.Context, conn *pgx.Conn, cmd commitSeedDataCommand) {
	commitDepartments(ctx, conn, cmd.departments)
	commitPositions(ctx, conn, cmd.positions)
	commitLocations(ctx, conn, cmd.locations)

	commitDepartmentPositions(ctx, conn, cmd.departmentPositions)
	commitDepartmentLocations(ctx, conn, cmd.departmentLocations)
}

func commitDepartments(ctx context.Context, conn *pgx.Conn, departments []*seeders.Department) {
	const sql = `INSERT INTO departments (id, name, slug, path, parent_id, created_at, updated_at,) 
		VALUES ($1, $2, $3, $4, $5, $6, $7);`

	batch := &pgx.Batch{}

	for _, d := range departments {
		batch.Queue(
			sql, d.ID,
			d.Name, d.Slug, d.Path, d.ParentID,
			d.CreatedAt, d.UpdatedAt,
		)
	}

	br := conn.SendBatch(ctx, batch)
	defer br.Close()

	for i := range len(departments) {
		commandTag, err := br.Exec()
		if err != nil {
			log.Fatalf("Batch failed at index %d: %s", i, err)
		}

		fmt.Printf("Statement %d successful. Rows affected: %d\n", i, commandTag.RowsAffected())
	}
}

func commitPositions(ctx context.Context, conn *pgx.Conn, positions []*seeders.Position) {
	const sql = `INSERT INTO positions (id, name, created_at, updated_at) 
		VALUES ($1, $2, $3, $4);`

	batch := &pgx.Batch{}

	for _, p := range positions {
		batch.Queue(
			sql, p.ID,
			p.Name, p.CreatedAt, p.UpdatedAt,
		)
	}

	br := conn.SendBatch(ctx, batch)
	defer br.Close()

	for i := range len(positions) {
		commandTag, err := br.Exec()
		if err != nil {
			log.Fatalf("Batch failed at index %d: %s", i, err)
		}

		fmt.Printf("Statement %d successful. Rows affected: %d\n", i, commandTag.RowsAffected())
	}
}

func commitLocations(ctx context.Context, conn *pgx.Conn, locations []*seeders.Location) {
	const sql = `INSERT INTO locations (id, name, country, region, city, district, street, house_number, postal_code, created_at, updated_at) 
		VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11);`

	batch := &pgx.Batch{}

	for _, l := range locations {
		batch.Queue(
			sql, l.ID,
			l.Name, l.Country, l.Region, l.City,
			l.District, l.Street, l.HouseNumber, l.PostalCode,
			l.CreatedAt, l.UpdatedAt,
		)
	}

	br := conn.SendBatch(ctx, batch)
	defer br.Close()

	for i := range len(locations) {
		commandTag, err := br.Exec()
		if err != nil {
			log.Fatalf("Batch failed at index %d: %s", i, err)
		}

		fmt.Printf("Statement %d successful. Rows affected: %d\n", i, commandTag.RowsAffected())
	}
}

func commitDepartmentPositions(ctx context.Context, conn *pgx.Conn, departmentPositions []*seeders.DepartmentPosition) {
	const sql = `INSERT INTO department_positions (id, department_id, position_id) 
		VALUES ($1, $2, $3);`

	batch := &pgx.Batch{}

	for _, dp := range departmentPositions {
		batch.Queue(
			sql, dp.ID,
			dp.DepartmentID, dp.PositionID,
		)
	}

	br := conn.SendBatch(ctx, batch)
	defer br.Close()

	for i := range len(departmentPositions) {
		commandTag, err := br.Exec()
		if err != nil {
			log.Fatalf("Batch failed at index %d: %s", i, err)
		}

		fmt.Printf("Statement %d successful. Rows affected: %d\n", i, commandTag.RowsAffected())
	}
}

func commitDepartmentLocations(ctx context.Context, conn *pgx.Conn, departmentLocations []*seeders.DepartmentLocation) {
	const sql = `INSERT INTO department_locations (id, department_id, location_id, is_primary) 
		VALUES ($1, $2, $3, false);`

	batch := &pgx.Batch{}

	for _, dl := range departmentLocations {
		batch.Queue(
			sql, dl.ID,
			dl.DepartmentID, dl.LocationID,
		)
	}

	br := conn.SendBatch(ctx, batch)
	defer br.Close()

	for i := range len(departmentLocations) {
		commandTag, err := br.Exec()
		if err != nil {
			log.Fatalf("Batch failed at index %d: %s", i, err)
		}

		fmt.Printf("Statement %d successful. Rows affected: %d\n", i, commandTag.RowsAffected())
	}
}
