import { expect, test, type Page } from "@playwright/test";

type RecordItem = { id: string; name: string; isActive: boolean };
type Directory = "departments" | "locations" | "positions";

async function mockDirectory(page: Page, directory: Directory, archivedCount = 1) {
  const records: RecordItem[] = Array.from({ length: archivedCount + 1 }, (_, i) => ({
    id: String(i),
    name: `Запись ${i}`,
    isActive: i === 0,
  }));
  const requests: URL[] = [];
  let attempts = 0;
  let errorCode: string | undefined;
  let release: (() => void) | undefined;

  await page.route("**/api/**", async (route) => {
    const url = new URL(route.request().url());
    if (url.pathname.endsWith("/restore")) {
      attempts++;
      expect(route.request().method()).toBe("POST");
      expect(route.request().postData()).toBeNull();
      await new Promise<void>((resolve) => { release = resolve; });
      if (errorCode) {
        await route.fulfill({ status: 409, json: {
          isError: true,
          error: { type: "conflict", messages: [{ code: errorCode, message: "Conflict" }] },
        } });
      } else {
        records.find((item) => item.id === url.pathname.split("/").at(-2))!.isActive = true;
        await route.fulfill({ status: 204 });
      }
      return;
    }
    requests.push(url);
    if (url.pathname === "/api/departments/11111111-1111-4111-8111-111111111111") {
      await route.fulfill({ json: { isError: false, result: {
        id: "11111111-1111-4111-8111-111111111111", name: "Выбранное подразделение",
      } } });
      return;
    }
    const active = url.searchParams.get("isActive");
    const filtered = records.filter((item) => active === null || item.isActive === (active === "true"));
    const currentPage = Number(url.searchParams.get("page") ?? 1);
    const pageSize = Number(url.searchParams.get("pageSize") ?? 9);
    await route.fulfill({ json: {
      isError: false,
      result: {
        results: filtered.slice((currentPage - 1) * pageSize, currentPage * pageSize).map((item) => ({
          ...item, slug: `record-${item.id}`, path: `record_${item.id}`,
          createdAt: "2026-10-01T00:00:00Z", departmentCount: 0,
          address: { country: "Россия", city: "Москва", street: "Лесная", houseNumber: "1" },
        })),
        totalCount: filtered.length, page: currentPage, pageSize,
      },
    } });
  });
  return {
    requests,
    get attempts() { return attempts; },
    failWith(code?: string) { errorCode = code; },
    finish() { release!(); },
  };
}

