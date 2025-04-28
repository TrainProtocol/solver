# Build stage for better optimization
FROM node:20 AS builder

WORKDIR /app
COPY js/ .

RUN npm i --workspace=@blockchain/common
RUN npm run build --workspace=@blockchain/common

# Final production image
FROM node:20 AS final

WORKDIR /app
COPY --from=builder /app .

ARG NETWORK
ENV NETWORK=${NETWORK}

RUN npm run build --workspace=@blockchain/${NETWORK}
RUN npm run build --workspace=@blockchain/${NETWORK}
CMD ["sh", "-c", "npm run start --workspace=@blockchain/${NETWORK}"]