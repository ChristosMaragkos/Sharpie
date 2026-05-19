#include "../headers/sharpie.h"

// cube.c contains an entirely software-driven 3D wireframe renderer that runs
// natively on Sharpie. It runs thanks to v0.4's VRAM access instructions and a
// few neat party tricks (like Bresenham's algorithm and precomputed LUTs for
// sine and cosine).

#define sin(angle) sin_table[(angle)]
// because a full revolution in 8-bit fixed point is 256, 90 degrees is 64.
#define cos(angle) sin_table[(unsigned char)((angle) + 64)]

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

const unsigned char FOREGROUND = 1;
const unsigned char WIDTH = 255;
const unsigned char HEIGHT = 255;
const unsigned char CENTER = 128;

const int FOV = 96;

int dz = 0;

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
  if (p->x > WIDTH)
    return;
  if (p->x < 0)
    return;
  if (p->y > HEIGHT)
    return;
  if (p->y < 0)
    return;

  write_vram((unsigned char)p->x, (unsigned char)p->y, FOREGROUND);
}
/*
 *   Converts a given 2D vector representing world coordinates to one
 *   representing screen coordinates.
 */
Vec2 world_to_screen(Vec3 *p) {
  Vec2 out;

  if (p->z <= 0) {
    out.x = -1;
    out.y = -1;
    return out;
  }

  out.x = CENTER + (p->x * FOV) / p->z;
  out.y = CENTER + (p->y * FOV) / p->z;
  return out;
}

void translate_z(Vec3 *p) { p->z += dz; }

int main(void) {
  set_blit_mode(NONE);

  Vec3 vertices[] = {
      {-32, 32, 96},
      {32, 32, 96},
      {32, -32, 96},
      {-32, -32, 96},
  };

  while (1) {
    clear_screen(0);

    Vec3 v = vertices[0];
    Vec2 p = world_to_screen(&v);
    draw_point(&p);

    v = vertices[1];
    p = world_to_screen(&v);
    draw_point(&p);

    v = vertices[2];
    p = world_to_screen(&v);
    draw_point(&p);

    v = vertices[3];
    p = world_to_screen(&v);
    draw_point(&p);

    yield();
  }
  return 0;
}
