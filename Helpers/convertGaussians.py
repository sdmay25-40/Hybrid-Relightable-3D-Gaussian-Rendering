import argparse
import struct
import sys
from typing import BinaryIO, List


class Gaussian:
    def __init__(self):
        self.pos = []
        self.normal = []
        self.shRCoefficients = []
        self.shGCoefficients = []
        self.shBCoefficients = []
        self.opacity = 0.0
        self.scale = []
        self.rotation = []



def getArgs():
    parser = argparse.ArgumentParser()
    parser.add_argument("-c", "-input-custom", action="store_true", help="Input a custom gaussian and output standard. (Default is the opposite)")
    parser.add_argument("input_fileName")
    parser.add_argument("output_fileName")

    return parser.parse_args()

# Get the ASCII header for the custom Gaussian format with the specified number of gaussians
def getCustomHeader(numGaussians : int):
    return f"""ply
format binary_little_endian 1.0
element vertex {numGaussians}
property float x
property float y
property float z
property float f0
property float f1
property float f2
property float f3
property float f4
property float f5
property float f6
property float f7
property float f8
property float f9
property float f10
property float f11
property float f12
property float f13
property float f14
property float f15
property float f16
property float f17
property float f18
property float f19
property float f20
property float f21
property float f22
property float f23
property float f24
property float f25
property float f26
property float scale_x
property float scale_y
property float scale_z
property float opacity
end_header
"""


# Get the ASCII header for the custom Gaussian format with the specified number of gaussians
def getStandardHeader(numGaussians : int):
    return f"""ply
format binary_little_endian 1.0
element vertex {numGaussians}
property float x
property float y
property float z
property float nx
property float ny
property float nz
property float f_dc_0
property float f_dc_1
property float f_dc_2
property float f_rest_0
property float f_rest_1
property float f_rest_2
property float f_rest_3
property float f_rest_4
property float f_rest_5
property float f_rest_6
property float f_rest_7
property float f_rest_8
property float f_rest_9
property float f_rest_10
property float f_rest_11
property float f_rest_12
property float f_rest_13
property float f_rest_14
property float f_rest_15
property float f_rest_16
property float f_rest_17
property float f_rest_18
property float f_rest_19
property float f_rest_20
property float f_rest_21
property float f_rest_22
property float f_rest_23
property float f_rest_24
property float f_rest_25
property float f_rest_26
property float f_rest_27
property float f_rest_28
property float f_rest_29
property float f_rest_30
property float f_rest_31
property float f_rest_32
property float f_rest_33
property float f_rest_34
property float f_rest_35
property float f_rest_36
property float f_rest_37
property float f_rest_38
property float f_rest_39
property float f_rest_40
property float f_rest_41
property float f_rest_42
property float f_rest_43
property float f_rest_44
property float opacity
property float scale_0
property float scale_1
property float scale_2
property float rot_0
property float rot_1
property float rot_2
property float rot_3
end_header
"""


def readFloat(file : BinaryIO):
    bytes = file.read(4)
    return struct.unpack("f", bytes)[0]

# Read in a gaussian from a standard format file
def parseStandardGauss(filename : str):
    file = open(filename, 'rb')

    headerBuffer = ""
    readingHeader = True
    # read through header
    while readingHeader:

        nextByte = file.read(1)
        nextChar = nextByte.decode("ASCII")
        headerBuffer += nextChar

        if len(headerBuffer) >= 11:
            readingHeader = not (headerBuffer[-11 : ] == "end_header\n")

    gaussians = []
    # Read in the body 
    while file.peek(1) != bytes():
        g = Gaussian()

        # Read in position
        g.pos.append(readFloat(file))
        g.pos.append(readFloat(file))
        g.pos.append(readFloat(file))

        # Read in normal
        g.normal.append(readFloat(file))
        g.normal.append(readFloat(file))
        g.normal.append(readFloat(file))

        # Read in dc spherical harmonics
        g.shRCoefficients.append(readFloat(file))
        g.shGCoefficients.append(readFloat(file))
        g.shBCoefficients.append(readFloat(file))

        # Read in higher order spherical harmonics
        for i in range(15):
            g.shRCoefficients.append(readFloat(file))
    
        for i in range(15):
            g.shGCoefficients.append(readFloat(file))
            
        for i in range(15):
            g.shBCoefficients.append(readFloat(file))

        print(g.shRCoefficients)
        print(g.shGCoefficients)
        print(g.shBCoefficients)
        print("")

        # Read in opacity 
        g.opacity = readFloat(file)

        # Read in scale
        g.scale.append(readFloat(file))
        g.scale.append(readFloat(file))
        g.scale.append(readFloat(file))
        
        # Read in rotation
        g.rotation.append(readFloat(file))
        g.rotation.append(readFloat(file))
        g.rotation.append(readFloat(file))
        g.rotation.append(readFloat(file))

        gaussians.append(g)

    file.close()
    return gaussians

