import { expect, test, type Page } from "@playwright/test";

const source = {
  id: "source",
  name: "Разработка",
  slug: "development",
  path: "company.development",
  parentId: "company",
  depth: 1,
  createdAt: "2026-01-01T00:00:00Z",
  hasChildren: true,
  childCount: 1,
};
const company = {
  ...source,
  id: "company",
  name: "Компания",
  slug: "company",
  path: "company",
  parentId: null,
  depth: 0,
};
const target = {
  ...company,
  id: "target",
  name: "Операции",
  slug: "operations",
  path: "operations",
  hasChildren: false,
  childCount: 0,
};
const descendants = [
  { id: "child", name: "Backend", parentId: source.id },
  { id: "grandchild", name: "API", parentId: "child" },
  { id: "archived", name: "Архив API", parentId: "grandchild" },
];

async function setup(page: Page, failDescendants = false) {
  const state = {
    current: { ...source },
    moves: [] as { newParentId: string | null }[],
    listRequests: [] as URL[],
    treeReads: 0,
    detailReads: 0,
    descendantsReads: 0,
    failDescendants,
    failTransfer: false,
    transferGate: undefined as Promise<void> | undefined,
    childReads: [] as string[],
  };
  const options = [
    source,
    ...descendants.map((item) => ({ ...source, ...item })),
    target,
    ...Array.from({ length: 12 }, (_, i) => ({
      ...target,
      id: `other-${i}`,
      name: `Отдел ${i}`,
    })),
  ].map((item) => ({ ...item, isActive: item.id !== "archived" }));

  await page.route("**/api/**", async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    const ok = (result: unknown) =>
      route.fulfill({ json: { isError: false, result } });
    const paged = (results: unknown[]) =>
      ok({ results, totalCount: results.length, page: 1, pageSize: 20 });

    if (path.endsWith("/parent") && request.method() === "PUT") {
      const body = request.postDataJSON();
      state.moves.push(body);
      await state.transferGate;
      if (state.failTransfer) {
        return route.fulfill({
          status: 409,
          json: {
            isError: true,
            error: {
              type: "conflict",
              messages: [
                { code: "department.transfer.conflict", message: "Conflict" },
              ],
            },
          },
        });
      }
      state.current = {
        ...state.current,
        parentId: body.newParentId,
        path: body.newParentId ? "operations.development" : "development",
        depth: body.newParentId ? 1 : 0,
      };
      return ok(state.current);
    }
    if (path === "/api/departments/tree") {
      state.treeReads++;
      return paged([
        {
          ...company,
          hasChildren: state.current.parentId === company.id,
          childCount: state.current.parentId === company.id ? 1 : 0,
        },
        {
          ...target,
          hasChildren: state.current.parentId === target.id,
          childCount: state.current.parentId === target.id ? 1 : 0,
        },
        ...(state.current.parentId === null ? [state.current] : []),
      ]);
    }
    if (path.endsWith("/children")) {
      const parentId = path.split("/").at(-2)!;
      state.childReads.push(parentId);
      return paged(state.current.parentId === parentId ? [state.current] : []);
    }
    if (path.endsWith("/descendants")) {
      state.descendantsReads++;
      if (state.failDescendants)
        return route.fulfill({ status: 503, json: {} });
      return ok(descendants);
    }
    if (path.endsWith("/positions")) return paged([]);
    if (path === "/api/departments/source") {
      state.detailReads++;
      return ok(state.current);
    }
    if (path === "/api/departments/company") return ok(company);
    if (path === "/api/departments/target") return ok(target);
    if (path === "/api/departments") {
      state.listRequests.push(url);
      const excluded = url.searchParams.getAll("excludeIds");
      const search = url.searchParams.get("search")?.toLowerCase() ?? "";
      const active = url.searchParams.get("isActive");
      const filtered = options.filter(
        (item) =>
          !excluded.includes(item.id) &&
          item.name.toLowerCase().includes(search) &&
          (active === null || item.isActive === (active === "true")),
      );
      const pageNumber = Number(url.searchParams.get("page"));
      const pageSize = Number(url.searchParams.get("pageSize"));
      return ok({
        results: filtered.slice(
          (pageNumber - 1) * pageSize,
          pageNumber * pageSize,
        ),
        totalCount: filtered.length,
        page: pageNumber,
        pageSize,
      });
    }
    throw new Error(`Unexpected API request: ${request.method()} ${url}`);
  });

  await page.goto("/organization-structure");
  await page
    .getByRole("button", { name: "Развернуть Компания", exact: true })
    .click();
  await page.getByRole("button", { name: "Разработка 1", exact: true }).click();
  await page.getByRole("button", { name: "Перенести", exact: true }).click();
  return state;
}

