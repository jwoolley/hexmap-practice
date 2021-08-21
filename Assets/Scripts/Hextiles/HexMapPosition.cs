using System;
public class HexMapPosition : Tuple<int, int> {
    public HexMapPosition(int x, int y) : base(x, y) { }
    public HexMapPosition(HexMapPosition original) : base(original.x, original.y) { }
    public int x { get { return Item1; } }
    public int y { get { return Item2; } }

    public override bool Equals(object obj) => this.Equals(obj as HexMapPosition);



    public bool Equals(HexMapPosition obj) {
        return x == obj.x && y == obj.y;
    }

    public override int GetHashCode() {
        int hashCode = 1934200729;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        hashCode = hashCode * -1521134295 + x.GetHashCode();
        hashCode = hashCode * -1521134295 + y.GetHashCode();
        return hashCode;
    }
}