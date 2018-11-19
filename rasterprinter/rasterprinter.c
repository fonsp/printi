// Based on:
// https://github.com/klirichek/zj-58
/*
 * Include necessary headers...
 */
#include <cups/cups.h>
#include <cups/raster.h>
#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <unistd.h>
#include <fcntl.h>
#include <signal.h>
#include <math.h>


#define ALLIGN_TO_32BIT(X)  ((X+31)&(~31))
#define ALLIGN_TO_16BITS(X)  ((X+15)&(~15))
#define ALLIGN_TO_8BITS(X)  ((X+7)&(~7))
#define HEIGHT_PIXEL	24
#define CMD_LEN	8

#define EJECT_CASH_DRAWER1_BEFORE_PRINT	1
#define EJECT_CASH_DRAWER2_BEFORE_PRINT	2
#define EJECT_CASH_DRAWER12_BEFORE_PRINT	3
#define EJECT_CASH_DRAWER1_AFTER_PRINT	4
#define EJECT_CASH_DRAWER2_AFTER_PRINT	5
#define EJECT_CASH_DRAWER12_AFTER_PRINT	6

#define BEEP_AFTER_PAGE	1
#define BEEP_BEFORE_PAGE	2
#define BEEP_AFTER_DOC	3
#define BEEP_BEFORE_DOC	4

#define DO_NOT_PRINT_LOGO	0

#define PRINT_LOGO1	1
#define PRINT_LOGO2	2
#define PRINT_LOGO3	3
#define PRINT_LOGO4	4
#define PRINT_LOGO5	5
#define PRINT_LOGO6	6
#define PRINT_LOGO7	7
#define PRINT_LOGO8	8
/*
 * Globals...
 */

int		Page;			/* Current page number */

int     cashDrawerSetting;
int     blankSpaceSetting;
int     feedDistSetting;
int     beeperSetting;
int     logoSetting;

/*
 * Prototypes...
 */
void	Setup(void);
void	StartPage(void);
void	EndPage(void);
void	Shutdown(void);
void	CancelJob(int sig);

static int getPaperWidth();

/*
 * 'Setup()' - Prepare the printer for printing.
 */
void
Setup(void)
{
	/*
	 * Send a reset sequence.
	 */
	putchar(0x1b);
	putchar(0x40);
}

/*
 * 'Shutdown()' - Shutdown the printer.
 */
void
Shutdown(void)
{
	/*
	 * Send a reset sequence.
	 */
	putchar(0x1b);
	putchar(0x40);
}

/*
 * 'StartPage()' - Start a page of graphics.
 */

void
StartPage()
{
	int	plane;				/* Looping var */
#if defined(HAVE_SIGACTION) && !defined(HAVE_SIGSET)
	struct sigaction action;		/* Actions for POSIX signals */
#endif /* HAVE_SIGACTION && !HAVE_SIGSET */


	/*
	 * Register a signal handler to eject the current page if the
	 * job is cancelled.
	 */

#ifdef HAVE_SIGSET /* Use System V signals over POSIX to avoid bugs */
	sigset(SIGTERM, CancelJob);
#elif defined(HAVE_SIGACTION)
	memset(&action, 0, sizeof(action));

	sigemptyset(&action.sa_mask);
	action.sa_handler = CancelJob;
	sigaction(SIGTERM, &action, NULL);
#else
	signal(SIGTERM, CancelJob);
#endif /* HAVE_SIGSET */

}


/*
 * 'EndPage()' - Finish a page of graphics.
 */

void
EndPage(void)
{
#if defined(HAVE_SIGACTION) && !defined(HAVE_SIGSET)
  struct sigaction action;	/* Actions for POSIX signals */
#endif /* HAVE_SIGACTION && !HAVE_SIGSET */


 /*
  * Eject the current page...
  */

  fflush(stdout);

 /*
  * Unregister the signal handler...
  */

#ifdef HAVE_SIGSET /* Use System V signals over POSIX to avoid bugs */
  sigset(SIGTERM, SIG_IGN);
#elif defined(HAVE_SIGACTION)
  memset(&action, 0, sizeof(action));

  sigemptyset(&action.sa_mask);
  action.sa_handler = SIG_IGN;
  sigaction(SIGTERM, &action, NULL);
#else
  signal(SIGTERM, SIG_IGN);
#endif /* HAVE_SIGSET */

  //handle feedDist option
  int feedDistArray[15] = {3,6,9,12,15,18,21,24,27,30,33,36,39,42,45};
  int dist = feedDistArray[feedDistSetting];

  for(; dist > 0; dist -= 3)
  {
		unsigned char buf[2048];
		int lineWidthBytes;

		buf[0] = 0x1d;
		buf[1] = 0x76;
		buf[2] = 0x30;
		buf[3] = 00;
		buf[4] = (unsigned char)(lineWidthBytes%256);
		buf[5] = (unsigned char)(lineWidthBytes/256);
		buf[6] = (unsigned char)(HEIGHT_PIXEL%256);
		buf[7] = (unsigned char)(HEIGHT_PIXEL/256);

		lineWidthBytes = ALLIGN_TO_8BITS(getPaperWidth())/8;

		memset(buf + CMD_LEN, 0, HEIGHT_PIXEL * lineWidthBytes);
		fwrite(buf, HEIGHT_PIXEL * lineWidthBytes + CMD_LEN , 1, stdout);
		buf[0] =0x1b;
		buf[1] =0x4a;
		buf[2] =0x15;
		fwrite(buf, 3, 1, stdout);
  }
}




