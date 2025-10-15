ARG NODE_VERSION

FROM node:${NODE_VERSION} AS builder

RUN apt-get update && apt-get install -y python3 make g++ && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY js/ .
RUN npm ci
RUN npm run build

FROM node:${NODE_VERSION} AS final

RUN apt-get update && apt-get install -y python3 make g++ && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=builder /app .

CMD ["sh", "-c", "npm run start"]
