package seeders

import (
	"fmt"
	"math/rand/v2"
	"time"
	"uuid"
)

type Department struct {
	ID uuid.UUID

	Name string
	Slug string
	Path string

	ParentID *uuid.UUID
	Depth    int

	CreatedAt time.Time
	UpdatedAt time.Time
}

const pathSeparator string = "."

func NewDepartment(name string, slug string, parent *Department) *Department {
	path := slug

	var parentId *uuid.UUID

	depth := 0

	if parent != nil {
		path = parent.Path + pathSeparator + slug
		parentId = &parent.ID

		depth = parent.Depth + 1
	}

	now := time.Now()

	return &Department{
		ID:        uuid.New(),
		Name:      name,
		Slug:      slug,
		Path:      path,
		ParentID:  parentId,
		Depth:     depth,
		CreatedAt: now.UTC(),
		UpdatedAt: now.UTC(),
	}
}

func SeedDepartments(count int, minRoot int, maxRoot int) []*Department {
	var departments []*Department

	rootDepartmentsCount := randomRange(minRoot, maxRoot)

	for i := range rootDepartmentsCount {
		departments = append(departments, NewDepartment(
			fmt.Sprintf("Department %d", i),
			fmt.Sprintf("department-%d", i),
			nil,
		))
	}

	for i := rootDepartmentsCount; i <= count; i++ {
		parent, ok := choice(departments)
		if !ok {
			panic("departments slice is empty")
		}

		departments = append(departments, NewDepartment(
			fmt.Sprintf("Department %d", i),
			fmt.Sprintf("department-%d", i),
			parent,
		))
	}

	return departments
}

type nameSlugPair struct {
	Name string `json:"name"`
	Slug string `json:"slug"`
}

func randomRange(min, max int) int {
	if min > max {
		min, max = max, min
	}
	return rand.IntN(max-min+1) + min
}
