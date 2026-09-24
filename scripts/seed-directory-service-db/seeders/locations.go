package seeders

import (
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

	seen := make(map[string]struct{}, count)

	for len(locations) < count {
		now := time.Now()

		l := &Location{
			ID: uuid.New(),

			Name: gofakeit.AdjectiveDescriptive() + " " + gofakeit.NounConcrete() + " " + gofakeit.CompanySuffix(),

			Country:     gofakeit.Country(),
			Region:      new(gofakeit.State()),
			City:        gofakeit.City(),
			District:    new(gofakeit.Word() + " " + "District"),
			Street:      gofakeit.Street(),
			HouseNumber: gofakeit.Unit(),
			PostalCode:  new(gofakeit.Zip()),

			CreatedAt: now.UTC(),
			UpdatedAt: now.UTC(),
		}

		if _, ok := seen[l.Name]; ok {
			continue
		}

		locations = append(locations, l)
		seen[l.Name] = struct{}{}
	}

	return locations
}
