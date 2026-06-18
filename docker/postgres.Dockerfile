FROM postgres:17

# Copy seed.sql into the container
# PostgreSQL automatically runs .sql files in /docker-entrypoint-initdb.d/ on first startup
COPY database/seed.sql /docker-entrypoint-initdb.d/

# Set environment variables
ENV POSTGRES_DB=microservice_hub
ENV POSTGRES_USER=postgres
ENV POSTGRES_PASSWORD=postgres123

EXPOSE 5432