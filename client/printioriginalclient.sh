#!/bin/bash

! mkdir tmp
cd tmp

while true
do
	if (wget --content-disposition --trust-server-names api.printi.me/nextinqueue)
	then
		downloadedfile=$(ls)

		extension="${downloadedfile#*.}"
		newname=$(ls .. | wc -l).$extension

		mv "$downloadedfile" ../$newname

		#! python normalize_quantiles.py "$tmpdir$downloadedfile" "$tmpdirNORM$downloadedfile"
		convert ../$newname -rotate "90>" ../toprint.BMP

		height=$(identify -format "%h" ../toprint.BMP)
		if [ $height -le 576 ]
		then
			lp -o orientation-requested=4 ../toprint.BMP
		else
			lp -o orientation-requested=3 ../toprint.BMP
		fi
	fi
done
