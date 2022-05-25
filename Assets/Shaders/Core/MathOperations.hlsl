int maxPower;

int stride;

#define GetValue(NAME, index, power) NAME[index * stride + maxPower + power]

bool CheckForZero(RWStructuredBuffer<float> mat, int index)
{
    bool res = true;
    for (int i =0;i< stride & res; i++)
        res = res && GetValue(mat,index,i) == 0;

    return res;
}

#define SetValue(NAME, index, power,val) NAME[index * stride + maxPower + power] = val;

#define AddValue(NAME, index, power, val) NAME[index * stride + maxPower + power] += val;

#define Divide(NAME1, NAME2, aIndex, bIndex, resIndex, identifier) int identifier##bLowestPower = maxPower+1;\
float identifier##lowestPowerVal = 1;\
for (int index= - maxPower; index<=maxPower; index++)\
{\
    float val = GetValue(NAME1, bIndex, index);\
    if(val == 0);\
    else\
    {\
        identifier##bLowestPower = index;\
        identifier##lowestPowerVal = val;\
        break;\
    }\
}\
if(identifier##bLowestPower > maxPower)\
    identifier##bLowestPower = 0;\
for (int index= max(- maxPower, - maxPower - identifier##bLowestPower); index<=min(maxPower, maxPower - identifier##bLowestPower) ; index++)\
{\
    float aValue = GetValue(NAME1, aIndex, index);\
    SetValue(NAME2, resIndex, index - identifier##bLowestPower, aValue/identifier##lowestPowerVal);\
}

#define Multiply(NAME1, NAME2, aIndex, bIndex, resIndex)for (int index= - maxPower; index<=maxPower; index++)\
{\
    float aFactor = GetValue(NAME1,aIndex,index);\
    for (int j = max(- maxPower,-maxPower-index) ; j <= min(maxPower, maxPower-index);j++)\
    {\
        float bFactor = GetValue(NAME1,bIndex, j );\
        AddValue(NAME2, resIndex, index + j, aFactor * bFactor);\
    }\
}


#define Multiply(NAME1, NAME2, NAME3, aIndex, bIndex, resIndex)for (int index= - maxPower; index<=maxPower; index++)\
{\
float aFactor = GetValue(NAME1,aIndex,index);\
for (int j = max(- maxPower,-maxPower-index) ; j <= min(maxPower, maxPower-index);j++)\
{\
float bFactor = GetValue(NAME2,bIndex, j );\
AddValue(NAME3, resIndex, index + j, aFactor * bFactor);\
}\
}
#define Multiply(NAME1, NAME2, NAME3, aIndex, bIndex, resIndex, factor)for (int index= - maxPower; index<=maxPower; index++)\
{\
float aFactor = GetValue(NAME1,aIndex,index);\
for (int j = max(- maxPower,-maxPower-index) ; j <= min(maxPower, maxPower-index);j++)\
{\
float bFactor = GetValue(NAME2,bIndex, j );\
AddValue(NAME3, resIndex, index + j, aFactor * bFactor*factor);\
}\
}

#define Add(NAME1, NAME2, aIndex, bIndex, resIndex, sign)for (int index= - maxPower; index<=maxPower; index++)\
{\
    float aValue = GetValue(NAME1, aIndex, index);\
    float bValue = GetValue(NAME1, bIndex, index);\
    SetValue(NAME2, resIndex, index, aValue+bValue*sign);\
}\


#define Dot(NAME1, NAME2, NAME3, rowA, columnB, resIndex, dimension)\
    for (int index = 0; index < dimension; index++)\
    {\
        int aIndex = rowA * dimension + index;\
        int bIndex = index * dimension + columnB;\
        Multiply(NAME1, NAME2, NAME3, aIndex, bIndex, resIndex,1);\
    }\
