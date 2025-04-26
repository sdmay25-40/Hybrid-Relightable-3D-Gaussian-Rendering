


// Functions for precomputer associated legendre polynomails. used in spherical harmonics
// There are more than we need here I had the number we were using wrong when I generated them
float alp_0_0(float theta){return 1;}
float alp_1_n1(float theta){return (1.0/2.0)*sqrt(1 - nPow(cos(theta), 2.0));}
float alp_1_0(float theta){return cos(theta);}
float alp_1_1(float theta){return -sqrt(1 - nPow(cos(theta), 2.0));}
float alp_2_n2(float theta){return 1.0/8.0 - 1.0/8.0*nPow(cos(theta), 2.0);}
float alp_2_n1(float theta){return (1.0/2.0)*sqrt(1 - nPow(cos(theta), 2.0))*cos(theta);}
float alp_2_0(float theta){return (3.0/2.0)*nPow(cos(theta), 2.0) - 1.0/2.0;}
float alp_2_1(float theta){return -3*sqrt(1 - nPow(cos(theta), 2.0))*cos(theta);}
float alp_2_2(float theta){return 3 - 3*nPow(cos(theta), 2.0);}
float alp_3_n3(float theta){return (1.0/48.0)*nPow(1 - nPow(cos(theta), 2.0), 1.5);}
float alp_3_n2(float theta){return (1.0/8.0)*(1 - nPow(cos(theta), 2.0))*cos(theta);}
float alp_3_n1(float theta){return (1.0/12.0)*sqrt(1 - nPow(cos(theta), 2.0))*((15.0/2.0)*nPow(cos(theta), 2.0) - 3.0/2.0);}
float alp_3_0(float theta){return (5.0/2.0)*nPow(cos(theta), 3.0) - 3.0/2.0*cos(theta);}
float alp_3_1(float theta){return -sqrt(1 - nPow(cos(theta), 2.0))*((15.0/2.0)*nPow(cos(theta), 2.0) - 3.0/2.0);}
float alp_3_2(float theta){return 15*(1 - nPow(cos(theta), 2.0))*cos(theta);}
float alp_3_3(float theta){return -15*nPow(1 - nPow(cos(theta), 2.0), 1.5);}
float alp_4_n4(float theta){return (1.0/384.0)*nPow(1 - nPow(cos(theta), 2.0), 2.0);}
float alp_4_n3(float theta){return (1.0/48.0)*nPow(1 - nPow(cos(theta), 2.0), 1.5)*cos(theta);}
float alp_4_n2(float theta){return (1.0/360.0)*(1 - nPow(cos(theta), 2.0))*((105.0/2.0)*nPow(cos(theta), 2.0) - 15.0/2.0);}
float alp_4_n1(float theta){return (1.0/20.0)*sqrt(1 - nPow(cos(theta), 2.0))*((35.0/2.0)*nPow(cos(theta), 3.0) - 15.0/2.0*cos(theta));}
float alp_4_0(float theta){return (35.0/8.0)*nPow(cos(theta), 4.0) - 15.0/4.0*nPow(cos(theta), 2.0) + 3.0/8.0;}
float alp_4_1(float theta){return -sqrt(1 - nPow(cos(theta), 2.0))*((35.0/2.0)*nPow(cos(theta), 3.0) - 15.0/2.0*cos(theta));}
float alp_4_2(float theta){return (1 - nPow(cos(theta), 2.0))*((105.0/2.0)*nPow(cos(theta), 2.0) - 15.0/2.0);}
float alp_4_3(float theta){return -105*nPow(1 - nPow(cos(theta), 2.0), 1.5)*cos(theta);}
float alp_4_4(float theta){return 105*nPow(1 - nPow(cos(theta), 2.0), 2.0);}
float alp_5_n5(float theta){return (1.0/3840.0)*nPow(1 - nPow(cos(theta), 2.0), 2.5);}
float alp_5_n4(float theta){return (1.0/384.0)*nPow(1 - nPow(cos(theta), 2.0), 2.0)*cos(theta);}
float alp_5_n3(float theta){return (1.0/20160.0)*nPow(1 - nPow(cos(theta), 2.0), 1.5)*((945.0/2.0)*nPow(cos(theta), 2.0) - 105.0/2.0);}
float alp_5_n2(float theta){return (1.0/840.0)*(1 - nPow(cos(theta), 2.0))*((315.0/2.0)*nPow(cos(theta), 3.0) - 105.0/2.0*cos(theta));}
float alp_5_n1(float theta){return (1.0/30.0)*sqrt(1 - nPow(cos(theta), 2.0))*((315.0/8.0)*nPow(cos(theta), 4.0) - 105.0/4.0*nPow(cos(theta), 2.0) + 15.0/8.0);}
float alp_5_0(float theta){return (63.0/8.0)*nPow(cos(theta), 5.0) - 35.0/4.0*nPow(cos(theta), 3.0) + (15.0/8.0)*cos(theta);}
float alp_5_1(float theta){return -sqrt(1 - nPow(cos(theta), 2.0))*((315.0/8.0)*nPow(cos(theta), 4.0) - 105.0/4.0*nPow(cos(theta), 2.0) + 15.0/8.0);}
float alp_5_2(float theta){return (1 - nPow(cos(theta), 2.0))*((315.0/2.0)*nPow(cos(theta), 3.0) - 105.0/2.0*cos(theta));}
float alp_5_3(float theta){return -nPow(1 - nPow(cos(theta), 2.0), 1.5)*((945.0/2.0)*nPow(cos(theta), 2.0) - 105.0/2.0);}
float alp_5_4(float theta){return 945*nPow(1 - nPow(cos(theta), 2.0), 2.0)*cos(theta);}
float alp_5_5(float theta){return -945*nPow(1 - nPow(cos(theta), 2.0), 2.5);}
float alp_6_n6(float theta){return (1.0/46080.0)*nPow(1 - nPow(cos(theta), 2.0), 3.0);}
float alp_6_n5(float theta){return (1.0/3840.0)*nPow(1 - nPow(cos(theta), 2.0), 2.5)*cos(theta);}
float alp_6_n4(float theta){return (1.0/1814400.0)*nPow(1 - nPow(cos(theta), 2.0), 2.0)*((10395.0/2.0)*nPow(cos(theta), 2.0) - 945.0/2.0);}
float alp_6_n3(float theta){return (1.0/60480.0)*nPow(1 - nPow(cos(theta), 2.0), 1.5)*((3465.0/2.0)*nPow(cos(theta), 3.0) - 945.0/2.0*cos(theta));}
float alp_6_n2(float theta){return (1.0/1680.0)*(1 - nPow(cos(theta), 2.0))*((3465.0/8.0)*nPow(cos(theta), 4.0) - 945.0/4.0*nPow(cos(theta), 2.0) + 105.0/8.0);}
float alp_6_n1(float theta){return (1.0/42.0)*sqrt(1 - nPow(cos(theta), 2.0))*((693.0/8.0)*nPow(cos(theta), 5.0) - 315.0/4.0*nPow(cos(theta), 3.0) + (105.0/8.0)*cos(theta));}
float alp_6_0(float theta){return (231.0/16.0)*nPow(cos(theta), 6.0) - 315.0/16.0*nPow(cos(theta), 4.0) + (105.0/16.0)*nPow(cos(theta), 2.0) - 5.0/16.0;}
float alp_6_1(float theta){return -sqrt(1 - nPow(cos(theta), 2.0))*((693.0/8.0)*nPow(cos(theta), 5.0) - 315.0/4.0*nPow(cos(theta), 3.0) + (105.0/8.0)*cos(theta));}
float alp_6_2(float theta){return (1 - nPow(cos(theta), 2.0))*((3465.0/8.0)*nPow(cos(theta), 4.0) - 945.0/4.0*nPow(cos(theta), 2.0) + 105.0/8.0);}
float alp_6_3(float theta){return -nPow(1 - nPow(cos(theta), 2.0), 1.5)*((3465.0/2.0)*nPow(cos(theta), 3.0) - 945.0/2.0*cos(theta));}
float alp_6_4(float theta){return nPow(1 - nPow(cos(theta), 2.0), 2.0)*((10395.0/2.0)*nPow(cos(theta), 2.0) - 945.0/2.0);}
float alp_6_5(float theta){return -10395*nPow(1 - nPow(cos(theta), 2.0), 2.5)*cos(theta);}
float alp_6_6(float theta){return 10395*nPow(1 - nPow(cos(theta), 2.0), 3.0);}
float alp_7_n7(float theta){return (1.0/645120.0)*nPow(1 - nPow(cos(theta), 2.0), 3.5);}
float alp_7_n6(float theta){return (1.0/46080.0)*nPow(1 - nPow(cos(theta), 2.0), 3.0)*cos(theta);}
float alp_7_n5(float theta){return (1.0/239500800.0)*nPow(1 - nPow(cos(theta), 2.0), 2.5)*((135135.0/2.0)*nPow(cos(theta), 2.0) - 10395.0/2.0);}
float alp_7_n4(float theta){return (1.0/6652800.0)*nPow(1 - nPow(cos(theta), 2.0), 2.0)*((45045.0/2.0)*nPow(cos(theta), 3.0) - 10395.0/2.0*cos(theta));}
float alp_7_n3(float theta){return (1.0/151200.0)*nPow(1 - nPow(cos(theta), 2.0), 1.5)*((45045.0/8.0)*nPow(cos(theta), 4.0) - 10395.0/4.0*nPow(cos(theta), 2.0) + 945.0/8.0);}
float alp_7_n2(float theta){return (1.0/3024.0)*(1 - nPow(cos(theta), 2.0))*((9009.0/8.0)*nPow(cos(theta), 5.0) - 3465.0/4.0*nPow(cos(theta), 3.0) + (945.0/8.0)*cos(theta));}
float alp_7_n1(float theta){return (1.0/56.0)*sqrt(1 - nPow(cos(theta), 2.0))*((3003.0/16.0)*nPow(cos(theta), 6.0) - 3465.0/16.0*nPow(cos(theta), 4.0) + (945.0/16.0)*nPow(cos(theta), 2.0) - 35.0/16.0);}
float alp_7_0(float theta){return (429.0/16.0)*nPow(cos(theta), 7.0) - 693.0/16.0*nPow(cos(theta), 5.0) + (315.0/16.0)*nPow(cos(theta), 3.0) - 35.0/16.0*cos(theta);}
float alp_7_1(float theta){return -sqrt(1 - nPow(cos(theta), 2.0))*((3003.0/16.0)*nPow(cos(theta), 6.0) - 3465.0/16.0*nPow(cos(theta), 4.0) + (945.0/16.0)*nPow(cos(theta), 2.0) - 35.0/16.0);}
float alp_7_2(float theta){return (1 - nPow(cos(theta), 2.0))*((9009.0/8.0)*nPow(cos(theta), 5.0) - 3465.0/4.0*nPow(cos(theta), 3.0) + (945.0/8.0)*cos(theta));}
float alp_7_3(float theta){return -nPow(1 - nPow(cos(theta), 2.0), 1.5)*((45045.0/8.0)*nPow(cos(theta), 4.0) - 10395.0/4.0*nPow(cos(theta), 2.0) + 945.0/8.0);}
float alp_7_4(float theta){return nPow(1 - nPow(cos(theta), 2.0), 2.0)*((45045.0/2.0)*nPow(cos(theta), 3.0) - 10395.0/2.0*cos(theta));}
float alp_7_5(float theta){return -nPow(1 - nPow(cos(theta), 2.0), 2.5)*((135135.0/2.0)*nPow(cos(theta), 2.0) - 10395.0/2.0);}
float alp_7_6(float theta){return 135135*nPow(1 - nPow(cos(theta), 2.0), 3.0)*cos(theta);}
float alp_7_7(float theta){return -135135*nPow(1 - nPow(cos(theta), 2.0), 3.5);}
float alp_8_n8(float theta){return (1.0/10321920.0)*nPow(1 - nPow(cos(theta), 2.0), 4.0);}
float alp_8_n7(float theta){return (1.0/645120.0)*nPow(1 - nPow(cos(theta), 2.0), 3.5)*cos(theta);}
float alp_8_n6(float theta){return (1.0/43589145600.0)*nPow(1 - nPow(cos(theta), 2.0), 3.0)*((2027025.0/2.0)*nPow(cos(theta), 2.0) - 135135.0/2.0);}
float alp_8_n5(float theta){return (1.0/1037836800.0)*nPow(1 - nPow(cos(theta), 2.0), 2.5)*((675675.0/2.0)*nPow(cos(theta), 3.0) - 135135.0/2.0*cos(theta));}
float alp_8_n4(float theta){return (1.0/19958400.0)*nPow(1 - nPow(cos(theta), 2.0), 2.0)*((675675.0/8.0)*nPow(cos(theta), 4.0) - 135135.0/4.0*nPow(cos(theta), 2.0) + 10395.0/8.0);}
float alp_8_n3(float theta){return (1.0/332640.0)*nPow(1 - nPow(cos(theta), 2.0), 1.5)*((135135.0/8.0)*nPow(cos(theta), 5.0) - 45045.0/4.0*nPow(cos(theta), 3.0) + (10395.0/8.0)*cos(theta));}
float alp_8_n2(float theta){return (1.0/5040.0)*(1 - nPow(cos(theta), 2.0))*((45045.0/16.0)*nPow(cos(theta), 6.0) - 45045.0/16.0*nPow(cos(theta), 4.0) + (10395.0/16.0)*nPow(cos(theta), 2.0) - 315.0/16.0);}
float alp_8_n1(float theta){return (1.0/72.0)*sqrt(1 - nPow(cos(theta), 2.0))*((6435.0/16.0)*nPow(cos(theta), 7.0) - 9009.0/16.0*nPow(cos(theta), 5.0) + (3465.0/16.0)*nPow(cos(theta), 3.0) - 315.0/16.0*cos(theta));}
float alp_8_0(float theta){return (6435.0/128.0)*nPow(cos(theta), 8.0) - 3003.0/32.0*nPow(cos(theta), 6.0) + (3465.0/64.0)*nPow(cos(theta), 4.0) - 315.0/32.0*nPow(cos(theta), 2.0) + 35.0/128.0;}
float alp_8_1(float theta){return -sqrt(1 - nPow(cos(theta), 2.0))*((6435.0/16.0)*nPow(cos(theta), 7.0) - 9009.0/16.0*nPow(cos(theta), 5.0) + (3465.0/16.0)*nPow(cos(theta), 3.0) - 315.0/16.0*cos(theta));}
float alp_8_2(float theta){return (1 - nPow(cos(theta), 2.0))*((45045.0/16.0)*nPow(cos(theta), 6.0) - 45045.0/16.0*nPow(cos(theta), 4.0) + (10395.0/16.0)*nPow(cos(theta), 2.0) - 315.0/16.0);}
float alp_8_3(float theta){return -nPow(1 - nPow(cos(theta), 2.0), 1.5)*((135135.0/8.0)*nPow(cos(theta), 5.0) - 45045.0/4.0*nPow(cos(theta), 3.0) + (10395.0/8.0)*cos(theta));}
float alp_8_4(float theta){return nPow(1 - nPow(cos(theta), 2.0), 2.0)*((675675.0/8.0)*nPow(cos(theta), 4.0) - 135135.0/4.0*nPow(cos(theta), 2.0) + 10395.0/8.0);}
float alp_8_5(float theta){return -nPow(1 - nPow(cos(theta), 2.0), 2.5)*((675675.0/2.0)*nPow(cos(theta), 3.0) - 135135.0/2.0*cos(theta));}
float alp_8_6(float theta){return nPow(1 - nPow(cos(theta), 2.0), 3.0)*((2027025.0/2.0)*nPow(cos(theta), 2.0) - 135135.0/2.0);}
float alp_8_7(float theta){return -2027025*nPow(1 - nPow(cos(theta), 2.0), 3.5)*cos(theta);}
float alp_8_8(float theta){return 2027025*nPow(1 - nPow(cos(theta), 2.0), 4.0);}

