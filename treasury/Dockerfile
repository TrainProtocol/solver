ARG NODE_VERSION=22
FROM node:${NODE_VERSION}-alpine AS builder

WORKDIR /usr/src/app

COPY treasury/package*.json ./

RUN npm ci

COPY . .

RUN npm run build

# Production stage with production dependencies only
FROM node:${NODE_VERSION}-alpine AS production
WORKDIR /usr/src/app

COPY package*.json ./

RUN npm ci --production && npm cache clean --force

COPY --from=builder /usr/src/app/dist ./dist

EXPOSE 6969

CMD ["node", "dist/main"]