import random
import sys

# TODO: Rework for new Gaussian Format
# Create a string holding representing a randomly generated Gaussian
def generateRandomGaussian():
    x = round(random.uniform(-0.5, 0.5), 2)
    y = round(random.uniform(-0.5, 0.5), 2)
    z = round(random.uniform(-0.5, 0.5), 2)
    sX = round(random.uniform(0, 0.5), 2)
    sY = round(random.uniform(0, 0.5), 2)
    sZ = round(random.uniform(0, 0.5), 2)
    qX = round(random.random(), 2)
    qY = round(random.random(), 2)
    qZ = round(random.random(), 2)
    qW = round(random.random(), 2)
    r = round(random.random(), 2)
    g = round(random.random(), 2)
    b = round(random.random(), 2)
    a = 1
    return f"{x} {y} {z} {sX} {sY} {sZ} {qX} {qY} {qZ} {qW} {r} {g} {b} {a}"

# Create the header of the randomly generated gaussian file
def headerString(numGaussians):
    return f"""ply
format ascii 1.0
element simpleGaussian3D {numGaussians}
comment Coordinates in world space for the center of this Gaussians
property float x
property float y
property float z
comment Values used to construct a scale matrix for this gaussian
property float xScale
property float yScale
property float zScale
comment Quaternion values used to construct a rotation matrix for this gaussian
property float qX
property float qY
property float qZ
property float qW
comment These two matrices togther can be used to create the covariance
comment Color value of this Gaussian
property float r
property float g
property float b
property float a
end header"""

def main():
    # Handle command line arguments
    numGaussians = 50
    if(len(sys.argv) > 1):
            try:
                 numGaussians = int(sys.argv[1])
            except ValueError:
                 print("Command line argument must be an integer")
    
    outFile = open("RandomlyGeneratedGaussians.ply", 'w')
    outFile.write(headerString(numGaussians) + '\n')

    for i in range(numGaussians):
        if i < numGaussians - 1:
            outFile.write(generateRandomGaussian() + "\n")
        else:
            outFile.write(generateRandomGaussian())

    outFile.close()

main()