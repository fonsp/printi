import requests
import time
import configparser
import urllib
import sys

def printTextToPaper(text):
	print("🖨: "+text)
	return


def printImageDataToPaper(data):
	print("🖨🌈: printing {0} bytes...".format(len(data)))
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
		if ping("https://www.google.com/", 10):
			if ping("https://printi.me/ping", 10):
				return True
			else:
				if firstPrintiFailure:
					printTextToPaper("Connected to the internet, but the printi.me server is not responding.")
					printTextToPaper("Ask Fons (fonsvdplas@gmail.com) for help.")
				firstPrintiFailure = False
		else:
			if firstGoogleFailure:
				printTextToPaper("This printi is not connected to the internet :(")
				printTextToPaper("")
				printTextToPaper("=> Step 1:")
				printTextToPaper("On your phone/laptop, connect to the wifi network emitted by this printi.")
				printTextToPaper("      Name: "+"printi-?????")
				printTextToPaper("  Password: "+"12345678")
				printTextToPaper("")
				printTextToPaper("=> Step 2:")
				printTextToPaper("After connecting, open a web browser and navigate to:")
				printTextToPaper("http://192.168.3.1/")
				printTextToPaper("")
				printTextToPaper("=> Step 3:")
				printTextToPaper("Type in the name and password of the WiFi network that you want your printi to connect to, and press Save")
				printTextToPaper("")
				printTextToPaper("Tip: This page can also be used to change the name of your printer! Just repeat steps 1 & 2 to come back to this page whenever you wish.")
				printTextToPaper("")
				printTextToPaper("That's it! Happy printing!")
			firstGoogleFailure = False
	return False


def printWelcomeMessage(config):
	printTextToPaper("Connected! Go to: ")
	printTextToPaper("  printi.me/" + urllib.parse.unquote(config["printi"]["name"]))
	return


session = requests.Session()
config = configparser.RawConfigParser()
configPath = "../../PrintiConfigServer/PrintiConfigServer/config.ini"

try:
	config.read(configPath)
except:
	print("Could not find config file! Exiting...")
	printTextToPaper("Could not find config file! Exiting...")
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

	except requests.exceptions.ReadTimeout as err:
		print("no response, let's try again...")
	except requests.exceptions.ConnectionError as err:
		print("connection error:", err)
		waitForPrintiConnection()
		config.read("../../PrintiConfigServer/PrintiConfigServer/config.ini")
		printWelcomeMessage(config)
	except:
		print("something strange happened: ", sys.exc_info()[0])
		raise
