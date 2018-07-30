#! /bin/sh

# Example build script for Unity3D project. See the entire example: https://github.com/JonathanPorta/ci-build

# Change this the name of your project. This will be the name of the final executables as well.
# bo-sheep="ci-build"

find $(pwd) 

echo "Attempting to build $project, WebGL target"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -silent-crashes \
  -logFile $(pwd)/unity.log \
  -projectPath $(pwd)/bo-sheep \
  -buildTarget WebGL \
  -username 'eugene@bective.plus.com' \
  -password "${UNITY_PASSWORD}" \
  -quit

echo 'Logs from build'
cat $(pwd)/unity.log grep -v password