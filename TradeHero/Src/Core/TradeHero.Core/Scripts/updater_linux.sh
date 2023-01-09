#!/bin/sh

ENV="$1"
PROCESS_ID="$2"
DOWNLOADED_APP_PATH="$3"
APP_FOLDER="$4"
APP_NAME_TO_RUN="$5"

echo "$ENV"
echo "$PROCESS_ID"
echo "$DOWNLOADED_APP_PATH"
echo "$APP_FOLDER"
echo "$APP_NAME_TO_RUN"

pkill "$PROCESS_ID"
echo "After process killing"

cd / || exit
mv "$DOWNLOADED_APP_PATH" "$APP_FOLDER//$APP_NAME_TO_RUN"
echo "Downloaded app moved to base directory"

cd "$APP_FOLDER" || exit
chmod -R 777 "./$APP_NAME_TO_RUN"
echo "Set permissions to application"

eval "./$APP_NAME_TO_RUN --update --env=$ENV"
exit 0