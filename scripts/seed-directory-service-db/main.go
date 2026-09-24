package main

import (
	"context"
	"fmt"
	"log"
	"os"
	"seed-db/seeders"
	"strconv"

	"github.com/jackc/pgx/v5"
	"github.com/joho/godotenv"
)

func main() {
	ctx := context.Background()

	if err := godotenv.Load(); err != nil {
		log.Fatalf("Failed to load the env variables")
	}

	connStr := getConnStr()

	conn, err := pgx.Connect(ctx, connStr)
	if err != nil {
		log.Fatalf("failed to connect the db: %s", err)
	}
	defer conn.Close(ctx)

	fmt.Printf("Connected to PostgreSQL database!\n")

	commitCmd := seedData()

	commitSeedData(ctx, conn, commitCmd)
}

func seedData() commitSeedDataCommand {
	departmentsCount, _ := strconv.Atoi(os.Getenv("DEPARTMENTS_COUNT"))
	departmentsRootMinCount, _ := strconv.Atoi(os.Getenv("DEPARTMENTS_ROOT_MIN_COUNT"))
	departmentsRootMaxCount, _ := strconv.Atoi(os.Getenv("DEPARTMENTS_ROOT_MAX_COUNT"))

	positionsCount, _ := strconv.Atoi(os.Getenv("POSITIONS_COUNT"))
	locationsCount, _ := strconv.Atoi(os.Getenv("LOCATIONS_COUNT"))

	departmentsPositionsCount, _ := strconv.Atoi(os.Getenv("DEPARTMENTS_POSITIONS_COUNT"))
	departmentsLocationsCount, _ := strconv.Atoi(os.Getenv("DEPARTMENTS_LOCATIONS_COUNT"))

	departments := seeders.SeedDepartments(
		departmentsCount,
		departmentsRootMinCount,
		departmentsRootMaxCount,
	)
	positions := seeders.SeedPositions(positionsCount)
	locations := seeders.SeedLocations(locationsCount)

	departmentPositions := seeders.SeedDepartmentPositions(
		departmentsPositionsCount,
		departments,
		positions,
	)
	departmentLocations := seeders.SeedDepartmentLocations(
		departmentsLocationsCount,
		departments,
		locations,
	)

	return commitSeedDataCommand{
		departments:         departments,
		positions:           positions,
		locations:           locations,
		departmentPositions: departmentPositions,
		departmentLocations: departmentLocations,
	}
}

func getConnStr() string {
	host := os.Getenv("POSTGRES_HOST")
	port := os.Getenv("POSTGRES_PORT")
	user := os.Getenv("POSTGRES_USER")
	password := os.Getenv("POSTGRES_PASSWORD")
	dbName := os.Getenv("POSTGRES_DB")

	return fmt.Sprintf(
		"postgres://%s:%s@%s:%s/%s",
		user, password, host, port, dbName,
	)
}
