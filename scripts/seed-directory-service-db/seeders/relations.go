package seeders

import (
	"uuid"
)

type DepartmentPosition struct {
	ID           uuid.UUID
	DepartmentID uuid.UUID
	PositionID   uuid.UUID
}

type departmentPositionIDPair struct {
	departmentID uuid.UUID
	positionID   uuid.UUID
}

func SeedDepartmentPositions(count int, departments []*Department, positions []*Position) []*DepartmentPosition {
	var relations []*DepartmentPosition

	seen := make(map[departmentPositionIDPair]struct{}, count)

	if count > len(departments)*len(positions) {
		count = len(departments) * len(positions)
	}

	for len(relations) < count {
		d, ok := choice(departments)
		if !ok {
			panic("departments slice is empty")
		}

		p, ok := choice(positions)
		if !ok {
			panic("positions slice is empty")
		}

		dp := DepartmentPosition{
			ID:           uuid.New(),
			DepartmentID: d.ID,
			PositionID:   p.ID,
		}

		pair := departmentPositionIDPair{dp.DepartmentID, dp.PositionID}

		if _, ok := seen[pair]; ok {
			continue
		}

		relations = append(relations, &dp)
		seen[pair] = struct{}{}
	}

	return relations
}

type DepartmentLocation struct {
	ID           uuid.UUID
	DepartmentID uuid.UUID
	LocationID   uuid.UUID
	IsPrimary    bool
}

type departmentLocationIDPair struct {
	departmentID uuid.UUID
	locationID   uuid.UUID
}

func SeedDepartmentLocations(count int, departments []*Department, locations []*Location) []*DepartmentLocation {
	var relations []*DepartmentLocation

	seen := make(map[departmentLocationIDPair]struct{}, count)

	if count > len(departments)*len(locations) {
		count = len(departments) * len(locations)
	}

	for len(relations) < count {
		d, ok := choice(departments)
		if !ok {
			panic("departments slice is empty")
		}

		l, ok := choice(locations)
		if !ok {
			panic("locations slice is empty")
		}

		dl := DepartmentLocation{
			ID:           uuid.New(),
			DepartmentID: d.ID,
			LocationID:   l.ID,
			IsPrimary:    false,
		}

		pair := departmentLocationIDPair{dl.DepartmentID, dl.LocationID}

		if _, ok := seen[pair]; ok {
			continue
		}

		relations = append(relations, &dl)
		seen[pair] = struct{}{}
	}

	return relations
}
