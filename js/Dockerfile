# Build stage for better optimization
ARG NODE_VERSION

FROM node:${NODE_VERSION} AS builder

RUN apk add --no-cache python3 make g++

WORKDIR /app
COPY js/ .
RUN npm ci
RUN npm run build

# Final production image
FROM node:${NODE_VERSION} AS final
RUN apk add --no-cache python3 make g++

WORKDIR /app
COPY --from=builder /app .

CMD ["sh", "-c", "npm run start"]