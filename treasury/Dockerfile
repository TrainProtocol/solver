ARG NODE_VERSION=22
FROM node:${NODE_VERSION}-alpine AS builder

WORKDIR /usr/src/app

# Install build dependencies for native modules
RUN apk add --no-cache python3 make g++

COPY ./treasury/package*.json ./

RUN npm ci

COPY ./treasury .

RUN npm run build

# Production stage with production dependencies only
FROM node:${NODE_VERSION}-alpine AS production
WORKDIR /usr/src/app

# Install build dependencies for production dependencies
RUN apk add --no-cache python3 make g++

COPY ./treasury/package*.json ./

RUN npm ci --production && npm cache clean --force

COPY --from=builder /usr/src/app/dist ./dist

EXPOSE 8080

CMD ["node", "dist/main"]