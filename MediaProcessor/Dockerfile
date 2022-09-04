FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MediaProcessor/MediaProcessor.csproj", "MediaProcessor/"]
RUN dotnet restore "MediaProcessor/MediaProcessor.csproj"
COPY . .
WORKDIR "/src/MediaProcessor"
RUN dotnet build "MediaProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MediaProcessor.csproj" -c Release -o /app/publish

FROM ubuntu:latest AS ffmpeg
RUN apt clean && apt autoclean && apt update
RUN apt-get -y install wget
RUN apt-get -y install xz-utils
RUN wget https://johnvansickle.com/ffmpeg/builds/ffmpeg-git-amd64-static.tar.xz
RUN tar -xf ffmpeg-git-amd64-static.tar.xz

FROM base AS final

#ENV MediaIO__FFMPEG = /ffmpeg
#ENV MediaIO__ProcessedMediaLocation = /processed
# make this arg
#ENV MediaIO__SampleAudio = /sampleaudio.wav 
COPY ["sampleaudio.wav", "."]
RUN mkdir /processed
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=ffmpeg ffmpeg-git-20220622-amd64-static/ffmpeg /usr/local/bin/
COPY --from=ffmpeg ffmpeg-git-20220622-amd64-static/ffprobe /usr/local/bin/
COPY sampleaudio.wav /
ENTRYPOINT ["dotnet", "MediaProcessor.dll"]