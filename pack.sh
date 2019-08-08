#!/bin/sh

if [ $# -eq 0 ]; then
	echo Please, provide version to pack as an argument
	exit 1
else
	version="$1"
fi;

if [ ! -z "$(git status --porcelain)" ]; then
	echo "WARNING! Your status is not clean"
fi;

dotnet pack -p:nupkgversion="$version" -p:commitsha=$(git rev-parse HEAD)