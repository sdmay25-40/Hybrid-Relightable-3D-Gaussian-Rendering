import struct 
import sys

# Write a new header for the binary file
def writeNewHeader(oldHeader : str, file):
    oldHeader = oldHeader.replace("ascii", "binary_little_endian")
    file.write(oldHeader.encode("ascii"))

# Write the body in binary 
def writeConvertedBody(oldBody : str, file):
    # Split the body up by line
    body = oldBody.split('\n')

    # Write the body as bianry
    for element in body:
        for item in element.split(' '):
            if item == '':
                continue
            itemAsFloat = float(item)
            itemFourBytes = struct.pack('f', itemAsFloat)
            file.write(itemFourBytes)

# Convert the file specified on the command line
def main():
    fileName = sys.argv[1]
    oldFile = open(fileName, 'r')
    contents = oldFile.read()
    oldFile.close()

    extensionIndex = fileName.rfind('.')
    fileNameNoExtension = fileName[0 : extensionIndex]
    fileExtension = fileName[extensionIndex:]

    newFile = open(fileNameNoExtension + "Bin" + fileExtension, 'wb')

    headerLoc = 0
    if "end_header" in contents:
        headerLoc = contents.find("end_header") + 11
    elif "end header" in contents:
        headerLoc = contents.find("end header") + 11
    else:
        print("Error: Could not find the end of the header in file " + fileName)
        exit(0)
    
    header = contents[0 : headerLoc]
    body = contents[headerLoc : ]
    writeNewHeader(header, newFile)
    writeConvertedBody(body, newFile)

    newFile.close()

main()