#!/bin/bash

# Comprehensive script to rename IdevsWork to Idevs
# This script will rename directories, files, and content systematically

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to replace content in files
replace_content_in_files() {
    local pattern="$1"
    local replacement="$2"
    local file_pattern="$3"
    
    print_status "Replacing '$pattern' with '$replacement' in $file_pattern files"
    
    # Use find with -type f to only process files, and handle spaces in filenames
    find . -type f -name "$file_pattern" -print0 | while IFS= read -r -d '' file; do
        if [[ -f "$file" ]]; then
            # Use sed with backup and then remove backup if successful
            if sed -i.bak "s|$pattern|$replacement|g" "$file" 2>/dev/null; then
                rm -f "${file}.bak"
                echo "  Updated: $file"
            else
                # If sed failed, restore backup
                if [[ -f "${file}.bak" ]]; then
                    mv "${file}.bak" "$file"
                fi
                print_warning "Failed to update: $file"
            fi
        fi
    done
}

# Function to rename files
rename_files() {
    local old_pattern="$1"
    local new_pattern="$2"
    
    print_status "Renaming files from $old_pattern to $new_pattern"
    
    # Find and rename files
    find . -type f -name "*$old_pattern*" -print0 | while IFS= read -r -d '' file; do
        local dir=$(dirname "$file")
        local filename=$(basename "$file")
        local new_filename="${filename//$old_pattern/$new_pattern}"
        local new_path="$dir/$new_filename"
        
        if [[ "$file" != "$new_path" ]]; then
            mv "$file" "$new_path"
            echo "  Renamed: $file -> $new_path"
        fi
    done
}

# Function to rename directories
rename_directories() {
    local old_pattern="$1"
    local new_pattern="$2"
    
    print_status "Renaming directories from $old_pattern to $new_pattern"
    
    # Find directories from deepest to shallowest to avoid issues with nested renames
    find . -type d -name "*$old_pattern*" | sort -r | while read -r dir; do
        local parent_dir=$(dirname "$dir")
        local dir_name=$(basename "$dir")
        local new_dir_name="${dir_name//$old_pattern/$new_pattern}"
        local new_dir="$parent_dir/$new_dir_name"
        
        if [[ "$dir" != "$new_dir" && -d "$dir" ]]; then
            mv "$dir" "$new_dir"
            echo "  Renamed: $dir -> $new_dir"
        fi
    done
}

print_status "Starting comprehensive rename from IdevsWork to Idevs..."
print_warning "This will modify many files. Make sure you have committed your changes!"

# Confirm before proceeding
read -p "Are you sure you want to proceed? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    print_error "Operation cancelled."
    exit 1
fi

# Step 1: Replace content in all relevant files
print_status "=== Step 1: Updating file contents ==="

# Replace in C# files
replace_content_in_files "IdevsWork" "Idevs" "*.cs"

# Replace in project files
replace_content_in_files "IdevsWork" "Idevs" "*.csproj"

# Replace in solution files
replace_content_in_files "IdevsWork" "Idevs" "*.sln"

# Replace in documentation files
replace_content_in_files "IdevsWork" "Idevs" "*.md"

# Replace in JSON files
replace_content_in_files "IdevsWork" "Idevs" "*.json"

# Replace in YAML files
replace_content_in_files "IdevsWork" "Idevs" "*.yml"
replace_content_in_files "IdevsWork" "Idevs" "*.yaml"

# Replace in shell scripts
replace_content_in_files "IdevsWork" "Idevs" "*.sh"

# Replace in props files
replace_content_in_files "IdevsWork" "Idevs" "*.props"

# Replace in other text files
replace_content_in_files "IdevsWork" "Idevs" "*.txt"

# Step 2: Rename files
print_status "=== Step 2: Renaming files ==="
rename_files "IdevsWork" "Idevs"

# Step 3: Rename directories
print_status "=== Step 3: Renaming directories ==="
rename_directories "IdevsWork" "Idevs"

print_success "Rename operation completed!"
print_status "Please verify the changes and test the build."

# Suggest next steps
echo ""
echo "Recommended next steps:"
echo "1. Review the changes: git status"
echo "2. Test the build: dotnet build"
echo "3. Run tests: dotnet test"
echo "4. Commit the changes: git add . && git commit -m 'refactor: rename IdevsWork to Idevs'"
