#### Build the UI ###
FROM node AS ui

# Add the code
ADD ./ui /code
WORKDIR /code

# Install dependencies and build the UI
RUN npm install && npm run build

#### Build the API ###
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS api

# Add the code
ADD ./api /code
WORKDIR /code

# Build the app
RUN dotnet publish -c Release

#### Build the API container ###
FROM mcr.microsoft.com/dotnet/aspnet:7.0

# Install graphics support
RUN apt-get update && apt-get install -y libfontconfig1

# Copy the build files
COPY --from=api /code/bin/Release/net6.0/publish /app
WORKDIR /app

# Copy the static assets
COPY --from=ui /code/build /app/static

# Adjust permissions
RUN chmod +x .

# Copy the compiled assets back to the working dir
COPY /app /out