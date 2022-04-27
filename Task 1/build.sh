# Remove the release directory
rm -rf release

# Create the release directory
mkdir release
mkdir release/static

# Mark it as executable
chmod +x release

# Enter the api folder
cd api

# Build the dotnet api
dotnet publish -c Release

# Enter the ui folder
cd ../ui

# Build the ui
npm install --force && npm run build

# back to the parent folder
cd ..

# Copy the build assets
cp -r api/bin/Release/net6.0/publish/* ./release
cp -r ui/build/* ./release/static

# Enter the release directory
cd release

# Run the application
sudo dotnet api.dll