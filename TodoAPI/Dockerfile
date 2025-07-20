# BUILD STAGE

FROM mcr.microsoft.com/dotnet/sdk:9.0.303-alpine3.21-amd64 AS build

WORKDIR /app
COPY . .

RUN addgroup -g 10001 -S builder && \
    adduser -u 10000 -S -G builder builder && \
    chown -R builder:builder /app
USER builder:builder

RUN dotnet restore && \
    dotnet publish -o out


# RUNTIME STAGE
FROM mcr.microsoft.com/dotnet/aspnet:9.0.7-alpine3.21-amd64

WORKDIR /app
RUN addgroup -g 10001 -S prod && \
    adduser -u 10000 -S -G prod prod && \
    chown -R prod:prod /app
USER prod:prod

COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://0.0.0.0:5143
ENV ASPNETCORE_ENVIRONMENT=Development

# HTTP port
EXPOSE 5143

ENTRYPOINT ["dotnet", "TodoAPI.dll"]
