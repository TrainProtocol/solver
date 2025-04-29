# Build stage for better optimization
FROM node:20 AS builder

WORKDIR /app
COPY js/ .

RUN npm ci

# Final production image
FROM node:20 AS final

WORKDIR /app
COPY --from=builder /app .

ARG NETWORK
ENV NETWORK=${NETWORK}
RUN npx turbo run build --filter=@blockchain/${NETWORK}

RUN npm run build --workspace=@blockchain/${NETWORK}
RUN npm run build --workspace=@blockchain/${NETWORK}
CMD ["sh", "-c", "npm run start --workspace=@blockchain/${NETWORK}"]