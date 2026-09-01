package main

import (
	"context"
	"fmt"
	"os"
	"seed-db/seeders"
	"strings"

	"github.com/jackc/pgx/v5"
)

func main() {
	ctx := context.Background()

	args := os.Args[1:]

	if len(args) < 3 {
		fmt.Println("usage: seed-db <host>:<port> <username>:<password> <db_name>")
		os.Exit(1)
	}

	connStr := getConnStr(args)

	conn, err := pgx.Connect(ctx, connStr)
	if err != nil {
		fmt.Fprintf(os.Stderr, "failed to connect the db: %s", err)
	}
	defer conn.Close(ctx)

	fmt.Printf("Connected to PostgreSQL database on %s\n", args[0])

	departments := seeders.SeedDepartments(400, 300, 350)
	positions := seeders.SeedPositions(150)
	locations := seeders.SeedLocations(280)

	departmentPositions := seeders.SeedDepartmentPositions(800, departments, positions)

	_ = departmentPositions

	departmentLocations := seeders.SeedDepartmentLocations(800, departments, locations)

	_ = departmentLocations
}

func getConnStr(args []string) string {
	netAddr := strings.Split(args[0], ":")
	creds := strings.Split(args[1], ":")
	dbName := args[2]

	return fmt.Sprintf(
		"postgres://%s:%s@%s:%s/%s",
		creds[0], creds[1],
		netAddr[0], netAddr[1],
		dbName,
	)
}
