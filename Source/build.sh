#! /bin/bash
FrameworkPathOverride=$(dirname $(which mono))/../lib/mono/4.7.2-api/ dotnet build StorageRefrigeratorThresholds.csproj /property:Configuration=Release
if test $? -eq 0; then
    # no idea why these get created, but they break game loading
    shopt -s extglob
    rm -f $(ls -1 ../*.dll | grep -v StorageRefrigeratorThresholds)
fi
version=$(cat StorageRefrigeratorThresholds.csproj | grep AssemblyVersion | sed 's#.*<AssemblyVersion>\(.*\)</AssemblyVersion>.*#\1#')
sed -i "s/VERSION_HERE/$version/" ../mod_info.yaml
