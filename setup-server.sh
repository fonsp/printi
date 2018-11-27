#!/bin/sh

sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb https://download.mono-project.com/repo/ubuntu stable-bionic main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
sudo apt update
sudo apt install mono-complete nuget chromium-browser

cd /bin/
git clone https://github.com/fons-/printi.git
cd printi
nuget restore
msbuild

cp printi.service /etc/systemd/system/
systemctl start printi
systemctl enable printi

sudo reboot
