# Build stage for better optimization
ARG NODE_VERSION

FROM node:${NODE_VERSION} AS builder

WORKDIR /app
COPY js/ .
RUN npm ci --only=production
RUN npm run build

# Final production image
FROM node:${NODE_VERSION} AS final

WORKDIR /app
COPY --from=builder /app .

CMD ["sh", "-c", "npm run start"]