#!/bin/sh

sudo apt-get update
sudo apt-get install unzip

wget https://github.com/denoland/deno/releases/download/v1.18.0/deno-x86_64-unknown-linux-gnu.zip -O temp.zip
unzip temp.zip

sudo mv ./deno /bin/deno
rm temp.zip

cd /bin/
git clone https://github.com/fonsp/printi.git
cd printi

cp printi.service /etc/systemd/system/
systemctl start printi
systemctl enable printi

sudo reboot
