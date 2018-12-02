#!/usr/bin/env python3

import logging
import urllib
import cgi
import configparser
from http.server import BaseHTTPRequestHandler, HTTPServer, SimpleHTTPRequestHandler, CGIHTTPRequestHandler
from sys import argv
import subprocess
import shlex
import json
from pathlib import Path

config = configparser.RawConfigParser()
if not Path("/etc/printi").is_dir():
	Path("/etc/printi").mkdir()

configPath = Path("/etc/printi/config.ini")
if not configPath.is_file():
	with configPath.open("w") as cf, open("config.ini","r") as original:
		configPath.write_text(original.read())

config.read(str(configPath))


def getWifiList():
	ubusOutput = subprocess.check_output("ubus call onion wifi-scan \"{\'device\':\'ra0\'}\"", shell=True).decode()
	ubusJson = json.loads(ubusOutput)
	networks = ubusJson["results"]
	result = []
	query = [("WPA2","psk2"),("WPA","psk"),("WEP","wep"),("","none")]
	for n in networks:
		matches = (enctype for id, enctype in query if id in n["encryption"])
		result.append((n["ssid"], next(matches)))

	return result


def connectToWifi(name, password):
	wifiList = getWifiList()

	foundNetworks = (n for n in wifiList if n[0] == name)
	encType = "none" if len(password)==0 else "psk2"
	try:
		encType = next(foundNetworks)[1]
		logging.log("Network was detected. Connecting...")
	except:
		logging.log("Network was not detected, trying to connect anyway... (assuming WPA2)")
	
	output = subprocess.check_output("wifisetup clear", shell=True)
	output = subprocess.check_output("wifisetup add -ssid {0} -encr {1} -password {2}".format(shlex.quote(name), encType, shlex.quote(password)))
	# TODO: evaluate output
	return False


def updateHtml():
	form = ""
	config.read(str(configPath))
	for section in config:
		if section == "DEFAULT":
			continue
		form += "<h1>{0}</h1>\n".format(section)
		for setting in config[section]:
			form += "{0}: <br />\n".format(setting)
			form += "<input type=\"text\" title=\"{0}/{1}\" name=\"{0}/{1}\" value=\"{2}\" /><br /><br />\n".format(section, setting, urllib.parse.unquote(config[section][setting]))
		form += "<br /><br /><br /><br /><br />\n"
	with open("Config.pyhtml", 'r', encoding='utf-8') as file:
		html = file.read()
	html = html.replace("PRINTERNAME", urllib.parse.unquote(config["printi"]["name"]))
	html = html.replace("CONFIGFORM", form)
	with open("index.html", 'w', encoding='utf-8') as file:
		file.write(html)
	logging.info("index.html updated.\n")
	return


def updateConfig(postvars):
	old_wifi_name = config["Internet Connection"]["wifi name"]
	old_wifi_password = config["Internet Connection"]["wifi password"]

	for section in config:
		if section == "DEFAULT":
			continue
		for setting in config[section]:
			config[section][setting] = urllib.parse.quote(postvars["{0}/{1}".format(section,setting)][0].decode())
	
	try:
		with open(str(configPath), 'w') as file:
			config.write(file,False)
		logging.info("Config updated.\n")
	except:
		logging.error("Failed to write to config file.")
	updateHtml()
	
	wifi_name = config["Internet Connection"]["wifi name"]
	wifi_password = config["Internet Connection"]["wifi password"]

	if old_wifi_name != wifi_name or old_wifi_password != wifi_password:
		connectToWifi(wifi_name, wifi_password)
	return


class S(SimpleHTTPRequestHandler):
	def do_POST(self):
		if self.path=="/setconfig":
			content_type = ""
			for spelling in ['content-type', 'Content-Type']:
				if spelling in self.headers:
					content_type = self.headers[spelling]
			ctype, pdict = cgi.parse_header(content_type)
			pdict["boundary"] = pdict["boundary"].encode()
			
			postvars = cgi.parse_multipart(self.rfile, pdict)
			
			updateConfig(postvars)
			self.send_response(301)
			self.send_header('Location','/')
			self.end_headers()
			return


def run(server_class=HTTPServer, handler_class=S, address="192.168.3.1", port=80):
	logging.basicConfig(level=logging.INFO)
	server_address = (address, port)
	httpd = server_class(server_address, handler_class)
	logging.info("Running config server on port %s\n",port)
	try:
		httpd.serve_forever()
	except KeyboardInterrupt:
		pass
	httpd.server_close()
	logging.info("Stopping httpd...\n")
	return


if __name__ == "__main__":
	updateHtml()
	
	if len(argv) == 2:
		run(address="127.0.0.1", port=int(argv[1]))
	else:
		run()