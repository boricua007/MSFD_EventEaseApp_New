#!/bin/bash
# Set base href for local development
sed -i 's|{{BASE_HREF}}|/|g' wwwroot/index.html
echo "Base href set for local development"
dotnet run