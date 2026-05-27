#!/bin/bash

rm -f src/Sharpie.Tests/bin/Debug/net10.0/fixture_cache.json
dotnet test 2>&1
