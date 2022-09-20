#!/bin/sh

# FYI i never tried this script, but i hope it works!

sudo apt-get update
sudo apt-get upgrade
sudo apt-get install unzip

wget https://github.com/denoland/deno/releases/download/v1.25.3/deno-x86_64-unknown-linux-gnu.zip -O temp.zip
unzip temp.zip

sudo mv ./deno /bin/deno
rm temp.zip

cd /bin/
git clone https://github.com/fonsp/printi.git
cd printi

sudo cp printi.service /etc/systemd/system/
sudo systemctl start printi
sudo systemctl enable printi

sudo reboot
