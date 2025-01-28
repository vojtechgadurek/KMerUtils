# bin/bash
# Define the directory containing the files
PATH="Results"

# Iterate over all files in the directory
for filename in "$PATH"/*; do
    # Check if it's a regular file
    if [ -f "$filename" ]; then
        # Execute the Python script with the filename as an argument
        /usr/bin/python3 "./create-graph.py" $filename $filename
    fi
done