package seeders

import "math/rand/v2"

func choice[T any](s []T) (T, bool) {
	var zero T

	if s == nil {
		return zero, false
	}

	idx := rand.IntN(len(s))
	return s[idx], true
}
