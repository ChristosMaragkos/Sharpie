#include "../headers/sharpie.h"

#define sin(angle) sin_table[(angle)]
// because a full revolution in 8-bit fixed point is 256, 90 degrees is 64.
#define cos(angle) sin_table[(unsigned char)((angle) + 64)]

const unsigned char FOREGROUND = 1;
const unsigned char WIDTH = 255;
const unsigned char HEIGHT = 255;
const unsigned char CENTER = 128;

const unsigned char FOV = 128;

const int sin_table[256] = {
    0,    6,    12,   18,   25,   31,   37,   43,   49,   56,   62,   68,
    74,   80,   86,   92,   97,   103,  109,  115,  120,  126,  131,  136,
    142,  147,  152,  157,  162,  167,  171,  176,  180,  185,  189,  193,
    197,  201,  205,  208,  212,  215,  219,  222,  225,  228,  231,  233,
    236,  238,  240,  242,  244,  246,  247,  249,  250,  251,  252,  253,
    254,  255,  255,  256,  256,  256,  255,  255,  254,  253,  252,  251,
    250,  249,  247,  246,  244,  242,  240,  238,  236,  233,  231,  228,
    225,  222,  219,  215,  212,  208,  205,  201,  197,  193,  189,  185,
    180,  176,  171,  167,  162,  157,  152,  147,  142,  136,  131,  126,
    120,  115,  109,  103,  97,   92,   86,   80,   74,   68,   62,   56,
    49,   43,   37,   31,   25,   18,   12,   6,    0,    -6,   -12,  -18,
    -25,  -31,  -37,  -43,  -49,  -56,  -62,  -68,  -74,  -80,  -86,  -92,
    -97,  -103, -109, -115, -120, -126, -131, -136, -142, -147, -152, -157,
    -162, -167, -171, -176, -180, -185, -189, -193, -197, -201, -205, -208,
    -212, -215, -219, -222, -225, -228, -231, -233, -236, -238, -240, -242,
    -244, -246, -247, -249, -250, -251, -252, -253, -254, -255, -255, -256,
    -256, -256, -255, -255, -254, -253, -252, -251, -250, -249, -247, -246,
    -244, -242, -240, -238, -236, -233, -231, -228, -225, -222, -219, -215,
    -212, -208, -205, -201, -197, -193, -189, -185, -180, -176, -171, -167,
    -162, -157, -152, -147, -142, -136, -131, -126, -120, -115, -109, -103,
    -97,  -92,  -86,  -80,  -74,  -68,  -62,  -56,  -49,  -43,  -37,  -31,
    -25,  -18,  -12,  -6};

typedef struct {
  int x, y;
} Vec2;

typedef struct {
  int x, y, z;
} Vec3;

/*
 *   Draws a single pixel at the X and Y points of a 2D vector.
 */
void draw_point(Vec2 *p) {
  if (p->x > WIDTH || p->x < 0 || p->y > HEIGHT || p->y < 0)
    return;

  write_vram((unsigned char)p->x, (unsigned char)p->y, FOREGROUND);
}

/*
 *   Converts a given 2D vector representing world coordinates to one
 *   representing screen coordinates.
 */
Vec2 world_to_screen(Vec3 *p) {
  int x = ((p->x * FOV) / p->z) + CENTER;
  int y = ((p->y * FOV) / p->z) + CENTER;
  Vec2 v = {x, y};
  return v;
}

unsigned int dz = 0;
unsigned char ddz = 0;

int main(void) {
  set_blit_mode(NONE);
  while (1) {
    clear_screen(0);
    if (++ddz == 20) {
      ++dz;
      ddz = 0;
    }
    Vec3 v = {40, 40, 40 + dz};
    Vec2 projected = world_to_screen(&v);
    draw_point(&projected);

    v = (Vec3){80, 40, 40 + dz};
    projected = world_to_screen(&v);
    draw_point(&projected);
    yield();
  }
}
