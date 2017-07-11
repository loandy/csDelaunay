using System.Collections;
using System.Collections.Generic;

namespace csDelaunay {
	public class LineSegment {
		// Properties.
		public Vector2f P0 { get; set; }
		public Vector2f P1 { get; set; }

		// Methods.
		public static List<LineSegment> VisibleLineSegments(List<Edge> edges) {
			List<LineSegment> segments = new List<LineSegment>();

			foreach (Edge edge in edges) {
				if (edge.Visible) {
					Vector2f p1 = edge.ClippedVertices[LR.LEFT];
					Vector2f p2 = edge.ClippedVertices[LR.RIGHT];
					segments.Add(new LineSegment(p1, p2));
				}
			}

			return segments;
		}

		public static float CompareLengths_MAX(LineSegment segment0, LineSegment segment1) {
			float length0 = (segment0.P0 - segment0.P1).Magnitude;
			float length1 = (segment1.P0 - segment1.P1).Magnitude;
			if (length0 < length1) {
				return 1;
			}
			if (length0 > length1) {
				return -1;
			}
			return 0;
		}

		public static float CompareLengths(LineSegment edge0, LineSegment edge1) {
			return - CompareLengths_MAX(edge0, edge1);
		}

		public LineSegment(Vector2f p0, Vector2f p1) {
			this.P0 = p0;
			this.P1 = p1;
		}
	}
}