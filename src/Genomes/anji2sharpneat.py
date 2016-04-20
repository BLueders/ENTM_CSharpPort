from lxml import etree
import xml.sax
import sys

# XML SAX parser
class GenomeHandler(xml.sax.ContentHandler):

	# Call when an element starts
	def startElement(self, tag, attributes):
		if tag == "neuron":
			neurons.append(Neuron(attributes["id"], attributes["type"], attributes["activation"]))
		elif tag == "connection":
			connections.append(Connection(attributes["id"], attributes["src-id"], attributes["dest-id"], attributes["weight"]))

class Neuron(object):
	id = 0
	ntype = ""
	act = ""

	def __init__(self, id, ntype, act):
		self.id = int(id) + 1
		self.ntype = ntype
		self.act = act

	def to_xml(self):
		return etree.Element("Node", type=self.ntype, id=str(self.id))

class Connection(object):
	id = 0
	src = 0
	dest = 0
	weight = ""

	def __init__(self, id, src, dest, weight):
		self.id = int(id) + 1
		self.src = int(src) + 1
		self.dest = int(dest) + 1
		self.weight = weight

	def to_xml(self):
		return etree.Element("Con", id=str(self.id), src=str(self.src), tgt=str(self.dest), wght=self.weight)

if len(sys.argv) != 3:
    print "Usage: arg 1: input filename, arg 2: output file name"
    exit()

input = sys.argv[1]
output = sys.argv[2]

neurons = []
connections = []

Handler = GenomeHandler()

parser = xml.sax.make_parser()
parser.setContentHandler( Handler )

parser.parse(input)

bias = None
for n in neurons :
	if n.ntype == "out" :
		break
	bias = n 

neurons.remove(bias)
biasId = bias.id
bias.id = 0
bias.ntype = "bias"
neurons.insert(0, bias)

for c in connections :
	if c.src == biasId :
		c.src = 0
	if c.dest == biasId :
		c.dest = 0

root = etree.Element("Network")

xmlNodes = etree.Element("Nodes")

for n in neurons:
	xmlNode = n.to_xml()
	xmlNodes.append(xmlNode)

root.append(xmlNodes)

xmlConns = etree.Element("Connections")

for c in connections:
	xmlCon = c.to_xml()
	xmlConns.append(xmlCon)

root.append(xmlConns)

file = open(output + ".xml", "w+")

et = etree.ElementTree(root)
et.write(file, pretty_print=True)