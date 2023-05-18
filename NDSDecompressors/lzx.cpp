#include "lzx.h"
#include "pch.h"

#include <io.h>
#include <cstdio>
#include <stdlib.h>
#include <string.h>

/*----------------------------------------------------------------------------*/
#define CMD_DECODE    0x00       // decode
#define CMD_CODE_11   0x11       // LZX big endian magic number
#define CMD_CODE_40   0x40       // LZX low endian magic number

#define LZX_WRAM      0x00       // VRAM file not compatible (0)
#define LZX_VRAM      0x01       // VRAM file compatible (1)

#define LZX_SHIFT     1          // bits to shift
#define LZX_MASK      0x80       // first bit to check
								 // ((((1 << LZX_SHIFT) - 1) << (8 - LZX_SHIFT)

#define LZX_THRESHOLD 2          // max number of bytes to not encode
#define LZX_N         0x1000     // max offset (1 << 12)
#define LZX_F         0x10       // max coded (1 << 4)
#define LZX_F1        0x110      // max coded ((1 << 4) + (1 << 8))
#define LZX_F2        0x10110    // max coded ((1 << 4) + (1 << 8) + (1 << 16))

#define RAW_MINIM     0x00000000 // empty file, 0 bytes
#define RAW_MAXIM     0x00FFFFFF // 3-bytes length, 16MB - 1

#define LZX_MINIM     0x00000004 // header only (empty RAW file)
#define LZX_MAXIM     0x01400000 // 0x01200006, padded to 20MB:
								 // * header, 4
								 // * length, RAW_MAXIM
								 // * flags, (RAW_MAXIM + 7) / 8
								 // * 3 (flag + 2 end-bytes)
								 // 4 + 0x00FFFFFF + 0x00200000 + 3 + padding

/*----------------------------------------------------------------------------*/
int lzx_vram;

/*----------------------------------------------------------------------------*/
#define BREAK(text) { printf(text); return; }
#define EXIT(text)  { printf(text); exit(-1); }

/*----------------------------------------------------------------------------*/
unsigned char* Load(char* filename, unsigned int* length, int min, int max) {
	FILE* fp;
	int   fs;
	unsigned char* fb;

	if (fopen_s(&fp, filename, "rb")) EXIT(filename);//EXIT("\nFile open error\n");
	fs = _filelength(_fileno(fp));
	if ((fs < min) || (fs > max)) EXIT("\nFile size error\n");
	fb = (unsigned char*)calloc(fs + 3, sizeof(char));
	if (fb == NULL) EXIT("\nMemory error\n");
	if (fread(fb, 1, fs, fp) != fs) EXIT("\nFile read error\n");
	if (fclose(fp) == EOF) EXIT("\nFile close error\n");

	*length = fs;

	return(fb);
}

/*----------------------------------------------------------------------------*/
void Save(char* filename, unsigned char* buffer, int length) {
	FILE* fp;

	if (fopen_s(&fp, filename, "wb")) EXIT("\nFile create error\n");
	if (fwrite(buffer, 1, length, fp) != length) EXIT("\nFile write error\n");
	if (fclose(fp) == EOF) EXIT("\nFile close error\n");
}

/*----------------------------------------------------------------------------*/
void LZX_Decode(char* filename) {
	unsigned char* pak_buffer, * raw_buffer, * pak, * raw, * pak_end, * raw_end;
	unsigned int   pak_len, raw_len, header, len, pos, threshold, tmp;
	unsigned char  flags, mask;

	pak_buffer = Load(filename, &pak_len, LZX_MINIM, LZX_MAXIM);

	header = *pak_buffer;
	if ((header != CMD_CODE_11) && ((header != CMD_CODE_40))) {
		free(pak_buffer);
		BREAK(", WARNING: file is not LZX encoded!\n");
	}

	raw_len = *(unsigned int*)pak_buffer >> 8;
	raw_buffer = (unsigned char*)calloc(raw_len, sizeof(char));
	if (raw_buffer == NULL) EXIT("\nMemory error\n");

	pak = pak_buffer + 4;
	raw = raw_buffer;
	pak_end = pak_buffer + pak_len;
	raw_end = raw_buffer + raw_len;

	mask = 0;

	while (raw < raw_end) {
		if (!(mask >>= LZX_SHIFT)) {
			if (pak == pak_end) break;
			flags = *pak++;
			if (header == CMD_CODE_40) flags = -flags;
			mask = LZX_MASK;
		}

		if (!(flags & mask)) {
			if (pak == pak_end) break;
			*raw++ = *pak++;
		}
		else {
			if (header == CMD_CODE_11) {
				if (pak + 1 >= pak_end) break;
				pos = *pak++;
				pos = (pos << 8) | *pak++;

				tmp = pos >> 12;
				if (tmp < LZX_THRESHOLD) {
					pos &= 0xFFF;
					if (pak == pak_end) break;
					pos = (pos << 8) | *pak++;
					threshold = LZX_F;
					if (tmp) {
						if (pak == pak_end) break;
						pos = (pos << 8) | *pak++;
						threshold = LZX_F1;
					}
				}
				else {
					threshold = 0;
				}

				len = (pos >> 12) + threshold + 1;
				pos = (pos & 0xFFF) + 1;
			}
			else {
				if (pak + 1 == pak_end) break;
				pos = *pak++;
				pos |= *pak++ << 8;

				tmp = pos & 0xF;
				if (tmp < LZX_THRESHOLD) {
					if (pak == pak_end) break;
					len = *pak++;
					threshold = LZX_F;
					if (tmp) {
						if (pak == pak_end) break;
						len = (*pak++ << 8) | len;
						threshold = LZX_F1;
					}
				}
				else {
					len = tmp;
					threshold = 0;
				}

				len += threshold;
				pos >>= 4;
			}

			if (raw + len > raw_end) {
				printf(", WARNING: wrong decoded length!");
				len = raw_end - raw;
			}

			while (len--) *raw++ = *(raw - pos);
		}
	}

	if (header == CMD_CODE_40) pak += *pak == 0x80 ? 3 : 2;

	raw_len = raw - raw_buffer;

	if (raw != raw_end) printf(", WARNING: unexpected end of encoded file!");

	Save(filename, raw_buffer, raw_len);

	free(raw_buffer);
	free(pak_buffer);
}