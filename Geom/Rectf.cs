using System.Collections;

namespace csDelaunay {
	public struct Rectf {
		// Attributes.
		public static readonly Rectf ZERO = new Rectf(0,0,0,0);
		public static readonly Rectf ONE = new Rectf(1,1,1,1);

		// Properties.
		public float X { get; set; }
		public float Y { get; set; }
		public float Width { get; set; }
		public float Height { get; set; }

		public float Left {
			get {
				return this.X;
			}
		}
		public float Right {
			get {
				return this.X + this.Width;
			}
		}
		public float Top {
			get {
				return this.Y;
			}
		}
		public float Bottom {
			get {
				return this.Y + this.Height;
			}
		}

		public Vector2f TopLeft {
			get {
				return new Vector2f(this.Left, this.Top);
			}
		}

		public Vector2f BottomRight {
			get {
				return new Vector2f(this.Right, this.Bottom);
			}
		}

		// Methods.
		public Rectf(float x, float y, float width, float height) {
			this.X = x;
			this.Y = y;
			this.Width = width;
			this.Height = height;
		}
	}
}