# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["QuizWorker/QuizWorker.csproj", "./"]
RUN dotnet restore

# Copy the remaining source and publish
COPY ./QuizWorker ./
RUN dotnet publish -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Install libgssapi for Npgsql
RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2 \
 && rm -rf /var/lib/apt/lists/*

# IMPORTANT: Replace with your actual DLL name (from .csproj)
ENTRYPOINT ["dotnet", "QuizWorker.dll"]