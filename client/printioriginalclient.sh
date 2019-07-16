#!/bin/bash

while true
do
	tmpdir=`mktemp -d -p .`

	cd $tmpdir
	
	if (wget --content-disposition --trust-server-names api.printi.me/nextinqueue)
	then
		downloadedfile=$(ls)

		cd ..

		mv "$tmpdir/$downloadedfile" "$tmpdir$downloadedfile"
		! convert -normalize "$tmpdir$downloadedfile" "$tmpdirNORM$downloadedfile"
		lp -o fit-to-page "$tmpdirNORM$downloadedfile"
	else
		cd ..
	fi
	rmdir "$tmpdir"
done
