MDTOOL ?= /Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool

.PHONY: all clean

all: Akavache.dll

Akavache.dll:
	/usr/bin/env mono ./.nuget/NuGet.exe restore ./Akavache_XSAll.sln
	$(MDTOOL) build -c:Release Akavache_XSAll.sln

clean:
	$(MDTOOL) build -t:Clean -c:Release Akavache_XSAll.sln
