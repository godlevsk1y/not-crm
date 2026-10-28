This is a [Next.js](https://nextjs.org) project bootstrapped with [`create-next-app`](https://nextjs.org/docs/app/api-reference/cli/create-next-app).

## List status filters

The department, location, and position lists initially request active records.
Filtering uses the API implemented in [backend subissue #102](https://github.com/godlevsk1y/not-crm/issues/102), linked to [#103](https://github.com/godlevsk1y/not-crm/issues/103).

| Endpoint | Active | Archived | All |
| --- | --- | --- | --- |
| `GET /api/departments` | `isActive=true` | `isActive=false` | Omit `isActive` |
| `GET /api/locations` | `isActive=true` | `isActive=false` | Unavailable |
| `GET /api/positions` | `isActive=true` | `isActive=false` | Unavailable |

Locations and positions accept only boolean values; empty and `null` values are invalid. Omitting `isActive` defaults to active records for these endpoints. Departments return both statuses when the parameter is omitted.

The server applies the status together with the other filters before pagination and computes `totalCount` for the matching records. Changing status preserves other filters and sorting, resets paginated lists to page one, and restarts position loading from its first page.

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

## Deploy on Vercel

The easiest way to deploy your Next.js app is to use the [Vercel Platform](https://vercel.com/new?utm_medium=default-template&filter=next.js&utm_source=create-next-app&utm_campaign=create-next-app-readme) from the creators of Next.js.

Check out our [Next.js deployment documentation](https://nextjs.org/docs/app/building-your-application/deploying) for more details.
