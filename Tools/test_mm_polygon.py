import unittest
from mm_polygon import earclip, project_polygon, polygon_area, area2

class PolygonTests(unittest.TestCase):
    def check_shape(self, points):
        poly=project_polygon(points)
        triangles=earclip(points)
        self.assertIsNotNone(triangles)
        expected=polygon_area(poly)
        areas=[area2(poly[a],poly[b],poly[c])/2 for a,b,c in triangles]
        self.assertAlmostEqual(sum(areas),expected)
        self.assertTrue(all(a*expected>=0 for a in areas))
        # A fan over this concavity has overlapping/outside triangles; its
        # absolute triangle area exceeds the actual polygon area.
        self.assertAlmostEqual(sum(abs(a) for a in areas),abs(expected))
    def test_concave_roof_all_windings_and_projections(self):
        outline=[(0,0),(5,0),(5,4),(3,1),(2,4),(0,4)]
        for rev in [False,True]:
            seq=outline[::-1] if rev else outline
            for axis in range(3):
                points=[(2,x,y) if axis==0 else (x,2,y) if axis==1 else (x,y,2) for x,y in seq]
                self.check_shape(points)
    def test_collinear_facade_vertices(self):
        self.check_shape([(0,0,0),(2,0,0),(4,0,0),(4,3,0),(0,3,0)])
    def test_reject_self_crossing_polygon(self):
        self.assertIsNone(earclip([(0,0,0),(2,2,0),(0,2,0),(2,0,0)]))

if __name__=='__main__': unittest.main()
