import requests
import time
import configparser
import urllib
from pathlib import Path
import sys
import subprocess

printerPath = "/dev/usb/lp0"

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
	Path(printerPath).chmod(0o777)
	with open(printerPath, "w") as p:
		for line in lines:
			p.write(line[:-1] + "\n")
	return


def printImageDataToPaper(data):
	print("🖨🌈: printing {0} bytes...".format(len(data)))
	Path(printerPath).chmod(0o777)
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
	except:
		return False
	return False


def waitForPrintiConnection(firstGoogleFailure = True, firstPrintiFailure = True):
	while True:
		if ping("https://www.google.com/", 10) or ping("https://www.google.com/", 10):
			if ping("https://printi.me/ping", 10):
				return True
			else:
				if firstPrintiFailure:
					printTextToPaper("Connected to the internet, but the printi.me server is not responding.")
					printTextToPaper("Ask Fons (fonsvdplas@gmail.com) for help.")
					feed()
				firstPrintiFailure = False
		else:
			if firstGoogleFailure:
				printTextToPaper("This printi is not connected to the internet :(")
				apName = "printi-******"
				password = "12345678"
				try:
					apName = subprocess.check_output("uci get wireless.ap.ssid",shell=True).decode()
					password = subprocess.check_output("uci get wireless.ap.key",shell=True).decode()
				except Exception as e:
					print("Can't get wifi AP name/pass: "+str(e))

				printTextToPaper("")
				printTextToPaper("=> Step 1:")
				printTextToPaper("On your phone/laptop, connect to the wifi network emitted by this printi:")
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
				printTextToPaper("Tip: This page can also be used to change the name of your printer! Just repeat steps 1 & 2 to come back to this page whenever you wish.")
				printTextToPaper("")
				printTextToPaper("That's it! Happy printing!")
				feed()
			firstGoogleFailure = False
	return False


def printWelcomeMessage(config):
	printTextToPaper("Connected! Go to: ")
	printTextToPaper("  printi.me/" + urllib.parse.unquote(config["printi"]["name"]))
	feed()
	return


session = requests.Session()
config = configparser.RawConfigParser()
configPath = "/etc/printi/config.ini"

tries = 0
while not Path(configPath).is_file():
	print("Could not find config file...")
	time.sleep(1.)
	tries += 1
	if tries == 30:
		printTextToPaper("Something went wrong. Try turning me off and on.")

try:
	config.read(configPath)
except:
	print("Could not find config file! Exiting...")
	printTextToPaper("Could not find config file! Exiting...")
	feed()
	exit(1)

try:
	with open("logo.h58", "rb") as logoFile:
		logoData = logoFile.read()
	printImageDataToPaper(logoData)
except FileNotFoundError as err:
	print("Could not find logo file!")
	printTextToPaper("  ~~ printi ~~")
except:
	print("Can't print!!! Exiting...")
	exit(1)


waitForPrintiConnection()
printWelcomeMessage(config)


while True:
	try:
		config.read(configPath)
		printerName = urllib.parse.unquote(config["printi"]["name"])


		r = session.get("https://printi.me/api/nextinqueue/"+printerName, timeout=10)
		print("response!")
		print(r.status_code)
		if r.status_code == 302:
			data = r.content
			printImageDataToPaper(data)
			feed()
		if r.status_code == 521:
			waitForPrintiConnection()
			config.read(configPath)
			printWelcomeMessage(config)


	except requests.exceptions.ReadTimeout as err:
		print("no response, let's try again...")
	except requests.exceptions.ConnectionError as err:
		print("connection error:", err)
		waitForPrintiConnection()
		config.read(configPath)
		printWelcomeMessage(config)
	except:
		print("something strange happened: ", sys.exc_info()[0])
		raise
