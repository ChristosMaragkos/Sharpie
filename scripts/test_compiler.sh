#!/bin/bash

# set -e

# --- Configuration ---
PROJ_DIR=$(pwd)
COMPILER_DLL="$PROJ_DIR/src/Sharpie.Cli/bin/Debug/net10.0/sharpie.dll"
COMPILER_CSPROJ="$PROJ_DIR/src/Sharpie.Cli/Sharpie.Cli.csproj"
FIXTURES_DIR="$PROJ_DIR/src/Sharpie.CCompiler/fixtures"

# Path where libclang and its dependency libLLVM live inside the devbox
LLVM_LIB_PATH="/usr/lib64/"

echo "=== Building Sharpie C Compiler..."
dotnet build $COMPILER_CSPROJ

echo "Project Directory: $PROJ_DIR"
echo "Searching for fixtures in: $FIXTURES_DIR"
echo

# Set up the dynamic linker paths so libclang can find libLLVM
export LD_LIBRARY_PATH="$LLVM_LIB_PATH:$LD_LIBRARY_PATH"
export SHARPIE_LIBCLANG_PATH="$LLVM_LIB_PATH/libclang.so.1"

ERRORS_TOTAL=0
FAILED_FILES=()

# Check if fixtures exist
if [ ! -d "$FIXTURES_DIR" ]; then
	echo "Error: Fixtures directory not found."
	exit 1
fi

# Recursively find all .c files
while read -r c_file; do
	filename=$(basename "$c_file")
	asm_file="${c_file%.c}.asm"

	echo "--------------------------------------------------"
	echo "[Processing] $c_file"
	echo "Output -> $asm_file"

	# Ensure output directory exists (in case of nested folders)
	mkdir -p "$(dirname "$asm_file")"

	if dotnet "$COMPILER_DLL" "$c_file" -O -S -o "$asm_file"; then
		echo "[✓] Compilation Successful"
		echo "--- Generated Assembly ---"
		cat "$asm_file"
		echo "--------------------------"
	else
		echo "[✗] Compilation FAILED for $c_file"
		((ERRORS_TOTAL++))
		FAILED_FILES+=("$c_file")
	fi
	echo
done < <(find "$FIXTURES_DIR" -type f -name "*.c")

echo "=== Batch Test Complete ==="
echo "Total fails: $ERRORS_TOTAL"

if [ ${#FAILED_FILES[@]} -ne 0 ]; then
	echo "The following files failed:"
	for failed in "${FAILED_FILES[@]}"; do
		echo "$failed - failed"
	done
fi
