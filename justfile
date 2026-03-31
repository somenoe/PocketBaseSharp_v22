# https://just.systems

set windows-shell := ["pwsh", "-c"]

default:
    @just --list

pb *COMMAND="--automigrate=false --dir='./temp_pb_data' serve":
    rm -r temp_pb_data || true
    cp -r PocketBase/pb_data temp_pb_data
    ./PocketBase/pocketbase.exe {{ COMMAND }}

PB_VERSION := "0.22.40"
PB_DOWNLOAD_URL := "https://github.com/pocketbase/pocketbase/releases/download/v" + PB_VERSION + "/pocketbase_" + PB_VERSION + "_windows_amd64.zip"

download-pb:
    curl -L {{ PB_DOWNLOAD_URL }} -o pocketbase.temp.zip
    unzip -o pocketbase.temp.zip -d PocketBase
    rm pocketbase.temp.zip

download-tw:
    mkdir .\PocketBaseSharp.Demo\tools -Force; cd .\PocketBaseSharp.Demo\tools; Invoke-WebRequest -Uri https://github.com/tailwindlabs/tailwindcss/releases/download/v4.1.8/tailwindcss-windows-x64.exe -OutFile tailwindcss.exe -UseBasicParsing;

setup: download-pb download-tw
    dotnet tool install -g dotnet-reportgenerator-globaltool

build:
    dotnet build

test:
    dotnet test

test-coverage:
    @if (Test-Path ./CoverageResults) { Remove-Item ./CoverageResults -Force -Recurse }
    dotnet test --collect:"XPlat Code Coverage" --results-directory ./CoverageResults
    @if (Test-Path ./CoverageResults) { reportgenerator -reports:"CoverageResults/**/*.xml" -targetdir:"CoverageResults" -reporttypes:TextSummary }

demo-build:
    dotnet publish PocketBaseSharp.Demo/Demo.csproj

demo-serve:
    dotnet serve -d PocketBaseSharp.Demo\bin\Release\net10.0\publish\wwwroot --port 3000  --fallback-file index.html
