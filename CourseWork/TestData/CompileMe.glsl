#version 450

layout(local_size_x = 64) in;

uniform float u_time;

void main() {
    float x = u_time * 2.0;
    if (x > 1.0) {
        x = x / 2.0;
    }
}