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

		mv $downloadedfile ../$newname

		#! python normalize_quantiles.py "$tmpdir$downloadedfile" "$tmpdirNORM$downloadedfile"
		lp -o fit-to-page ../$newname
	fi
done
