package main

import (
	"time"
	"uuid"

	"github.com/brianvoe/gofakeit/v7"
)

type Position struct {
	ID   uuid.UUID
	Name string

	CreatedAt time.Time
	UpdatedAt time.Time
}

func SeedPositions(count int) []*Position {
	var positions []*Position

	for range count {
		now := time.Now()

		p := &Position{
			ID:   uuid.New(),
			Name: gofakeit.JobDescriptor() + " " + gofakeit.JobTitle() + " " + gofakeit.JobLevel(),

			CreatedAt: now.UTC(),
			UpdatedAt: now.UTC(),
		}

		positions = append(positions, p)
	}

	return positions
}
