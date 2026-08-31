package main

import (
	"encoding/json"
	"math/rand/v2"
	"os"
	"time"
	"uuid"
)

type Department struct {
	ID uuid.UUID

	Name string
	Slug string
	Path string

	ParentID *uuid.UUID

	CreatedAt time.Time
	UpdatedAt time.Time
}

func NewDepartment(name string, slug string, parent *Department) *Department {
	path := slug
	var parentId *uuid.UUID = nil

	if parent != nil {
		path = parent.Path + "/" + slug
		parentId = &parent.ID
	}

	now := time.Now()

	return &Department{
		ID:        uuid.New(),
		Name:      name,
		Slug:      slug,
		Path:      path,
		ParentID:  parentId,
		CreatedAt: now.UTC(),
		UpdatedAt: now.UTC(),
	}
}

func SeedDepartments(count int, minRoot int, maxRoot int) []*Department {
	var departments []*Department

	loadedPairs := getNameSlugPairs("data/departments.json")

	pairsPool := selectRandomNameSlugPairs(loadedPairs, count)

	rootDepartmentsCount := randomRange(minRoot, maxRoot)

	for range rootDepartmentsCount {
		departments = append(departments, NewDepartment(
			pairsPool[0].Name,
			pairsPool[0].Slug,
			nil,
		))

		pairsPool = pairsPool[1:]
	}

	for _, pair := range pairsPool {
		parent := selectRandomDepartment(departments)

		departments = append(departments, NewDepartment(
			pair.Name,
			pair.Slug,
			parent,
		))
	}

	return departments
}

type nameSlugPair struct {
	Name string `json:"name"`
	Slug string `json:"slug"`
}

func getNameSlugPairs(location string) []nameSlugPair {
	rawJson, err := os.ReadFile(location)
	if err != nil {
		panic(err)
	}

	var pairs []nameSlugPair

	err = json.Unmarshal(rawJson, &pairs)
	if err != nil {
		panic(err)
	}

	return pairs
}

func selectRandomNameSlugPairs(pairs []nameSlugPair, count int) []nameSlugPair {
	if count <= 0 || len(pairs) == 0 {
		return nil
	}

	if count > len(pairs) {
		count = len(pairs)
	}

	shuffled := make([]nameSlugPair, len(pairs))
	copy(shuffled, pairs)

	rand.Shuffle(len(shuffled), func(i, j int) {
		shuffled[i], shuffled[j] = shuffled[j], shuffled[i]
	})

	return shuffled[:count]
}

func selectRandomDepartment(departments []*Department) *Department {
	if len(departments) == 0 {
		return nil
	}

	idx := rand.IntN(len(departments))
	return departments[idx]
}

func randomRange(min, max int) int {
	if min > max {
		min, max = max, min
	}
	return rand.IntN(max-min+1) + min
}
