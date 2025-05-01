float shEval(int l, int m, float3 v){
    if(l == 0 && m == 0){
        return 0.28209479177387814;
    }
    else if(l == 1 && m == -1){
        return 0.4886025119029199 * v.y;
    }
    else if(l == 1 && m == 0){
        return 0.4886025119029199 * v.z;
    }
    else if(l == 1 && m == 1){
        return 0.4886025119029199 * v.x;
    }
    else if(l == 2 && m == -2){
        return 1.0925484305920792 * (v.x * v.y);
    }
    else if(l == 2 && m == -1){
        return -1.0925484305920792 * (v.y * v.z);
    }
    else if(l == 2 && m == 0){
        return 0.31539156525252005 * (2 * pow(v.z, 2) - pow(v.x, 2) - pow(v.y, 2));
    }
    else if (l == 2 && m == 1){
        return -1.0925484305920792 * (v.x * v.z);
    }
    else if(l == 2 && m == 2){
        return 0.5462742152960396 * (pow(v.x, 2) - pow(v.y, 2));
    }
    else{
        return 0;
    }


}