#! /bin/sh

# Example build script for Unity3D project. See the entire example: https://github.com/JonathanPorta/ci-build

# Change this the name of your project. This will be the name of the final executables as well.
# bo-sheep="ci-build"

echo "ifconfig ..."
ifconfig

echo "Looking for CACerts.pem (1) ..."
find $(pwd) -name CACerts.pem
echo "Looking for CACerts.pem (2) ..."
find ~/Library -name CACerts.pem

echo "Activating license ..."
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -username 'eugene@bective.plus.com' \
  -password "${UNITY_PASSWORD}" \
  -logfile $(pwd)/unity.log &

UNITY_PID=$!

echo"Sleeping while Unity hopefully generates some certificates ..."
sleep 20

echo "Terminating Unity ..."
kill -9 ${UNITY_PID}

echo "Looking for CACerts.pem (3) ..."
find $(pwd) -name CACerts.pem
echo "Looking for CACerts.pem (4) ..."
find ~/Library -name CACerts.pem

echo "Attempting to build $project, WebGL target ..."
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -silent-crashes \
  -logFile $(pwd)/unity.log \
  -projectPath $(pwd)/bo-sheep \
  -buildTarget WebGL \
  -username 'eugene@bective.plus.com' \
  -password "${UNITY_PASSWORD}" \
  -quit

echo "Logs from build:"
cat $(pwd)/unity.log grep -v password

echo "Returning license ..."
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -quit \
  -batchmode \
  -returnlicense \
  -logFile $(pwd)/unity.log

echo "Finished!"
