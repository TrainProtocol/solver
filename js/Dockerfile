# Build stage for better optimization
ARG NODE_VERSION

FROM node:${NODE_VERSION} AS builder

RUN apt-get update \
 && apt-get install -y python3 make g++ \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY js/package*.json ./
RUN npm ci

COPY js/ .
RUN npm run build

FROM node:${NODE_VERSION} AS final

WORKDIR /app
COPY --from=builder /app .

CMD ["npm", "run", "start"]
