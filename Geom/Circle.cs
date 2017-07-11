using System.Collections;

namespace csDelaunay {
	public class Circle {
		// Properties.
		public Vector2f Center { get; set; }
		public float Radius { get; set; }

		// Methods.
		public Circle(float centerX, float centerY, float radius) {
			this.Center = new Vector2f(centerX, centerY);
			this.Radius = radius;
		}

		public override string ToString() {
			return "Circle (center: " + this.Center + "; radius: " + this.Radius + ")";
		}
	}
}