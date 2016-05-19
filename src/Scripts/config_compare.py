import xml.sax
import sys

# XML SAX parser
class ConfigHandler(xml.sax.ContentHandler):
    config = None
    current = None

    def __init__(self, config):
        self.config = config

    def startElement(self, tag, attributes):
        self.current = tag
        self.config[self.current] = ""
    
    def characters(self, content):
        scontent = str(content)
        if not scontent.isspace():
            self.config[self.current] += scontent
                


if len(sys.argv) != 3:
    print "Usage: arg 1: config1, arg 2: config2. Both configs must be in the same directory as the script."
    exit()

input1 = sys.argv[1]
input2 = sys.argv[2]

config1 = {}
config2 = {}

parser = xml.sax.make_parser()

parser.setContentHandler(ConfigHandler(config1))
parser.parse(input1)

parser.setContentHandler(ConfigHandler(config2))
parser.parse(input2)


print "Comparing " + input1 + " with " + input2 + "..."

different = False

for key in config1:
    if not config2.has_key(key):
        print "Missing param for " + input2 + ": " + key
        different = True

for key in config2:
    if not config1.has_key(key):
        print "Missing param for " + input1 + ": " + key
        different = True

for key in config1:
    if not config2.has_key(key):
        continue

    value1 = config1[key]
    value2 = config2[key]

    if not value1 == value2:
        print "Value for " + key + " - " + input1 + ": " + value1 + ", " + input2 + ": " + value2
        different = True

if not different:
    print "No differences found."