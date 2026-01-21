ARG DENO_VERSION="2.4.2"

# Build stage
FROM denoland/deno:${DENO_VERSION} AS builder
WORKDIR /app
COPY ./api .
RUN deno cache main.ts

# Production stage
FROM denoland/deno:${DENO_VERSION}
WORKDIR /app
COPY --from=builder /app .
CMD ["deno", "run", "--allow-net", "--allow-read", "--allow-env", "main.ts"]

