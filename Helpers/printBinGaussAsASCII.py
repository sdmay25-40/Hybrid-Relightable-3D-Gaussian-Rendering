import struct
import sys
from typing import BinaryIO


def readFloat(file : BinaryIO):
    bytes = file.read(4)
    return struct.unpack("f", bytes)[0]

def printGaussians(numToPrint : int, filename : str):
    file = open(filename, 'rb')

    headerBuffer = ""
    readingHeader = True

    # Read through header
    while readingHeader:

        nextByte = file.read(1)
        nextChar = nextByte.decode("ASCII")
        headerBuffer += nextChar
        if len(headerBuffer) >= 11:
            readingHeader = not (headerBuffer[-11 : ] == "end_header\n")
    
    # Print Gaussians
    for i in range(numToPrint):
        print(f"Gaussian {i}:")
        # Print position
        pos = []
        pos.append(readFloat(file))
        pos.append(readFloat(file))
        pos.append(readFloat(file))
        print(f"\tpos: {pos}")

        # Print sh coefficients
        shRCoefficients = []
        shGCoefficients = []
        shBCoefficients = []

        for c in range(9):
            shRCoefficients.append(readFloat(file))
        print(f"\tSH R: " + str(shRCoefficients))

        for c in range(9):
            shGCoefficients.append(readFloat(file))
        print(f"\tSH G: " + str(shGCoefficients))

        for c in range(9):
            shBCoefficients.append(readFloat(file))
        print(f"\tSH B: " + str(shBCoefficients))

        # Print scale
        scale = []
        scale.append(readFloat(file))
        scale.append(readFloat(file))
        scale.append(readFloat(file))
        print(f"\tscale: {scale}")

        # Print opacity
        print(f"opacity: {readFloat(file)}")

    file.close()

printGaussians(int(sys.argv[2]), sys.argv[1])