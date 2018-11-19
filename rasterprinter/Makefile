# Generated automatically from Makefile.in by configure.
# C compiler options
CC	= gcc
CFLAGS	= -g -O2
LIBS	= -lcups -lnsl

# Source files
SRCS	= common.c rasterprinter.c

# Object files
OBJS	= $(SRCS:.c=.o)

# Make everything
all:	rastertoprinter

# Make the rastertos58 application
rastertoprinter: rastertoprinter.o
	$(CC) $(CFLAGS) -o rastertoprinter rasterprinter.c -lcupsimage $(LIBS) -lm $(SRCS)


# Dependencies
$(OBJS): config.h Makefile
clean:
	rm -rf *.o rastertoprinter
install:
	chmod +x setup.sh
	exec ./setup.sh
