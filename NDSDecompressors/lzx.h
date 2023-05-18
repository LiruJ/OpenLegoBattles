#pragma once


unsigned char* Load(char* filename, unsigned int* length, int min, int max);
void Save(char* filename, unsigned char* buffer, int length);

extern "C" __declspec(dllexport) void __stdcall LZX_Decode(char* filename);