ARG NODE_VERSION=22
FROM node:${NODE_VERSION}-alpine AS builder

WORKDIR /usr/src/app

COPY package*.json ./

RUN npm ci --only=production=false

COPY . .

RUN npm run build

# Production stage with production dependencies only
FROM node:22-alpine AS production
WORKDIR /usr/src/app

COPY package*.json ./

RUN npm ci --only=production && npm cache clean --force

COPY --from=builder /usr/src/app/dist ./dist

EXPOSE 6969

CMD ["node", "dist/main"]