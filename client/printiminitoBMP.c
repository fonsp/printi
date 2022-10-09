#include <stdio.h>
#include <stdlib.h>

void writeInt(unsigned int x){
	/*for(int i = 0; i < 4; i++){
		putchar(x && 0xff);
		x = x >> 8;
	}*/
	putw(x, stdout);
}

int main(){
	unsigned int imageHeight = 0;
	unsigned int imageWidth = 384;

	char* imgData = malloc(24 * imageWidth / 8);

	getchar(); // 1b
	getchar(); // 40
	while(1){
		char a = getchar(); // 1b or 1d
		char b = getchar(); // 40 or 76

		if(a == 0x1b && b == 0x40){
			break;
		}
		getchar(); // 30
		getchar(); // 40

		// start of new line
		a = getchar();
		b = getchar();
		unsigned int bytesPerLine = a + 256*b;
		//imageWidth = bytesPerLine * 8;

		a = getchar();
		b = getchar();
		unsigned int sliceHeight = a + 256*b;

		char* imgDataNew = realloc(imgData, (imageHeight + sliceHeight) * imageWidth / 8);
		if(imgDataNew){
			imgData = imgDataNew;
		} else {
			// realloc failed
			printf("realloc failed");
			free(imgData);
			return 1;
		}

		for(int y = 0; y < sliceHeight; y++){
			for(int x = 0; x < imageWidth / 8; x++){
				imgData[(imageHeight + y) * imageWidth / 8 + x] = getchar();
			}
		}

		getchar(); // 1b
		getchar(); // 4a
		getchar(); // 15

		imageHeight += sliceHeight;
	}

	// see:
	// http://www.ece.ualberta.ca/~elliott/ee552/studentAppNotes/2003_w/misc/bmp_file_format/bmp_file_format.htm
	// for the BMP format

	// FILE HEADER
	printf("BM");
	unsigned int filesize = 14 + 40 + 8 + imageHeight * imageWidth / 8;
	writeInt(filesize);
	writeInt(0); // unused
	writeInt(14 + 40 + 8); // offset

	// IMAGE HEADER
	writeInt(40);	// header size
	writeInt(imageWidth);	// image width
	writeInt(imageHeight);	// image height
	putchar(1); // must be 1
	putchar(0);
	putchar(1); // bits per pixel
	putchar(0);
	writeInt(0);	// compression
	writeInt(imageWidth * imageHeight / 8);	// image size
	writeInt(384 * 1000 / 58);	// pixels per meter
	writeInt(384 * 1000 / 58);	// pixels per meter
	writeInt(1);	// colors used
	writeInt(0);	// colors important

	// color table
	putchar(255); // white
	putchar(255);
	putchar(255);
	putchar(0);

	putchar(0); // black
	putchar(0);
	putchar(0);
	putchar(0);

	for(int y = imageHeight - 1; y >= 0; y--){
		for(int x = 0; x < imageWidth / 8; x++){
			putchar(imgData[y * imageWidth / 8 + x]);
		}
	}

	free(imgData);
	return 0;
}
