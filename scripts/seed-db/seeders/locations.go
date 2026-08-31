package main

import (
	"fmt"
	"time"
	"uuid"

	"github.com/brianvoe/gofakeit/v7"
)

type Location struct {
	ID uuid.UUID

	Name string

	Country     string
	Region      *string
	City        string
	District    *string
	Street      string
	HouseNumber string
	PostalCode  *string

	CreatedAt time.Time
	UpdatedAt time.Time
}

func SeedLocations(count int) []*Location {
	var locations []*Location

	for range count {
		now := time.Now()

		l := &Location{
			ID: uuid.New(),

			Name: fmt.Sprintf("%s %s", gofakeit.Company(), gofakeit.CompanySuffix()),

			Country:     gofakeit.Country(),
			Region:      new(gofakeit.State()),
			City:        gofakeit.MinecraftVillagerJob(),
			District:    new(fmt.Sprintf("%s District", gofakeit.Word())),
			Street:      gofakeit.Street(),
			HouseNumber: gofakeit.Unit(),
			PostalCode:  new(gofakeit.Zip()),

			CreatedAt: now.UTC(),
			UpdatedAt: now.UTC(),
		}

		locations = append(locations, l)
	}

	return locations
}
