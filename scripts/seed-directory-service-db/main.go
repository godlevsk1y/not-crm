package main

import (
	"context"
	_ "embed"
	"errors"
	"fmt"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgconn"
)

//go:embed seed.sql
var seedSQL string

func main() {
	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()
	ctx, cancel := context.WithTimeout(ctx, 5*time.Minute)
	defer cancel()
	if err := run(ctx); err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
}

func run(ctx context.Context) error {
	dsn := os.Getenv("DATABASE_URL")
	if dsn == "" {
		return errors.New("set DATABASE_URL to the DirectoryService PostgreSQL connection URL")
	}
	conn, err := pgx.Connect(ctx, dsn)
	if err != nil {
		// Connection errors can contain credentials from the supplied URL.
		return errors.New("cannot connect to PostgreSQL: check DATABASE_URL and database availability")
	}
	defer conn.Close(context.Background())
	tx, err := conn.Begin(ctx)
	if err != nil {
		return fmt.Errorf("begin transaction: %w", err)
	}
	defer tx.Rollback(context.Background())
	if _, err := tx.Exec(ctx, seedSQL); err != nil {
		var pgErr *pgconn.PgError
		if errors.As(err, &pgErr) {
			return fmt.Errorf("seed failed (SQLSTATE %s): %s", pgErr.Code, pgErr.Message)
		}
		return fmt.Errorf("seed failed: %w", err)
	}
	if err := tx.Commit(ctx); err != nil {
		return fmt.Errorf("commit seed: %w", err)
	}
	fmt.Println("Seed completed: 15000 departments, 2500 locations, 500 positions; existing seed IDs were skipped.")
	return nil
}
