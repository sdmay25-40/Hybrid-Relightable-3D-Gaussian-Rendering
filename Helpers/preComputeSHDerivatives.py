import sympy as sp

# Print a polynomial as a callable HLSL (tehcnically GLSL) function
def printAsFunc(l, m, poly):

    polyAsCode = sp.glsl_code(poly)

    mStr = str(m)

    mStr = mStr.replace("-", "n")

    funcStr =f"float alp_{l}_{mStr}(float theta){{return {polyAsCode};}}"
    
    print(funcStr)


# Get the Associated Legendre polynomial for a given l and m value
def getPolyForLM(l , m): 
    x = sp.cos(sp.symbols("theta", real=True))
    return sp.assoc_legendre(l,  m, x)

# Number of bands is hardcoded based on the output of our  Gaussian Optimizer
def getAllPolys():
    for l in range(9):
        for m in range(-l, l + 1):
            printAsFunc(l, m, getPolyForLM(l, m))

getAllPolys()