#!/bin/bash
set -e

echo "Running Generator..."
dotnet run --project src/Sharpie.Generator

echo "Publishing Sharpie CLI..."
dotnet publish src/Sharpie.Cli -c Release -o distr/bin/

echo "Compiling firmware..."
./distr/bin/sharpie assets/bios/bios.asm -o assets/bios/bios.bin -f

echo "Publishing Raylib Runner..."
dotnet publish src/Sharpie.Runner/RaylibCs -c Release -o distr/bin/

chmod +x distr/bin/sharpie
chmod +x distr/bin/Sharpie

echo "Builds complete. Everything is in /distr/bin"
