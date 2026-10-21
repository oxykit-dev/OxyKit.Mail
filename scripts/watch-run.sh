#!/usr/bin/env bash
set -euo pipefail
project="src/OxyKit.Mail/OxyKit.Mail.csproj"
dotnet watch --project "$project" run
