#version 450

layout(local_size_x = 1) in;

layout(set = 0, binding = 0) uniform InputBlock {
    bool flag;
    int  a;
    int  b;
} inputData;

layout(set = 0, binding = 1) buffer OutputBlock {
    float result;
} outputData;

void main() {
    if (inputData.flag) {
        outputData.result = float(inputData.a * inputData.b);
    } else {
        outputData.result = float(inputData.a) / float(inputData.b);
    }
}