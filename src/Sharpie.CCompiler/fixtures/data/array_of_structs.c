typedef struct {
  char tag;
  int x;
  int y;
} Vec3;

Vec3 gverts[] = {
    {1, 10, 20},
    {2, -30, 40},
};

int main(void) {
  Vec3 verts[] = {
      {3, 5, 7},
      {4, -9, 11},
  };

  return gverts[0].tag + gverts[1].x + gverts[1].y + verts[0].tag + verts[0].x +
         verts[0].y + verts[1].tag + verts[1].x + verts[1].y;
}
