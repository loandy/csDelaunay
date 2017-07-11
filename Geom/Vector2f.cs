using System;
using System.Collections.Generic;

namespace csDelaunay {
	// Recreation of the UnityEngine.Vector3, so it can be used in other thread.
	public struct Vector2f : IEquatable<Vector2f> {
		// Attributes.
		public float x, y;

		// Properties.
		public static readonly Vector2f ZERO = new Vector2f(0,0);
		public static readonly Vector2f ONE = new Vector2f(1,1);

		public static readonly Vector2f RIGHT = new Vector2f(1,0);
		public static readonly Vector2f LEFT = new Vector2f(-1,0);

		public static readonly Vector2f UP = new Vector2f(0,1);
		public static readonly Vector2f DOWN = new Vector2f(0,-1);

		public float Magnitude {
			get {
				return (float)Math.Sqrt(x * x + y * y);
			}
		}

		public Vector2f Unit {
			get {
				float magnitude = this.Magnitude;
				return new Vector2f(this.x / magnitude, this.y / magnitude);
			}
		}

		// Methods.
		public static bool operator == (Vector2f a, Vector2f b) {
			return a.x == b.x && a.y == b.y;
		}

		public static bool operator != (Vector2f a, Vector2f b) {
			return a.x != b.x || a.y != b.y;
		}

		public static Vector2f operator - (Vector2f a, Vector2f b) {
			return new Vector2f(a.x-b.x, a.y-b.y);
		}

		public static Vector2f operator + (Vector2f a, Vector2f b) {
			return new Vector2f(a.x+b.x, a.y+b.y);
		}

		public static Vector2f operator * (Vector2f a, float i) {
			return new Vector2f(a.x * i, a.y * i);
		}

		public static Vector2f operator / (Vector2f a, float s) {
			return new Vector2f(a.x / s, a.y / s);
		}

		public static Vector2f operator / (Vector2f a, Vector2f b) {
			return new Vector2f(a.x / b.x, a.y / b.y);
		}

		public static Vector2f Min(Vector2f a, Vector2f b) {
			return new Vector2f(Math.Min(a.x, b.x), Math.Min(a.y, b.y));
		}

		public static Vector2f Max(Vector2f a, Vector2f b) {
			return new Vector2f(Math.Max(a.x, b.x), Math.Max(a.y, b.y));
		}

		public static Vector2f Normalize(Vector2f a) {
			float magnitude = a.Magnitude;
			return new Vector2f(a.x/magnitude, a.y/magnitude);
		}

		public static Vector2f Interpolate(Vector2f a, Vector2f b, float s) {
			return new Vector2f((a.x - b.x) * s, (a.y - b.y) * s);
		}

		public static float DistanceSquare(Vector2f a, Vector2f b) {
			float cx = b.x - a.x;
			float cy = b.y - a.y;
			return cx*cx + cy*cy;
		}

		public Vector2f(float x, float y) {
			this.x = x;
			this.y = y;
		}

		public Vector2f(double x, double y) {
			this.x = (float)x;
			this.y = (float)y;
		}

		public void Normalize() {
			float magnitude = this.Magnitude;
			x /= magnitude;
			y /= magnitude;
		}

		public float Dot(Vector2f v) {
			return this.x * v.x + this.y * v.y;
		}

		public float Cross(Vector2f v) {
			return this.x * v.y - this.y * v.x;
		}

		public bool Equals(Vector2f other) {
			return this.x == other.x && this.y == other.y;
		}

		public override bool Equals(object other) {
			if (!(other is Vector2f)) {
				return false;
			}
			return other != null && Equals(other);
		}

		public override string ToString () {
			return string.Format ("[Vector2f]"+x+","+y);
		}

		public override int GetHashCode () {
			return x.GetHashCode () ^ y.GetHashCode () << 2;
		}

		public float DistanceSquare(Vector2f v) {
			return Vector2f.DistanceSquare(this, v);
		}
	}
}