#pragma once

#include "sharpie/defs.h"

typedef enum {
    SQ_1,
    SQ_2,
    TR_1,
    TR_2,
    SAW_1,
    SAW_2,
    NS_1,
    NS_2,
} AudioChannel;

// BIOS

// Hardware
void __sharpie_play_note(AudioChannel channel, int note, int instr);
void __sharpie_play_song(const void *address);
void __sharpie_stop(int channel);
void __sharpie_mute(void);
void __sharpie_hard_mute(void);
void __sharpie_switch_bank(unsigned char bank);
unsigned char __sharpie_get_bank();
void __sharpie_save(void *start, size_t length);
void __sharpie_load(void *dest, size_t length);
void __sharpie_crash(void);
int __sharpie_random(int maxExclusive);
void __sharpie_debug(int value_to_print);

// --- Sharpie Macros & Aliases ---
// Automatically packs Attr (Low Byte) and Type (High Byte)

#define save_data(src, size) __sharpie_save(src, size)
#define load_data(src, size) __sharpie_load(src, size)

#define play_note(ch, note, i) __sharpie_play_note(ch, note, i)
#define play_song(addr) __sharpie_play_song(addr)
#define halt() __sharpie_halt()
#define random(maxExclusive) __sharpie_random(maxExclusive)
#define crash() __sharpie_crash()
#define print_breadcrumb(value) __sharpie_debug(value)
#define switch_bank(b) __sharpie_switch_bank(b)
#define get_current_bank() __sharpie_get_bank()