async function selectTarget(page: Page) {
  await page
    .getByRole("button", { name: "Новый родитель", exact: true })
    .click();
  await page.getByRole("option", { name: "Операции", exact: true }).click();
  await page.getByRole("button", { name: "Далее", exact: true }).click();
}

test("excludes the full unloaded subtree during pagination, search and status filtering", async ({
  page,
}) => {
  const state = await setup(page);
  const dialog = page.getByRole("dialog", {
    name: "Перенести подразделение",
    exact: true,
  });
  await expect(
    dialog.getByRole("button", { name: "Далее", exact: true }),
  ).toBeDisabled();
  await page
    .getByRole("button", { name: "Новый родитель", exact: true })
    .click();
  await expect(
    page.getByRole("option", { name: "Операции", exact: true }),
  ).toBeVisible();
  await page.getByRole("listbox").evaluate((list) => {
    list.scrollTop = list.scrollHeight;
  });
  await expect
    .poll(() =>
      state.listRequests.some((url) => url.searchParams.get("page") === "2"),
    )
    .toBe(true);
  await page
    .getByRole("combobox", { name: "Новый родитель", exact: true })
    .fill("API");
  await expect(
    page.getByText("Подразделения не найдены", { exact: true }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Архивные", exact: true }).click();
  await expect
    .poll(() =>
      state.listRequests.some(
        (url) => url.searchParams.get("isActive") === "false",
      ),
    )
    .toBe(true);
  for (const url of state.listRequests) {
    expect(url.searchParams.getAll("excludeIds").sort()).toEqual(
      [source.id, ...descendants.map((item) => item.id)].sort(),
    );
  }
  expect(state.childReads).toEqual([company.id]);
  expect(state.moves).toEqual([]);
});

test("confirms a new parent and refreshes the tree and selected department without navigation", async ({
  page,
}) => {
  const state = await setup(page);
  let navigations = 0;
  page.on("framenavigated", () => navigations++);
  await selectTarget(page);
  const dialog = page.getByRole("dialog", {
    name: "Подтвердить перенос",
    exact: true,
  });
  await expect(dialog.getByText("Разработка", { exact: true })).toBeVisible();
  await expect(dialog.getByText("Компания", { exact: true })).toBeVisible();
  await expect(dialog.getByText("Операции", { exact: true })).toBeVisible();
  await expect(
    dialog.getByText(
      "Все дочерние подразделения переместятся вместе с выбранным.",
    ),
  ).toBeVisible();
  await page.screenshot({ path: test.info().outputPath("confirmation.png") });
  expect(state.moves).toEqual([]);
  const treeReads = state.treeReads;
  const detailReads = state.detailReads;
  await dialog.getByRole("button", { name: "Перенести", exact: true }).click();
  await expect(dialog).toBeHidden();
  expect(state.moves).toEqual([{ newParentId: target.id }]);
  await expect(
    page
      .getByLabel("Выбранное подразделение")
      .getByText("operations.development", { exact: true }),
  ).toBeVisible();
  await expect.poll(() => state.treeReads).toBeGreaterThan(treeReads);
  await expect.poll(() => state.detailReads).toBeGreaterThan(detailReads);
  await page
    .getByRole("button", { name: "Развернуть Операции", exact: true })
    .click();
  await expect(
    page
      .getByRole("navigation", { name: "Дерево подразделений" })
      .getByRole("button", { name: "Разработка 1", exact: true }),
  ).toBeVisible();
  expect(navigations).toBe(0);
});

test("cancels both steps, clears a selected parent for a root move, and displays the current root", async ({
  page,
}) => {
  const state = await setup(page);
  await page.getByRole("button", { name: "Отмена", exact: true }).click();
  expect(state.moves).toEqual([]);
  await page.getByRole("button", { name: "Перенести", exact: true }).click();
  await selectTarget(page);
  await page.getByRole("button", { name: "Отмена", exact: true }).click();
  expect(state.moves).toEqual([]);
  await page.getByRole("button", { name: "Перенести", exact: true }).click();
  await selectTarget(page);
  await page.getByRole("button", { name: "Назад", exact: true }).click();
  await page
    .getByRole("button", { name: "Сделать корневым", exact: true })
    .click();
  const dialog = page.getByRole("dialog", {
    name: "Подтвердить перенос",
    exact: true,
  });
  await expect(dialog.getByText("Компания", { exact: true })).toBeVisible();
  await expect(
    dialog.getByText("Корень оргструктуры", { exact: true }),
  ).toBeVisible();
  expect(state.moves).toEqual([]);
  await dialog.getByRole("button", { name: "Перенести", exact: true }).click();
  await expect(dialog).toBeHidden();
  expect(state.moves).toEqual([{ newParentId: null }]);
  await expect(
    page
      .getByLabel("Выбранное подразделение")
      .getByRole("definition")
      .filter({ hasText: /^development$/ }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Перенести", exact: true }).click();
  await selectTarget(page);
  await expect(
    page.getByRole("dialog").getByText("Корень оргструктуры", { exact: true }),
  ).toBeVisible();
});

test("keeps confirmation on error and prevents duplicate requests during retry", async ({
  page,
}) => {
  const state = await setup(page);
  state.failTransfer = true;
  await selectTarget(page);
  const dialog = page.getByRole("dialog", {
    name: "Подтвердить перенос",
    exact: true,
  });
  await dialog.getByRole("button", { name: "Перенести", exact: true }).click();
  await expect(dialog.getByRole("alert")).toHaveText(
    "Структура изменилась во время переноса. Попробуйте ещё раз",
  );
  state.failTransfer = false;
  let release!: () => void;
  state.transferGate = new Promise<void>((resolve) => {
    release = resolve;
  });
  await dialog.getByRole("button", { name: "Перенести", exact: true }).click();
  await expect(
    dialog.getByRole("button", { name: "Перенос…", exact: true }),
  ).toBeDisabled();
  await expect(
    dialog.getByRole("button", { name: "Отмена", exact: true }),
  ).toBeDisabled();
  await expect(
    dialog.getByRole("button", { name: "Назад", exact: true }),
  ).toBeDisabled();
  await page.keyboard.press("Escape");
  await expect(dialog).toBeVisible();
  await expect.poll(() => state.moves.length).toBe(2);
  release();
  await expect(dialog).toBeHidden();
});

test("blocks parent selection until all descendants load and allows retry after a load error", async ({
  page,
}) => {
  const state = await setup(page, true);
  const dialog = page.getByRole("dialog", {
    name: "Перенести подразделение",
    exact: true,
  });
  await expect(dialog.getByRole("alert")).toContainText(
    "Не удалось загрузить данные для переноса",
  );
  await expect(
    dialog.getByRole("button", { name: "Далее", exact: true }),
  ).toBeDisabled();
  await expect(
    dialog.getByRole("button", { name: "Новый родитель", exact: true }),
  ).toHaveCount(0);
  state.failDescendants = false;
  await dialog.getByRole("button", { name: "Повторить", exact: true }).click();
  await expect(
    dialog.getByRole("button", { name: "Новый родитель", exact: true }),
  ).toBeEnabled();
  expect(state.moves).toEqual([]);
});
