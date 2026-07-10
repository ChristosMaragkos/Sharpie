#include "./include/sharpie.h"

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

const unsigned char FOREGROUND = 4;
const unsigned char WIDTH = 255;
const unsigned char HEIGHT = 255;
const unsigned char CENTER = 128;

const int FOV = 96;

/*
 *   Draws a single pixel at the X and Y points of a 2D vector.
 */
void draw_point(Vector2 *p) {
    if (p->x > WIDTH || p->x < 0 || p->y > HEIGHT || p->y < 0)
        return;

    write_vram((unsigned char)p->x, (unsigned char)p->y, FOREGROUND);
}
/*
 *   Converts a given 2D vector representing world coordinates to one
 *   representing screen coordinates.
 */
Vector2 world_to_screen(Vector3 *p) {
    Vector2 out;

    if (p->z <= 0) {
        out.x = -1;
        out.y = -1;
        return out;
    }

    out.x = CENTER + (p->x * FOV) / p->z;
    out.y = CENTER + (p->y * FOV) / p->z;
    return out;
}

void rotate_xz(Vector3 *p, unsigned char angle) {
    // x' = x * cos - z * sin, z' = x * sin + z * cos
    int s = sin(angle);
    int c = cos(angle);

    int old_x = p->x;
    int old_z = p->z;
    // our sine and cosine values are inflated to 256
    // because of fixed point math. We gotta  back.

    p->x = ((old_x * c) - (old_z * s)) / 256;
    p->z = ((old_x * s) + (old_z * c)) / 256;
}

/*
 * Shove our cube further down the "world" since Z is also centered on us
 */
void translate_z(Vector3 *p, int amount) { p->z += amount; }

void line_bresenham(Vector2 *p1, Vector2 *p2) {
    int stepX, stepY;
    int dx = p2->x - p1->x;
    if (dx > 0) {
        stepX = 1;
    } else {
        stepX = -1;
        dx = -dx;
    }

    int dy = p2->y - p1->y;
    if (dy > 0) {
        stepY = 1;
    } else {
        stepY = -1;
        dy = -dy;
    }

    int error;
    Vector2 v;

    int x = p1->x;
    int y = p1->y;

    if (dx > dy) {
        error = (2 * dy) - dx;

        while (x != p2->x) {
            if (x >= 0 && x <= WIDTH && y >= 0 && y <= HEIGHT)
                write_vram((unsigned char)x, (unsigned char)y, FOREGROUND);

            if (error >= 0) {
                y += stepY;
                error -= 2 * dx;
            }

            error += 2 * dy;
            x += stepX;
        }
    } else {
        error = (2 * dx) - dy;

        while (y != p2->y) {
            if (x >= 0 && x <= WIDTH && y >= 0 && y <= HEIGHT)
                write_vram((unsigned char)x, (unsigned char)y, FOREGROUND);

            if (error >= 0) {
                x += stepX;
                error -= 2 * dy;
            }

            error += 2 * dx;
            y += stepY;
        }
    }
    if (x >= 0 && x <= WIDTH && y >= 0 && y <= HEIGHT)
        write_vram((unsigned char)x, (unsigned char)y, FOREGROUND);
}

Vector3 vertices[8] = {
    {-32, 32, 32},
    {32, 32, 32},
    {32, -32, 32},
    {-32, -32, 32},

    {-32, 32, -32},
    {32, 32, -32},
    {32, -32, -32},
    {-32, -32, -32},
};

unsigned char edges[24] = {0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6,
                           6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7};

unsigned char angle = 0;
unsigned char dAngle = 0;

int main(void) {
    set_blit_mode(BLT_NONE);

    Vector3 v, v1;
    Vector2 p, p1;

    while (1) {
        clear_screen(0);

        for (int i = 0; i < sizeof(edges); i += 2) {
            v = vertices[edges[i]];
            v1 = vertices[edges[i + 1]];

            rotate_xz(&v, angle);
            rotate_xz(&v1, angle);
            v.z += 96;
            v1.z += 96;

            p = world_to_screen(&v);
            p1 = world_to_screen(&v1);

            line_bresenham(&p, &p1);
            restart_frame();
        }

        if (++dAngle == 30) {
            ++angle;
            dAngle = 0;
            yield();
        }
        if (angle == 192) {
            angle = 0;
        }
    }
    return 0;
}
