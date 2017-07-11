using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace csDelaunay {
	public class Polygon {
		// Properties.
		public List<Vector2f> Vertices { get; set; }

		public float Area {
			get {
				return Math.Abs(SignedDoubleArea() * 0.5f);
			}
		}

		public Winding PolyWinding {
			get {
				float signedDoubleArea = SignedDoubleArea();
				if (signedDoubleArea < 0) {
					return Winding.CLOCKWISE;
				}
				if (signedDoubleArea > 0) {
					return Winding.COUNTERCLOCKWISE;
				}
				return Winding.NONE;
			}
		}

		// Methods.
		public Polygon(List<Vector2f> vertices) {
			this.Vertices = vertices;
		}

		private float SignedDoubleArea() {
			int index, nextIndex;
			int n = this.Vertices.Count;
			Vector2f point, next;
			float signedDoubleArea = 0;

			for (index = 0; index < n; index++) {
				nextIndex = (index+1) % n;
				point = this.Vertices[index];
				next = this.Vertices[nextIndex];
				signedDoubleArea += point.x * next.y - next.x * point.y;
			}

			return signedDoubleArea;
		}
	}
}