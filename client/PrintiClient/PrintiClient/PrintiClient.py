import requests
import time
import configparser
import urllib
from pathlib import Path
import sys
import subprocess
import datetime

printerPath = "nul"  # win
printerPaths = ["/dev/null", "/dev/usb/lp1", "/dev/usb/lp0"]
if len(sys.argv) > 1:
	printerPaths.append(sys.argv[1])

for pp in printerPaths:
	try:
		if Path(pp).exists():
			printerPath = pp
	except Exception:
		pass

print("Using print device at: "+printerPath)

shouldPrintDetailedInstructions = True
shouldPrintWelcomeMessage = True
lastOnlineAt = datetime.datetime(1970,1,1)


def printTextToPaper(text):
	print("🖨: "+text)
	words = text.split(" ")
	lines = [""]
	for w in words:
		if len(lines[-1]+w) <= 32:
			lines[-1] = lines[-1]+w+" "
		else:
			while(len(w) > 0):
				lines.append(w[:32]+" ")
				w = w[32:]
	if "nul" not in printerPath:
		Path(printerPath).chmod(0o666)
	with open(printerPath, "w") as p:
		for line in lines:
			p.write(line[:-1] + "\n")
	return


def printImageDataToPaper(data):
	print("🖨🌈: printing {0} bytes...".format(len(data)))
	if "nul" not in printerPath:
		Path(printerPath).chmod(0o666)
	with open(printerPath, "wb") as pb:
		pb.write(data)
	return


def feed(n_lines=3):
	[printTextToPaper("") for _ in range(n_lines)]
	return


def ping(address, timeout=10):
	try:
		response = requests.get(address, timeout=timeout)
		return response.status_code == 200
	except Exception:
		return False
	return False


def waitForPrintiConnection(firstGoogleFailure=True, firstPrintiFailure=True):
	global shouldPrintDetailedInstructions
	global shouldPrintWelcomeMessage
	global lastOnlineAt
	while True:
		if ping("https://www.google.com/", 10) or ping("https://www.google.com/", 10):
			shouldPrintDetailedInstructions = False
			if ping("https://printi.me/ping", 10):
				return firstGoogleFailure
			else:
				if firstPrintiFailure:
					printTextToPaper("Connected to the internet, but the printi.me server is not responding.")
					printTextToPaper("Ask Fons (fonsvdplas@gmail.com) for help.")
					feed()
					# The past note looks scary, so the user must be notified when the connection returns
					shouldPrintWelcomeMessage = True
				firstPrintiFailure = False
		else:
			if firstGoogleFailure:
				if shouldPrintDetailedInstructions:
					apName = "printi-******"
					password = "12345678"
					try:
						apName = subprocess.check_output("uci get wireless.ap.ssid", shell=True).decode().replace("\n", "")
						password = subprocess.check_output("uci get wireless.ap.key", shell=True).decode().replace("\n", "")
					except Exception as err:
						print("Can't get wifi AP name/pass: "+str(err))
					printTextToPaper("This printi is not connected to the internet :(")
					printTextToPaper("")
					printTextToPaper("=> Step 1:")
					printTextToPaper("On your phone/laptop, connect to the wifi network emitted by this printi:")
					printTextToPaper("")
					printTextToPaper("      Name: "+apName)
					printTextToPaper("  Password: "+password)
					printTextToPaper("")
					printTextToPaper("=> Step 2:")
					printTextToPaper("After connecting, open a web browser and navigate to:")
					printTextToPaper("http://192.168.3.1/")
					printTextToPaper("")
					printTextToPaper("=> Step 3:")
					printTextToPaper("Type in the name and password of the WiFi network that you want your printi to connect to, and press Save.")
					printTextToPaper("")
					printTextToPaper("Tip: The settings page can also be used to change the name of your printer! Store these instructions somewhere safe (or visit help.printi.me).")
					printTextToPaper("")
					printTextToPaper("That's it! Happy printing!")
					feed()
					shouldPrintWelcomeMessage = True
					firstGoogleFailure = False
				else:
					if (datetime.datetime.now() - lastOnlineAt) > datetime.timedelta(hours=1):
						printTextToPaper("Lost internet connection :(")
						printTextToPaper("For setup instructions, go to:")
						printTextToPaper("  help.printi.me")
						feed()
						shouldPrintWelcomeMessage = True
						firstGoogleFailure = False
	return False


def printWelcomeMessage(config):
	global shouldPrintWelcomeMessage
	if shouldPrintWelcomeMessage:
		printTextToPaper("Connected! Go to: ")
		printTextToPaper("  printi.me/" + urllib.parse.unquote(config["printi"]["name"]))
		feed()
		shouldPrintWelcomeMessage = False
	blackout = datetime.datetime.now() - lastOnlineAt
	print('{|' + str(blackout.days) + "|" + str(blackout.seconds) + '', end="|}")
	return


session = requests.Session()
config = configparser.RawConfigParser()
configPath = "/etc/printi/config.ini"

tries = 0
while not Path(configPath).is_file():
	print("Could not find config file at: "+configPath)
	time.sleep(1.)
	tries += 1
	if tries == 30:
		printTextToPaper("Something went wrong. Try turning me off and on.")

try:
	config.read(configPath)
except Exception:
	print("Could not find config file! Exiting...")
	printTextToPaper("Could not find config file! Exiting...")
	feed()
	exit(1)

try:
	with open("logo.h58", "rb") as logoFile:
		logoData = logoFile.read()
	printImageDataToPaper(logoData)
except FileNotFoundError as err:
	print("Could not find logo file: "+str(err))
	printTextToPaper("  ~~ printi ~~")
except Exception as err:
	print("Can't print! : "+str(err))
	print("Exiting...")
	exit(1)


waitForPrintiConnection()
config.read(configPath)
lastOnlineAt = datetime.datetime.now()
printWelcomeMessage(config)


while True:
	try:
		config.read(configPath)
		printerName = urllib.parse.unquote(config["printi"]["name"])

		lastOnlineAt = datetime.datetime.now()
		r = session.get("https://api.printi.me/nextinqueue/"+printerName, timeout=10)
		print("response!")
		print(r.status_code)
		if r.status_code == 200:
			data = r.content
			printImageDataToPaper(data)
			feed()
		if r.status_code == 521:
			waitForPrintiConnection()
			config.read(configPath)
			printWelcomeMessage(config)

	except requests.exceptions.Timeout:
		print("-", end='')
	except requests.exceptions.ConnectionError as err:
		print("connection error:", str(err))
		print("sleeping 5 sec...")
		time.sleep(5)
		waitForPrintiConnection()
		config.read(configPath)
		printWelcomeMessage(config)
	except Exception as err:
		print("something strange happened: ", str(err))
		print("sleeping 5 sec...")
		time.sleep(5)
		waitForPrintiConnection()
		config.read(configPath)
		printWelcomeMessage(config)
