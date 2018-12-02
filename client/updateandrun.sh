#!/bin/sh

cd ~/printi
git reset --hard
git pull

(cd ~/printi/client/PrintiConfigServer/PrintiConfigServer; python3 PrintiConfigServer.py)&
(cd ~/printi/client/PrintiClient/PrintiClient; python3 PrintiClient.py)&
