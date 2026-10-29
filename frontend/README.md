This is a [Next.js](https://nextjs.org) project bootstrapped with [`create-next-app`](https://nextjs.org/docs/app/api-reference/cli/create-next-app).

## Getting Started

First, run the development server:

```bash
npm run dev
# or
yarn dev
# or
pnpm dev
# or
bun dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

You can start editing the page by modifying `app/page.tsx`. The page auto-updates as you edit the file.

This project uses [`next/font`](https://nextjs.org/docs/app/building-your-application/optimizing/fonts) to automatically optimize and load [Geist](https://vercel.com/font), a new font family for Vercel.

## Learn More

To learn more about Next.js, take a look at the following resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

You can check out [the Next.js GitHub repository](https://github.com/vercel/next.js) - your feedback and contributions are welcome!

## List status filters

The department, location, and position lists initially request active records.

| Endpoint               | Active          | Archived         | All             |
| ---------------------- | --------------- | ---------------- | --------------- |
| `GET /api/departments` | `isActive=true` | `isActive=false` | Omit `isActive` |
| `GET /api/locations`   | `isActive=true` | `isActive=false` | Unavailable     |
| `GET /api/positions`   | `isActive=true` | `isActive=false` | Unavailable     |

Locations and positions accept only boolean values; empty and `null` values are invalid. Omitting `isActive` defaults to active records for these endpoints. Departments return both statuses when the parameter is omitted.

The server applies the status together with the other filters before pagination and computes `totalCount` for the matching records. Changing status preserves other filters and sorting, resets paginated lists to page one, and restarts position loading from its first page.

## Restore archived records

Archived records have a «Восстановить» button. Departments also show it on archived cards in the «Все» view. Restoration refreshes directory queries, including selectors and the organization tree. Existing filters and sorting stay selected; a paginated list moves to its last available page if the current page disappears.

Run the browser tests from `frontend`:

```bash
npm ci
npx playwright install chromium
npm run test:e2e
```

The tests start a local development server on port 3112 and mock the API; no running backend is needed.
