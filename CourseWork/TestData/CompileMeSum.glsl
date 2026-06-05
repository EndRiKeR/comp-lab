#version 450

layout(local_size_x = 1) in;

layout(set = 0, binding = 0) uniform InputBlock {
    float a;
    float b;
} inputData;

layout(set = 0, binding = 1) buffer OutputBlock {
    float result;
} outputData;

void main() {
    outputData.result = inputData.a + inputData.b;
}