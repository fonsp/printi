#!/bin/sh

cd ~/printi
git reset --hard
git pull

(cd ~/printi/PrintiConfigServer/PrintiConfigServer; python3 PrintiConfigServer.py)&
(cd ~/printi/PrintiClient/PrintiClient; python3 PrintiClient.py)&
