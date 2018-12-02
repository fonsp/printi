#!/bin/sh

cd /root/printi
! git reset --hard
! git pull

(cd /root/printi/client/PrintiConfigServer/PrintiConfigServer; python3 PrintiConfigServer.py)&
(cd /root/printi/client/PrintiClient/PrintiClient; python3 PrintiClient.py)&
