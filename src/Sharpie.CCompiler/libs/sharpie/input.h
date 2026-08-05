#include "defs.h"

typedef enum Button {
    BTN_NONE = 0,
    BTN_UP = 1 << 0,
    BTN_DOWN = 1 << 1,
    BTN_LEFT = 1 << 2,
    BTN_RIGHT = 1 << 3,
    BTN_A = 1 << 4,
    BTN_B = 1 << 5,
    BTN_START = 1 << 6,
    BTN_OPTION = 1 << 7
} Button;

Button __sharpie_input(int controller);
#define get_input(controller) __sharpie_input(controller)

#define button_down(state, btn) (((state) & (btn)))
