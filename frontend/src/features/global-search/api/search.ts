import { apiRequest } from "@/shared/api";

type SearchItem = { id: string; name: string };
type SearchGroup = { items: SearchItem[]; hasMore: boolean };

export type SearchResult = {
  departments: SearchGroup;
  locations: SearchGroup;
  positions: SearchGroup;
};

export function searchDirectory(q: string, signal: AbortSignal) {
  return apiRequest<SearchResult>({
    method: "GET",
    url: "search",
    params: { q },
    signal,
  });
}
