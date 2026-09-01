package seeders

import (
	"uuid"
)

type DepartmentPosition struct {
	ID           uuid.UUID
	DepartmentID uuid.UUID
	PositionID   uuid.UUID
}

func SeedDepartmentPositions(count int, departments []*Department, positions []*Position) []*DepartmentPosition {
	var relations []*DepartmentPosition

	seen := make(map[DepartmentPosition]struct{}, count)

	for len(relations) < count {
		d, ok := choice(departments)
		if !ok {
			panic("departments slice is empty")
		}

		p, ok := choice(positions)
		if !ok {
			panic("departments slice is empty")
		}

		dp := DepartmentPosition{
			ID:           uuid.New(),
			DepartmentID: d.ID,
			PositionID:   p.ID,
		}

		if _, ok := seen[dp]; ok {
			continue
		}

		relations = append(relations, &dp)
		seen[dp] = struct{}{}
	}

	return relations
}

type DepartmentLocation struct {
	ID           uuid.UUID
	DepartmentID uuid.UUID
	LocationID   uuid.UUID
}
