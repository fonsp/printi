#!/bin/bash

! mkdir tmp
cd tmp

while true
do
	if (wget --content-disposition --trust-server-names api.printi.me/nextinqueue/$1)
	then
		downloadedfile=$(ls)

		extension="${downloadedfile#*.}"
		newname=$(ls .. | wc -l).bmp

		cat $downloadedfile | ../printiminitoBMP > ../$newname

		rm $downloadedfile
		(cd ..; /mnt/c/Program\ Files/IrfanView/i_view64.exe "$newname /hide=7 /pos=(0,0)") &
	fi
done
