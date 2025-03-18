# Build stage for better optimization
FROM node:20 AS builder

WORKDIR /app
COPY js/ .
RUN npm ci
RUN npm run build

# Final production image
FROM node:20 AS final
WORKDIR /app

COPY --from=builder /app .

RUN npm ci --only=production

CMD ["npm", "start"]