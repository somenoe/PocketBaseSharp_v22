# https://just.systems

set windows-shell := ["pwsh", "-c"]

default:
    @just --list

pb *COMMAND="serve":
    ./PocketBase/pocketbase.exe {{ COMMAND }}

PB_VERSION := "0.22.40"
PB_DOWNLOAD_URL := "https://github.com/pocketbase/pocketbase/releases/download/v" + PB_VERSION + "/pocketbase_" + PB_VERSION + "_windows_amd64.zip"

download-pb:
    curl -L {{ PB_DOWNLOAD_URL }} -o pocketbase.temp.zip
    unzip -o pocketbase.temp.zip -d PocketBase
    rm pocketbase.temp.zip

setup: download-pb
