# Use the official Ubuntu base image
FROM ubuntu:latest

# Install necessary dependencies
RUN apt-get update && \
    apt-get install -y wget apt-transport-https software-properties-common && \
    apt-get update

# Add the Microsoft package signing key to the list of trusted keys
RUN wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb

# Install .NET 8 SDK
RUN apt-get update && \
    apt-get install -y dotnet-sdk-8.0

# Install ffmpeg and ffprobe
RUN apt-get update && \
    apt-get install -y ffmpeg

# Copy the published application
COPY . /source

# Set the working directory
WORKDIR /source/IeeeVisUploader/IeeeVisUploaderWebApp
RUN dotnet publish -c release -o /webapp

# Expose the port your application runs on
EXPOSE 5103

# Set environment variables if necessary (e.g., ASPNETCORE_ENVIRONMENT)
# ENV ASPNETCORE_ENVIRONMENT=Production

WORKDIR /webapp

# Run the .NET application
CMD ["dotnet", "IeeeVisUploaderWebApp.dll"]
