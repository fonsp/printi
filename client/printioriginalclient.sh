#!/bin/bash


here=$(dirname $(readlink -f $0))

! rm -rf printitmp
! mkdir printitmp
cd printitmp

while true
do
	if (wget --header="Accept: text/html,image/png" --content-disposition --trust-server-names --quiet api.printi.me/nextinqueue)
	then
		downloadedfile=$(ls)

		extension="${downloadedfile#*.}"
		newname=$(ls .. | wc -l).$extension

		mv "./$downloadedfile" ../$newname

		# ! python normalize_quantiles.py "$tmpdir$downloadedfile" "$tmpdirNORM$downloadedfile"
		
		/home/fons/.env/bin/python "$here/printioriginalprintcommand.py" ../$newname
		continue
		
		convert ../$newname -rotate "90>" ../toprint.PNG

		height=$(identify -format "%h" ../toprint.PNG)
		if [ $height -le 576 ]
		then
			lp -o orientation-requested=4 ../toprint.PNG
		else
			lp -o orientation-requested=3 ../toprint.PNG
		fi
	fi
done
