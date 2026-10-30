import { apiRequest } from "@/shared/api";

type SearchItem = { id: string; name: string };
export type SearchPage = { items: SearchItem[]; hasMore: boolean };

export const SEARCH_PAGE_SIZE = 4;

export type SearchResult = {
  departments: SearchPage;
  locations: SearchPage;
  positions: SearchPage;
};

export function searchDirectory(q: string, signal: AbortSignal) {
  return apiRequest<SearchResult>({
    method: "GET",
    url: "search",
    params: { q, limitPerType: SEARCH_PAGE_SIZE },
    signal,
  });
}

export function searchDirectoryPage(
  entityType: keyof SearchResult,
  q: string,
  page: number,
  signal: AbortSignal,
) {
  return apiRequest<SearchPage>({
    method: "GET",
    url: `search/${entityType}`,
    params: { q, page, pageSize: SEARCH_PAGE_SIZE },
    signal,
  });
}