for (const directory of ["departments", "locations", "positions"] as const) {
  test(`${directory}: restore, pending state, refreshed counts and active list`, async ({ page }) => {
    const api = await mockDirectory(page, directory);
    await page.goto(`/${directory}`);
    await expect(page.getByRole("heading", { name: "Запись 0", exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: "Восстановить", exact: true })).toHaveCount(0);
    await page.getByRole("button", { name: "Архивный", exact: true }).click();
    await page.getByRole("button", { name: "Восстановить", exact: true }).click();
    const pending = page.getByRole("button", { name: "Восстановление…", exact: true });
    await expect(pending).toBeDisabled();
    await pending.dispatchEvent("click");
    await expect.poll(() => api.attempts).toBe(1);
    api.finish();
    await expect(page.getByText(/^(Подразделение|Локация|Должность) восстановлен[оа]$/)).toBeVisible();
    await expect(page.getByRole("heading", { name: "Запись 1", exact: true })).toHaveCount(0);
    await expect(page.getByText(/^(Подразделений|Локаций|Должностей): 0$/)).toBeVisible();
    await expect(page.getByRole("button", { name: "Архивный", exact: true })).toHaveAttribute("aria-pressed", "true");
    await page.getByRole("button", { name: "Активный", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Запись 1", exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: "Восстановить", exact: true })).toHaveCount(0);
  });

  test(`${directory}: error keeps record and allows retry`, async ({ page }) => {
    const api = await mockDirectory(page, directory);
    api.failWith(directory === "departments" ? "department.parent.archived" : "record.conflict");
    await page.goto(`/${directory}`);
    await page.getByRole("button", { name: "Архивный", exact: true }).click();
    await page.getByRole("button", { name: "Восстановить", exact: true }).click();
    await expect.poll(() => api.attempts).toBe(1);
    api.finish();
    await expect(page.getByText(directory === "departments"
      ? "Сначала восстановите родительское подразделение"
      : "Не удалось восстановить: конфликт с существующими данными")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Запись 1", exact: true })).toBeVisible();
    const restore = page.getByRole("button", { name: "Восстановить", exact: true });
    await expect(restore).toBeEnabled();
    api.failWith();
    await restore.click();
    await expect.poll(() => api.attempts).toBe(2);
    api.finish();
    await expect(page.getByRole("heading", { name: "Запись 1", exact: true })).toHaveCount(0);
  });
}

for (const directory of ["departments", "locations"] as const) {
  test(`${directory}: restoring last record on page goes back and preserves query`, async ({ page }) => {
    const api = await mockDirectory(page, directory, 10);
    const filter = directory === "locations" ? "?departmentIds=11111111-1111-4111-8111-111111111111" : "";
    await page.goto(`/${directory}${filter}`);
    await page.getByRole("button", { name: "Архивный", exact: true }).click();
    await page.getByRole("button", { name: "Страница 2", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Запись 10", exact: true })).toBeVisible();
    await page.getByRole("button", { name: "Восстановить", exact: true }).click();
    await expect.poll(() => api.attempts).toBe(1);
    api.finish();
    await expect(page.getByRole("heading", { name: "Запись 1", exact: true })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Запись 10", exact: true })).toHaveCount(0);
    const lastRequest = api.requests.filter((url) => url.pathname === `/api/${directory}`).at(-1)!;
    expect(lastRequest.searchParams.get("page")).toBe("1");
    expect(lastRequest.searchParams.get("isActive")).toBe("false");
    expect(lastRequest.searchParams.get("sortBy")).toBe("name");
    expect(lastRequest.searchParams.get("sortDirection")).toBe("asc");
    if (directory === "locations") {
      expect(lastRequest.searchParams.get("departmentIds")).toBe("11111111-1111-4111-8111-111111111111");
      await expect(page.getByText("Выбранное подразделение", { exact: true })).toBeVisible();
    }
  });
}

test("departments: All shows restore only on archived records", async ({ page }) => {
  const api = await mockDirectory(page, "departments");
  await page.goto("/departments");
  await page.getByRole("button", { name: "Все", exact: true }).click();
  await expect(page.getByRole("button", { name: "Восстановить", exact: true })).toHaveCount(1);
  await page.getByRole("button", { name: "Восстановить", exact: true }).click();
  await expect.poll(() => api.attempts).toBe(1);
  api.finish();
  await expect(page.getByRole("button", { name: "Восстановить", exact: true })).toHaveCount(0);
  await expect(page.getByRole("heading", { name: "Запись 1", exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Все", exact: true })).toHaveAttribute("aria-pressed", "true");
});

test("positions: restoring the last item in the loaded page preserves earlier items", async ({ page }) => {
  const api = await mockDirectory(page, "positions", 10);
  await page.goto("/positions");
  await page.getByRole("button", { name: "Архивный", exact: true }).click();
  await page.getByRole("heading", { name: "Запись 9", exact: true }).scrollIntoViewIfNeeded();
  const last = page.getByRole("listitem").filter({ has: page.getByRole("heading", { name: "Запись 10", exact: true }) });
  await last.getByRole("button", { name: "Восстановить", exact: true }).click();
  await expect.poll(() => api.attempts).toBe(1);
  api.finish();
  await expect(page.getByRole("heading", { name: "Запись 10", exact: true })).toHaveCount(0);
  await expect(page.getByRole("heading", { name: "Запись 9", exact: true })).toBeVisible();
  await expect(page.getByText("Должностей: 9", { exact: true })).toBeVisible();
});
