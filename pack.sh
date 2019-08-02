#!/bin/sh

if [ ! -z "$(git status --porcelain)" ]; then
	echo "Your status is not clean, can't continue"
	exit 1;
fi;

if [ $# -eq 0 ]; then
	echo Please, provide version to pack as an argument
	exit 1
else
	version="$1"
fi;

dotnet pack -p:nupkgversion="$version" -p:commitsha=$(git rev-parse HEAD)