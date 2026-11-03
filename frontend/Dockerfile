FROM node:24-alpine AS build

WORKDIR /app

COPY package.json package-lock.json* ./

RUN npm ci

COPY . .

ENV NEXT_PUBLIC_API_URL=/api

RUN npm run build

FROM node:24-alpine AS runtime

WORKDIR /app

COPY --from=build /app/public ./public
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static

EXPOSE 3000

ENV PORT=3000
ENV HOST="0.0.0.0"

CMD [ "node", "server.js" ]