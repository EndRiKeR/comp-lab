#version 430 core

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer FibonacciBuffer {
    uint numbers[];
};

uniform uint N = 10;

void main() {
    uint a = 0u;
    uint b = 1u;
    
    numbers[0] = a;
    if (N == 1u) return;
    
    numbers[1] = b;
    if (N == 2u) return;
    
    for (uint i = 2u; i < N; i++) {
        uint c = a + b;
        numbers[i] = c;
        a = b;
        b = c;
    }
}