# Read in gaussians in our format
def parseCustomGauss(filename : str):
    file = open(filename, 'rb')

    headerBuffer = ""
    readingHeader = True
    # read through header
    while readingHeader:

        nextByte = file.read(1)
        nextChar = nextByte.decode("ASCII")
        headerBuffer += nextChar

        if len(headerBuffer) >= 11:
            readingHeader = not (headerBuffer[-11 : ] == "end_header\n")

    gaussians = []
    # Read in the body 
    while file.peek(1) != bytes():
        g = Gaussian()
        
        # Read in position
        g.pos.append(readFloat(file))
        g.pos.append(readFloat(file))
        g.pos.append(readFloat(file))

        # Read in spherical harmonics
        for i in range(9):
            g.shRCoefficients.append(readFloat(file))

        for i in range(9):
            g.shGCoefficients.append(readFloat(file))
            
        for i in range(9):
            g.shBCoefficients.append(readFloat(file))

        # Read in scale
        g.scale.append(readFloat(file))
        g.scale.append(readFloat(file))
        g.scale.append(readFloat(file))

        # Read in opacity 
        g.opacity = readFloat(file)

        # Set rotation and normals
        g.normal.append(0)
        g.normal.append(0)
        g.normal.append(0)

        g.rotation.append(0)
        g.rotation.append(0)
        g.rotation.append(0)
        g.rotation.append(0)


        gaussians.append(g)

    file.close()
    return gaussians

# Write gaussians in our format
def writeCustomGaussians(gaussians : List[Gaussian], filename : str):
    file = open(filename, 'wb')

    # Write the header
    headerBytes = getCustomHeader(len(gaussians)).encode("ASCII")
    file.write(headerBytes)

    # Write the gaussians
    for g in gaussians:
        # Write position
        file.write(struct.pack("f", g.pos[0]))
        file.write(struct.pack("f", g.pos[1]))
        file.write(struct.pack("f", g.pos[2]))

        # Write sperical harmonics
        for i in range(9):
            file.write(struct.pack("f", g.shRCoefficients[i]))

        for i in range(9):
            file.write(struct.pack("f", g.shGCoefficients[i]))

        for i in range(9):
            file.write(struct.pack("f", g.shBCoefficients[i]))

        file.write(struct.pack("f", g.scale[0]))
        file.write(struct.pack("f", g.scale[1]))
        file.write(struct.pack("f", g.scale[2]))

        # Write opacity
        file.write(struct.pack("f", g.opacity))

    file.close()

# Write Gaussians in the standard format
def writeStandardGaussians(gaussians : List[Gaussian], filename : str):
    file = open(filename, 'wb')

    # Write the header
    headerBytes = getStandardHeader(len(gaussians)).encode("ASCII")
    file.write(headerBytes)

    # Write the gaussians
    for g in gaussians:
        # Write position
        file.write(struct.pack("f", g.pos[0]))
        file.write(struct.pack("f", g.pos[1]))
        file.write(struct.pack("f", g.pos[2]))

        # Write normal
        file.write(struct.pack("f", g.normal[0]))
        file.write(struct.pack("f", g.normal[1]))
        file.write(struct.pack("f", g.normal[2]))

        # Write base spherical harmonics
        file.write(struct.pack("f", g.shRCoefficients[0]))
        file.write(struct.pack("f", g.shGCoefficients[0]))
        file.write(struct.pack("f", g.shBCoefficients[0]))

        # Write the rest of the spherical harmonics
        # First eight we have
        for i in range(1, 9):
            file.write(struct.pack("f", g.shRCoefficients[i]))
        # Zero the rest 
        for i in range(7):
            file.write(struct.pack("f", 0.0))

         # First eight we have
        for i in range(1, 9):
            file.write(struct.pack("f", g.shGCoefficients[i]))
        # Zero the rest 
        for i in range(7):
            file.write(struct.pack("f", 0.0))

         # First eight we have
        for i in range(1, 9):
            file.write(struct.pack("f", g.shBCoefficients[i]))
        # Zero the rest 
        for i in range(7):
            file.write(struct.pack("f", 0.0))

        # Write opacity
        file.write(struct.pack("f", g.opacity))

        # Write scale
        file.write(struct.pack("f", g.scale[0]))
        file.write(struct.pack("f", g.scale[1]))
        file.write(struct.pack("f", g.scale[2]))

        # Write rotation
        file.write(struct.pack("f", g.rotation[0]))
        file.write(struct.pack("f", g.rotation[1]))
        file.write(struct.pack("f", g.rotation[2]))
        file.write(struct.pack("f", g.rotation[3]))

    file.close()

def main():
    args = getArgs()

    # If in default mode
    if(not args.c):
        # Read in first arg
        gaussians = parseStandardGauss(args.input_fileName)

        # Write file
        writeCustomGaussians(gaussians, args.output_fileName)
    # If in custom input mode
    else:
        gaussians = parseCustomGauss(args.input_fileName)

        writeStandardGaussians(gaussians, args.output_fileName)

main()