// Call the correct function for l and m values
float alpMap(int l, int m, float theta){
    if(l == 0 && m == 0){
        return alp_0_0(theta);
    }
    if(l == 1 && m == -1){
        return alp_1_n1(theta);
    }
    if(l == 1 && m == 0){
        return alp_1_0(theta);
    }
    if(l == 1 && m == 1){
        return alp_1_1(theta);
    }
    if(l == 2 && m == -2){
        return alp_2_n2(theta);
    }
    if(l == 2 && m == -1){
        return alp_2_n1(theta);
    }
    if(l == 2 && m == 0){
        return alp_2_0(theta);
    }
    if(l == 2 && m == 1){
        return alp_2_1(theta);
    }
    if(l == 2 && m == 2){
        return alp_2_2(theta);
    }
    if(l == 3 && m == -3){
        return alp_3_n3(theta);
    }
    if(l == 3 && m == -2){
        return alp_3_n2(theta);
    }
    if(l == 3 && m == -1){
        return alp_3_n1(theta);
    }
    if(l == 3 && m == 0){
        return alp_3_0(theta);
    }
    if(l == 3 && m == 1){
        return alp_3_1(theta);
    }
    if(l == 3 && m == 2){
        return alp_3_2(theta);
    }
    if(l == 3 && m == 3){
        return alp_3_3(theta);
    }
    if(l == 4 && m == -4){
        return alp_4_n4(theta);
    }
    if(l == 4 && m == -3){
        return alp_4_n3(theta);
    }
    if(l == 4 && m == -2){
        return alp_4_n2(theta);
    }
    if(l == 4 && m == -1){
        return alp_4_n1(theta);
    }
    if(l == 4 && m == 0){
        return alp_4_0(theta);
    }
    if(l == 4 && m == 1){
        return alp_4_1(theta);
    }
    if(l == 4 && m == 2){
        return alp_4_2(theta);
    }
    if(l == 4 && m == 3){
        return alp_4_3(theta);
    }
    if(l == 4 && m == 4){
        return alp_4_4(theta);
    }
    if(l == 5 && m == -5){
        return alp_5_n5(theta);
    }
    if(l == 5 && m == -4){
        return alp_5_n4(theta);
    }
    if(l == 5 && m == -3){
        return alp_5_n3(theta);
    }
    if(l == 5 && m == -2){
        return alp_5_n2(theta);
    }
    if(l == 5 && m == -1){
        return alp_5_n1(theta);
    }
    if(l == 5 && m == 0){
        return alp_5_0(theta);
    }
    if(l == 5 && m == 1){
        return alp_5_1(theta);
    }
    if(l == 5 && m == 2){
        return alp_5_2(theta);
    }
    if(l == 5 && m == 3){
        return alp_5_3(theta);
    }
    if(l == 5 && m == 4){
        return alp_5_4(theta);
    }
    if(l == 5 && m == 5){
        return alp_5_5(theta);
    }
    if(l == 6 && m == -6){
        return alp_6_n6(theta);
    }
    if(l == 6 && m == -5){
        return alp_6_n5(theta);
    }
    if(l == 6 && m == -4){
        return alp_6_n4(theta);
    }
    if(l == 6 && m == -3){
        return alp_6_n3(theta);
    }
    if(l == 6 && m == -2){
        return alp_6_n2(theta);
    }
    if(l == 6 && m == -1){
        return alp_6_n1(theta);
    }
    if(l == 6 && m == 0){
        return alp_6_0(theta);
    }
    if(l == 6 && m == 1){
        return alp_6_1(theta);
    }
    if(l == 6 && m == 2){
        return alp_6_2(theta);
    }
    if(l == 6 && m == 3){
        return alp_6_3(theta);
    }
    if(l == 6 && m == 4){
        return alp_6_4(theta);
    }
    if(l == 6 && m == 5){
        return alp_6_5(theta);
    }
    if(l == 6 && m == 6){
        return alp_6_6(theta);
    }
    if(l == 7 && m == -7){
        return alp_7_n7(theta);
    }
    if(l == 7 && m == -6){
        return alp_7_n6(theta);
    }
    if(l == 7 && m == -5){
        return alp_7_n5(theta);
    }
    if(l == 7 && m == -4){
        return alp_7_n4(theta);
    }
    if(l == 7 && m == -3){
        return alp_7_n3(theta);
    }
    if(l == 7 && m == -2){
        return alp_7_n2(theta);
    }
    if(l == 7 && m == -1){
        return alp_7_n1(theta);
    }
    if(l == 7 && m == 0){
        return alp_7_0(theta);
    }
    if(l == 7 && m == 1){
        return alp_7_1(theta);
    }
    if(l == 7 && m == 2){
        return alp_7_2(theta);
    }
    if(l == 7 && m == 3){
        return alp_7_3(theta);
    }
    if(l == 7 && m == 4){
        return alp_7_4(theta);
    }
    if(l == 7 && m == 5){
        return alp_7_5(theta);
    }
    if(l == 7 && m == 6){
        return alp_7_6(theta);
    }
    if(l == 7 && m == 7){
        return alp_7_7(theta);
    }
    if(l == 8 && m == -8){
        return alp_8_n8(theta);
    }
    if(l == 8 && m == -7){
        return alp_8_n7(theta);
    }
    if(l == 8 && m == -6){
        return alp_8_n6(theta);
    }
    if(l == 8 && m == -5){
        return alp_8_n5(theta);
    }
    if(l == 8 && m == -4){
        return alp_8_n4(theta);
    }
    if(l == 8 && m == -3){
        return alp_8_n3(theta);
    }
    if(l == 8 && m == -2){
        return alp_8_n2(theta);
    }
    if(l == 8 && m == -1){
        return alp_8_n1(theta);
    }
    if(l == 8 && m == 0){
        return alp_8_0(theta);
    }
    if(l == 8 && m == 1){
        return alp_8_1(theta);
    }
    if(l == 8 && m == 2){
        return alp_8_2(theta);
    }
    if(l == 8 && m == 3){
        return alp_8_3(theta);
    }
    if(l == 8 && m == 4){
        return alp_8_4(theta);
    }
    if(l == 8 && m == 5){
        return alp_8_5(theta);
    }
    if(l == 8 && m == 6){
        return alp_8_6(theta);
    }
    if(l == 8 && m == 7){
        return alp_8_7(theta);
    }
    if(l == 8 && m == 8){
        return alp_8_8(theta);
    }
    
    return -1;
}