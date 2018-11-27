#!/usr/bin/env python3

import logging
import urllib
import cgi
import configparser
from http.server import BaseHTTPRequestHandler, HTTPServer, SimpleHTTPRequestHandler, CGIHTTPRequestHandler

config = configparser.RawConfigParser()
config.read("config.ini")


s = "asfd🌈"

sq = urllib.parse.quote(s)
print(sq)
print(urllib.parse.unquote(sq))

print(urllib.parse.unquote(config["printi"]["name"]))

def updateHtml():
	form = ""
	config.read("config.ini")
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
			print(postvars)
			if True:
				for section in config:
					if section == "DEFAULT":
						continue
					for setting in config[section]:
						config[section][setting] = urllib.parse.quote(postvars["{0}/{1}".format(section,setting)][0].decode())
			
				with open("config.ini", 'w') as file:
					config.write(file,False)
				print("Config updated.")
				updateHtml()
				print("HTML updated.")
			self.send_response(301)
			self.send_header('Location','/')
			self.end_headers()
			return

def run(server_class=HTTPServer, handler_class=S, port=8080):
	logging.basicConfig(level=logging.INFO)
	server_address = ("", port)
	httpd = server_class(server_address, handler_class)
	logging.info("Running config server on port %s\n",port)
	try:
		httpd.serve_forever()
	except KeyboardInterrupt:
		pass
	httpd.server_close()
	logging.info("Stopping httpd...\n")


if __name__ == "__main__":
	from sys import argv
	updateHtml()

	if len(argv) == 2:
		run(port=int(argv[1]))
	else:
		run()