/*
 * 'CancelJob()' - Cancel the current job...
 */

void
CancelJob(int sig)			/* I - Signal */
{
  int	i;				/* Looping var */
  (void)sig;

 /*
  * Send out lots of NUL bytes to clear out any pending raster data...
  */

  for (i = 0; i < 600; i ++)
    putchar(0);

 /*
  * End the current page and exit...
  */

  EndPage();
  Shutdown();

  exit(0);
}




#define BYTES_PER_LINE 200
/*
 * 'main()' - Main entry and processing of driver.
 */

int 			/* O - Exit status */
main(int  argc,		/* I - Number of command-line arguments */
     char *argv[])	/* I - Command-line arguments */
{
	int			fd;	/* File descriptor */
	cups_raster_t		*ras;	/* Raster stream for printing */
	cups_page_header2_t	header;	/* Page header from file */
	int			y;	/* Current line */
	int			plane;	/* Current color plane */
	unsigned char dest [2048];
	int lineWidth;
	int lineWidthBytes;
	int blankheight;

	cups_option_t *options;
	int num_options;

	/*
	 * Make sure status messages are not buffered...
	*/
	setbuf(stderr, NULL);

	if (argc < 6 || argc > 7)
	{
		/*
		 * We don't have the correct number of arguments; write an error message
		 * and return.
		 */
		fputs("ERROR: rastertopcl job-id user title copies options [file]\n", stderr);
		return (1);
	}

	/*
	 * Open the page stream...
	 */

	if (argc == 7)
	{
		if ((fd = open(argv[6], O_RDONLY)) == -1)
		{
			perror("ERROR: Unable to open raster file - ");
			sleep(1);
			return (1);
		}
	}
	else
		fd = 0;

	ras = cupsRasterOpen(fd, CUPS_RASTER_READ);
	/*
	 * Initialize the print device...
	 */

  // I assume that no options are passed using the command line:

  Setup();
	/*
	 * Process pages as needed...
	 */

	Page = 0;

	while (cupsRasterReadHeader2(ras, &header))
	{
		/*
		 * Write a status message with the page number and number of copies.
		 */

		Page ++;

		fprintf(stderr, "PAGE: %d %d\n", Page, header.NumCopies);

		/*
		 * Start the page...
		 */

		blankheight = 0;
		StartPage();

		/*
		 * Loop for each line on the page...
		 */


		lineWidth = header.cupsWidth;

		if(lineWidth > getPaperWidth()){
			lineWidth = getPaperWidth();
		}
		lineWidthBytes = ALLIGN_TO_8BITS(lineWidth)/8;

    // printing is done in slices of 24 pixels high
    // y is the top coordinate of this slice
		for (y = 0; y < header.cupsHeight; )
		{
			/*
			 * Let the user know how far we have progressed...
			 */
			if ((y & 127) == 0)
      {
				fprintf(stderr, "INFO: Printing page %d, %d%% complete...\n", Page, 100 * y / header.cupsHeight);
      }



      dest[0] = 0x1d;
			dest[1] = 0x76;
			dest[2] = 0x30;
			dest[3] = 00;
			dest[4] = (unsigned char)(lineWidthBytes%256);
			dest[5] = (unsigned char)(lineWidthBytes/256);

			int h; //the height of the current slice to be printed
			if(header.cupsHeight - y > HEIGHT_PIXEL){
				//image has been clipped, so we always print the fixed height.
				h = HEIGHT_PIXEL;
			}else{
				h = header.cupsHeight - y;
			}
			y += h;

			dest[6] = (unsigned char)(h%256);
			dest[7] = (unsigned char)(h/256);

			/*
			 * Read h line of graphics...
			 */
       fprintf(stderr, "%d, %d \n", header.cupsBytesPerLine, lineWidth);
       fprintf(stderr, "%d, %d \n", header.cupsWidth, header.cupsHeight);

			int i;
			for(i = 0; i < h ; i++){
				if (cupsRasterReadPixels(ras, dest + CMD_LEN + i * lineWidthBytes, header.cupsBytesPerLine) < 1)
					break;
			}
			if(i < h){
				break;
			}
			unsigned char buf[2048];
      fwrite(dest, h * lineWidthBytes + CMD_LEN , 1, stdout);
			buf[0] =0x1b;
			buf[1] =0x4a;
			buf[2] =0x15;
			fwrite(buf, 3, 1, stdout);
		}
		EndPage();
	}

	/*
	 * Shutdown the printer...
	 */

	Shutdown();


	/*
	 * Close the raster stream...
	 */

	cupsRasterClose(ras);
	if (fd != 0)
		close(fd);

	/*
	 * If no pages were printed, send an error message...
	 */

	if (Page == 0)
		fputs("ERROR: No pages found!\n", stderr);
	else
		fputs("INFO: Ready to print.\n", stderr);

	return (Page == 0);
}

static int getPaperWidth()
{
    return 384;
}
